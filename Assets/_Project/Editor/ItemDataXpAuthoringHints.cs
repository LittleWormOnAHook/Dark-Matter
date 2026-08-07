using Project.Data;
using Project.Progression;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Editor helpers for ItemData XP authoring from level gates.
    /// Approved formulas (auto-written when gates change in ItemData inspectors):
    /// one-time = round(12 · gate^1.35 + 8); continuous = round(4 + 0.5 · gate).
    /// </summary>
    public static class ItemDataXpAuthoringHints
    {
        /// <summary>Base for one-time special-item XP from the highest active level gate.</summary>
        public const float OneTimeBase = 12f;

        /// <summary>Exponent for one-time special-item XP vs gate level.</summary>
        public const float OneTimeExponent = 1.35f;

        /// <summary>Flat add on one-time special-item XP.</summary>
        public const float OneTimeFlat = 8f;

        /// <summary>Base continuous (every pickup/use/gather) XP.</summary>
        public const float ContinuousBase = 4f;

        /// <summary>Per-gate-level add for continuous XP.</summary>
        public const float ContinuousPerGateLevel = 0.5f;

        /// <summary>
        /// Highest active level gate (equip/craft/use/pickup). Values ≤1 are inactive.
        /// </summary>
        public static int GetAuthoringGateLevel(ItemData item)
        {
            if (item == null)
                return 1;

            int gate = 1;
            gate = MaxActive(gate, item.requiredLevelToEquip);
            gate = MaxActive(gate, item.requiredLevelToCraft);
            gate = MaxActive(gate, item.requiredLevelToUse);
            gate = MaxActive(gate, item.requiredLevelToPickup);
            return gate;
        }

        public static int GetAuthoringGateLevel(
            int requiredLevelToEquip,
            int requiredLevelToCraft,
            int requiredLevelToUse,
            int requiredLevelToPickup)
        {
            int gate = 1;
            gate = MaxActive(gate, requiredLevelToEquip);
            gate = MaxActive(gate, requiredLevelToCraft);
            gate = MaxActive(gate, requiredLevelToUse);
            gate = MaxActive(gate, requiredLevelToPickup);
            return gate;
        }

        /// <summary>
        /// Suggested <see cref="ItemData.xpAmount"/> from level gates.
        /// Continuous drip stays small; one-time rewards scale with gate level.
        /// </summary>
        public static int GetSuggestedXpAmount(int gateLevel, bool grantEveryPickupOrUse)
        {
            gateLevel = Mathf.Max(1, gateLevel);
            if (grantEveryPickupOrUse)
            {
                return Mathf.Max(
                    1,
                    Mathf.RoundToInt(ContinuousBase + ContinuousPerGateLevel * gateLevel));
            }

            return Mathf.Max(
                1,
                Mathf.RoundToInt(OneTimeBase * Mathf.Pow(gateLevel, OneTimeExponent) + OneTimeFlat));
        }

        public static int GetSuggestedXpAmount(ItemData item)
        {
            if (item == null)
                return 0;

            return GetSuggestedXpAmount(GetAuthoringGateLevel(item), item.grantXpEveryPickupOrUse);
        }

        /// <summary>
        /// Writes suggested XP onto <paramref name="xpAmount"/> from the given gate fields.
        /// Returns true when the value changed.
        /// </summary>
        public static bool ApplySuggestedXpFromGates(
            SerializedProperty xpAmount,
            int requiredLevelToEquip,
            int requiredLevelToCraft,
            int requiredLevelToUse,
            int requiredLevelToPickup,
            bool grantEveryPickupOrUse)
        {
            if (xpAmount == null)
                return false;

            int gate = GetAuthoringGateLevel(
                requiredLevelToEquip,
                requiredLevelToCraft,
                requiredLevelToUse,
                requiredLevelToPickup);
            int suggested = GetSuggestedXpAmount(gate, grantEveryPickupOrUse);
            if (xpAmount.intValue == suggested)
                return false;

            xpAmount.intValue = suggested;
            return true;
        }

        public static string FormatPreviewLabel(int gateLevel, bool continuous, int suggested, int currentXp)
        {
            string mode = continuous ? "continuous" : "one-time";
            string match = suggested == currentXp ? "matches" : $"current {currentXp}";
            return $"Suggested XP (gate Lv {gateLevel}, {mode}): {suggested}  [{match}]";
        }

        private static int MaxActive(int current, int gate)
        {
            if (!LevelUnlockUtility.IsGateActive(gate))
                return current;
            return Mathf.Max(current, gate);
        }
    }
}
