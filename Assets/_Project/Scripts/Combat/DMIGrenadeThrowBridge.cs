using Invector.Throw;
using Project.Data;
using Project.Inventory;
using Project.Progression;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Syncs Dark Matter inventory grenade stacks with the existing Invector throw manager.
    /// Inventory is the source of truth; G-throw gameplay stays on <see cref="vThrowManager"/>.
    /// Cook / LT fuse is handled by <see cref="DMIGrenadeCookController"/>.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMIGrenadeThrowBridge : MonoBehaviour
    {
        public const string DefaultThrowableName = "Fragment Grenade";

        [Header("Inventory")]
        [SerializeField] private ItemData grenadeItem;
        [SerializeField] private InventorySystem inventory;

        [Header("Throw Manager")]
        [SerializeField] private vThrowManager throwManager;
        [SerializeField] private string throwableName = DefaultThrowableName;
        [SerializeField] private int maxCarry = 6;

        [Header("Throwable Prefab Override")]
        [Tooltip("Optional DMI throwable prefab (with DMIGrenadeExplosive). Replaces the ThrowManager entry.")]
        [SerializeField] private vThrowableObject dmiThrowablePrefab;

        private bool _consumingThrow;
        private bool _syncing;

        public ItemData GrenadeItem => grenadeItem;
        public string ThrowableName => throwableName;

        private void Awake()
        {
            ResolveRefs();
            ApplyThrowablePrefabOverride();
        }

        private void OnEnable()
        {
            ResolveRefs();

            if (inventory != null)
                inventory.OnInventoryChanged += HandleInventoryChanged;

            if (throwManager != null)
            {
                throwManager.onStartThrowObject.AddListener(HandleThrowStarted);
                throwManager.onThrowObject.AddListener(RefreshThrowUi);
            }

            SyncThrowAmountFromInventory();
        }

        private void OnDisable()
        {
            if (inventory != null)
                inventory.OnInventoryChanged -= HandleInventoryChanged;

            if (throwManager != null)
            {
                throwManager.onStartThrowObject.RemoveListener(HandleThrowStarted);
                throwManager.onThrowObject.RemoveListener(RefreshThrowUi);
            }
        }

        private void Start()
        {
            // ThrowManager Start() resets handler instances after our Awake — re-apply and sync.
            ApplyThrowablePrefabOverride();
            SyncThrowAmountFromInventory();
        }

        public void Configure(ItemData item, vThrowableObject throwablePrefab = null)
        {
            grenadeItem = item;
            if (throwablePrefab != null)
                dmiThrowablePrefab = throwablePrefab;

            ApplyThrowablePrefabOverride();
            SyncThrowAmountFromInventory();
        }

        private void ResolveRefs()
        {
            if (inventory == null)
                inventory = GetComponent<InventorySystem>() ?? GetComponentInParent<InventorySystem>();

            if (throwManager == null)
                throwManager = GetComponentInChildren<vThrowManager>(true);

            if (grenadeItem == null)
                grenadeItem = ItemRegistry.Resolve("Frag Grenade") ?? ItemRegistry.Resolve("DM_Frag_Grenade");
        }

        private void ApplyThrowablePrefabOverride()
        {
            if (throwManager == null || dmiThrowablePrefab == null)
                return;

            var list = throwManager.Throwables;
            if (list == null)
                return;

            for (int i = 0; i < list.Count; i++)
            {
                vThrowManager.Throwable entry = list[i];
                if (entry == null)
                    continue;

                if (!string.Equals(entry.name, throwableName, System.StringComparison.OrdinalIgnoreCase))
                    continue;

                entry.throwable = dmiThrowablePrefab;
                entry.ResetThrowable();

                if (grenadeItem != null && grenadeItem.icon != null)
                    entry.sprite = grenadeItem.icon;

                entry.maxAmount = Mathf.Max(1, maxCarry);
                break;
            }
        }

        private void HandleInventoryChanged()
        {
            if (_consumingThrow)
                return;

            SyncThrowAmountFromInventory();
        }

        private void HandleThrowStarted()
        {
            if (inventory == null || grenadeItem == null)
                return;

            if (!LevelUnlockUtility.PassesUseGate(grenadeItem, showToast: true))
            {
                SyncThrowAmountFromInventory();
                return;
            }

            _consumingThrow = true;
            try
            {
                // ThrowManager already decremented its local amount; consume the inventory stack.
                if (inventory.CountItem(grenadeItem) > 0)
                {
                    inventory.RemoveItem(grenadeItem, 1);
                    grenadeItem.TryGrantConfiguredXp();
                }

                SyncThrowAmountFromInventory();
            }
            finally
            {
                _consumingThrow = false;
            }
        }

        private void SyncThrowAmountFromInventory()
        {
            if (_syncing || throwManager == null)
                return;

            _syncing = true;
            try
            {
                int count = 0;
                if (inventory != null && grenadeItem != null
                    && LevelUnlockUtility.PassesUseGate(grenadeItem, showToast: false))
                    count = Mathf.Clamp(inventory.CountItem(grenadeItem), 0, Mathf.Max(1, maxCarry));

                var list = throwManager.Throwables;
                if (list == null)
                    return;

                for (int i = 0; i < list.Count; i++)
                {
                    vThrowManager.Throwable entry = list[i];
                    if (entry == null)
                        continue;

                    if (!string.Equals(entry.name, throwableName, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    entry.maxAmount = Mathf.Max(1, maxCarry);
                    entry.amount = count;
                    break;
                }

                RefreshThrowUi();
            }
            finally
            {
                _syncing = false;
            }
        }

        private void RefreshThrowUi()
        {
            if (throwManager == null)
                return;

            vThrowUI ui = throwManager.GetComponentInChildren<vThrowUI>(true);
            if (ui != null)
                ui.UpdateCount(throwManager);
        }
    }
}
