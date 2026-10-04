using UnityEngine;

namespace Project.Combat
{
    [DisallowMultipleComponent]
    public sealed class DMCombatIFrameController : MonoBehaviour
    {
        private float invulnerableUntil;

        public bool IsInvulnerable => Time.time < invulnerableUntil;

        public void GrantInvulnerability(float durationSeconds)
        {
            if (durationSeconds <= 0f)
                return;

            invulnerableUntil = Mathf.Max(invulnerableUntil, Time.time + durationSeconds);
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void SyncEventLogging()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile != null)
                CombatEvents.LogHitsToConsole = profile.logCombatEventsInPlay;
        }
    }
}
