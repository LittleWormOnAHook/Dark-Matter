using System.Collections.Generic;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>Which side of the enemy a blow lands on (PROTOFACTOR naming: Left = struck on its left, pushed to its right).</summary>
    public enum DMHitSide
    {
        Front = 0,
        Back = 1,
        Left = 2,
        Right = 3
    }

    /// <summary>
    /// Slice 2b.2: plays the directional PROTOFACTOR hit clips on the enemy controller's DM hit layers
    /// (Invector@ShooterMelee.controller, built by Tools > Dark Matter > Combat > Build Hit Reaction Layers):
    ///   DM_HitReact_Add  - additive, torso mask (arms / hands off)  - medium tier (GetHitLight*, alt GetHitHeavy*)
    ///   DM_HitReact_Body - override, body mask (arms / hands off)   - heavy tier (GetHitHeavy*)
    /// The arms keep the enemy's own grip from the lower layers (sword, rifle, pistol, unarmed). Layer weights are driven
    /// here (blend in / hold / blend out) and drop to 0 when idle. Runtime-added, no prefab edits. A controller without
    /// these layers (Gongo, creatures, non-humanoid) reports IsSupported = false and the caller falls back to the lean.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyHitReactionPlayer : MonoBehaviour
    {
        public const string AddLayerName = "DM_HitReact_Add";
        public const string BodyLayerName = "DM_HitReact_Body";
        public const string AddSpeedParam = "DM_HRAddSpeed";
        public const string BodySpeedParam = "DM_HRBodySpeed";
        public const string EmptyState = "Empty";

        public enum Tier
        {
            Medium = 0,
            Heavy = 1
        }

        private static readonly string[] SideTag = { "F", "B", "L", "R" };
        private static readonly int AddSpeedHash = Animator.StringToHash(AddSpeedParam);
        private static readonly int BodySpeedHash = Animator.StringToHash(BodySpeedParam);

        private struct LayerRun
        {
            public int Layer;
            public bool Active;
            public float Elapsed;   // clip seconds (scaled by animator speed and the state speed)
            public float Length;
            public float Speed;
            public float Peak;
            public float BlendIn;
            public float BlendOut;
            public float Weight;
        }

        private Animator _animator;
        private EnemyAiController _ai;
        private EnemyInvectorRagdollBridge _bridge;
        private bool _holdingAi;
        private bool _resolved;
        private bool _supported;
        private LayerRun _add;
        private LayerRun _body;
        private int _lastVariationMedium = -1;
        private int _lastVariationHeavy = -1;
        private readonly Dictionary<string, float> _clipLengths = new Dictionary<string, float>();

        public bool IsSupported => Resolve();
        public bool IsPlaying => _add.Active || _body.Active;

        public static DMEnemyHitReactionPlayer Ensure(GameObject enemy)
        {
            if (enemy == null)
                return null;

            DMEnemyHitReactionPlayer player = enemy.GetComponent<DMEnemyHitReactionPlayer>();
            return player != null ? player : enemy.AddComponent<DMEnemyHitReactionPlayer>();
        }

        /// <summary>
        /// Plays a directional hit clip. <paramref name="useAltClip"/> (medium only) swaps GetHitLight for GetHitHeavy.
        /// Returns false when the controller has no DM hit layers.
        /// </summary>
        public bool Play(Tier tier, DMHitSide side, bool useAltClip, float weight, float speed, float startOffsetNormalized, float blendIn, float blendOut)
        {
            if (!Resolve() || weight <= 0.001f)
                return false;

            bool heavyClip = tier == Tier.Heavy || useAltClip;
            string clip = (heavyClip ? "HR_Heavy_" : "HR_Light_") + SideTag[(int)side];
            ref LayerRun run = ref (tier == Tier.Medium ? ref _add : ref _body);
            if (run.Layer < 0)
                return false;

            float length = ClipLength(clip);
            speed = Mathf.Max(0.1f, speed);
            _animator.SetFloat(tier == Tier.Medium ? AddSpeedHash : BodySpeedHash, speed);
            float offset = Mathf.Clamp01(startOffsetNormalized);
            _animator.CrossFadeInFixedTime(clip, Mathf.Max(0.01f, blendIn), run.Layer, offset * length);

            run.Active = true;
            run.Elapsed = offset * length;
            run.Length = length;
            run.Speed = speed;
            run.Peak = Mathf.Clamp01(weight);
            run.BlendIn = Mathf.Max(0.01f, blendIn);
            run.BlendOut = Mathf.Max(0.02f, blendOut);
            if (tier == Tier.Heavy)
            {
                // Heavy body clip overrides the legs: hold AI locomotion so the feet do not slide (released on blend out).
                _holdingAi = true;
                if (_ai != null)
                    _ai.SetLocomotionPaused(true);
            }
            return true;
        }

        /// <summary>
        /// Picks a variation index (clip choice x start-offset bucket) that differs from the last one on this enemy and tier.
        /// </summary>
        public int PickVariation(Tier tier, bool allowAlt, float altChance, bool avoidRepeat, out bool useAlt, out bool lateStart)
        {
            int last = tier == Tier.Medium ? _lastVariationMedium : _lastVariationHeavy;
            int pick = 0;
            for (int attempt = 0; attempt < 6; attempt++)
            {
                bool alt = allowAlt && Random.value < altChance;
                bool late = Random.value < 0.5f;
                pick = (alt ? 2 : 0) + (late ? 1 : 0);
                if (!avoidRepeat || pick != last)
                    break;
            }

            if (avoidRepeat && pick == last)
                pick ^= 1; // flip the start bucket: guaranteed different

            if (tier == Tier.Medium)
                _lastVariationMedium = pick;
            else
                _lastVariationHeavy = pick;

            useAlt = (pick & 2) != 0;
            lateStart = (pick & 1) != 0;
            return pick;
        }

        public void Cancel()
        {
            Stop(ref _add);
            Stop(ref _body);
        }

        private void OnDisable()
        {
            Cancel();
        }

        private void Update()
        {
            if (!_supported || _animator == null)
                return;

            Tick(ref _add);
            Tick(ref _body);
        }

        private void Tick(ref LayerRun run)
        {
            if (!run.Active || run.Layer < 0)
                return;

            // Clip time follows the animator speed (hit stop slows it) and the state speed parameter.
            run.Elapsed += Time.deltaTime * Mathf.Max(0f, _animator.speed) * run.Speed;
            if (run.Elapsed >= run.Length)
            {
                Stop(ref run);
                return;
            }

            float remainingReal = (run.Length - run.Elapsed) / Mathf.Max(0.01f, run.Speed);
            if (run.Layer == _body.Layer && _holdingAi)
            {
                if (remainingReal < run.BlendOut)
                    ReleaseAi();
                else if (_ai != null)
                    _ai.SetLocomotionPaused(true); // a stagger routine ending meanwhile must not release it early
            }
            float target = remainingReal < run.BlendOut ? run.Peak * (remainingReal / run.BlendOut) : run.Peak;
            float w = target < run.Weight
                ? target
                : Mathf.MoveTowards(run.Weight, target, Time.deltaTime * Mathf.Max(0.01f, run.Peak) / run.BlendIn);

            run.Weight = w;
            _animator.SetLayerWeight(run.Layer, w);
        }

        private void ReleaseAi()
        {
            if (!_holdingAi)
                return;

            _holdingAi = false;
            if (_ai == null || DMEnemyKnockdown.IsDown(gameObject) || (_bridge != null && _bridge.IsHitStaggerActive))
                return;

            _ai.SetLocomotionPaused(false);
        }

        private void Stop(ref LayerRun run)
        {
            if (run.Layer >= 0 && run.Layer == _body.Layer)
                ReleaseAi();
            if (run.Layer >= 0 && _animator != null && _supported)
            {
                _animator.SetLayerWeight(run.Layer, 0f);
                if (run.Active && _animator.isActiveAndEnabled)
                    _animator.CrossFadeInFixedTime(EmptyState, 0.05f, run.Layer);
            }

            run.Active = false;
            run.Weight = 0f;
        }

        private float ClipLength(string stateClip)
        {
            if (_clipLengths.TryGetValue(stateClip, out float len))
                return len;

            len = 1f;
            RuntimeAnimatorController rac = _animator.runtimeAnimatorController;
            if (rac != null)
            {
                string clipName = "DM_" + stateClip;
                AnimationClip[] clips = rac.animationClips;
                for (int i = 0; i < clips.Length; i++)
                {
                    if (clips[i] != null && clips[i].name == clipName)
                    {
                        len = Mathf.Max(0.1f, clips[i].length);
                        break;
                    }
                }
            }

            _clipLengths[stateClip] = len;
            return len;
        }

        private bool Resolve()
        {
            if (_resolved)
                return _supported;

            _resolved = true;
            _add.Layer = -1;
            _body.Layer = -1;
            _ai = GetComponent<EnemyAiController>();
            _bridge = GetComponent<EnemyInvectorRagdollBridge>();
            _animator = GetComponent<Animator>();
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>(true);
            if (_animator == null || !_animator.isHuman || _animator.runtimeAnimatorController == null)
                return _supported = false;

            _add.Layer = _animator.GetLayerIndex(AddLayerName);
            _body.Layer = _animator.GetLayerIndex(BodyLayerName);
            _supported = _add.Layer >= 0 || _body.Layer >= 0;
            return _supported;
        }
    }
}
