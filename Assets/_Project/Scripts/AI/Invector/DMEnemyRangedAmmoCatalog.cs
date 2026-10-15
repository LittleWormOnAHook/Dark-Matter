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
                    return false;
            }
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
