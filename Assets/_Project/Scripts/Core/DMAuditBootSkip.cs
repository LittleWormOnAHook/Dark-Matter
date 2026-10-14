using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// 2026-10-14 (b): audit/test boot skip. When the Enemy Runtime Audit (or a scripted test) enters Play, the branded
    /// boot loader and the main menu are skipped and the session is marked started right after the scene loads, so
    /// nothing is shown before the audit's gameplay bypass. Driven only by editor SessionState keys the audit sets
    /// ("DMAudit.Enemy.Pending", or "DMAudit.SkipMenu" for scripted tests); always false in builds and normal play.
    /// </summary>
    public static class DMAuditBootSkip
    {
        public const string TestSkipKey = "DMAudit.SkipMenu";

        public static bool Active { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Evaluate()
        {
#if UNITY_EDITOR
            Active = UnityEditor.SessionState.GetBool("DMAudit.Enemy.Pending", false)
                || UnityEditor.SessionState.GetBool(TestSkipKey, false);
#else
            Active = false;
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void MarkStartedForAudit()
        {
            // Runs after scene Awake (MainMenuController resets the session there) and before Start (which would
            // show the menu). With the session started, ShowMainMenu and the menu flow stand down.
            if (Active && !GameSession.HasStarted)
                GameSession.MarkStarted();
        }
    }
}
