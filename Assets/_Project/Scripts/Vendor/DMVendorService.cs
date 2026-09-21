using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using UnityEngine;

namespace Project.Vendor
{
    public static class DMVendorService
    {
        public const string NotEnoughCreditMessage = "Not Enough Credit";
        public const int DefaultShopSlots = 42;

        public static bool AcceptsClass(DMVendorKind kind, DmVendorTradeClass tradeClass)
        {
            if (kind == DMVendorKind.Commissary)
                return tradeClass == DmVendorTradeClass.Commissary;
            return tradeClass == DmVendorTradeClass.TechGear || tradeClass == DmVendorTradeClass.TechUpgrade;
        }

        public static bool CanPlayerSellItem(
            DMVendorProfile profile,
            ItemData item,
            InventorySystem inventory,
            int ownedCount = -1)
        {
            if (profile == null || item == null || !item.ResolveCanSell())
                return false;
            if (!AcceptsClass(profile.kind, item.ResolveVendorClass())
                && FindCatalogIndex(profile, item) < 0)
                return false;
            if (item.cannotSellLastCopy)
            {
                int owned = ownedCount >= 0 ? ownedCount : CountOwned(inventory, item);
                if (owned <= 1)
                    return false;
            }

            return true;
        }

        public static int MaxAffordableSellQty(DMVendorProfile profile, ItemData item, int stackAmount)
        {
            if (profile == null || item == null || stackAmount <= 0)
                return 0;

            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            if (state == null || state.CurrentAc <= 0)
                return 0;

            int unit = ResolveListingSell(profile, item);
            if (unit <= 0)
                return 0;

            int byPurse = state.CurrentAc / unit;
            return Mathf.Clamp(byPurse, 0, stackAmount);
        }

        public static int MaxAffordableBuyQty(
            DMVendorProfile profile,
            DMVendorListing listing,
            ItemData item,
            int stock)
        {
            if (item == null || stock <= 0)
                return 0;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            int credits = roster != null ? Mathf.Max(0, Mathf.RoundToInt(roster.AetherCredits)) : 0;
            if (credits <= 0)
                return 0;

            int unit = ResolveListingBuy(profile, listing, item, 1);
            if (unit <= 0)
                return stock;

            return Mathf.Clamp(credits / unit, 0, stock);
        }

        public static int ResolveListingBuy(DMVendorProfile profile, DMVendorListing listing, ItemData item, int qty)
        {
            if (listing != null && listing.buyPriceOverride > 0)
                return listing.buyPriceOverride * Mathf.Max(1, qty);
            float markup = profile != null ? profile.buyMarkup : DMVendorPricing.DefaultBuyMarkup;
            return DMVendorPricing.ResolveBuyPrice(item, markup, qty);
        }

        public static int ResolveListingSell(DMVendorProfile profile, ItemData item, int qty = 1)
        {
            float rate = profile != null ? profile.sellRate : DMVendorPricing.DefaultSellRate;
            return DMVendorPricing.ResolveSellPrice(item, rate, qty);
        }

        public static bool TryBuy(
            DMVendorProfile profile,
            int listingIndex,
            int quantity,
            InventorySystem inventory,
            out string message)
        {
            message = string.Empty;
            if (profile == null || inventory == null || quantity <= 0)
            {
                message = "Cannot buy.";
                return false;
            }

            int extraIndex;
            ItemData item = ResolveOffer(profile, listingIndex, out DMVendorListing listing, out extraIndex);
            if (item == null || (listing != null && listing.armorPlaceholder))
            {
                message = "Not for sale.";
                return false;
            }

            if (!item.ResolveCanBuy() || !AcceptsClass(profile.kind, item.ResolveVendorClass()))
            {
                message = "This vendor does not sell that.";
                return false;
            }

            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            if (state == null)
            {
                message = "Cannot buy.";
                return false;
            }

            int stock = GetOfferStock(profile, listingIndex);
            if (stock <= 0)
            {
                message = "Sold out today.";
                return false;
            }

            int qty = Mathf.Min(quantity, stock);
            int cost = ResolveListingBuy(profile, listing, item, qty);
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            int credits = roster != null ? Mathf.Max(0, Mathf.RoundToInt(roster.AetherCredits)) : 0;
            if (roster == null || credits < cost || !roster.TrySpendAetherCredits(cost))
            {
                message = NotEnoughCreditMessage;
                return false;
            }

            int added = inventory.AddItem(item, qty);
            if (added < qty)
            {
                if (added > 0)
                    inventory.RemoveItem(item, added);
                roster.AddAetherCredits(cost, "VendorRefund");
                message = "Inventory is full.";
                return false;
            }

            if (extraIndex >= 0)
                state.TakeExtraOffer(profile.catalog, extraIndex, qty);
            else
                state.TakeSale(listingIndex, item, qty);

            state.CurrentAc += cost;
            DMVendorRuntime.NotifyChanged();
            message = "Purchased " + item.itemName + ".";
            return true;
        }

        public static bool TrySell(
            DMVendorProfile profile,
            ItemData item,
            int quantity,
            InventorySystem inventory,
            out string message)
        {
            return TrySellFromSlot(profile, FindFirstSlot(inventory, item), quantity, inventory, out message);
        }

        public static bool TrySellFromSlot(
            DMVendorProfile profile,
            int slotIndex,
            int quantity,
            InventorySystem inventory,
            out string message)
        {
            message = string.Empty;
            if (profile == null || inventory == null || quantity <= 0
                || slotIndex < 0 || slotIndex >= inventory.slots.Count)
            {
                message = "Cannot sell.";
                return false;
            }

            InventorySystem.InventorySlot slot = inventory.slots[slotIndex];
            ItemData item = slot != null && !slot.IsEmpty ? slot.item : null;
            if (item == null)
            {
                message = "Cannot sell.";
                return false;
            }

            if (!CanPlayerSellItem(profile, item, inventory))
            {
                message = "Cannot sell that here.";
                return false;
            }

            int qty = Mathf.Min(quantity, slot.amount);
            if (qty <= 0)
            {
                message = "Cannot sell.";
                return false;
            }

            int affordable = MaxAffordableSellQty(profile, item, qty);
            if (affordable < qty)
            {
                message = NotEnoughCreditMessage;
                return false;
            }

            int paid = ResolveListingSell(profile, item, qty);
            if (!inventory.RemoveItemAt(slotIndex, qty))
            {
                message = "Could not remove item.";
                return false;
            }

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            roster?.AddAetherCredits(paid, "VendorSell");

            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            if (state != null)
            {
                state.CurrentAc = Mathf.Max(0, state.CurrentAc - paid);
                int listingIndex = FindCatalogIndex(profile, item);
                if (listingIndex >= 0)
                    state.SetStock(listingIndex, state.GetStock(listingIndex) + qty);
                else
                    state.AddHeld(item, qty);
            }

            DMVendorRuntime.NotifyChanged();
            message = "Sold " + item.itemName + ".";
            return true;
        }

        public static int ShopSlotCount(DMVendorProfile profile)
        {
            int slots = profile != null ? profile.shopSlots : 0;
            return slots > 0 ? slots : DefaultShopSlots;
        }

        public static int FindCatalogIndex(DMVendorProfile profile, ItemData item)
        {
            if (profile == null || profile.catalog == null || profile.catalog.listings == null || item == null)
                return -1;

            DMVendorListing[] listings = profile.catalog.listings;
            for (int i = 0; i < listings.Length; i++)
            {
                DMVendorListing listing = listings[i];
                if (listing == null || listing.armorPlaceholder || listing.item == null)
                    continue;
                if (DMVendorRuntimeState.SameItem(listing.item, item))
                    return i;
            }

            return -1;
        }

        public static int GetOfferCount(DMVendorProfile profile)
        {
            int catalog = CatalogCount(profile);
            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            int extra = state != null ? state.CountExtraOffers(profile != null ? profile.catalog : null) : 0;
            return catalog + extra;
        }

        public static int GetOfferStock(DMVendorProfile profile, int offerIndex)
        {
            ItemData item = ResolveOffer(profile, offerIndex, out _, out int extraIndex);
            if (item == null)
                return 0;

            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            if (state == null)
                return 0;

            if (extraIndex >= 0)
                return state.GetExtraOfferStock(profile.catalog, extraIndex);

            return state.GetDisplayStock(offerIndex, item);
        }

        public static ItemData ResolveOffer(
            DMVendorProfile profile,
            int offerIndex,
            out DMVendorListing listing,
            out int extraIndex)
        {
            listing = null;
            extraIndex = -1;
            int catalog = CatalogCount(profile);
            if (offerIndex < catalog)
            {
                listing = GetListing(profile, offerIndex);
                return listing != null ? listing.item : null;
            }

            extraIndex = offerIndex - catalog;
            DMVendorRuntimeState state = DMVendorRuntime.GetOrCreate(profile);
            return state != null ? state.GetExtraOfferItem(profile != null ? profile.catalog : null, extraIndex) : null;
        }

        public static int CatalogCount(DMVendorProfile profile)
        {
            if (profile == null || profile.catalog == null || profile.catalog.listings == null)
                return 0;
            return profile.catalog.listings.Length;
        }

        public static int CountOwned(InventorySystem inventory, ItemData item)
        {
            if (item == null)
                return 0;
            int owned = inventory != null ? inventory.CountItem(item) : 0;
            owned += Project.Storage.DMStorageCrateRuntime.CountItem(item);
            return owned;
        }

        private static int FindFirstSlot(InventorySystem inventory, ItemData item)
        {
            if (inventory == null || item == null || inventory.slots == null)
                return -1;

            for (int i = 0; i < inventory.slots.Count; i++)
            {
                InventorySystem.InventorySlot slot = inventory.slots[i];
                if (slot != null && !slot.IsEmpty && slot.item == item)
                    return i;
            }

            return -1;
        }

        private static DMVendorListing GetListing(DMVendorProfile profile, int listingIndex)
        {
            if (profile == null || profile.catalog == null || profile.catalog.listings == null)
                return null;
            if (listingIndex < 0 || listingIndex >= profile.catalog.listings.Length)
                return null;
            return profile.catalog.listings[listingIndex];
        }
    }
}
