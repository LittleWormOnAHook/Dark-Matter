using System;
using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Building
{
    /// <summary>What a part is. Snapping, size and support rules read the shape, never the id.</summary>
    public enum DMBuildingShape
    {
        Foundation,
        TriFoundation,
        Floor,
        TriFloor,
        Ceiling,
        Hatch,
        Wall,
        HalfWall,
        Window,
        DoorFrame,
        Door,
        Passage,
        TriWallLeft,
        TriWallRight,
        Railing,
        Stairs,
        Ramp,
        Roof,
        RoofCorner,
        RoofInner,
        /// <summary>Small item (light, decoration) that sticks to the face of a built piece.</summary>
        SurfaceItem,
        /// <summary>Any other prefab. Uses the module grid with its own mesh size.</summary>
        Custom,

        // Kit phase 2 (0926). Appended after Custom so saved shape numbers in style assets stay valid.
        FoundationSteps,
        SupportPillar,
        StairwellFloor,
        Balcony,
        QuarterWall,
        WideWindow,
        SlitWindow,
        Archway,
        InvTriWallLeft,
        InvTriWallRight,
        VentWall,
        Column,
        HalfColumn,
        Beam,
        Brace,
        HalfRailing,
        HalfStairs,
        SpiralStairs,
        HalfRamp,
        Ladder,
        SteepRoof,
        RidgeCap,
        Rooftop,
        HatchLid,

        // Kit phase 3 (0926): double doors, 8 m gates, half-cell foundations.
        DoubleDoorFrame,
        DoubleDoor,
        GateFrame,
        Gate,
        HalfFoundation,
        QuarterFoundation
    }

    public enum DMBuildingCategory
    {
        Foundations,
        Walls,
        FloorsAndRoofs,
        StructureAndStairs,
        Doors,
        Decor
    }

    /// <summary>Finished material for built pieces. The first finish of a style is its default; M cycles them.</summary>
    [Serializable]
    public sealed class DMBuildingMaterialVariant
    {
        public string id = "stone_rough";
        public string displayName = "Rough Stone";
        public Material finishedMaterial;
        public bool overrideGhostTint;
        public Color ghostTint = new Color(0.92f, 0.38f, 0.32f, 1f);
    }

    /// <summary>Detail sizes the kit builder uses for this style. Snap sizes stay fixed on the 4 m lattice.</summary>
    [Serializable]
    public sealed class DMBuildingKitSettings
    {
        public float windowWidthMeters = 2f;
        public float windowHeightMeters = 1.6f;
        [Tooltip("Window bottom above the wall base.")]
        public float windowSillMeters = 1.2f;
        public float glassThicknessMeters = 0.04f;
        [Tooltip("Passage opening width and height.")]
        public float passageWidthMeters = 2.8f;
        public float passageHeightMeters = 3.4f;
        [Tooltip("Square hole in the hatch slab.")]
        public float hatchOpeningMeters = 1.4f;
        public int stairSteps = 12;
        public int railingPosts = 3;

        [Header("Kit phase 2")]
        public float wideWindowWidthMeters = 3.2f;
        public float wideWindowHeightMeters = 1.8f;
        public float wideWindowSillMeters = 1.1f;
        public float slitWidthMeters = 0.3f;
        public float slitHeightMeters = 1.4f;
        public float slitSillMeters = 1.5f;
        [Tooltip("Archway opening width. The arch top is a half circle over it.")]
        public float archWidthMeters = 2.8f;
        public float archSpringMeters = 2.2f;
        public int archSegments = 12;
        public float ventWidthMeters = 2f;
        public float ventHeightMeters = 0.8f;
        public float ventSillMeters = 2.7f;
        public int ventSlats = 4;
        public int foundationStepCount = 4;
        [Tooltip("Square hole length in the stairwell floor, toward the stair top (+Z).")]
        public float stairwellOpeningMeters = 2.8f;
        public int spiralSteps = 16;
        public int ladderRungs = 12;
        public float rooftopEdgeHeightMeters = 0.6f;

        [Header("Kit phase 3")]
        [Tooltip("Gap left between the two leaves of a double door or gate.")]
        public float doorLeafGapMeters = 0.03f;
        [Tooltip("Horizontal bars across each face of a gate leaf.")]
        public int gateCrossbars = 3;

        public float DoorLeafGap => Mathf.Clamp(doorLeafGapMeters, 0f, 0.2f);
        public int GateCrossbars => Mathf.Clamp(gateCrossbars, 0, 8);

        public float WideWindowWidth => Mathf.Clamp(wideWindowWidthMeters, 0.4f, 3.6f);
        public float WideWindowHeight => Mathf.Clamp(wideWindowHeightMeters, 0.4f, 3.6f);
        public float WideWindowSill => Mathf.Clamp(wideWindowSillMeters, 0.1f, 3f);
        public float SlitWidth => Mathf.Clamp(slitWidthMeters, 0.1f, 1f);
        public float SlitHeight => Mathf.Clamp(slitHeightMeters, 0.3f, 3.4f);
        public float SlitSill => Mathf.Clamp(slitSillMeters, 0.1f, 3f);
        public float ArchWidth => Mathf.Clamp(archWidthMeters, 1f, 3.6f);
        public float ArchSpring => Mathf.Clamp(archSpringMeters, 0.5f, 3.2f);
        public int ArchSegments => Mathf.Clamp(archSegments, 4, 32);
        public float VentWidth => Mathf.Clamp(ventWidthMeters, 0.4f, 3.6f);
        public float VentHeight => Mathf.Clamp(ventHeightMeters, 0.2f, 2f);
        public float VentSill => Mathf.Clamp(ventSillMeters, 0.1f, 3.6f);
        public int VentSlats => Mathf.Clamp(ventSlats, 1, 12);
        public int FoundationStepCount => Mathf.Clamp(foundationStepCount, 2, 12);
        public float StairwellOpening => Mathf.Clamp(stairwellOpeningMeters, 1f, 3.6f);
        public int SpiralSteps => Mathf.Clamp(spiralSteps, 8, 32);
        public int LadderRungs => Mathf.Clamp(ladderRungs, 4, 24);
        public float RooftopEdgeHeight => Mathf.Clamp(rooftopEdgeHeightMeters, 0.1f, 1.5f);

        public float WindowWidth => Mathf.Clamp(windowWidthMeters, 0.4f, 3.4f);
        public float WindowHeight => Mathf.Clamp(windowHeightMeters, 0.4f, 3.4f);
        public float WindowSill => Mathf.Clamp(windowSillMeters, 0.1f, 3f);
        public float GlassThickness => Mathf.Clamp(glassThicknessMeters, 0.01f, 0.25f);
        public float PassageWidth => Mathf.Clamp(passageWidthMeters, 1f, 3.6f);
        public float PassageHeight => Mathf.Clamp(passageHeightMeters, 2f, 3.8f);
        public float HatchOpening => Mathf.Clamp(hatchOpeningMeters, 0.6f, 3.4f);
        public int StairSteps => Mathf.Clamp(stairSteps, 4, 32);
        public int RailingPosts => Mathf.Clamp(railingPosts, 2, 8);
    }

    /// <summary>One buildable part in a style library. Drop a prefab here and it appears on the build hotbar.</summary>
    [Serializable]
    public sealed class DMBuildingPartEntry
    {
        public string id = "new_part";
        public string displayName = "New Part";
        public DMBuildingShape shape = DMBuildingShape.Custom;
        public DMBuildingCategory category = DMBuildingCategory.Decor;
        public GameObject prefab;
        public Texture2D icon;
        [Min(0)] public int cost = 1;
        public bool enabled = true;
        [Tooltip("Paint the style finish on this part. Off keeps the prefab's own materials.")]
        public bool applyStyleFinish = true;
        [Tooltip("Surface items only: gap between the item and the face it sticks to (m).")]
        public float surfaceOffsetMeters = 0.01f;
        [Tooltip("Custom and surface items: footprint override (m). Zero uses the prefab mesh bounds.")]
        public Vector3 sizeOverride = Vector3.zero;
        [Tooltip("Custom and surface items: extra rotation (degrees) applied to the model inside the piece, for prefabs authored facing the wrong way.")]
        public Vector3 modelRotation = Vector3.zero;
    }

    /// <summary>
    /// One building style (Stone, Iron, Silicate...). Lives in Resources/Building/Styles.
    /// Holds its parts, finishes, cost item and kit settings. Edit it in Building Studio / Genesis Studio, Library tab.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Building/Style Library")]
    public sealed class DMBuildingStyleLibrary : ScriptableObject
    {
        public const string ResourceFolder = "Building/Styles";
        public const string AssetFolder = "Assets/_Project/Resources/Building/Styles";

        [Header("Style")]
        public string styleId = "stone";
        public string displayName = "Stone";
        public int order;
        public Texture2D icon;
        public Color accent = new Color(0.75f, 0.72f, 0.68f, 1f);
        [Tooltip("Colour of the resource amount under the build hotbar for this style's parts.")]
        public Color resourceTextColor = new Color(0.93f, 0.91f, 0.89f, 1f);

        [Header("Cost")]
        [Tooltip("Item spent per part cost point. Names below also match (inventory and storage).")]
        public ItemData costItem;
        public List<string> costItemNames = new List<string>();
        [Min(0f)] public float costMultiplier = 1f;

        [Header("Materials")]
        public List<DMBuildingMaterialVariant> finishes = new List<DMBuildingMaterialVariant>();
        public Material doorMaterial;
        public Material glassMaterial;

        [Header("Kit")]
        [Tooltip("Id prefix the kit builder uses for generated parts, for example stone_.")]
        public string partIdPrefix = "stone_";
        public DMBuildingKitSettings kit = new DMBuildingKitSettings();

        [Header("Parts")]
        public List<DMBuildingPartEntry> parts = new List<DMBuildingPartEntry>();

        public string CostItemName
        {
            get
            {
                if (costItem != null)
                    return string.IsNullOrEmpty(costItem.itemName) ? costItem.name : costItem.itemName;
                return costItemNames != null && costItemNames.Count > 0 ? costItemNames[0] : displayName;
            }
        }

        public DMBuildingPartEntry FindPart(string partId)
        {
            if (parts == null || string.IsNullOrEmpty(partId))
                return null;
            for (int i = 0; i < parts.Count; i++)
            {
                if (parts[i] != null && parts[i].id == partId)
                    return parts[i];
            }

            return null;
        }

        public DMBuildingMaterialVariant FindFinish(string variantId)
        {
            if (finishes == null || string.IsNullOrEmpty(variantId))
                return null;
            for (int i = 0; i < finishes.Count; i++)
            {
                if (finishes[i] != null && finishes[i].id == variantId)
                    return finishes[i];
            }

            return null;
        }

        public DMBuildingMaterialVariant FirstFinish()
        {
            if (finishes == null)
                return null;
            for (int i = 0; i < finishes.Count; i++)
            {
                if (finishes[i] != null)
                    return finishes[i];
            }

            return null;
        }

        public DMBuildingMaterialVariant NextFinish(string currentId)
        {
            if (finishes == null || finishes.Count == 0)
                return null;
            int start = -1;
            for (int i = 0; i < finishes.Count; i++)
            {
                if (finishes[i] != null && finishes[i].id == currentId)
                {
                    start = i;
                    break;
                }
            }

            for (int step = 1; step <= finishes.Count; step++)
            {
                DMBuildingMaterialVariant candidate = finishes[(start + step + finishes.Count) % finishes.Count];
                if (candidate != null)
                    return candidate;
            }

            return null;
        }

        public bool MatchesCostItem(ItemData item)
        {
            if (item == null)
                return false;
            if (costItem != null && item == costItem)
                return true;
            if (costItemNames == null || costItemNames.Count == 0)
                return false;

            string itemName = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            for (int i = 0; i < costItemNames.Count; i++)
            {
                string candidate = costItemNames[i];
                if (!string.IsNullOrEmpty(candidate)
                    && (candidate.Equals(itemName, StringComparison.OrdinalIgnoreCase)
                        || candidate.Equals(item.name, StringComparison.OrdinalIgnoreCase)))
                    return true;
            }

            return false;
        }

        public int ScaledCost(int baseCost)
        {
            if (baseCost <= 0)
                return 0;
            return Mathf.Max(1, Mathf.RoundToInt(baseCost * Mathf.Max(0f, costMultiplier)));
        }

        void OnValidate()
        {
            DMBuildingStyles.Invalidate();
        }

        void OnEnable()
        {
            DMBuildingStyles.Invalidate();
        }
    }

    /// <summary>Loads every style library from Resources/Building/Styles, sorted by order.</summary>
    public static class DMBuildingStyles
    {
        public const string DefaultId = "stone";

        static List<DMBuildingStyleLibrary> cache;
        static bool loading;

        /// <summary>Bumps whenever a style asset changes. The catalog rebuilds its piece list when this moves.</summary>
        public static int Version { get; private set; }

        public static IReadOnlyList<DMBuildingStyleLibrary> All
        {
            get
            {
                if (cache == null)
                {
                    // 0926-perf: loading fires OnEnable on each style, which calls Invalidate. Ignore those so the
                    // list is not dropped mid-load and the catalog does not rebuild a second time.
                    DMBuildingStyleLibrary[] loaded;
                    loading = true;
                    try
                    {
                        loaded = Resources.LoadAll<DMBuildingStyleLibrary>(DMBuildingStyleLibrary.ResourceFolder);
                    }
                    finally
                    {
                        loading = false;
                    }

                    var list = new List<DMBuildingStyleLibrary>();
                    for (int i = 0; i < loaded.Length; i++)
                    {
                        if (loaded[i] != null && !string.IsNullOrEmpty(loaded[i].styleId))
                            list.Add(loaded[i]);
                    }

                    list.Sort((a, b) => a.order != b.order ? a.order.CompareTo(b.order) : string.CompareOrdinal(a.styleId, b.styleId));
                    cache = list;
                }

                return cache;
            }
        }

        public static string DefaultStyleId => All.Count > 0 ? All[0].styleId : DefaultId;

        public static void Invalidate()
        {
            if (loading)
                return;
            cache = null;
            Version++;
        }

        public static DMBuildingStyleLibrary Find(string styleId)
        {
            if (string.IsNullOrEmpty(styleId))
                return null;
            IReadOnlyList<DMBuildingStyleLibrary> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i].styleId == styleId)
                    return all[i];
            }

            return null;
        }

        public static DMBuildingPartEntry FindPart(string partId, out DMBuildingStyleLibrary style)
        {
            IReadOnlyList<DMBuildingStyleLibrary> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                DMBuildingPartEntry part = all[i].FindPart(partId);
                if (part != null)
                {
                    style = all[i];
                    return part;
                }
            }

            style = null;
            return null;
        }

        public static DMBuildingMaterialVariant FindFinish(string variantId)
        {
            IReadOnlyList<DMBuildingStyleLibrary> all = All;
            for (int i = 0; i < all.Count; i++)
            {
                DMBuildingMaterialVariant finish = all[i].FindFinish(variantId);
                if (finish != null)
                    return finish;
            }

            return null;
        }
    }
}