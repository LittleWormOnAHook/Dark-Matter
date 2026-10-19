using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Maps <see cref="DMEnemyRangedAmmoChoice"/> to live <see cref="ItemData"/> ammo rows (ItemRegistry).
    /// <see cref="DMEnemyRangedAmmoChoice.UseGripDefault"/> is treated as Standard (Gunpowder) before lookup.
    /// </summary>
    public static class DMEnemyRangedAmmoCatalog
    {
        public static ItemData ResolveItem(DMEnemyRangedAmmoChoice choice)
        {
            // Unset / Use Grip Default = Standard for all grips (legacy pistol/rifle split is not applied here).
            if (choice == DMEnemyRangedAmmoChoice.UseGripDefault)
                choice = DMEnemyRangedAmmoChoice.Standard;

            if (TryGetVendorAmmo(choice, out string vendorItemName, out AmmoType vendorType))
            {
                ItemData vendorItem = FindAmmoItemByName(vendorItemName);
                if (vendorItem != null)
                    return vendorItem;
                return FindAmmoItem(vendorType);
            }

            if (!TryMapToAmmoType(choice, out AmmoType type))
                type = AmmoType.Gunpowder;

            return FindAmmoItem(type);
        }

        public static bool TryMapToAmmoType(DMEnemyRangedAmmoChoice choice, out AmmoType type)
        {
            type = default;
            switch (choice)
            {
                case DMEnemyRangedAmmoChoice.UseGripDefault:
                    return false;
                case DMEnemyRangedAmmoChoice.Standard:
                    type = AmmoType.Gunpowder;
                    return true;
                case DMEnemyRangedAmmoChoice.Plasma:
                    type = AmmoType.Plasma;
                    return true;
                case DMEnemyRangedAmmoChoice.Ion:
                    type = AmmoType.Ion;
                    return true;
                case DMEnemyRangedAmmoChoice.Ice:
                    type = AmmoType.Ice;
                    return true;
                case DMEnemyRangedAmmoChoice.Electricity:
                    type = AmmoType.Electricity;
                    return true;
                case DMEnemyRangedAmmoChoice.ResonanceStabilizer:
                    type = AmmoType.ResonanceStabilizer;
                    return true;
                case DMEnemyRangedAmmoChoice.Laser:
                    type = AmmoType.Laser;
                    return true;
                case DMEnemyRangedAmmoChoice.Fire:
                    type = AmmoType.Fire;
                    return true;
                case DMEnemyRangedAmmoChoice.Explosive:
                    type = AmmoType.Explosive;
                    return true;
                default:
                    return TryGetVendorAmmo(choice, out _, out type);
            }
        }

        /// <summary>
        /// Vendor ammo (FORGE3D Sci-Fi Effects, 2026-10-19): enemy choice -> ammo item name + the shared
        /// <see cref="AmmoType"/> it uses for weapon compatibility. Profiles live in Data/Items/Ammo/Vendor.
        /// </summary>
        public static bool TryGetVendorAmmo(DMEnemyRangedAmmoChoice choice, out string itemName, out AmmoType type)
        {
            switch (choice)
            {
                case DMEnemyRangedAmmoChoice.VendorLaserImpulse:
                    itemName = "Laser Impulse"; type = AmmoType.Laser; return true;
                case DMEnemyRangedAmmoChoice.VendorPlasmaGun:
                    itemName = "Plasma Gun"; type = AmmoType.Plasma; return true;
                case DMEnemyRangedAmmoChoice.VendorVulcan:
                    itemName = "Vulcan"; type = AmmoType.Gunpowder; return true;
                case DMEnemyRangedAmmoChoice.VendorSoloGun:
                    itemName = "Solo Gun"; type = AmmoType.Fire; return true;
                case DMEnemyRangedAmmoChoice.VendorSeekerBolt:
                    itemName = "Seeker Bolt"; type = AmmoType.Ion; return true;
                case DMEnemyRangedAmmoChoice.VendorShotGun:
                    itemName = "Shot Gun"; type = AmmoType.Gunpowder; return true;
                case DMEnemyRangedAmmoChoice.VendorSniper:
                    itemName = "Sniper"; type = AmmoType.Gunpowder; return true;
                case DMEnemyRangedAmmoChoice.VendorRailgun:
                    itemName = "Railgun"; type = AmmoType.Electricity; return true;
                case DMEnemyRangedAmmoChoice.VendorLightningGun:
                    itemName = "Lightning Gun"; type = AmmoType.Electricity; return true;
                case DMEnemyRangedAmmoChoice.VendorPlasmaBeamBlue:
                    itemName = "Plasma Beam Blue"; type = AmmoType.Plasma; return true;
                case DMEnemyRangedAmmoChoice.VendorPlasmaBeamHeavyRed:
                    itemName = "Plasma Beam Heavy Red"; type = AmmoType.Plasma; return true;
                case DMEnemyRangedAmmoChoice.VendorFlameRed:
                    itemName = "Flame Red"; type = AmmoType.Fire; return true;
                default:
                    itemName = null;
                    type = default;
                    return false;
            }
        }

        private static ItemData FindAmmoItemByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName))
                return null;

            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item != null && item.CountsAsAmmo
                    && string.Equals(item.itemName, itemName, System.StringComparison.Ordinal))
                    return item;
            }

            return null;
        }

        private static ItemData FindAmmoItem(AmmoType type)
        {
            ItemData[] items = ItemRegistry.GetAllItems();
            for (int i = 0; i < items.Length; i++)
            {
                ItemData item = items[i];
                if (item == null || !item.CountsAsAmmo || item.ammoType != type)
                    continue;

                return item;
            }

            return null;
        }
    }
}
