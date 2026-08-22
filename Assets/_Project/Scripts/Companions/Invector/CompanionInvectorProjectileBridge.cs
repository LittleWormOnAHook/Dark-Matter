using Invector.vShooter;
using Project.AI;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Strips Invector native projectile/audio on shot so companion fire uses the unified
    /// <see cref="Project.Combat.CombatProjectileSpawner"/> path from CompanionInvectorCombatBridge.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionInvectorProjectileBridge : MonoBehaviour
    {
        private vShooterManager _shooterManager;
        private CompanionHealth _health;

        private void Awake()
        {
            _shooterManager = GetComponent<vShooterManager>();
            _health = GetComponent<CompanionHealth>();
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

            if (invectorWeapon == null)
                return;

            invectorWeapon.projectile = null;
            invectorWeapon.fireClip = null;
            invectorWeapon.emittShurykenParticle = null;
            invectorWeapon.lightOnShot = null;
            invectorWeapon.isInfinityAmmo = true;
            invectorWeapon.dontUseReload = true;

            int clip = invectorWeapon.clipSize > 0 ? invectorWeapon.clipSize : 999;
            if (invectorWeapon.ammo <= 0)
                invectorWeapon.AddAmmo(clip);
        }
    }
}
