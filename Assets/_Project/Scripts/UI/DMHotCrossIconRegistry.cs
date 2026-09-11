using System;
using System.Collections.Generic;
using System.Text;
using Project.Data;
using UnityEngine;

namespace Project.UI
{
    [Serializable]
    public struct DMHotCrossIconEntry
    {
        public ItemData item;
        [Tooltip("Hot Cross cutout only. Overrides the HotCrossIcons folder match.")]
        public Sprite icon;
        public Color tint;
        [Range(0f, 1f)]
        public float alpha;
        public Color emissionColor;
        [Range(0f, 4f)]
        public float emission;
    }

    [CreateAssetMenu(fileName = "DMHotCrossIconRegistry", menuName = "Dark Matter Genesis/UI/Hot Cross Icon Registry")]
    public class DMHotCrossIconRegistry : ScriptableObject
    {
        public const string ResourcesName = "DMHotCrossIconRegistry";
        public const string CutoutResourceFolder = "UI/HotCrossIcons";
        public const string CutoutAssetFolder = "Assets/_Project/Resources/UI/HotCrossIcons";

        [Header("Defaults")]
        public Color defaultTint = Color.white;
        [Range(0f, 1f)]
        public float defaultAlpha = 1f;
        public Color defaultEmissionColor = Color.white;
        [Range(0f, 4f)]
        public float defaultEmission;

        [Header("Per item (Hot Cross only)")]
        public DMHotCrossIconEntry[] entries = Array.Empty<DMHotCrossIconEntry>();

        private static DMHotCrossIconRegistry cached;
        private static Dictionary<string, Sprite> cutouts;
        private static readonly Dictionary<EntityId, Sprite> cutoutByItemId = new Dictionary<EntityId, Sprite>(64);

        public static DMHotCrossIconRegistry LoadDefault()
        {
            if (cached != null)
                return cached;
            cached = Resources.Load<DMHotCrossIconRegistry>(ResourcesName);
            return cached;
        }

        public bool TryResolve(ItemData item, out Sprite sprite, out Color tint, out Color emissionColor, out float emission)
        {
            sprite = FindCutout(item);
            tint = defaultTint;
            tint.a = defaultAlpha;
            emissionColor = defaultEmissionColor;
            emission = defaultEmission;

            if (item == null)
                return sprite != null;

            if (entries != null)
            {
                for (int i = 0; i < entries.Length; i++)
                {
                    DMHotCrossIconEntry entry = entries[i];
                    if (entry.item != item)
                        continue;

                    if (entry.icon != null && entry.icon.texture != null
                        && entry.icon.texture.width > 0 && entry.icon.rect.width > 0f)
                        sprite = entry.icon;
                    tint = entry.tint;
                    tint.a = entry.alpha;
                    emissionColor = entry.emissionColor;
                    emission = entry.emission;
                    return sprite != null;
                }
            }

            return sprite != null;
        }

        public static Sprite FindCutout(ItemData item)
        {
            if (item == null)
                return null;

            EntityId id = item.GetEntityId();
            if (cutoutByItemId.TryGetValue(id, out Sprite cachedCutout) && cachedCutout != null)
                return cachedCutout;

            EnsureCutouts();
            Sprite found = ResolveCutoutUncached(item);
            if (found != null)
                cutoutByItemId[id] = found;
            return found;
        }

        private static Sprite ResolveCutoutUncached(ItemData item)
        {
            if (cutouts == null || cutouts.Count == 0)
                return null;

            if (TryExactCutout(item.itemName, out Sprite sprite)
                || TryExactCutout(item.name, out sprite)
                || TryExactCutout(Alias(item.itemName), out sprite)
                || TryExactCutout(Alias(item.name), out sprite))
                return sprite;

            string itemKey = Normalize(item.itemName);
            if (itemKey.Length == 0)
                itemKey = Normalize(item.name);
            if (itemKey.Length == 0)
                return null;

            foreach (KeyValuePair<string, Sprite> pair in cutouts)
            {
                if (pair.Key.Contains(itemKey) || itemKey.Contains(pair.Key))
                    return pair.Value;
            }

            return null;
        }

        private static bool TryExactCutout(string raw, out Sprite sprite)
        {
            sprite = null;
            string key = Normalize(raw);
            return key.Length > 0 && cutouts.TryGetValue(key, out sprite);
        }

        private static void EnsureCutouts()
        {
            if (cutouts != null && cutouts.Count > 0)
                return;

            cutouts = new Dictionary<string, Sprite>(64);
            Sprite[] loaded = Resources.LoadAll<Sprite>(CutoutResourceFolder);
            for (int i = 0; i < loaded.Length; i++)
                AddCutout(loaded[i]);

            if (cutouts.Count > 0)
                return;

            Texture2D[] textures = Resources.LoadAll<Texture2D>(CutoutResourceFolder);
            for (int i = 0; i < textures.Length; i++)
            {
                Texture2D texture = textures[i];
                if (texture == null)
                    continue;
                Sprite sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
                sprite.name = texture.name;
                AddCutout(sprite);
            }

#if UNITY_EDITOR
            if (cutouts.Count > 0)
                return;

            TryLoadEditorCutouts(CutoutAssetFolder);
#endif
        }

#if UNITY_EDITOR
        private static void TryLoadEditorCutouts(string folder)
        {
            if (!UnityEditor.AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            for (int i = 0; i < (guids != null ? guids.Length : 0); i++)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]);
                AddCutout(UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path));
            }
        }
#endif

        private static void AddCutout(Sprite sprite)
        {
            if (sprite == null)
                return;
            string key = Normalize(sprite.name);
            if (key.Length == 0 || cutouts.ContainsKey(key))
                return;
            cutouts[key] = sprite;
        }

        private static string Alias(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string n = value.Trim();
            if (n.IndexOf("binocular", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Binnoculars";
            if (n.IndexOf("scan", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Multi-Purpose Scan Tool";
            if (n.Equals("Standard", StringComparison.OrdinalIgnoreCase))
                return "Standard Ammo";
            if (n.IndexOf("laser", StringComparison.OrdinalIgnoreCase) >= 0 && n.IndexOf("ammo", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Laser Ammo";
            if (n.Equals("Ice", StringComparison.OrdinalIgnoreCase))
                return "Ice";
            if (n.Equals("Fire", StringComparison.OrdinalIgnoreCase))
                return "Fire";
            if (n.Equals("Electricity", StringComparison.OrdinalIgnoreCase))
                return "Electricity";
            if (n.Equals("Explosive", StringComparison.OrdinalIgnoreCase))
                return "Explosive";
            if (n.Equals("Ion", StringComparison.OrdinalIgnoreCase))
                return "Ion";
            if (n.IndexOf("resonance", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Resonance Stabilizer";
            if (n.IndexOf("pistol", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Pistol";
            if (n.IndexOf("grenade", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Grenade";
            if (n.IndexOf("hover", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Hover Vehicle";
            if (n.IndexOf("mining", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Mining Drill";
            if (n.IndexOf("storage", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Storage Module";
            if (n.IndexOf("silicate", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Silicon";
            if (n.IndexOf("fear", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sword of Fear";
            if (n.IndexOf("2 hander", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("two-handed", StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("two handed", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Two Handed Sword";
            return n;
        }

        private static string Normalize(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return string.Empty;

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                if (char.IsLetterOrDigit(c))
                    sb.Append(char.ToLowerInvariant(c));
            }

            string key = sb.ToString();
            if (key.StartsWith("dm"))
                key = key.Substring(2);
            return key
                .Replace("binnoculars", "binoculars")
                .Replace("brimestone", "brimstone")
                .Replace("forrest", "forest");
        }
    }
}
