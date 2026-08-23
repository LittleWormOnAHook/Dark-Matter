using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Play Mode toggle cluster for the Player v7 animation plan.
    /// When all three feature bools are false, behavior matches the pre-plan live game.
    /// Added at runtime by PioneerInvectorBootstrap if missing - no prefab YAML required.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerAnimationPlanSettings : MonoBehaviour
    {
        [Header("Player v7 Animation Plan (reversible)")]
        [Tooltip("Drawn one-hand ranged weapons use unarmed hang until ADS / reload / fire / equip.")]
        public bool enableUnarmedHangWhenDrawn = true;

        [Tooltip("Leave OFF. Two-hand rifles keep the armed two-hand pose (no hang).")]
        public bool includeTwoHandRangedInHang = false;

        [Tooltip("CrossFade LowBack / HighBack and delay the mesh swap on draw / holster.")]
        public bool enableDrawHolsterAnims = true;

        [Tooltip("Roll 10-25% chance to play TriggerReaction on incoming enemy hits. Off = stock every-hit reactions.")]
        public bool enableHitReactionChance = true;

        [Tooltip("Lower bound for the per-hit reaction chance.")]
        [Range(0f, 1f)]
        public float smallHitChanceMin = 0.10f;

        [Tooltip("Upper bound for the per-hit reaction chance. Big hits lerp toward this as HP approaches 0.")]
        [Range(0f, 1f)]
        public float smallHitChanceMax = 0.25f;

        public static PioneerAnimationPlanSettings Resolve(GameObject host)
        {
            if (host == null)
                return null;

            PioneerAnimationPlanSettings settings = host.GetComponent<PioneerAnimationPlanSettings>();
            if (settings == null)
                settings = host.AddComponent<PioneerAnimationPlanSettings>();

            return settings;
        }
    }
}
