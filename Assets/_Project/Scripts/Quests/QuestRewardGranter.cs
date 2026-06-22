using Project.Data;
using Project.Inventory;
using Project.Managers;
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

        public static void GrantReward(QuestRewardDefinition reward, string source)
        {
            if (reward == null)
                return;

            switch (reward.type)
            {
                case QuestRewardType.Pi:
                    GrantPi(reward.amount, source);
                    break;

                case QuestRewardType.Item:
                    GrantItem(reward.item, reward.amount);
                    break;

                case QuestRewardType.StatUpgrade:
                    Debug.Log($"[QuestReward] Stat upgrade placeholder: {reward.statUpgradeId} (+{reward.amount}) — not implemented yet.");
                    break;
            }
        }

        private static void GrantPi(int amount, string source)
        {
            if (amount <= 0)
                return;

            UIManager ui = Object.FindAnyObjectByType<UIManager>();
            if (ui != null)
            {
                ui.ShowPiReward(amount, source ?? "Quest");
                return;
            }

            SimpleGameManager.Instance?.AddPi(amount, source ?? "Quest");
        }

        private static void GrantItem(ItemData item, int amount)
        {
            if (item == null || amount <= 0)
                return;

            InventorySystem inventory = Object.FindAnyObjectByType<InventorySystem>();
            if (inventory == null)
            {
                Debug.LogWarning("QuestRewardGranter: No InventorySystem found to grant item reward.");
                return;
            }

            int added = inventory.AddItem(item, amount);
            if (added < amount)
                Debug.LogWarning($"QuestRewardGranter: Could only add {added}/{amount} of {item.itemName}.");
        }
    }
}
