#if UNITY_EDITOR
using Project.Combat;
using Project.Core;

namespace Project.EditorTools.Audit
{
    /// <summary>
    /// 2026-10-13 (i): audits run with god mode ON when DM_CombatCoreProfile.auditGodMode (player + enemies unkillable, infinite ammo);
    /// the previous god-mode state is restored in Finish. With auditGodMode off, the respawn fallback (DMGodModeCheat) applies.
    /// </summary>
    public static partial class DMEnemyRuntimeAudit
    {
        private static bool _guardGodSet, _guardGodWas;

        internal static void AuditPlayerGuard()
        {
            DM_CombatCoreProfile p = DM_CombatCoreProfile.Live;
            if (p != null && !p.auditGodMode) return;
            if (!_guardGodSet) { _guardGodWas = DMDevCommandState.GodMode; _guardGodSet = true; }
            if (!DMDevCommandState.GodMode) DMGodModeCheat.SetGodMode(true);
        }

        internal static void ReleaseAuditPlayerGuard()
        {
            if (_guardGodSet) DMGodModeCheat.SetGodMode(_guardGodWas);
            _guardGodSet = false;
        }
    }
}
#endif
