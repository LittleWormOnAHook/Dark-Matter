using Project.Data;
using UnityEngine;

namespace Project.Vendor
{
    public static class DMVendorPricing
    {
        public const float DefaultBuyMarkup = 1.25f;
        public const float DefaultSellRate = 0.5f;

        public static float RarityMultiplier(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Uncommon: return 1.25f;
                case ItemRarity.Rare: return 1.6f;
                default: return 1f;
            }
        }

        public static float LevelMultiplier(int tradeLevel)
        {
            int gate = Mathf.Max(1, tradeLevel);
            return 1f + 0.1f * Mathf.Max(0, gate - 1);
        }

        public static int InferDefaultAc(ItemData item)
        {
            return item != null ? item.InferDefaultAcValue() : 0;
        }

        public static int ResolveUnit(ItemData item)
        {
            if (item == null)
                return 0;

            int authored = item.ResolveTradeBaseAc();
            if (authored <= 0)
                return 0;

            float value = authored * RarityMultiplier(item.rarity) * LevelMultiplier(item.ResolveTradeLevel());
            return Mathf.Max(1, Mathf.RoundToInt(value));
        }

        public static int ResolveBuyPrice(ItemData item, float markup, int quantity = 1)
        {
            int unit = ResolveUnit(item);
            if (unit <= 0 || quantity <= 0)
                return 0;
            float buyMarkup = markup > 0f ? markup : DefaultBuyMarkup;
            return Mathf.Max(1, Mathf.CeilToInt(unit * buyMarkup)) * quantity;
        }

        public static int ResolveSellPrice(ItemData item, float sellRate, int quantity = 1)
        {
            int unit = ResolveUnit(item);
            if (unit <= 0 || quantity <= 0)
                return 0;
            float rate = sellRate > 0f ? sellRate : DefaultSellRate;
            return Mathf.Max(1, Mathf.FloorToInt(unit * rate)) * quantity;
        }
    }
}
