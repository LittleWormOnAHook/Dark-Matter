using System.Collections.Generic;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// 2026-10-20d: keeps the player's own child colliders from shoving the root capsule.
    /// - Every solid child collider gets Physics.IgnoreCollision with the root capsule, re-applied every interval
    ///   (Unity drops ignore pairs whenever a collider is disabled/re-enabled: death/revive, climb, dash, re-equip).
    /// - Solid colliders that are part of the ROOT rigidbody's compound (re-equipped weapon pickups etc.) become triggers.
    /// Skipped while ragdolled/dead so the death ragdoll keeps its solid bones. Settings: DM_CombatCoreProfile.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMPlayerSelfCollisionGuard : MonoBehaviour
    {
        private Collider _capsule;
        private Rigidbody _body;
        private vThirdPersonMotor _motor;
        private float _next;
        private readonly List<Collider> _cols = new List<Collider>(64);
        private readonly HashSet<int> _reported = new HashSet<int>();

        public static void Ensure(GameObject player)
        {
            if (player != null && player.GetComponent<DMPlayerSelfCollisionGuard>() == null)
                player.AddComponent<DMPlayerSelfCollisionGuard>();
        }

        private void Awake()
        {
            _capsule = GetComponent<CapsuleCollider>();
            _body = GetComponent<Rigidbody>();
            _motor = GetComponent<vThirdPersonMotor>();
        }

        private void LateUpdate()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.playerSelfCollisionGuard || _capsule == null)
                return;
            if (Time.unscaledTime < _next)
                return;
            _next = Time.unscaledTime + Mathf.Max(0.05f, p.playerSelfCollisionGuardInterval);
            if (_motor != null && (_motor.ragdolled || _motor.isDead))
                return;

            _cols.Clear();
            GetComponentsInChildren(true, _cols);
            for (int i = 0; i < _cols.Count; i++)
            {
                Collider c = _cols[i];
                if (c == null || c == _capsule || c.isTrigger)
                    continue;

                if (p.playerSelfCollisionForceRootTriggers && _body != null && c.attachedRigidbody == _body)
                {
                    c.isTrigger = true;
                    Report(c, "made trigger (was solid in root compound)");
                    continue;
                }

                if (c.enabled && c.gameObject.activeInHierarchy && !Physics.GetIgnoreCollision(_capsule, c))
                {
                    Physics.IgnoreCollision(_capsule, c, true);
                    Report(c, "ignore vs capsule (re)applied");
                }
            }
        }

        private void Report(Collider c, string what)
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p == null || !p.playerSelfCollisionGuardLog || !_reported.Add(c.GetHashCode() ^ what.Length))
                return;
            Debug.Log("[SelfCollisionGuard] " + c.name + " (" + c.GetType().Name + ", parent " +
                      (c.transform.parent != null ? c.transform.parent.name : "-") + "): " + what);
        }
    }
}
