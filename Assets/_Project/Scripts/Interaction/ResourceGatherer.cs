using Project.Data;
using Project.Inventory;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    public class ResourceGatherer : MonoBehaviour
    {
        [Header("Gathering")]
        public float gatherRange = 3.5f;
        public LayerMask resourceLayer;

        [Header("Item Pickup")]
        public float pickupRange = 4f;
        public LayerMask itemLayer;

        private UIManager uiManager;

        private void Awake()
        {
            uiManager = FindAnyObjectByType<UIManager>();
            EnsureInteractionLayers();
        }

        private void EnsureInteractionLayers()
        {
            int defaultLayer = LayerMask.GetMask("Default");
            int resource = LayerMask.GetMask("Resource");
            int item = LayerMask.GetMask("Item");

            if (resourceLayer.value == 0)
                resourceLayer = defaultLayer | resource | item;
            else
                resourceLayer |= defaultLayer;

            if (itemLayer.value == 0)
                itemLayer = defaultLayer | item;
            else
                itemLayer |= defaultLayer;
        }

        public bool TryGather(ItemData item, int amount)
        {
            InventorySystem inventory = GetComponent<InventorySystem>();
            if (inventory == null || item == null || amount <= 0)
                return false;

            int added = inventory.AddItem(item, amount);
            if (added > 0)
            {
                QuestManager questManager = QuestManager.EnsureExists();
                questManager?.NotifyItemCollected(item, added);
                Project.Achievements.AchievementManager.EnsureExists()
                    ?.ReportProgress(Project.Achievements.AchievementTriggerType.CollectItem, item.name, added);
            }

            if (added >= amount)
                return true;

            if (added == 0)
                ShowInventoryFullFeedback();

            return false;
        }

        private void ShowInventoryFullFeedback()
        {
            if (uiManager != null)
                uiManager.ShowInteractionPrompt("Inventory is full!");
            else
                Debug.Log("Inventory is full!");
        }
    }
}
