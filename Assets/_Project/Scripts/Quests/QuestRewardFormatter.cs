namespace Project.Quests
{
    /// <summary>
    /// Display helpers for quest/loot rewards. QuestRewardType.Pi grants in-game Aether Credits (AC).
    /// </summary>
    public static class QuestRewardFormatter
    {
        public static string FormatShort(QuestRewardDefinition reward)
        {
            if (reward == null)
                return string.Empty;

            switch (reward.type)
            {
                case QuestRewardType.Pi:
                    return FormatAetherCredits(reward.amount);

                case QuestRewardType.Item:
                    if (reward.item != null)
                        return $"{reward.amount}x {reward.item.itemName}";
                    return $"{reward.amount}x Item";

                case QuestRewardType.StatUpgrade:
                    return string.IsNullOrWhiteSpace(reward.statUpgradeId)
                        ? "Stat upgrade"
                        : $"{reward.statUpgradeId} +{reward.amount}";

                default:
                    return reward.amount.ToString();
            }
        }

        public static string FormatLootLine(QuestRewardDefinition reward)
        {
            if (reward == null)
                return string.Empty;

            string label = FormatShort(reward);
            return string.IsNullOrEmpty(label) ? string.Empty : $"- {label}";
        }

        public static string FormatAetherCredits(int amount)
        {
            return amount == 1 ? "1 AC" : $"{amount} AC";
        }

        public static string FormatAetherCreditsLong(int amount)
        {
            return amount == 1 ? "1 Aether Credit" : $"{amount} Aether Credits";
        }
    }
}
