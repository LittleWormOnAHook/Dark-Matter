using System;
using System.Collections.Generic;
using Project.Data;
using UnityEngine;

namespace Project.Vendor
{
    public sealed class DMVendorRuntimeState
    {
        public string VendorId;
        public int LastRestockDay = -1;
        public int CurrentAc;
        public readonly List<int> Stock = new List<int>();
        public readonly List<VendorHeldStack> Held = new List<VendorHeldStack>();

        public void EnsureListingCount(int count)
        {
            while (Stock.Count < count)
                Stock.Add(0);
            if (Stock.Count > count)
                Stock.RemoveRange(count, Stock.Count - count);
        }

        public void ResolveHeldItems()
        {
            for (int i = Held.Count - 1; i >= 0; i--)
            {
                VendorHeldStack stack = Held[i];
                if (stack == null || stack.amount <= 0 || string.IsNullOrEmpty(stack.itemId))
                {
                    Held.RemoveAt(i);
                    continue;
                }

                if (stack.item == null)
                    stack.item = ItemRegistry.Resolve(stack.itemId);
                if (stack.item == null)
                    Held.RemoveAt(i);
            }
        }

        public int GetStock(int listingIndex)
        {
            if (listingIndex < 0 || listingIndex >= Stock.Count)
                return 0;
            return Stock[listingIndex];
        }

        public int GetHeldCount(ItemData item)
        {
            if (item == null)
                return 0;

            int total = 0;
            for (int i = 0; i < Held.Count; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack != null && stack.amount > 0 && Matches(stack, item))
                    total += stack.amount;
            }

            return total;
        }

        public int GetDisplayStock(int listingIndex, ItemData item)
        {
            return GetStock(listingIndex);
        }

        public void SetStock(int listingIndex, int amount)
        {
            if (listingIndex < 0)
                return;

            while (Stock.Count <= listingIndex)
                Stock.Add(0);
            Stock[listingIndex] = Mathf.Max(0, amount);
        }

        public void AddHeld(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            ResolveHeldItems();
            for (int i = 0; i < Held.Count; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack == null || !Matches(stack, item))
                    continue;
                stack.amount += amount;
                stack.item = item;
                return;
            }

            Held.Add(new VendorHeldStack
            {
                itemId = ItemKey(item),
                amount = amount,
                item = item
            });
        }

        public int TakeHeld(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return 0;

            ResolveHeldItems();
            int remaining = amount;
            for (int i = 0; i < Held.Count && remaining > 0; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack == null || stack.amount <= 0 || !Matches(stack, item))
                    continue;

                int take = Mathf.Min(remaining, stack.amount);
                stack.amount -= take;
                remaining -= take;
            }

            for (int i = Held.Count - 1; i >= 0; i--)
            {
                if (Held[i] == null || Held[i].amount <= 0)
                    Held.RemoveAt(i);
            }

            return amount - remaining;
        }

        public int TakeSale(int listingIndex, ItemData item, int amount)
        {
            int daily = GetStock(listingIndex);
            int fromDaily = Mathf.Min(Mathf.Max(0, amount), daily);
            if (fromDaily > 0)
                SetStock(listingIndex, daily - fromDaily);
            return fromDaily;
        }

        public int CountExtraOffers(DMVendorCatalog catalog)
        {
            ResolveHeldItems();
            int count = 0;
            for (int i = 0; i < Held.Count; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack != null && stack.amount > 0 && stack.item != null && !CatalogContains(catalog, stack.item))
                    count++;
            }

            return count;
        }

        public void CollectExtraOffers(DMVendorCatalog catalog, List<VendorHeldStack> dest)
        {
            if (dest == null)
                return;

            dest.Clear();
            ResolveHeldItems();
            for (int i = 0; i < Held.Count; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack != null && stack.amount > 0 && stack.item != null && !CatalogContains(catalog, stack.item))
                    dest.Add(stack);
            }
        }

        public ItemData GetExtraOfferItem(DMVendorCatalog catalog, int extraIndex)
        {
            return GetExtraOffer(catalog, extraIndex, out _);
        }

        public int GetExtraOfferStock(DMVendorCatalog catalog, int extraIndex)
        {
            GetExtraOffer(catalog, extraIndex, out int amount);
            return amount;
        }

        public int TakeExtraOffer(DMVendorCatalog catalog, int extraIndex, int amount)
        {
            ItemData item = GetExtraOffer(catalog, extraIndex, out _);
            return TakeHeld(item, amount);
        }

        private ItemData GetExtraOffer(DMVendorCatalog catalog, int extraIndex, out int amount)
        {
            amount = 0;
            if (extraIndex < 0)
                return null;

            ResolveHeldItems();
            int seen = 0;
            for (int i = 0; i < Held.Count; i++)
            {
                VendorHeldStack stack = Held[i];
                if (stack == null || stack.amount <= 0 || stack.item == null || CatalogContains(catalog, stack.item))
                    continue;
                if (seen == extraIndex)
                {
                    amount = stack.amount;
                    return stack.item;
                }

                seen++;
            }

            return null;
        }

        private static bool CatalogContains(DMVendorCatalog catalog, ItemData item)
        {
            if (catalog == null || catalog.listings == null || item == null)
                return false;

            for (int i = 0; i < catalog.listings.Length; i++)
            {
                DMVendorListing listing = catalog.listings[i];
                if (listing == null || listing.armorPlaceholder || listing.item == null)
                    continue;
                if (SameItem(listing.item, item))
                    return true;
            }

            return false;
        }

        public static bool SameItem(ItemData a, ItemData b)
        {
            if (a == null || b == null)
                return false;
            if (a == b)
                return true;
            if (!string.IsNullOrEmpty(a.StableItemId)
                && !string.IsNullOrEmpty(b.StableItemId)
                && a.StableItemId == b.StableItemId)
                return true;
            return a.name == b.name || a.itemName == b.itemName;
        }

        private static string ItemKey(ItemData item)
        {
            if (item == null)
                return string.Empty;
            return !string.IsNullOrEmpty(item.StableItemId) ? item.StableItemId : item.name;
        }

        private static bool Matches(VendorHeldStack stack, ItemData item)
        {
            if (stack == null || item == null)
                return false;
            if (stack.item != null && SameItem(stack.item, item))
                return true;
            if (string.IsNullOrEmpty(stack.itemId))
                return false;
            return stack.itemId == ItemKey(item) || stack.itemId == item.name || stack.itemId == item.itemName;
        }
    }

    [Serializable]
    public class VendorHeldStack
    {
        public string itemId;
        public int amount;
        [NonSerialized] public ItemData item;
    }

    [Serializable]
    public class VendorRuntimeSave
    {
        public string vendorId;
        public int lastRestockDay;
        public int currentAc;
        public int[] stock;
        public VendorHeldStack[] held;
    }
}
