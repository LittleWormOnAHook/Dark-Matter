using Invector.vShooter;
using Project.AI;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Keeps Invector enemy weapons on infinite ammo and strips native projectile/audio so the
    /// unified CombatProjectile pipeline owned by EnemyInvectorCombatBridge can fire reliably.
    /// </summary>
    [DisallowMultipleComponent]
    public class EnemyInvectorProjectileBridge : MonoBehaviour
    {
        private vShooterManager _shooterManager;
        private EnemyHealth _health;

        private void Awake()
        {
            _shooterManager = GetComponent<vShooterManager>();
            _health = GetComponent<EnemyHealth>();
        }

        private void OnEnable()
        {
            if (_shooterManager != null)
                _shooterManager.onShot.AddListener(HandleShot);
        }

        private void OnDisable()
        {
            if (_shooterManager != null)
                _shooterManager.onShot.RemoveListener(HandleShot);
        }

        private void HandleShot(vShooterWeapon invectorWeapon)
        {
            if (_health != null && _health.IsDead)
                return;

            if (invectorWeapon == null || invectorWeapon.muzzle == null)
                return;

            // See PioneerInvectorProjectileBridge.HandleShot: clear Invector's own native bullet so
            // it doesn't spawn a second, ammo-agnostic projectile/trail alongside ours, mute its
            // bundled fire sound so it doesn't double up with our ammoItem/weapon fire sound, and
            // take Invector's own ammo counter out of the loop entirely so it can never silently
            // gate/stop enemy fire (matches CompanionInvectorLoadoutBridge's existing approach).
            invectorWeapon.projectile = null;
            invectorWeapon.fireClip = null;
            invectorWeapon.emittShurykenParticle = null;
            invectorWeapon.lightOnShot = null;
            invectorWeapon.isInfinityAmmo = true;
            invectorWeapon.dontUseReload = true;
            invectorWeapon.ammo = invectorWeapon.clipSize > 0 ? invectorWeapon.clipSize : 999;
        }
    }
}
