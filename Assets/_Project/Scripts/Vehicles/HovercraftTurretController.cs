using Project.Combat;
using Project.Data;
using Project.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Vehicles
{
    /// <summary>
    /// Mouse-look plasma turret with CombatProjectileSpawner firing.
    /// </summary>
    [DisallowMultipleComponent]
    public class HovercraftTurretController : MonoBehaviour
    {
        [SerializeField] private HovercraftProfile profile;
        [SerializeField] private Transform turretYawPivot;
        [SerializeField] private Transform turretPitchPivot;
        [SerializeField] private Transform muzzle;
        [SerializeField] private AudioSource fireAudioSource;

        [Header("Secondary Turret (optional)")]
        [SerializeField] private Transform turretYawPivot2;
        [SerializeField] private Transform turretPitchPivot2;
        [SerializeField] private Transform muzzle2;
        [SerializeField] private AudioSource fireAudioSource2;

        [Header("Fire Clip")]
        [Tooltip("Optional per-prefab override. Drag an imported MP3/WAV/OGG AudioClip from the Project window. Uses profile.turretFireClip when empty.")]
        [SerializeField] private AudioClip turretFireClip;
        [Tooltip("Optional second turret clip override. Uses profile.turretFireClip2 when empty.")]
        [SerializeField] private AudioClip turretFireClip2;

        private float _localYaw;
        private float _localPitch;
        private float _nextFireTime;
        private bool _active;
        private PlayerController _player;

        public Transform Muzzle => muzzle;
        public Vector3 AimDirection => muzzle != null ? muzzle.forward : transform.forward;
        public bool HasSecondaryTurret => muzzle2 != null;

        public void Configure(
            HovercraftProfile hoverProfile,
            Transform yawPivot,
            Transform pitchPivot,
            Transform muzzleTransform,
            AudioSource fireSource = null)
        {
            profile = hoverProfile;
            turretYawPivot = yawPivot;
            turretPitchPivot = pitchPivot;
            muzzle = muzzleTransform;
            fireAudioSource = fireSource;
        }

        public void ConfigureSecondary(
            Transform yawPivot,
            Transform pitchPivot,
            Transform muzzleTransform,
            AudioSource fireSource = null)
        {
            turretYawPivot2 = yawPivot;
            turretPitchPivot2 = pitchPivot;
            muzzle2 = muzzleTransform;
            fireAudioSource2 = fireSource;
        }

        public void SetTurretFireClip(AudioClip clip)
        {
            turretFireClip = clip;
        }

        public void SetTurretFireClip2(AudioClip clip)
        {
            turretFireClip2 = clip;
        }

        public void Activate(PlayerController player)
        {
            _player = player;
            _active = true;
            SyncInitialAim();
        }

        public void Deactivate()
        {
            _active = false;
            _player = null;
        }

        public void ApplyLookInput(Vector2 lookDelta)
        {
            if (!_active || profile == null || (turretYawPivot == null && turretYawPivot2 == null))
                return;

            if (_player != null && _player.BlocksCombatInput)
                return;

            if (lookDelta.sqrMagnitude < 0.0001f)
                return;

            float arc = profile.turretArcDegrees;
            _localYaw += lookDelta.x * profile.turretSensitivity.x;
            _localPitch += lookDelta.y * profile.turretSensitivity.y; // mouse up -> pitch up (was inverted)
            _localYaw = Mathf.Clamp(_localYaw, -arc, arc);
            _localPitch = Mathf.Clamp(_localPitch, -arc, arc);
            ApplyLocalRotation();
        }

        public void TryFire()
        {
            if (!_active || profile == null || (muzzle == null && muzzle2 == null))
                return;

            if (_player != null && _player.BlocksCombatInput)
                return;

            if (Time.time < _nextFireTime)
                return;

            ItemData weapon = profile.weaponItem;
            ItemData ammo = profile.ammoItem != null ? profile.ammoItem : weapon?.defaultAmmoItem;
            if (weapon == null)
                return;

            float cooldown = profile.turretFireCooldown;
            if (weapon.fireRate > 0.01f)
                cooldown = 1f / weapon.fireRate;

            _nextFireTime = Time.time + cooldown;

            float aimDistance = EstimateAimDistance(muzzle != null ? muzzle : muzzle2, weapon, ammo);
            float spread = RangedFireSolver.ResolveEffectiveSpreadDegrees(
                weapon,
                ammo,
                isAiming: true,
                aimDistance,
                applyPlayerSkillBonus: true);
            FireFromMuzzle(muzzle, weapon, ammo, spread, false);
            if (muzzle2 != null)
                FireFromMuzzle(muzzle2, weapon, ammo, spread, true);
        }

        private float EstimateAimDistance(Transform fireMuzzle, ItemData weapon, ItemData ammo)
        {
            if (fireMuzzle == null)
                return weapon != null ? weapon.closeRangeFullAccuracyDistance : 12f;

            float maxRange = ammo != null && ammo.rangedRange > 0.01f
                ? ammo.rangedRange
                : weapon != null ? weapon.rangedRange : 45f;
            maxRange = Mathf.Max(1f, maxRange);

            if (Physics.Raycast(
                    fireMuzzle.position,
                    fireMuzzle.forward,
                    out RaycastHit hit,
                    maxRange,
                    Physics.DefaultRaycastLayers,
                    QueryTriggerInteraction.Ignore))
            {
                return hit.distance;
            }

            return maxRange;
        }

        private void FireFromMuzzle(Transform fireMuzzle, ItemData weapon, ItemData ammo, float spread, bool secondary)
        {
            if (fireMuzzle == null)
                return;

            CombatProjectileSpawner.Spawn(
                gameObject,
                fireMuzzle,
                weapon,
                ammo,
                fireMuzzle.forward,
                spread);

            PlayFireAudio(fireMuzzle, weapon, ammo, secondary);
        }

        private void PlayFireAudio(Transform fireMuzzle, ItemData weapon, ItemData ammo, bool secondary)
        {
            AudioClip clip = ResolveFireClip(weapon, ammo, secondary);
            if (clip == null || fireMuzzle == null)
                return;

            AudioSource source = secondary ? fireAudioSource2 : fireAudioSource;
            if (source != null)
                source.PlayOneShot(clip);
            else
                AudioSource.PlayClipAtPoint(clip, fireMuzzle.position);
        }

        private AudioClip ResolveFireClip(ItemData weapon, ItemData ammo, bool secondary)
        {
            if (secondary)
            {
                if (turretFireClip2 != null)
                    return turretFireClip2;

                if (profile != null && profile.turretFireClip2 != null)
                    return profile.turretFireClip2;
            }

            if (turretFireClip != null)
                return turretFireClip;

            if (profile != null && profile.turretFireClip != null)
                return profile.turretFireClip;

            if (ammo != null && ammo.fireSound != null)
                return ammo.fireSound;

            if (weapon != null && weapon.fireSound != null)
                return weapon.fireSound;

            return null;
        }

        private void SyncInitialAim()
        {
            _localYaw = 0f;
            _localPitch = 0f;
            ApplyLocalRotation();
        }

        private void ApplyLocalRotation()
        {
            ApplyPivotRotation(turretYawPivot, turretPitchPivot, _localYaw, _localPitch);
            ApplyPivotRotation(turretYawPivot2, turretPitchPivot2, _localYaw, _localPitch);
        }

        private static void ApplyPivotRotation(Transform yawPivot, Transform pitchPivot, float yaw, float pitch)
        {
            if (yawPivot != null)
                yawPivot.localRotation = Quaternion.Euler(0f, yaw, 0f);

            if (pitchPivot != null)
                pitchPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }

        public void PollMouseLook()
        {
            if (!_active || Mouse.current == null)
                return;

            if (_player != null && _player.BlocksCombatInput)
                return;

            Vector2 delta = Mouse.current.delta.ReadValue();
            if (delta.sqrMagnitude > 0.0001f)
                ApplyLookInput(delta);
        }
    }
}
