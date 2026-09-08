using System.Collections.Generic;
using System.Text;
using Project.Data;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    [InitializeOnLoad]
    public static class DMHotCrossIconAutoImport
    {
        static DMHotCrossIconAutoImport()
        {
            EditorApplication.delayCall += DMHotCrossIconRegistryEditor.ImportHotCrossSprites;
        }
    }

    [CustomEditor(typeof(DMHotCrossIconRegistry))]
    public class DMHotCrossIconRegistryEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            EditorGUILayout.HelpBox(
                "Hot Cross cutouts only from Resources/UI/HotCrossIcons. Inventory / hotbar tiles use Resources/UI/Game Icons.",
                MessageType.Info);

            if (GUILayout.Button("Import HotCrossIcons as Sprites"))
                ImportHotCrossSprites();

            if (GUILayout.Button("Rebuild entries from Item Registry + HotCrossIcons"))
                RebuildEntries((DMHotCrossIconRegistry)target);
        }

        [MenuItem("Dark Matter Genesis/UI/Hot Cross/Import Cutout Icons as Sprites")]
        public static void ImportHotCrossSprites()
        {
            ImportFolder(DMHotCrossIconRegistry.CutoutAssetFolder);
        }

        private static void ImportFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return;

            string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
            int changed = 0;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                var importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                    continue;

                bool dirty = importer.textureType != TextureImporterType.Sprite
                    || importer.spriteImportMode != SpriteImportMode.Single
                    || importer.mipmapEnabled
                    || !importer.alphaIsTransparency
                    || importer.npotScale != TextureImporterNPOTScale.None;

                if (!dirty)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.npotScale = TextureImporterNPOTScale.None;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
                changed++;
            }

            if (changed > 0)
                Debug.Log($"Hot Cross icons: imported {changed} sprite textures from {folder}.");
        }

        [MenuItem("Dark Matter Genesis/UI/Hot Cross/Rebuild Icon Registry")]
        public static void RebuildDefaultRegistry()
        {
            DMHotCrossIconRegistry registry = DMHotCrossIconRegistry.LoadDefault();
            if (registry == null)
            {
                const string assetPath = "Assets/_Project/Resources/DMHotCrossIconRegistry.asset";
                registry = CreateInstance<DMHotCrossIconRegistry>();
                AssetDatabase.CreateAsset(registry, assetPath);
            }

            RebuildEntries(registry);
        }

        public static void RebuildEntries(DMHotCrossIconRegistry registry)
        {
            if (registry == null)
                return;

            ItemData[] items = ItemRegistry.GetAllItems();
            Sprite[] sprites = LoadSprites(DMHotCrossIconRegistry.CutoutAssetFolder);

            Dictionary<string, Sprite> byKey = new Dictionary<string, Sprite>();
            for (int i = 0; i < sprites.Length; i++)
            {
                Sprite sprite = sprites[i];
                if (sprite == null)
                    continue;
                string key = Normalize(sprite.name);
                if (!byKey.ContainsKey(key))
                    byKey[key] = sprite;
            }

            List<DMHotCrossIconEntry> next = new List<DMHotCrossIconEntry>(items.Length);
            int matched = 0;
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null)
                    continue;

                Sprite found = FindSprite(item, byKey);
                if (found != null)
                    matched++;

                next.Add(new DMHotCrossIconEntry
                {
                    item = item,
                    icon = found,
                    tint = Color.white,
                    alpha = 1f,
                    emissionColor = Color.white,
                    emission = 0f
                });
            }

            Undo.RecordObject(registry, "Rebuild Hot Cross Icon Registry");
            registry.entries = next.ToArray();
            EditorUtility.SetDirty(registry);
            AssetDatabase.SaveAssets();
            Debug.Log($"Hot Cross icon registry: {next.Count} items, {matched} cutout sprites matched.");
        }

        private static Sprite[] LoadSprites(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
                return System.Array.Empty<Sprite>();

            string[] guids = AssetDatabase.FindAssets("t:Sprite", new[] { folder });
            var sprites = new List<Sprite>(guids.Length);
            for (int i = 0; i < guids.Length; i++)
            {
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath(guids[i]));
                if (sprite != null)
                    sprites.Add(sprite);
            }

            return sprites.ToArray();
        }

        private static Sprite FindSprite(ItemData item, Dictionary<string, Sprite> byKey)
        {
            string[] candidates =
            {
                item.itemName,
                item.name,
                Alias(item.itemName),
                Alias(item.name)
            };

            for (int i = 0; i < candidates.Length; i++)
            {
                string key = Normalize(candidates[i]);
                if (key.Length == 0)
                    continue;
                if (byKey.TryGetValue(key, out Sprite sprite))
                    return sprite;
            }

            string itemKey = Normalize(item.itemName);
            foreach (KeyValuePair<string, Sprite> pair in byKey)
            {
                if (itemKey.Length > 0 && (pair.Key.Contains(itemKey) || itemKey.Contains(pair.Key)))
                    return pair.Value;
            }

            return null;
        }

        private static string Alias(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value;

            string n = value.Trim();
            if (n.Equals("Binnoculars", System.StringComparison.OrdinalIgnoreCase)
                || n.Equals("Binoculars", System.StringComparison.OrdinalIgnoreCase))
                return "Binoculars";
            if (n.Equals("Brimestone Blade", System.StringComparison.OrdinalIgnoreCase))
                return "Brimstone Blade";
            if (n.Equals("Forrest Stew", System.StringComparison.OrdinalIgnoreCase)
                || n.Equals("Forest Stew", System.StringComparison.OrdinalIgnoreCase))
                return "Forest Stew";
            if (n.IndexOf("Scan", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Multi-Purpose Scan Tool";
            if (n.IndexOf("Binocular", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return "Binnoculars";
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
            if (key.StartsWith("blueprint"))
                key = "blueprint" + key.Substring("blueprint".Length);
            return key
                .Replace("binnoculars", "binoculars")
                .Replace("brimestone", "brimstone")
                .Replace("forrest", "forest")
                .Replace("survivalrifle", "survivalrifle");
        }
    }
}
