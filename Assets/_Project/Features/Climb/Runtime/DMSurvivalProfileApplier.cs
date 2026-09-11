using Project.Core;
using Project.Progression;
using Project.Survival;
using UnityEngine;

namespace Project.Features.Climb
{
    /// <summary>
    /// Applies <see cref="DM_ClimbDashProfile"/> as the live authority for <see cref="SurvivalStats"/>.
    /// Play-mode inspector edits on the profile asset apply immediately; values persist via DMProfilePlayModeSaver.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-560)]
    [RequireComponent(typeof(SurvivalStats))]
    public sealed class DMSurvivalProfileApplier : MonoBehaviour
    {
        private SurvivalStats stats;
        private ProgressionStatScaler scaler;
        private float lastAppliedMaxStamina = -1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void EnsureOnPlayer()
        {
            if (!Application.isPlaying)
                return;

            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                player = GameObject.Find("Player_v7") ?? GameObject.Find("Player_v7 Variant");
            if (player == null || player.GetComponent<DMSurvivalProfileApplier>() != null)
                return;

            player.AddComponent<DMSurvivalProfileApplier>();
        }

        private void Awake()
        {
            stats = GetComponent<SurvivalStats>();
            scaler = GetComponent<ProgressionStatScaler>();
            ApplyFromProfile(recaptureProgressionBases: true);
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || stats == null)
                return;

            DM_ClimbDashProfile profile = DM_ClimbDashProfile.Live;
            if (profile == null)
                return;

            bool maxChanged = !Mathf.Approximately(lastAppliedMaxStamina, profile.maxStamina);
            ApplyFromProfile(recaptureProgressionBases: maxChanged);
        }

        private void ApplyFromProfile(bool recaptureProgressionBases)
        {
            DM_ClimbDashProfile profile = DM_ClimbDashProfile.Live;
            if (profile == null || stats == null)
                return;

            profile.ApplyToSurvivalStats(stats);
            lastAppliedMaxStamina = profile.maxStamina;

            if (recaptureProgressionBases)
            {
                stats.RecaptureAuthoredMaxima();
                if (scaler != null)
                {
                    scaler.CaptureBaseMaxValues();
                    scaler.ApplyLevelScaling();
                }
            }
        }
    }
}
