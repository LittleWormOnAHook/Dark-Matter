using Invector.vMelee;
using Project.AI;
using Project.AI.Invector;
using Project.Features.Locomotion;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-20q Downed-Target Combo. While the player's melee target (DMMeleeAttackFacing soft-lock target if it is
    /// down, else the nearest downed enemy in the front cone within reach) is in an animation knockdown
    /// (DMEnemyKnockdown.IsDown, not dead), light attacks become ground strikes; normal combo resumes when it gets up.
    ///  - Authored: Downed Attack 1..N = FullBody layer state paths (profile default list or per-weapon set). Each tap
    ///    plays the next slot (wraps), crossfaded on the FullBody layer like the light combo.
    ///  - Fallback (no states): the normal light combo plays, the spine/chest pitch down (DMPlayerBodyWeight external
    ///    pitch, stacking-safe), and a low strike lands on the downed target inside the strike window if the weapon
    ///    hitbox missed the low body. Weapon placements / hitbox colliders are not touched.
    ///  - Damage x multiplier (hitbox hits are topped up). Downed enemies take no stagger (knockdown gate + bridge gate).
    /// Runtime-added to the player by a poller; no prefab edits. Settings: DM_CombatCoreProfile "Downed Combo".
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(3310)]
    public sealed class DMDownedCombo : MonoBehaviour
    {
        private PioneerShooterMeleeInput _input;
        private DMMeleeAttackFacing _facing;
        private vMeleeManager _melee;
        private Animator _anim;
        private EnemyHealth _target;
        private int _slot;
        private bool _wasAttacking;
        private int _swingState;
        private float _swingStartHealth;
        private bool _swingResolved, _swingAtDowned, _authoredSwing;
        private float _pitch, _pitchVel;
        private int _authoredFullHash;
        private static readonly Collider[] s_cols = new Collider[48];

        public static bool IsActive { get; private set; }
        public static EnemyHealth CurrentTarget { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { IsActive = false; CurrentTarget = null; DMPlayerBodyWeight.ExternalPitchDegrees = 0f; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            var go = new GameObject("DMDownedComboBoot") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(go);
            go.AddComponent<Poller>();
        }

        private sealed class Poller : MonoBehaviour
        {
            private float _next;
            private void Update()
            {
                if (Time.unscaledTime < _next) return;
                _next = Time.unscaledTime + 1f;
                GameObject pl = Project.Core.PlayerLocator.FindPlayerObject();
                if (pl != null && pl.GetComponent<DMDownedCombo>() == null && pl.GetComponent<PioneerShooterMeleeInput>() != null)
                    pl.AddComponent<DMDownedCombo>();
            }
        }

        private void Awake()
        {
            _input = GetComponent<PioneerShooterMeleeInput>();
            _melee = GetComponent<vMeleeManager>();
            _anim = GetComponent<Animator>();
            if (_anim == null) _anim = GetComponentInChildren<Animator>();
        }

        private void OnDisable()
        {
            IsActive = false; CurrentTarget = null; DMPlayerBodyWeight.ExternalPitchDegrees = 0f;
        }

        /// <summary>Called by PioneerShooterMeleeInput.PlayLightSwordSwing. True = an authored downed state was played.</summary>
        public static bool TryPlayDownedSwing(PioneerShooterMeleeInput input, Animator anim, int layer)
        {
            if (input == null || anim == null || layer < 0) return false;
            DMDownedCombo dc = input.GetComponent<DMDownedCombo>();
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (dc == null || p == null || !p.downedComboEnabled) return false;
            dc.RefreshTarget(p);
            if (dc._target == null) return false;
            dc.FaceTarget(p);
            string[] states = dc.States(p, out _, out _);
            if (states == null || states.Length == 0) return false; // fallback path: normal swing + pitch + low strike
            for (int tries = 0; tries < states.Length; tries++)
            {
                int i = (dc._slot + tries) % states.Length;
                int hash = Resolve(anim, layer, states[i]);
                if (hash == 0) continue;
                anim.CrossFadeInFixedTime(hash, p.downedComboBlendSeconds, layer, 0f);
                dc._slot = (i + 1) % states.Length;
                dc._authoredSwing = true;
                dc._authoredFullHash = Animator.StringToHash(anim.GetLayerName(layer) + "." + states[i]);
                if (p.downedComboLog) Debug.Log($"[DownedCombo] play '{states[i]}' on {dc._target.name}", dc);
                return true;
            }
            if (p.downedComboLog) Debug.LogWarning("[DownedCombo] no configured downed state exists on the FullBody layer; using fallback", dc);
            return false;
        }

        private static int Resolve(Animator anim, int layer, string path)
        {
            if (string.IsNullOrEmpty(path)) return 0;
            int h = Animator.StringToHash(path);
            if (anim.HasState(layer, h)) return h;
            h = Animator.StringToHash(anim.GetLayerName(layer) + "." + path);
            return anim.HasState(layer, h) ? h : 0;
        }

        private string[] States(DM_CombatCoreProfile p, out float mult, out float reach)
        {
            mult = p.downedComboDamageMultiplier; reach = p.downedComboReach;
            vMeleeWeapon w = _melee != null ? _melee.rightWeapon : null;
            if (w != null && p.downedComboWeaponSets != null)
            {
                string n = w.gameObject.name.ToLowerInvariant();
                foreach (var set in p.downedComboWeaponSets)
                {
                    if (set == null || string.IsNullOrEmpty(set.weaponNameContains) || !n.Contains(set.weaponNameContains.ToLowerInvariant())) continue;
                    if (set.damageMultiplier > 0f) mult = set.damageMultiplier;
                    if (set.reach > 0f) reach = set.reach;
                    if (set.states != null && set.states.Length > 0) return set.states;
                    break;
                }
            }
            return p.downedComboStates;
        }

        private static bool Downed(EnemyHealth e) => e != null && !e.IsDead && e.isActiveAndEnabled && DMEnemyKnockdown.IsDown(e.gameObject);

        private void RefreshTarget(DM_CombatCoreProfile p)
        {
            States(p, out _, out float reach);
            if (_facing == null) _facing = GetComponent<DMMeleeAttackFacing>();
            EnemyHealth t = _facing != null ? _facing.Target : null;
            if (!Downed(t) || Planar(t.transform.position - transform.position) > reach) t = null;
            if (t == null && Downed(_target) && Planar(_target.transform.position - transform.position) <= reach) t = _target;
            if (t == null)
            {
                float best = float.MaxValue;
                int n = Physics.OverlapSphereNonAlloc(transform.position, reach, s_cols, ~0, QueryTriggerInteraction.Collide);
                for (int i = 0; i < n; i++)
                {
                    EnemyHealth e = s_cols[i] != null ? s_cols[i].GetComponentInParent<EnemyHealth>() : null;
                    if (!Downed(e)) continue;
                    Vector3 to = e.transform.position - transform.position; to.y = 0f;
                    float d = to.magnitude;
                    if (d > reach || (d > 0.3f && Vector3.Angle(transform.forward, to) > p.downedComboConeDegrees)) continue;
                    if (d < best) { best = d; t = e; }
                }
            }
            if (t != _target && p.downedComboLog) Debug.Log($"[DownedCombo] target {(t != null ? t.name : "none")}", this);
            if (t == null) _slot = 0;
            _target = t;
            IsActive = t != null;
            CurrentTarget = t;
        }

        private void FaceTarget(DM_CombatCoreProfile p)
        {
            if (!p.downedComboFaceTarget || _target == null) return;
            Vector3 to = _target.transform.position - transform.position; to.y = 0f;
            if (to.sqrMagnitude < 0.01f) return;
            float yaw = Vector3.SignedAngle(transform.forward, to, Vector3.up);
            yaw = Mathf.Clamp(yaw, -p.downedComboFaceMaxDegrees, p.downedComboFaceMaxDegrees);
            Rigidbody rb = GetComponent<Rigidbody>();
            Quaternion r = Quaternion.AngleAxis(yaw, Vector3.up) * transform.rotation;
            if (rb != null) rb.MoveRotation(r); else transform.rotation = r;
        }

        private static float Planar(Vector3 v) { v.y = 0f; return v.magnitude; }

        private void LateUpdate()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.downedComboEnabled || _input == null || _anim == null)
            {
                IsActive = false; CurrentTarget = null;
                DMPlayerBodyWeight.ExternalPitchDegrees = 0f;
                return;
            }

            RefreshTarget(p);
            bool attacking = _input.isAttacking;
            int layer = _anim.GetLayerIndex("FullBody");
            AnimatorStateInfo st = layer >= 0 ? (_anim.IsInTransition(layer) ? _anim.GetNextAnimatorStateInfo(layer) : _anim.GetCurrentAnimatorStateInfo(layer)) : default;

            // New swing: attack start or a chained state change while attacking.
            if (attacking && (!_wasAttacking || (layer >= 0 && st.fullPathHash != _swingState)))
            {
                _swingState = layer >= 0 ? st.fullPathHash : 0;
                _swingAtDowned = _target != null;
                _swingResolved = !_swingAtDowned;
                _swingStartHealth = _target != null ? _target.CurrentHealth : 0f;
                if (_wasAttacking && _swingAtDowned) FaceTarget(p); // chained fallback swings re-aim too
                _authoredSwing = layer >= 0 && st.fullPathHash == _authoredFullHash && _authoredFullHash != 0;
            }
            if (!attacking) _authoredSwing = false;
            _wasAttacking = attacking;

            // Fallback pitch (authored states carry their own pose).
            float goal = attacking && _swingAtDowned && !_authoredSwing && _target != null ? p.downedComboFallbackPitchDegrees : 0f;
            _pitch = Mathf.SmoothDamp(_pitch, goal, ref _pitchVel, p.downedComboPitchBlendSeconds, Mathf.Infinity, Mathf.Max(1e-4f, Time.deltaTime));
            DMPlayerBodyWeight.ExternalPitchDegrees = Mathf.Abs(_pitch) < 0.01f ? 0f : _pitch;

            // Low strike / damage top-up once per swing, inside the window.
            if (!attacking || _swingResolved || _target == null || !p.downedComboLowStrike) return;
            float n = st.normalizedTime % 1f;
            if (n < p.downedComboStrikeWindow.x) return;
            States(p, out float mult, out float reach);
            if (Planar(_target.transform.position - transform.position) > reach + 0.3f) { if (n > p.downedComboStrikeWindow.y) _swingResolved = true; return; }

            float dropped = _swingStartHealth - _target.CurrentHealth;
            if (dropped > 0.01f)
            {
                // The weapon hitbox already hit the low body: top up to the multiplier.
                if (mult > 1f) _target.TakeDamage(dropped * (mult - 1f), gameObject, false);
                _swingResolved = true;
                if (p.downedComboLog) Debug.Log($"[DownedCombo] hitbox hit {_target.name} {dropped:0.#} (+{dropped * Mathf.Max(0f, mult - 1f):0.#})", this);
                return;
            }
            if (n >= Mathf.Lerp(p.downedComboStrikeWindow.x, p.downedComboStrikeWindow.y, 0.5f) || n >= p.downedComboStrikeWindow.y)
            {
                float baseDmg = p.downedComboBaseDamage;
                vMeleeWeapon w = _melee != null ? _melee.rightWeapon : null;
                if (w != null && w.damage != null && w.damage.damageValue > 0) baseDmg = w.damage.damageValue;
                float dmg = baseDmg * mult;
                if (dmg > 0f) _target.TakeDamage(dmg, gameObject, false);
                _swingResolved = true;
                if (p.downedComboLog) Debug.Log($"[DownedCombo] low strike {_target.name} {dmg:0.#}", this);
            }
        }
    }
}
