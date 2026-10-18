using System.Collections.Generic;
using FORGE3D;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Reads FORGE3D <see cref="F3DTurretScriptable"/> barrel prefabs
    /// (<c>Barrels</c> catalog + per-template <c>WeaponBarrels</c>).
    /// Empty id = Auto (socket enable by kind).
    /// </summary>
    public static class DMTurretBarrelCatalog
    {
        public const string DatabasePath = "Assets/FORGE3D/Sci-Fi Effects/Turrets/Database/database.asset";
        public const string AutoId = "";

        public readonly struct Option
        {
            public readonly string Id;
            public readonly string DisplayName;
            public readonly GameObject Prefab;

            public Option(string id, string displayName, GameObject prefab)
            {
                Id = id;
                DisplayName = displayName;
                Prefab = prefab;
            }
        }

        public static F3DTurretScriptable LoadDatabase(F3DTurretScriptable assigned)
        {
            if (assigned != null)
                return assigned;
#if UNITY_EDITOR
            return UnityEditor.AssetDatabase.LoadAssetAtPath<F3DTurretScriptable>(DatabasePath);
#else
            return null;
#endif
        }

        public static void CollectOptions(F3DTurretScriptable db, List<Option> buffer)
        {
            buffer.Clear();
            if (db == null)
                return;

            if (db.Barrels != null)
            {
                for (int i = 0; i < db.Barrels.Count; i++)
                    TryAdd(buffer, db.Barrels[i]);
            }

            if (db.Turrets == null)
                return;

            for (int t = 0; t < db.Turrets.Count; t++)
            {
                TurretStructure structure = db.Turrets[t];
                if (structure == null || structure.WeaponBarrels == null)
                    continue;
                for (int b = 0; b < structure.WeaponBarrels.Count; b++)
                    TryAdd(buffer, structure.WeaponBarrels[b]);
            }
        }

        public static GameObject FindPrefab(F3DTurretScriptable db, string id)
        {
            if (string.IsNullOrEmpty(id) || db == null)
                return null;

            if (db.Barrels != null)
            {
                for (int i = 0; i < db.Barrels.Count; i++)
                {
                    GameObject prefab = db.Barrels[i];
                    if (prefab != null && IdsEqual(prefab.name, id))
                        return prefab;
                }
            }

            if (db.Turrets == null)
                return null;

            for (int t = 0; t < db.Turrets.Count; t++)
            {
                TurretStructure structure = db.Turrets[t];
                if (structure == null || structure.WeaponBarrels == null)
                    continue;
                for (int b = 0; b < structure.WeaponBarrels.Count; b++)
                {
                    GameObject prefab = structure.WeaponBarrels[b];
                    if (prefab != null && IdsEqual(prefab.name, id))
                        return prefab;
                }
            }

            return null;
        }

        public static string ToId(GameObject prefab)
        {
            return prefab != null ? prefab.name : AutoId;
        }

        public static string ToDisplayName(string id)
        {
            if (string.IsNullOrEmpty(id))
                return "Auto";

            string name = id;
            const string prefix = "TURRET_BARREL_";
            if (name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(prefix.Length);
            const string lod = "_LOD0";
            if (name.EndsWith(lod, System.StringComparison.OrdinalIgnoreCase))
                name = name.Substring(0, name.Length - lod.Length);
            name = name.Replace('_', ' ');
            return string.IsNullOrEmpty(name) ? id : name;
        }

        public static bool SocketMatches(string socketName, string barrelId)
        {
            if (string.IsNullOrEmpty(socketName) || string.IsNullOrEmpty(barrelId))
                return false;

            DMTurretBarrelKind kind = KindFromId(barrelId);
            if (kind != DMTurretBarrelKind.Auto && BarrelTokenMatches(socketName, kind))
                return true;

            string token = CoreToken(barrelId);
            return !string.IsNullOrEmpty(token)
                && socketName.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static DMTurretBarrelKind KindFromId(string id)
        {
            if (string.IsNullOrEmpty(id))
                return DMTurretBarrelKind.Auto;
            if (Contains(id, "FLAMER"))
                return DMTurretBarrelKind.Flamer;
            if (Contains(id, "ELECTRIC"))
                return DMTurretBarrelKind.Electric;
            if (Contains(id, "REPEATER"))
                return DMTurretBarrelKind.Repeater;
            if (Contains(id, "FLACK"))
                return DMTurretBarrelKind.Flack;
            if (Contains(id, "GATLING"))
                return DMTurretBarrelKind.Gatling;
            if (Contains(id, "GAUSS"))
                return DMTurretBarrelKind.Gauss;
            if (Contains(id, "MORTAR"))
                return DMTurretBarrelKind.Mortar;
            if (Contains(id, "LASER"))
                return DMTurretBarrelKind.Laser;
            if (Contains(id, "HEAVY") || Contains(id, "MLRS") || Contains(id, "MISSLE") || Contains(id, "MISSILE"))
                return DMTurretBarrelKind.HeavyMissile;
            return DMTurretBarrelKind.Auto;
        }

        public static bool NamesMatch(string objectName, string prefabName)
        {
            if (string.IsNullOrEmpty(objectName) || string.IsNullOrEmpty(prefabName))
                return false;
            if (string.Equals(objectName, prefabName, System.StringComparison.OrdinalIgnoreCase))
                return true;
            return objectName.StartsWith(prefabName, System.StringComparison.OrdinalIgnoreCase);
        }

        public static bool IdsEqual(string a, string b)
        {
            return string.Equals(a, b, System.StringComparison.OrdinalIgnoreCase);
        }

        static void TryAdd(List<Option> buffer, GameObject prefab)
        {
            if (prefab == null)
                return;
            string id = prefab.name;
            for (int i = 0; i < buffer.Count; i++)
            {
                if (IdsEqual(buffer[i].Id, id))
                    return;
            }

            buffer.Add(new Option(id, ToDisplayName(id), prefab));
        }

        static bool BarrelTokenMatches(string socketName, DMTurretBarrelKind kind)
        {
            switch (kind)
            {
                case DMTurretBarrelKind.Flamer: return Contains(socketName, "FLAMER");
                case DMTurretBarrelKind.Electric: return Contains(socketName, "ELECTRIC");
                case DMTurretBarrelKind.Repeater: return Contains(socketName, "REPEATER");
                case DMTurretBarrelKind.Flack: return Contains(socketName, "FLACK");
                case DMTurretBarrelKind.Gatling: return Contains(socketName, "GATLING");
                case DMTurretBarrelKind.Gauss: return Contains(socketName, "GAUSS");
                case DMTurretBarrelKind.Mortar: return Contains(socketName, "MORTAR");
                case DMTurretBarrelKind.Laser: return Contains(socketName, "LASER");
                case DMTurretBarrelKind.HeavyMissile:
                    return Contains(socketName, "HEAVY") || Contains(socketName, "MLRS") || Contains(socketName, "MISSILE");
                default: return false;
            }
        }

        static string CoreToken(string id)
        {
            string name = ToDisplayName(id).Replace(" Mobile", string.Empty).Replace(" ", string.Empty);
            return name;
        }

        static bool Contains(string value, string token)
        {
            return !string.IsNullOrEmpty(value)
                && value.IndexOf(token, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
