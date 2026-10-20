using Invector.vMelee;
using UnityEngine;

namespace Project.Combat
{
    public enum DMSwingDirectionSource
    {
        BladeVelocity = 0,
        ComboTable = 1
    }

    /// <summary>
    /// Slice 2b.1: samples the player's blade (right weapon hitbox centres, else the right hand) relative to the player root
    /// every LateUpdate, so a landed melee hit can read the strike direction (DMEnemyHitReaction). Runtime-added next to
    /// PioneerMeleeDamageWindowTracker (no prefab edits). One player, fixed ring buffer, no allocations.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(900)]
    public sealed class DMMeleeSwingVelocity : MonoBehaviour
    {
        private const int Capacity = 12;

        private readonly Vector3[] _pos = new Vector3[Capacity];
        private readonly float[] _time = new float[Capacity];
        private int _head = -1;
        private int _count;
        private vMeleeManager _melee;
        private Animator _animator;
        private Transform _hand;
        private bool _handResolved;

        public static DMMeleeSwingVelocity Ensure(GameObject root)
        {
            if (root == null)
                return null;

            DMMeleeSwingVelocity sampler = root.GetComponent<DMMeleeSwingVelocity>();
            return sampler != null ? sampler : root.AddComponent<DMMeleeSwingVelocity>();
        }

        private void Awake()
        {
            _melee = GetComponent<vMeleeManager>();
            _animator = GetComponent<Animator>();
        }

        private void OnEnable()
        {
            _count = 0;
            _head = -1;
        }

        private void LateUpdate()
        {
            if (!TrySampleLocal(out Vector3 local))
                return;

            _head = (_head + 1) % Capacity;
            _pos[_head] = local;
            _time[_head] = Time.time;
            if (_count < Capacity)
                _count++;
        }

        /// <summary>Blade velocity (world axes, m/s) relative to the player root over roughly <paramref name="windowSeconds"/>.</summary>
        public bool TryGetBladeVelocity(float windowSeconds, out Vector3 velocity)
        {
            velocity = Vector3.zero;
            if (_count < 1 || !TrySampleLocal(out Vector3 now))
                return false;

            float t = Time.time;
            int pick = -1;
            for (int i = 0; i < _count; i++)
            {
                int k = (_head - i + Capacity) % Capacity;
                float age = t - _time[k];
                if (age <= 0.0001f)
                    continue; // same frame (or a physics step that runs before it)

                pick = k;
                if (age >= windowSeconds)
                    break;
            }

            if (pick < 0)
                return false;

            float dt = t - _time[pick];
            if (dt < 0.0001f)
                return false;

            velocity = (now - _pos[pick]) / dt;
            return true;
        }

        private bool TrySampleLocal(out Vector3 local)
        {
            local = Vector3.zero;
            if (!TryGetBladePoint(out Vector3 world))
                return false;

            local = world - transform.position;
            return true;
        }

        private bool TryGetBladePoint(out Vector3 world)
        {
            world = Vector3.zero;
            vMeleeWeapon weapon = _melee != null ? _melee.rightWeapon : null;
            if (weapon != null && weapon.isActiveAndEnabled && weapon.hitBoxes != null && weapon.hitBoxes.Count > 0)
            {
                Vector3 sum = Vector3.zero;
                int n = 0;
                for (int i = 0; i < weapon.hitBoxes.Count; i++)
                {
                    vHitBox box = weapon.hitBoxes[i];
                    if (box == null)
                        continue;

                    sum += box.transform.position;
                    n++;
                }

                if (n > 0)
                {
                    world = sum / n;
                    return true;
                }
            }

            if (!_handResolved)
            {
                _handResolved = true;
                if (_animator != null && _animator.isHuman)
                    _hand = _animator.GetBoneTransform(HumanBodyBones.RightHand);
            }

            if (_hand == null)
                return false;

            world = _hand.position;
            return true;
        }
    }
}
