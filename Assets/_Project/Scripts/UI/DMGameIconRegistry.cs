using System;
using System.Collections.Generic;
using System.Text;
using Project.Data;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Inventory / hotbar plated tiles from Resources/UI/Game Icons.
    /// Hot Cross cutouts stay on <see cref="DMHotCrossIconRegistry"/>.
    /// </summary>
    public static class DMGameIconRegistry
    {
        public const string IconResourceFolder = "UI/Game Icons";

        private static Dictionary<string, Sprite> icons;
        private static readonly Dictionary<EntityId, Sprite> iconByItemId = new Dictionary<EntityId, Sprite>(64);

        public static Sprite FindIcon(ItemData item)
        {
            if (item == null)
                return null;

            EntityId id = item.GetEntityId();
            if (iconByItemId.TryGetValue(id, out Sprite cached) && cached != null)
                return cached;

            EnsureIcons();
            Sprite found = ResolveUncached(item);
            if (found != null)
                iconByItemId[id] = found;
            return found != null ? found : item.icon;
        }

        private static Sprite ResolveUncached(ItemData item)
        {
            if (icons == null || icons.Count == 0)
                return null;

            if (TryExact(item.itemName, out Sprite sprite)
                || TryExact(item.name, out sprite)
                || TryExact(Alias(item.itemName), out sprite)
                || TryExact(Alias(item.name), out sprite))
                return sprite;

            string itemKey = Normalize(item.itemName);
            if (itemKey.Length == 0)
                itemKey = Normalize(item.name);
            if (itemKey.Length < 6)
                return null;

            foreach (KeyValuePair<string, Sprite> pair in icons)
            {
                if (pair.Key.Contains(itemKey) || itemKey.Contains(pair.Key))
                    return pair.Value;
            }

            return null;
        }

        private static bool TryExact(string raw, out Sprite sprite)
        {
            sprite = null;
            string key = Normalize(raw);
            return key.Length > 0 && icons.TryGetValue(key, out sprite);
        }

        private static void EnsureIcons()
        {
            if (icons != null && icons.Count > 0)
                return;

            icons = new Dictionary<string, Sprite>(64);
            Sprite[] loaded = Resources.LoadAll<Sprite>(IconResourceFolder);
            for (int i = 0; i < loaded.Length; i++)
                AddIcon(loaded[i]);

            if (icons.Count > 0)
                return;

            Texture2D[] textures = Resources.LoadAll<Texture2D>(IconResourceFolder);
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
                AddIcon(sprite);
            }
        }

        private static void AddIcon(Sprite sprite)
        {
            if (sprite == null)
                return;
            string key = Normalize(sprite.name);
            if (key.Length == 0 || icons.ContainsKey(key))
                return;
            icons[key] = sprite;
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
            if (n.IndexOf("laser", StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("ammo", StringComparison.OrdinalIgnoreCase) >= 0)
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
            if (n.IndexOf("oxygen", StringComparison.OrdinalIgnoreCase) >= 0
                && n.IndexOf("mini", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Oxygen Mini Tank";
            if (n.IndexOf("fear", StringComparison.OrdinalIgnoreCase) >= 0)
                return "Sword Of Fear";
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
                .Replace("forrest", "forest")
                .Replace("oxygenminitank", "oxygentankmini")
                .Replace("swordoffear", "swordoffear");
        }
    }
}
