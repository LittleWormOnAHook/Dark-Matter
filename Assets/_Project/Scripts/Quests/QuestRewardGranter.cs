using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Pioneers;
using Project.Managers;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Quests
{
    public static class QuestRewardGranter
    {
        public static void GrantRewards(QuestDefinition quest)
        {
            if (quest == null || quest.rewards == null)
                return;

            foreach (QuestRewardDefinition reward in quest.rewards)
            {
                if (reward == null)
                    continue;

                GrantReward(reward, quest.title);
            }
        }

        /// <summary>
        /// Grants a reward. For items, returns how many were actually added to the player inventory
        /// (may be less than <see cref="QuestRewardDefinition.amount"/> when inventory is full).
        /// Non-item rewards return the requested amount on success, or 0 on failure.
        /// </summary>
        public static int GrantReward(QuestRewardDefinition reward, string source)
        {
            if (reward == null)
                return 0;

            switch (reward.type)
            {
                case QuestRewardType.Pi:
                    GrantAetherCredits(reward.amount, source);
                    return reward.amount > 0 ? reward.amount : 0;

                case QuestRewardType.Item:
                    return GrantItem(reward.item, reward.amount);

                case QuestRewardType.StatUpgrade:
                    ProgressionRewardGranter.GrantXp(
                        reward.amount > 0 ? reward.amount : 25,
                        XpSource.Quest,
                        $"quest-stat:{source}:{reward.statUpgradeId}");
                    return reward.amount > 0 ? reward.amount : 25;

                case QuestRewardType.Xp:
                    ProgressionRewardGranter.GrantXp(reward.amount, XpSource.Quest, $"quest-xp:{source}:{reward.amount}");
                    return reward.amount > 0 ? reward.amount : 0;

                default:
                    return 0;
            }
        }

        private static void GrantAetherCredits(int amount, string source)
        {
            if (amount <= 0)
                return;

            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                roster.AddAetherCredits(amount, source ?? "Quest");
                return;
            }

            UIManager ui = Object.FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                ui.ShowAcReward(amount, source ?? "Quest");
                return;
            }

            SimpleGameManager.Instance?.AddAetherCredits(amount, source ?? "Quest");
        }

        private static int GrantItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return 0;

            InventorySystem inventory = ResolvePlayerInventory();
            if (inventory == null)
            {
                Debug.LogWarning("QuestRewardGranter: No InventorySystem found to grant item reward.");
                return 0;
            }

            int added = inventory.AddItem(item, amount);
            if (added < amount)
                Debug.LogWarning($"QuestRewardGranter: Could only add {added}/{amount} of {item.itemName}.");

            return added;
        }

        private static InventorySystem ResolvePlayerInventory()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player != null)
            {
                InventorySystem onPlayer = player.GetComponent<InventorySystem>();
                if (onPlayer != null)
                    return onPlayer;
            }

            return Object.FindAnyObjectByType<InventorySystem>();
        }
    }
}
