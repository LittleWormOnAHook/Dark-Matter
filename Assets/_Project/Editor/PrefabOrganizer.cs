using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// One-shot GUID-safe Prefabs tree cleanup (mirrors Data/Items category layout).
    /// </summary>
    public static class PrefabOrganizer
    {
        private static readonly string[] EnsureFolders =
        {
            ProjectAssetPaths.PrefabsBuildings,
            ProjectAssetPaths.PrefabsCombat,
            ProjectAssetPaths.PrefabsCombatEnemies,
            ProjectAssetPaths.PrefabsCombatProjectiles,
            ProjectAssetPaths.PrefabsCombatVfx,
            ProjectAssetPaths.PrefabsCompanions,
            ProjectAssetPaths.PrefabsCrafting,
            ProjectAssetPaths.PrefabsCraftingStations,
            ProjectAssetPaths.PrefabsEnvironment,
            ProjectAssetPaths.PrefabsEnvironmentCameraShake,
            ProjectAssetPaths.PrefabsEnvironmentExposure,
            ProjectAssetPaths.PrefabsItems,
            ProjectAssetPaths.PrefabsItemsHeld,
            ProjectAssetPaths.PrefabsItemsWorld,
            ProjectAssetPaths.PrefabsItemsAmmo,
            ProjectAssetPaths.PrefabsWeapons,
            ProjectAssetPaths.PrefabsWeaponsMelee,
            ProjectAssetPaths.PrefabsWeaponsRanged,
            ProjectAssetPaths.PrefabsTools,
            ProjectAssetPaths.PrefabsNpcs,
            ProjectAssetPaths.PrefabsPets,
            ProjectAssetPaths.PrefabsPlayers,
            ProjectAssetPaths.PrefabsUi,
            ProjectAssetPaths.PrefabsVehicles,
            ProjectAssetPaths.PrefabsWorld,
            ProjectAssetPaths.PrefabsWorldResources,
        };

        private static readonly (string source, string destination)[] Moves =
        {
            // Root VFX
            ("Assets/_Project/Prefabs/SparksLong.prefab",
                ProjectAssetPaths.PrefabsCombatVfx + "/SparksLong.prefab"),

            // Enemies → Combat/Enemies
            ("Assets/_Project/Prefabs/Enemys/Gongo.prefab",
                ProjectAssetPaths.PrefabsCombatEnemies + "/Gongo.prefab"),
            ("Assets/_Project/Prefabs/Combat/HumanoidEnemy_Invector.prefab",
                ProjectAssetPaths.PrefabsCombatEnemies + "/HumanoidEnemy_Invector.prefab"),
            ("Assets/_Project/Prefabs/Combat/The_Evil_One.prefab",
                ProjectAssetPaths.PrefabsCombatEnemies + "/The_Evil_One.prefab"),
            ("Assets/_Project/Prefabs/Combat/TrainingDummy.prefab",
                ProjectAssetPaths.PrefabsCombatEnemies + "/TrainingDummy.prefab"),

            // Gameplay mining nodes → World/Resources (leave Environment templates in place)
            ("Assets/_Project/Prefabs/Environment/Nodes Minerals and Plants/ResourceNode_Boulder_IronOre.prefab",
                ProjectAssetPaths.PrefabsWorldResources + "/ResourceNode_Boulder_IronOre.prefab"),
            ("Assets/_Project/Prefabs/Environment/Nodes Minerals and Plants/ResourceNode_Boulder_SilicateOre.prefab",
                ProjectAssetPaths.PrefabsWorldResources + "/ResourceNode_Boulder_SilicateOre.prefab"),
            ("Assets/_Project/Prefabs/Environment/Nodes Minerals and Plants/ResourceNode_SulfurNeedleTuft.prefab",
                ProjectAssetPaths.PrefabsWorldResources + "/ResourceNode_SulfurNeedleTuft.prefab"),

            // Loose Items root → World / Ammo
            ("Assets/_Project/Prefabs/Items/ammo_gunpowder_rounds_World.prefab",
                ProjectAssetPaths.PrefabsItemsAmmo + "/ammo_gunpowder_rounds_World.prefab"),
            ("Assets/_Project/Prefabs/Items/Plasma Fuel_World.prefab",
                ProjectAssetPaths.PrefabsItemsWorld + "/Plasma Fuel_World.prefab"),

            // Items/World/ammo → Items/Ammo
            ("Assets/_Project/Prefabs/Items/World/ammo/Laser Pistol Ammo_Pickup.prefab",
                ProjectAssetPaths.PrefabsItemsAmmo + "/Laser Pistol Ammo_Pickup.prefab"),
            ("Assets/_Project/Prefabs/Items/World/ammo/Laser_Pickup.prefab",
                ProjectAssetPaths.PrefabsItemsAmmo + "/Laser_Pickup.prefab"),
            ("Assets/_Project/Prefabs/Items/World/ammo/Plasma_Pickup.prefab",
                ProjectAssetPaths.PrefabsItemsAmmo + "/Plasma_Pickup.prefab"),
            ("Assets/_Project/Prefabs/Items/World/ammo/Standard_Pickup.prefab",
                ProjectAssetPaths.PrefabsItemsAmmo + "/Standard_Pickup.prefab"),

            // Melee weapons
            ("Assets/_Project/Prefabs/Items/Held/2 Hander_Held.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/2 Hander_Held.prefab"),
            ("Assets/_Project/Prefabs/Items/Held/two_handed.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/two_handed.prefab"),
            ("Assets/_Project/Prefabs/Items/World/2 Hander.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/2 Hander.prefab"),
            ("Assets/_Project/Prefabs/Items/World/Death Axe.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/Death Axe.prefab"),
            ("Assets/_Project/Prefabs/Items/World/Spear of Fate.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/Spear of Fate.prefab"),
            ("Assets/_Project/Prefabs/Items/World/Sword of Fear.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/Sword of Fear.prefab"),
            ("Assets/_Project/Prefabs/Items/World/Wood Axe.prefab",
                ProjectAssetPaths.PrefabsWeaponsMelee + "/Wood Axe.prefab"),

            // Ranged weapons
            ("Assets/_Project/Prefabs/Items/Held/sci_fi_pistol_Held.prefab",
                ProjectAssetPaths.PrefabsWeaponsRanged + "/sci_fi_pistol_Held.prefab"),
            ("Assets/_Project/Prefabs/Items/Held/survival_rifle_Held.prefab",
                ProjectAssetPaths.PrefabsWeaponsRanged + "/survival_rifle_Held.prefab"),
            ("Assets/_Project/Prefabs/Items/World/sci_fi_pistol.prefab",
                ProjectAssetPaths.PrefabsWeaponsRanged + "/sci_fi_pistol.prefab"),
            ("Assets/_Project/Prefabs/Items/World/survival_rifle.prefab",
                ProjectAssetPaths.PrefabsWeaponsRanged + "/survival_rifle.prefab"),
            ("Assets/_Project/Prefabs/Items/World/FlameThrower.prefab",
                ProjectAssetPaths.PrefabsWeaponsRanged + "/FlameThrower.prefab"),

            // Tools
            ("Assets/_Project/Prefabs/Items/Held/Binnos 250.prefab",
                ProjectAssetPaths.PrefabsTools + "/Binnos 250.prefab"),
            ("Assets/_Project/Prefabs/Items/Held/Scanner B44.prefab",
                ProjectAssetPaths.PrefabsTools + "/Scanner B44.prefab"),
            ("Assets/_Project/Prefabs/Items/Held/DM_Mining_Tool_Held.prefab",
                ProjectAssetPaths.PrefabsTools + "/DM_Mining_Tool_Held.prefab"),
            ("Assets/_Project/Prefabs/Items/World/DM_Mining_Tool.prefab",
                ProjectAssetPaths.PrefabsTools + "/DM_Mining_Tool.prefab"),
        };

        private static readonly string[] DeleteIfEmptyFolders =
        {
            "Assets/_Project/Prefabs/Items/Held",
            "Assets/_Project/Prefabs/Items/World/ammo",
            "Assets/_Project/Prefabs/Enemys",
            "Assets/_Project/Prefabs/Ammo Pickups",
        };

        [MenuItem(SurvivalPioneerEditorMenus.Project + "Organize Prefabs Folders", false, 11)]
        public static void OrganizePrefabsMenu()
        {
            if (!EditorUtility.DisplayDialog(
                    "Organize Prefabs",
                    "Creates Weapons/Tools/Items/Ammo/Combat/Enemies folders and moves prefabs with AssetDatabase.MoveAsset. Continue?",
                    "Organize",
                    "Cancel"))
                return;

            OrganizePrefabs();
        }

        /// <summary>Batch / menu entry point.</summary>
        public static void OrganizePrefabs()
        {
            for (int i = 0; i < EnsureFolders.Length; i++)
                CraftingEditorUtility.EnsureFolder(EnsureFolders[i]);

            int moved = 0;
            int skipped = 0;
            var log = new List<string>();

            for (int i = 0; i < Moves.Length; i++)
            {
                (string source, string destination) move = Moves[i];
                if (!AssetExists(move.source))
                {
                    skipped++;
                    continue;
                }

                if (AssetExists(move.destination))
                {
                    // Duplicate loose Plasma Fuel: drop orphan source if World copy already exists.
                    if (move.source.EndsWith("Plasma Fuel_World.prefab") &&
                        AssetDatabase.DeleteAsset(move.source))
                    {
                        log.Add($"DEL duplicate: {move.source}");
                        moved++;
                    }
                    else
                    {
                        log.Add($"SKIP exists: {move.destination}");
                        skipped++;
                    }

                    continue;
                }

                string destFolder = System.IO.Path.GetDirectoryName(move.destination)?.Replace('\\', '/');
                if (!string.IsNullOrEmpty(destFolder))
                    CraftingEditorUtility.EnsureFolder(destFolder);

                string error = AssetDatabase.MoveAsset(move.source, move.destination);
                if (!string.IsNullOrEmpty(error))
                {
                    log.Add($"FAIL {move.source} -> {move.destination}: {error}");
                    skipped++;
                }
                else
                {
                    log.Add($"OK {move.source} -> {move.destination}");
                    moved++;
                }
            }

            for (int i = 0; i < DeleteIfEmptyFolders.Length; i++)
                TryDeleteEmptyFolder(DeleteIfEmptyFolders[i]);

            // Duplicate Plasma Fuel if already under World (source move may skip)
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            ProjectFolderColorizer.RefreshFolderColors();

            string summary = $"PrefabOrganizer complete. Moved {moved}, skipped {skipped}.";
            Debug.Log(summary + "\n" + string.Join("\n", log));
        }

        private static bool AssetExists(string assetPath)
        {
            return !string.IsNullOrEmpty(assetPath) &&
                   (!string.IsNullOrEmpty(AssetDatabase.AssetPathToGUID(assetPath)) ||
                    System.IO.File.Exists(assetPath));
        }

        private static void TryDeleteEmptyFolder(string folderPath)
        {
            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] assets = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            if (assets.Length > 0)
                return;

            AssetDatabase.DeleteAsset(folderPath);
        }
    }
}
