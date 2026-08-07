using Project.Combat;
using Project.Data;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Zeros unused serialized noise on <see cref="ItemData"/> by inspector category.
    /// Does not delete C# fields and never changes <see cref="ItemData.StableItemId"/>.
    /// Skips <see cref="MineHarvestItemData"/> (use its own prune).
    /// </summary>
    public static class ItemDataPruneUtility
    {
        public static void Prune(ItemData item)
        {
            if (item == null || item is MineHarvestItemData)
                return;

            ItemDataInspectorCategory category = ItemDataInspectorCategoryResolver.Resolve(item);
            Prune(item, category);
        }

        public static void Prune(ItemData item, ItemDataInspectorCategory category)
        {
            if (item == null || item is MineHarvestItemData)
                return;

            switch (category)
            {
                case ItemDataInspectorCategory.ThrowableConsumable:
                    ClearEquipment(item);
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    break;

                case ItemDataInspectorCategory.HealConsumable:
                case ItemDataInspectorCategory.GenericConsumable:
                    ClearEquipment(item);
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearTools(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    if (category == ItemDataInspectorCategory.GenericConsumable)
                        ClearRestores(item);
                    break;

                case ItemDataInspectorCategory.Ammo:
                    ClearEquipment(item);
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearMining(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    // Keep ranged damage/projectile override fields + ammo + VFX + elemental.
                    ClearWeaponMagazineAuthoring(item);
                    break;

                case ItemDataInspectorCategory.RangedWeapon:
                    ClearMelee(item);
                    ClearAmmoPickup(item);
                    ClearMining(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    break;

                case ItemDataInspectorCategory.MiningTool:
                    ClearMelee(item);
                    ClearAmmoPickup(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    ClearElemental(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    break;

                case ItemDataInspectorCategory.MeleeWeapon:
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    break;

                case ItemDataInspectorCategory.OpticsTool:
                case ItemDataInspectorCategory.GenericTool:
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    if (category == ItemDataInspectorCategory.GenericTool)
                    {
                        // Keep toolType / ranges; clear optics-only FOV if not optics.
                        if (!item.IsOpticsTool)
                            ClearOpticsFov(item);
                    }
                    break;

                case ItemDataInspectorCategory.Resource:
                case ItemDataInspectorCategory.Component:
                case ItemDataInspectorCategory.Module:
                case ItemDataInspectorCategory.Operations:
                case ItemDataInspectorCategory.Quest:
                    ClearEquipment(item);
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearTools(item);
                    ClearRestores(item);
                    ClearVehicle(item);
                    if (category != ItemDataInspectorCategory.Component)
                        item.componentCategory = ComponentCategory.None;
                    if (category != ItemDataInspectorCategory.Module)
                        item.unlocksInventoryStorageRow = false;
                    break;

                case ItemDataInspectorCategory.Vehicle:
                    ClearEquipment(item);
                    ClearInvector(item);
                    ClearMelee(item);
                    ClearRanged(item);
                    ClearAmmo(item);
                    ClearProjectile(item);
                    ClearMining(item);
                    ClearProjectileAudio(item);
                    ClearElemental(item);
                    ClearTools(item);
                    ClearRestores(item);
                    item.componentCategory = ComponentCategory.None;
                    item.unlocksInventoryStorageRow = false;
                    break;
            }

            if (item.maxStack < 1)
                item.maxStack = 1;
        }

        public static int PruneAllProjectItems(bool saveAssets = true)
        {
            string[] guids = AssetDatabase.FindAssets("t:ItemData", new[] { ProjectAssetPaths.ItemsData });
            int pruned = 0;

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path))
                    continue;
                if (path.IndexOf("/Nodes/", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    continue;

                ItemData item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null || item is MineHarvestItemData)
                    continue;

                Undo.RecordObject(item, "Prune ItemData");
                Prune(item);
                EditorUtility.SetDirty(item);
                pruned++;
            }

            if (saveAssets && pruned > 0)
                AssetDatabase.SaveAssets();

            return pruned;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Prune Unused ItemData Fields", false, 50)]
        public static void MenuPruneAll()
        {
            int count = PruneAllProjectItems();
            EditorUtility.DisplayDialog(
                "Prune ItemData",
                count > 0
                    ? $"Pruned unused serialized fields on {count} ItemData asset(s).\nMineHarvest + Nodes skipped."
                    : "No ItemData assets needed pruning (or none found).",
                "OK");
        }

        private static void ClearEquipment(ItemData item)
        {
            item.weaponGrip = WeaponGrip.OneHanded;
            item.heldPrefab = null;
            item.equipSocketName = "RightHand";
            item.heldLocalPosition = Vector3.zero;
            item.heldLocalEuler = Vector3.zero;
            item.useHeldLocalRotation = false;
            item.heldLocalRotation = Quaternion.identity;
            item.heldLocalScale = Vector3.one;
            item.swingEulerAngles = new Vector3(-120f, 0f, 0f);
            item.sheatheSocketName = "Spine";
            item.sheathedLocalPosition = new Vector3(0.02f, 0.18f, -0.22f);
            item.sheathedLocalEuler = new Vector3(75f, 90f, 90f);
            item.useSheathedLocalRotation = false;
            item.sheathedLocalRotation = Quaternion.identity;
            item.sheathedLocalScale = Vector3.one;
        }

        private static void ClearInvector(ItemData item)
        {
            item.invectorWeaponPrefab = null;
            item.invectorWeaponId = string.Empty;
        }

        private static void ClearMelee(ItemData item)
        {
            item.meleeDamage = 0f;
            item.meleeDamageRandomRange = 0f;
            item.criticalChance = 0f;
            item.criticalDamageMultiplier = 0f;
            item.meleeRange = 0f;
            item.meleeCooldown = 0f;
            item.attackAnimationSpeed = 0f;
            item.meleeStaminaCost = 0f;
            item.meleeKnockback = 0f;
            item.gatherPower = 0;
            item.attackTrigger = "Attack";
        }

        private static void ClearRanged(ItemData item)
        {
            item.rangedDamage = 0f;
            item.rangedDamageRandomRange = 0f;
            item.rangedRange = 0f;
            item.projectileSpeed = 0f;
            item.projectileSpreadDegrees = 0f;
            item.weaponAccuracy = 0f;
            item.closeRangeFullAccuracyDistance = 0f;
            item.closeRangeSpreadScale = 0f;
            item.recoilVertical = 0f;
            item.recoilHorizontal = 0f;
            item.recoilFireRateScale = 0f;
            item.fireRate = 0f;
            item.magazineSize = 0;
            item.reloadTimeSeconds = 0f;
            item.defaultAmmoType = AmmoType.Gunpowder;
            item.compatibleAmmoTypes = System.Array.Empty<AmmoType>();
            item.defaultAmmoItem = null;
            item.grantRandomStartingAmmo = false;
            item.startingAmmoMin = 0;
            item.startingAmmoMax = 0;
            item.projectilePrefab = null;
            item.muzzleSocketName = "Muzzle";
            item.aimFovMultiplier = 0f;
            item.hipFireMaxDeviationDegrees = 0f;
            item.hipFireSpreadMultiplier = 0f;
            item.useAimHeldGrip = false;
            item.aimHeldLocalPosition = Vector3.zero;
            item.aimHeldLocalEuler = Vector3.zero;
            item.useAimHeldLocalRotation = false;
            item.aimHeldLocalRotation = Quaternion.identity;
            item.aimHeldLocalScale = Vector3.one;
        }

        private static void ClearWeaponMagazineAuthoring(ItemData item)
        {
            // Ammo items may override damage/accuracy/speed; clear magazine/reload/ADS authoring.
            item.magazineSize = 0;
            item.reloadTimeSeconds = 0f;
            item.defaultAmmoItem = null;
            item.compatibleAmmoTypes = System.Array.Empty<AmmoType>();
            item.grantRandomStartingAmmo = false;
            item.startingAmmoMin = 0;
            item.startingAmmoMax = 0;
            item.useAimHeldGrip = false;
            item.aimHeldLocalPosition = Vector3.zero;
            item.aimHeldLocalEuler = Vector3.zero;
            item.useAimHeldLocalRotation = false;
            item.aimHeldLocalRotation = Quaternion.identity;
            item.aimHeldLocalScale = Vector3.one;
            item.recoilVertical = 0f;
            item.recoilHorizontal = 0f;
            item.recoilFireRateScale = 0f;
            item.fireRate = 0f;
            item.hipFireMaxDeviationDegrees = 0f;
            item.hipFireSpreadMultiplier = 0f;
            item.aimFovMultiplier = 0f;
            item.muzzleSocketName = "Muzzle";
        }

        private static void ClearAmmo(ItemData item)
        {
            item.ammoType = AmmoType.Gunpowder;
            item.ammoPerPickup = 0;
            item.ammoPickupGrant = 0f;
        }

        private static void ClearAmmoPickup(ItemData item)
        {
            item.ammoPerPickup = 0;
            item.ammoPickupGrant = 0f;
            // Keep ammoType / defaultAmmoType on ranged weapons.
        }

        private static void ClearProjectile(ItemData item)
        {
            item.isHitscanBeam = false;
            item.projectileGravityScale = 0f;
            item.splashRadius = 0f;
            item.splashDamageFalloff = 0f;
            item.muzzleFlashPrefab = null;
            item.tracerPrefab = null;
            item.impactVfxPrefab = null;
            item.beamVfxPrefab = null;
            item.projectilePrefab = null;
        }

        private static void ClearMining(ItemData item)
        {
            item.isMiningTool = false;
            item.miningPassesRequired = 0;
            item.miningDropMin = 0;
            item.miningDropMax = 0;
            item.miningLockBreakDegrees = 0f;
            item.miningPassDuration = 0f;
            item.miningChunkVfxPrefab = null;
            item.miningChargeDrainPerSecond = 0f;
            item.miningChargePerPlasmaFuel = 0f;
            item.miningScanLoopSound = null;
            item.miningScanSuccessSound = null;
            item.miningScanDeniedSound = null;
        }

        private static void ClearProjectileAudio(ItemData item)
        {
            item.fireSound = null;
            item.projectileTravelSound = null;
            item.isContinuousLaser = false;
            item.continuousLoopSound = null;
            item.continuousStartSound = null;
            item.continuousStopSound = null;
        }

        private static void ClearElemental(ItemData item)
        {
            item.statusEffectOverride = StatusEffectType.None;
            item.statusEffectDamagePerTick = 0f;
            item.statusEffectTickInterval = 0f;
            item.statusEffectDuration = 0f;
            item.statusEffectVfxPrefab = null;
        }

        private static void ClearTools(ItemData item)
        {
            item.toolType = ToolType.None;
            item.toolRange = 0f;
            item.scanRange = 0f;
            ClearOpticsFov(item);
        }

        private static void ClearOpticsFov(ItemData item)
        {
            item.opticsZoomFov = 0f;
            item.opticsMinZoomFov = 0f;
            item.opticsMaxZoomFov = 0f;
        }

        private static void ClearRestores(ItemData item)
        {
            item.healthRestore = 0f;
            item.energyRestore = 0f;
            item.staminaRestore = 0f;
            item.oxygenRestore = 0f;
        }

        private static void ClearVehicle(ItemData item)
        {
            item.deployedPrefab = null;
        }
    }
}
