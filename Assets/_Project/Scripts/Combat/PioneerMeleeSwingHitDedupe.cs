using System.Collections.Generic;
using Invector.vMelee;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// One damage application per target root per Invector melee damage window (out-swing and return share a window).
    /// </summary>
    public static class PioneerMeleeSwingHitDedupe
    {
        private static int _trackedSwingId = -1;
        private static readonly HashSet<int> _targetRootsThisSwing = new HashSet<int>();

        public static bool TryAcceptHit(GameObject sender, GameObject hitColliderObject)
        {
            if (sender == null || hitColliderObject == null)
                return true;

            PioneerInvectorBootstrap bootstrap = PioneerInvectorBootstrap.Instance;
            if (bootstrap == null)
                return true;

            Transform playerRoot = bootstrap.transform.root;
            if (!sender.transform.IsChildOf(playerRoot) && sender.transform.root != playerRoot)
                return true;

            int swingId = PioneerMeleeDamageWindowTracker.CurrentSwingId;
            if (swingId != _trackedSwingId)
            {
                _trackedSwingId = swingId;
                _targetRootsThisSwing.Clear();
            }

            int targetRootId = hitColliderObject.transform.root.GetInstanceID();
            return _targetRootsThisSwing.Add(targetRootId);
        }
    }

    /// <summary>
    /// Increments <see cref="CurrentSwingId"/> when Invector enables melee damage on an equipped weapon.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PioneerMeleeDamageWindowTracker : MonoBehaviour
    {
        public static int CurrentSwingId { get; private set; }

        private vMeleeManager _meleeManager;
        private bool _wasDamageActive;

        private void Awake()
        {
            _meleeManager = GetComponent<vMeleeManager>();
        }

        private void Update()
        {
            bool active = IsMeleeDamageActive();
            if (active && !_wasDamageActive)
                CurrentSwingId++;

            _wasDamageActive = active;
        }

        private bool IsMeleeDamageActive()
        {
            if (_meleeManager == null)
                return false;

            if (_meleeManager.rightWeapon != null && _meleeManager.rightWeapon.canApplyDamage)
                return true;

            if (_meleeManager.leftWeapon != null && _meleeManager.leftWeapon.canApplyDamage)
                return true;

            return false;
        }
    }
}
