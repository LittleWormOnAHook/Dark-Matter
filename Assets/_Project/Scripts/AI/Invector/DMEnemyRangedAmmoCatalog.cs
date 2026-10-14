using Project.Combat;
using Project.Data;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Maps <see cref="DMEnemyRangedAmmoChoice"/> to live <see cref="ItemData"/> ammo rows (ItemRegistry).
    /// </summary>
    public static class DMEnemyRangedAmmoCatalog
    {
        public static ItemData ResolveItem(DMEnemyRangedAmmoChoice choice)
        {
            if (choice == DMEnemyRangedAmmoChoice.UseGripDefault)
                return null;

            AmmoType type = choice switch
            {
                DMEnemyRangedAmmoChoice.Standard => AmmoType.Gunpowder,
                DMEnemyRangedAmmoChoice.Plasma => AmmoType.Plasma,
                DMEnemyRangedAmmoChoice.Ion => AmmoType.Ion,
                _ => AmmoType.Gunpowder
            };

            return FindAmmoItem(type);
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
