using System;
using Project.Data;
using UnityEngine;

namespace Project.Quests
{
    [Serializable]
    public class QuestRewardDefinition
    {
        public QuestRewardType type = QuestRewardType.Pi;
        public int amount = 1;
        public ItemData item;
        [Tooltip("Placeholder for future stat upgrade rewards.")]
        public string statUpgradeId;
    }
}
