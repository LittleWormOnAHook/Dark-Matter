using Invector.vMelee;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Re-applies melee block cone and player weapon hitbox scales when <see cref="DM_CombatCoreProfile.Live"/> changes in Play.
    /// Clip assignment stays editor-only (<see cref="Project.EditorTools.Combat.DMMeleeAnimationSetApplier"/>).
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMMeleeCombatProfileLiveRefresh : MonoBehaviour
    {
        private PioneerInvectorBootstrap _bootstrap;
        private float _lastBlockHalfAngle = float.NaN;
        private float _lastHitboxWidth = float.NaN;
        private float _lastHitboxReach = float.NaN;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
        }

        private void Update()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            if (!Mathf.Approximately(profile.meleeBlockDefenseHalfAngle, _lastBlockHalfAngle))
            {
                _lastBlockHalfAngle = profile.meleeBlockDefenseHalfAngle;
                DMMeleeCombatProfileApplier.ApplyToPlayer(_bootstrap);
            }

            if (!Mathf.Approximately(profile.meleeHitboxWidthScale, _lastHitboxWidth)
                || !Mathf.Approximately(profile.meleeHitboxReachScale, _lastHitboxReach))
            {
                _lastHitboxWidth = profile.meleeHitboxWidthScale;
                _lastHitboxReach = profile.meleeHitboxReachScale;
                ApplyEquippedPlayerWeaponHitboxes();
            }
        }

        private void ApplyEquippedPlayerWeaponHitboxes()
        {
            if (_bootstrap == null)
                return;

            vMeleeManager meleeManager = _bootstrap.MeleeManager;
            if (meleeManager == null)
                return;

            if (meleeManager.rightWeapon != null)
                PioneerMeleeHitboxTuning.ApplyToWeapon(meleeManager.rightWeapon, enemyWeapon: false);

            if (meleeManager.leftWeapon != null)
                PioneerMeleeHitboxTuning.ApplyToWeapon(meleeManager.leftWeapon, enemyWeapon: false);
        }
    }
}
