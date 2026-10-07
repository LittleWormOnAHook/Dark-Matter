using UnityEngine;

namespace Project.Events
{
    /// <summary>When the World Reloot window starts counting (loot plan 6.1).</summary>
    public enum DMLootChestTimerStart
    {
        /// <summary>Starts the first time the player closes the window with items left (default).</summary>
        FirstExitWithLeftovers = 0,

        /// <summary>Starts the first time the chest is opened.</summary>
        FirstOpen = 1
    }

    /// <summary>
    /// Live loot chest tuning (loot plan 6.1). Genesis Studio edits this asset under World:
    /// "Loot Chests - Timers" and "Loot Chests - Presentation".
    /// Lid (phase 3), timers, holds and dissolve (phase 4, ticked by DMLootChestRuntime) read it.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Loot/Loot Chest Profile")]
    public sealed class DMLootChestProfile : ScriptableObject
    {
        public const string ResourcePath = "Loot/DM_LootChestProfile";
        public const string AssetPath = "Assets/_Project/Resources/Loot/DM_LootChestProfile.asset";

        public const float DefaultCloseRangeMeters = 4.5f;

        [Header("World Reloot")]
        [Tooltip("Seconds (game time, closed only) a World Reloot chest keeps its leftovers before it dissolves for good (D1).")]
        [Min(0f)] public float relootWindowSeconds = 120f;

        [Tooltip("When the reloot window starts counting.")]
        public DMLootChestTimerStart timerStart = DMLootChestTimerStart.FirstExitWithLeftovers;

        [Tooltip("Seconds after the last item is taken before an emptied chest dissolves.")]
        [Min(0f)] public float emptiedDissolveDelay = 2f;

        [Header("Single Loot")]
        [Tooltip("Seconds from window close until a Single Loot chest is removed. The dissolve plays in the final Dissolve Seconds.")]
        [Min(0f)] public float postExitDestroySeconds = 5f;

        [Header("Full-inventory hold")]
        [Tooltip("Seconds a chest keeps items the player could not carry (30 minutes). Restarts on every revisit.")]
        [Min(0f)] public float fullInventoryHoldSeconds = 1800f;

        [Header("Storage")]
        [Tooltip("Slot count for storage chests that do not set their own.")]
        [Min(1)] public int defaultSlotCount = 20;

        [Tooltip("When on, walking away also closes the storage window.")]
        public bool rangeCloseAppliesToStorage;

        [Header("Shared presentation")]
        [Min(0f)] public float lidOpenSeconds = 1f;
        [Min(0f)] public float lidCloseSeconds = 0.8f;

        [Tooltip("Seconds after the lid starts opening before the loot window shows.")]
        [Min(0f)] public float openLootWindowDelay = 1f;

        [Tooltip("Meters from the chest where the open prompt shows.")]
        [Min(0f)] public float interactRange = 3f;

        [Tooltip("Meters from the container where an open loot window closes on its own.")]
        [Min(0f)] public float closeRangeMeters = DefaultCloseRangeMeters;

        [Tooltip("Dissolve length in seconds. Never longer than Post Exit Destroy Seconds.")]
        [Min(0f)] public float dissolveSeconds = 1.6f;

        [Min(0f)] public float dissolveEdgeWidth = 0.06f;
        public Color dissolveEdgeColor = new Color(0.831f, 0.627f, 0.090f, 1f);

        [Tooltip("Dissolve shader reference (no Shader.Find at runtime).")]
        public Shader dissolveShader;

        [Tooltip("Optional dissolve material template. Wins over the shader when set.")]
        public Material dissolveMaterial;

        [Header("Enemy drops")]
        [Tooltip("Loot bag / drop box spawned when an enemy dies (EnemyLootable). " +
                 "An enemy prefab or EnemyDefinition with its own bag prefab still wins.")]
        public GameObject enemyLootBagPrefab;

        static DMLootChestProfile live;

        public static DMLootChestProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMLootChestProfile>(ResourcePath);
                return live;
            }
        }

        /// <summary>Dissolve time actually used: clamped so it fits inside the post-exit window.</summary>
        public float EffectiveDissolveSeconds => Mathf.Min(dissolveSeconds, postExitDestroySeconds);

        /// <summary>
        /// Enemy drop prefab from the profile (Prefabs/Combat/EnemyLootBag). Loot plan phase 7 retired the old
        /// Resources sphere bag fallback: an enemy / EnemyDefinition prefab wins, then this, else no bag (warning).
        /// </summary>
        public static GameObject ResolveEnemyLootBagPrefab()
        {
            DMLootChestProfile profile = Live;
            return profile != null ? profile.enemyLootBagPrefab : null;
        }

        public static float ResolveCloseRangeMeters()
        {
            DMLootChestProfile profile = Live;
            return profile != null ? Mathf.Max(0.5f, profile.closeRangeMeters) : DefaultCloseRangeMeters;
        }

        private void OnValidate()
        {
            if (dissolveSeconds > postExitDestroySeconds)
                dissolveSeconds = postExitDestroySeconds;
        }
    }
}
