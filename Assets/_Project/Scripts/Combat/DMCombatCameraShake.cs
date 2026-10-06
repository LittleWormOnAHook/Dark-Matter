using Project.CameraFx;
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
            if (amplitude <= 0f)
                return;

            float trauma = Mathf.Clamp01(amplitude);
            CameraShake.AddTrauma(trauma);

            if (durationSeconds > 0.001f)
                DMCombatCameraShakeSustain.Begin(trauma, durationSeconds);
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
