using UnityEngine;
using Project.Core;
using Project.Survival;
using Project.Inventory;
using Project.UI;
using Project.Data;
using Project.Pioneers;

namespace Project.Managers
{
    public class SimpleGameManager : MonoBehaviour
    {
        public static SimpleGameManager Instance { get; private set; }

        [Header("Starting Items")]
        public ItemData[] startingItems;
        public int[] startingAmounts;

        private bool playerInitialized;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            if (startingItems != null)
                ItemRegistry.RegisterRuntimeItems(startingItems);

            PioneerRosterManager.EnsureExists();
        }

        private void Start()
        {
            Debug.Log("GameManager Initialized - Pi Pioneer Survival");
        }

        public void BeginNewGameSession(bool grantStartingItems = true)
        {
            if (playerInitialized)
                return;

            if (grantStartingItems)
                InitializePlayer();

            playerInitialized = true;
        }

        private void InitializePlayer()
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
            {
                Debug.LogError("Player not found in scene!");
                return;
            }

            InventorySystem inventory = player.GetComponent<InventorySystem>();
            if (inventory != null && startingItems != null)
            {
                for (int i = 0; i < startingItems.Length; i++)
                {
                    if (startingItems[i] != null)
                    {
                        int amount = (startingAmounts != null && startingAmounts.Length > i) ? startingAmounts[i] : 5;
                        inventory.AddItem(startingItems[i], amount);
                    }
                }
            }
        }

        public void AddAetherCredits(int amount, string source = "Unknown")
        {
            PioneerRosterManager roster = PioneerRosterManager.EnsureExists();
            if (roster != null)
            {
                roster.AddAetherCredits(amount, source);
                return;
            }

            UIManager ui = FindAnyObjectByType<UIManager>();
            ui?.ShowAcReward(amount, source);
        }

        public void AddPi(int amount, string source = "Unknown")
        {
            AddAetherCredits(amount, source);
        }

        public void SaveGame(int slotIndex = 0)
        {
            if (GameSaveSystem.TrySave(slotIndex, out string message))
                Debug.Log(message);
            else
                Debug.LogWarning(message);
        }

        public void LoadGame(int slotIndex = 0)
        {
            if (GameSaveSystem.TryLoad(slotIndex, out string message))
                Debug.Log(message);
            else
                Debug.LogWarning(message);
        }
    }
}
