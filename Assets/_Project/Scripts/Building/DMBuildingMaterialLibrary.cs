using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project.Building
{
    public enum DMBuildingMaterialTier
    {
        Stone,
        Iron,
        Steel,
        Silicate,
        Amalgam
    }

    [Serializable]
    public sealed class DMBuildingMaterialVariant
    {
        public string id = "stone_rough";
        public string displayName = "Rough Stone";
        public DMBuildingMaterialTier tier = DMBuildingMaterialTier.Stone;
        public Material finishedMaterial;
        public bool overrideGhostTint;
        public Color ghostTint = new Color(0.92f, 0.38f, 0.32f, 1f);
    }

    /// <summary>
    /// Finished-mesh materials for built pieces. Stone has two finishes; later tiers stay empty.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Building/Material Library")]
    public sealed class DMBuildingMaterialLibrary : ScriptableObject
    {
        public const string ResourcePath = "Building/DM_BuildingMaterialLibrary";
        public const string AssetPath = "Assets/_Project/Resources/Building/DM_BuildingMaterialLibrary.asset";

        public List<DMBuildingMaterialVariant> variants = new List<DMBuildingMaterialVariant>();

        static DMBuildingMaterialLibrary live;

        public static DMBuildingMaterialLibrary Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMBuildingMaterialLibrary>(ResourcePath);
                return live;
            }
        }

        public static DMBuildingMaterialVariant Find(string id)
        {
            DMBuildingMaterialLibrary library = Live;
            if (library == null || library.variants == null || string.IsNullOrEmpty(id))
                return null;

            for (int i = 0; i < library.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = library.variants[i];
                if (variant != null && variant.id == id)
                    return variant;
            }

            return null;
        }

        public static DMBuildingMaterialVariant FirstForTier(DMBuildingMaterialTier tier)
        {
            DMBuildingMaterialLibrary library = Live;
            if (library == null || library.variants == null)
                return null;

            for (int i = 0; i < library.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = library.variants[i];
                if (variant != null && variant.tier == tier)
                    return variant;
            }

            return null;
        }

        public static DMBuildingMaterialVariant NextForTier(DMBuildingMaterialTier tier, string currentId)
        {
            DMBuildingMaterialLibrary library = Live;
            if (library == null || library.variants == null)
                return null;

            int first = -1;
            int current = -1;
            for (int i = 0; i < library.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = library.variants[i];
                if (variant == null || variant.tier != tier || !IsStructuralFinish(variant.id))
                    continue;
                if (first < 0)
                    first = i;
                if (variant.id == currentId)
                    current = i;
            }

            if (first < 0)
                return null;

            for (int i = current + 1; i < library.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = library.variants[i];
                if (variant != null && variant.tier == tier && IsStructuralFinish(variant.id))
                    return variant;
            }

            return library.variants[first];
        }

        static bool IsStructuralFinish(string id)
        {
            return id != "door" && id != "window_glass";
        }
    }
}
