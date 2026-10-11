using Project.CameraFx;
using Project.Data;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Combat-event camera shake. Reads <see cref="DM_CombatCoreProfile.Live"/> and
    /// drives the existing trauma hub (<see cref="CameraShake"/>). Amplitude 0 = off.
    /// </summary>
    public static class DMCombatCameraShake
    {
        public static void TryPlayChargedHit(GameObject source)
        {
            if (!IsPlayerChargedHit(source))
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            Play(profile.chargedHitShakeAmplitude, profile.chargedHitShakeDurationSeconds);
        }

        /// <summary>Slice 1 / A2: shake for a landed player melee hit. Charged keeps chargedHitShake*; otherwise light, or heavy for a crit / poise break / kill.</summary>
        public static void TryPlayMeleeHit(GameObject source, bool isCritical, bool poiseBreak)
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null || source == null)
                return;

            if (IsPlayerChargedHit(source))
            {
                Play(profile.chargedHitShakeAmplitude, profile.chargedHitShakeDurationSeconds);
                return;
            }

            if (source.GetComponentInParent<PioneerShooterMeleeInput>() == null)
                return;

            if (isCritical || poiseBreak)
                Play(profile.heavyHitShakeAmplitude, profile.heavyHitShakeDurationSeconds);
            else
                Play(profile.lightHitShakeAmplitude, profile.lightHitShakeDurationSeconds);
        }

        public static void PlayParry()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            Play(profile.parryShakeAmplitude, profile.parryShakeDurationSeconds);
        }

        public static void PlayBlock()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            Play(profile.blockShakeAmplitude, profile.blockShakeDurationSeconds);
        }

        public static void Play(float amplitude, float durationSeconds)
        {
            PlayGlobal(amplitude, durationSeconds);
        }

        /// <summary>Player ranged fire — per-ammo <see cref="DMAmmoFxProfile.fireTrauma"/>.</summary>
        public static void TryPlayAmmoFire(GameObject owner, ItemData ammoItem, ItemData weapon)
        {
            if (!CombatPlayerSourceUtility.IsPlayerOrPioneerSource(owner))
                return;

            DMAmmoFxProfile profile = DMCombatFx.ResolveProfile(ammoItem, weapon);
            if (profile == null || profile.fireTrauma <= 0f)
                return;

            PlayGlobal(profile.fireTrauma, profile.fireTraumaDurationSeconds);
        }

        /// <summary>Player ranged hit — per-ammo <see cref="DMAmmoFxProfile.impactTrauma"/>.</summary>
        public static void TryPlayAmmoImpact(
            GameObject owner,
            ItemData ammoItem,
            ItemData weapon,
            Vector3 hitPoint)
        {
            if (!CombatPlayerSourceUtility.IsPlayerOrPioneerSource(owner))
                return;

            DMAmmoFxProfile profile = DMCombatFx.ResolveProfile(ammoItem, weapon);
            if (profile == null || profile.impactTrauma <= 0f)
                return;

            if (profile.impactTraumaMinDistance > 0.001f)
            {
                Vector3 listener = ResolveListenerWorldPosition();
                if ((hitPoint - listener).sqrMagnitude < profile.impactTraumaMinDistance * profile.impactTraumaMinDistance)
                    return;
            }

            if (profile.impactTraumaRadius > 0.01f)
                PlayAt(hitPoint, profile.impactTrauma, profile.impactTraumaDurationSeconds, profile.impactTraumaRadius);
            else
                PlayGlobal(profile.impactTrauma, profile.impactTraumaDurationSeconds);
        }

        private static void PlayGlobal(float amplitude, float durationSeconds)
        {
            if (amplitude <= 0f)
                return;

            float trauma = Mathf.Clamp01(amplitude);
            CameraShake.AddTrauma(trauma);

            if (durationSeconds > 0.001f)
                DMCombatCameraShakeSustain.Begin(trauma, durationSeconds);
        }

        private static void PlayAt(Vector3 worldPosition, float amplitude, float durationSeconds, float radius)
        {
            if (amplitude <= 0f)
                return;

            float trauma = Mathf.Clamp01(amplitude);
            CameraShake.ShakeAt(worldPosition, trauma, Mathf.Max(0.01f, radius));

            if (durationSeconds > 0.001f)
                DMCombatCameraShakeSustain.Begin(trauma, durationSeconds);
        }

        private static Vector3 ResolveListenerWorldPosition()
        {
            CameraShakeService service = CameraShakeService.Instance;
            if (service != null && service.ActiveListener != null)
                return service.ActiveListener.transform.position;

            Camera cam = Camera.main;
            return cam != null ? cam.transform.position : Vector3.zero;
        }

        private static bool IsPlayerChargedHit(GameObject source)
        {
            if (source == null)
                return false;

            PioneerShooterMeleeInput melee = source.GetComponentInParent<PioneerShooterMeleeInput>();
            return melee != null && melee.IsStrongMeleeDamageActive;
        }
    }

    /// <summary>
    /// Holds trauma for the authored duration, then lets <see cref="CameraShakeService"/> decay.
    /// Lives on the shake service root so it survives scene loads with that hub.
    /// </summary>
    [DisallowMultipleComponent]
    internal sealed class DMCombatCameraShakeSustain : MonoBehaviour
    {
        private float _trauma;
        private float _until;

        public static void Begin(float trauma, float durationSeconds)
        {
            CameraShakeService service = CameraShakeService.EnsureExists();
            if (service == null)
                return;

            DMCombatCameraShakeSustain runner = service.GetComponent<DMCombatCameraShakeSustain>();
            if (runner == null)
                runner = service.gameObject.AddComponent<DMCombatCameraShakeSustain>();

            runner._trauma = Mathf.Max(runner._trauma, Mathf.Clamp01(trauma));
            runner._until = Mathf.Max(runner._until, Time.time + Mathf.Max(0f, durationSeconds));
            runner.enabled = true;
        }

        private void Update()
        {
            if (Time.time >= _until)
            {
                _trauma = 0f;
                enabled = false;
                return;
            }

            CameraShake.Sustain(_trauma);
        }
    }
}
