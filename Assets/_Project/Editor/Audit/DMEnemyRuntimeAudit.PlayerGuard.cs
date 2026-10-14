#if UNITY_EDITOR
using Project.Combat;
using Project.Core;
using UnityEditor;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// 2026-10-14 (b): audits/tests run an audit SESSION (DMDevCommandState.AuditSessionActive) when
    /// DM_CombatCoreProfile.auditGodMode: player-only protection + infinite ammo + respawn fallback. Enemies take
    /// real damage. The typed godmode cheat is never touched. The session ends in Finish, on play-mode exit and on
    /// domain reload, so nothing persists into normal play.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        [InitializeOnLoad]
        private static class AuditSessionPlayModeHook
        {
            static AuditSessionPlayModeHook()
            {
                EditorApplication.playModeStateChanged -= EndAuditSessionOnPlayModeChange;
                EditorApplication.playModeStateChanged += EndAuditSessionOnPlayModeChange;
            }
        }

        private static void EndAuditSessionOnPlayModeChange(PlayModeStateChange change)
        {
            if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.EnteredEditMode)
                DMDevCommandState.EndAuditSession();
        }

        internal static void AuditPlayerGuard()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p != null && !p.auditGodMode) return;
            if (!DMDevCommandState.AuditSessionActive) DMDevCommandState.BeginAuditSession();
        }

        internal static void ReleaseAuditPlayerGuard()
        {
            DMDevCommandState.EndAuditSession();
        }
    }
}
#endif
