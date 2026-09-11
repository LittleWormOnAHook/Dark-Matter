using System;
using System.IO;
using System.Text.RegularExpressions;
using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Appends a new member to <see cref="AmmoType"/> so the ammo creator can register types.
    /// Existing integer values are never changed.
    /// </summary>
    public static class DMAmmoTypeEnumUtility
    {
        public const string AmmoTypePath = "Assets/_Project/Scripts/Combat/AmmoType.cs";
        public const string StatusEffectPath = "Assets/_Project/Scripts/Combat/StatusEffectType.cs";

        public static bool TryAddType(
            string rawName,
            StatusEffectType defaultStatus,
            bool addToMultiAmmoWeapons,
            out AmmoType type,
            out string error)
        {
            type = default;
            error = null;

            if (!TryMakeIdentifier(rawName, out string id, out error))
                return false;

            if (!TryReadEnum(out int nextValue, out error))
                return false;

            if (NameExists(id))
            {
                error = $"AmmoType '{id}' already exists.";
                return false;
            }

            if (!AppendEnumMember(id, nextValue, out error))
                return false;

            if (defaultStatus != StatusEffectType.None)
                AppendStatusDefault(id, defaultStatus);

            type = (AmmoType)nextValue;
            if (addToMultiAmmoWeapons)
                AddTypeToMultiAmmoWeapons(type);

            AssetDatabase.ImportAsset(AmmoTypePath);
            if (defaultStatus != StatusEffectType.None)
                AssetDatabase.ImportAsset(StatusEffectPath);

            return true;
        }

        public static bool TryMakeIdentifier(string rawName, out string id, out string error)
        {
            id = null;
            error = null;
            if (string.IsNullOrWhiteSpace(rawName))
            {
                error = "New type name is required (e.g. Acid or CryoCells).";
                return false;
            }

            string trimmed = rawName.Trim();
            string[] parts = Regex.Split(trimmed, @"[^A-Za-z0-9]+");
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.IsNullOrEmpty(parts[i]))
                    continue;
                string part = parts[i];
                sb.Append(char.ToUpperInvariant(part[0]));
                if (part.Length > 1)
                    sb.Append(part.Substring(1));
            }

            id = sb.ToString();
            if (string.IsNullOrEmpty(id) || !Regex.IsMatch(id, @"^[A-Za-z_][A-Za-z0-9_]*$"))
            {
                error = "Type name must start with a letter (e.g. Acid, CryoCells).";
                return false;
            }

            if (id == "AmmoType" || IsCSharpKeyword(id))
            {
                error = $"'{id}' is not a valid C# type name.";
                return false;
            }

            return true;
        }

        private static bool TryReadEnum(out int nextValue, out string error)
        {
            nextValue = 0;
            error = null;
            string path = ToDiskPath(AmmoTypePath);
            if (!File.Exists(path))
            {
                error = $"Missing {AmmoTypePath}.";
                return false;
            }

            int max = -1;
            foreach (Match match in Regex.Matches(File.ReadAllText(path), @"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(\d+)", RegexOptions.Multiline))
            {
                if (int.TryParse(match.Groups[2].Value, out int value) && value > max)
                    max = value;
            }

            nextValue = max + 1;
            return true;
        }

        private static bool NameExists(string id)
        {
            foreach (string name in Enum.GetNames(typeof(AmmoType)))
            {
                if (string.Equals(name, id, StringComparison.Ordinal))
                    return true;
            }

            string path = ToDiskPath(AmmoTypePath);
            if (!File.Exists(path))
                return false;
            return Regex.IsMatch(File.ReadAllText(path), $@"\b{Regex.Escape(id)}\s*=");
        }

        private static bool AppendEnumMember(string id, int value, out string error)
        {
            error = null;
            string path = ToDiskPath(AmmoTypePath);
            string text = File.ReadAllText(path);
            int close = text.LastIndexOf('}');
            if (close < 0)
            {
                error = "Could not parse AmmoType.cs.";
                return false;
            }

            int enumClose = text.LastIndexOf('}', close - 1);
            if (enumClose < 0)
            {
                error = "Could not find the AmmoType enum body.";
                return false;
            }

            string insert = $",{System.Environment.NewLine}        {id} = {value}";
            text = text.Insert(enumClose, insert);
            File.WriteAllText(path, text);
            return true;
        }

        private static void AppendStatusDefault(string id, StatusEffectType status)
        {
            string path = ToDiskPath(StatusEffectPath);
            if (!File.Exists(path))
                return;

            string text = File.ReadAllText(path);
            string caseLine = $"                case AmmoType.{id}:";
            if (text.Contains(caseLine))
                return;

            const string marker = "                default:";
            int at = text.IndexOf(marker, StringComparison.Ordinal);
            if (at < 0)
                return;

            string block =
                $"                case AmmoType.{id}:{System.Environment.NewLine}" +
                $"                    return StatusEffectType.{status};{System.Environment.NewLine}";
            File.WriteAllText(path, text.Insert(at, block));
        }

        private static void AddTypeToMultiAmmoWeapons(AmmoType type)
        {
            ItemData[] items = CraftingEditorUtility.LoadAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null || item.itemType != ItemType.RangedWeapon || item.isMiningTool)
                    continue;
                if (item.compatibleAmmoTypes == null || item.compatibleAmmoTypes.Length == 0)
                    continue;
                if (item.AcceptsAmmoType(type))
                    continue;

                ArrayUtility.Add(ref item.compatibleAmmoTypes, type);
                EditorUtility.SetDirty(item);
            }
        }

        private static string ToDiskPath(string assetPath)
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", assetPath));
        }

        private static bool IsCSharpKeyword(string id)
        {
            switch (id)
            {
                case "abstract":
                case "as":
                case "base":
                case "bool":
                case "break":
                case "byte":
                case "case":
                case "catch":
                case "char":
                case "checked":
                case "class":
                case "const":
                case "continue":
                case "decimal":
                case "default":
                case "delegate":
                case "do":
                case "double":
                case "else":
                case "enum":
                case "event":
                case "explicit":
                case "extern":
                case "false":
                case "finally":
                case "fixed":
                case "float":
                case "for":
                case "foreach":
                case "goto":
                case "if":
                case "implicit":
                case "in":
                case "int":
                case "interface":
                case "internal":
                case "is":
                case "lock":
                case "long":
                case "namespace":
                case "new":
                case "null":
                case "object":
                case "operator":
                case "out":
                case "override":
                case "params":
                case "private":
                case "protected":
                case "public":
                case "readonly":
                case "ref":
                case "return":
                case "sbyte":
                case "sealed":
                case "short":
                case "sizeof":
                case "stackalloc":
                case "static":
                case "string":
                case "struct":
                case "switch":
                case "this":
                case "throw":
                case "true":
                case "try":
                case "typeof":
                case "uint":
                case "ulong":
                case "unchecked":
                case "unsafe":
                case "ushort":
                case "using":
                case "virtual":
                case "void":
                case "volatile":
                case "while":
                    return true;
                default:
                    return false;
            }
        }
    }
}
