using Invector.vCamera;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// When melee block/attack assist rotates the player toward a threat, ease the Invector third-person
    /// camera yaw so it stays behind the body instead of leaving the view on the old bearing.
    /// </summary>
    public static class DMCombatCameraThreatSnap
    {
        public static void ApplyYawBehindPlayer(Transform player, vThirdPersonCamera tpCamera, DM_CombatCoreProfile profile)
        {
            if (player == null || tpCamera == null)
                return;

            float duration = profile != null ? profile.combatCameraSnapSeconds : 0.25f;
            duration = Mathf.Clamp(duration, 0.1f, 0.5f);
            float lambda = 4f / duration;

            float targetYaw = player.eulerAngles.y;
            float currentYaw = tpCamera.mouseX;
            float delta = Mathf.DeltaAngle(currentYaw, targetYaw);
            if (Mathf.Abs(delta) < 0.25f)
                return;

            float blend = 1f - Mathf.Exp(-lambda * Time.deltaTime);
            tpCamera.mouseX = Mathf.LerpAngle(currentYaw, targetYaw, blend);
        }

        /// <summary>Maps profile snap duration to exponential damp lambda for ECM2 combat focus yaw.</summary>
        public static float ResolveCombatFocusSmoothLambda(DM_CombatCoreProfile profile, float fallbackLambda)
        {
            if (profile == null)
                return fallbackLambda;

            float duration = Mathf.Clamp(profile.combatCameraSnapSeconds, 0.1f, 0.5f);
            return 4f / duration;
        }
    }
}
