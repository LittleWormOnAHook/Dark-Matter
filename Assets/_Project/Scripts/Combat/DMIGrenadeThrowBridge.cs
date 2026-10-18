using System.Reflection;
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
    /// Inventory is the source of truth; throw animation/physics stay on <see cref="vThrowManager"/>.
    /// Input: Hot Cross G/LB hold loads to hand; hold LMB/LT cook, release LMB/LT throw — <see cref="DMIGrenadeCookController"/>.
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

        [Header("Hand Socket")]
        [Tooltip("Right-hand socket for in-hand grenade mesh (Invector ThrowManager throwable handler).")]
        [SerializeField] private Transform grenadeHandHandlerOverride;
        [SerializeField] private string grenadeHandlerObjectName = "GrenadeHandler";

        private bool _consumingThrow;
        private bool _syncing;

        private static FieldInfo s_throwableInHandlerField;

        public ItemData GrenadeItem => grenadeItem;
        public string ThrowableName => throwableName;

        private bool _lastGrenadesEnabled = true;

        /// <summary>Master grenade switch (DM_CombatCoreProfile.enableGrenades, default off).</summary>
        public static bool GrenadesEnabled
        {
            get
            {
                DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
                return profile != null && profile.enableGrenades;
            }
        }

        private void Update()
        {
            // Re-apply when the flag flips (Genesis Studio Play tweak) or when the throw manager re-enabled itself in Start.
            bool enabledNow = GrenadesEnabled;
            if (!enabledNow && throwManager != null && throwManager.canUseThrow)
                throwManager.CanUseThrow(false);

            if (enabledNow == _lastGrenadesEnabled)
                return;

            _lastGrenadesEnabled = enabledNow;
            if (throwManager != null)
                throwManager.CanUseThrow(enabledNow);
            SyncThrowAmountFromInventory();
        }

        private void Awake()
        {
            ResolveRefs();
            ApplyThrowablePrefabOverride();
            EnsureFragmentGrenadeHandHandler();
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
            EnsureFragmentGrenadeHandHandler();
            SyncThrowAmountFromInventory();
        }

        public void Configure(ItemData item, vThrowableObject throwablePrefab = null)
        {
            grenadeItem = item;
            if (throwablePrefab != null)
                dmiThrowablePrefab = throwablePrefab;

            ApplyThrowablePrefabOverride();
            EnsureFragmentGrenadeHandHandler();
            SyncThrowAmountFromInventory();
        }

        /// <summary>Re-read stack count from the Hot Cross BR focus slot (after G tap cycle).</summary>
        public void SyncThrowAmountFromHotCrossFocus() => SyncThrowAmountFromInventory();

        /// <summary>Drop orphaned inactive in-hand clones when throw aim is cancelled.</summary>
        public void CleanupInactiveHandGrenade()
        {
            if (throwManager == null)
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

                DestroyInactiveHandlerInstance(entry);
                break;
            }
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
                DestroyInactiveHandlerInstance(entry);
                entry.ResetThrowable();

                if (grenadeItem != null && grenadeItem.icon != null)
                    entry.sprite = grenadeItem.icon;

                entry.maxAmount = Mathf.Max(1, maxCarry);
                break;
            }
        }

        private void EnsureFragmentGrenadeHandHandler()
        {
            if (throwManager == null)
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

                ApplyThrowableHandHandler(entry);
                break;
            }
        }

        /// <summary>
        /// Invector instantiates the in-hand throwable under <see cref="vThrowManager.Throwable.handler"/>.
        /// Player prefabs ship a <c>GrenadeHandler</c> under right-hand handlers but the nested ThrowManager
        /// entry often leaves handler null — mesh never parents to the hand.
        /// </summary>
        private void ApplyThrowableHandHandler(vThrowManager.Throwable entry)
        {
            if (entry == null)
                return;

            Transform handler = ResolveGrenadeHandHandler();
            if (handler == null)
            {
                DestroyInactiveHandlerInstance(entry);
                entry.ResetThrowable();
                return;
            }

            if (entry.handler == handler)
                return;

            entry.handler = handler;
            DestroyInactiveHandlerInstance(entry);
            entry.ResetThrowable();
        }

        /// <summary>
        /// Invector <see cref="vThrowManager.Throwable.ResetThrowable"/> only nulls the reference.
        /// Re-syncing prefab/handler leaves inactive clones under GrenadeHandler — destroy those only.
        /// </summary>
        private static void DestroyInactiveHandlerInstance(vThrowManager.Throwable entry)
        {
            if (entry == null)
                return;

            vThrowableObject existing = GetHandlerInstance(entry);
            if (existing == null)
                return;

            Transform t = existing.transform;
            if (t.parent == null)
                return;

            if (existing.selfRigidbody != null && !existing.selfRigidbody.isKinematic)
                return;

            Object.Destroy(existing.gameObject);
            entry.ResetThrowable();
        }

        private static vThrowableObject GetHandlerInstance(vThrowManager.Throwable entry)
        {
            if (entry == null)
                return null;

            s_throwableInHandlerField ??= typeof(vThrowManager.Throwable).GetField(
                "_throwableInHandler",
                BindingFlags.Instance | BindingFlags.NonPublic);

            return s_throwableInHandlerField?.GetValue(entry) as vThrowableObject;
        }

        private Transform ResolveGrenadeHandHandler()
        {
            if (grenadeHandHandlerOverride != null)
                return grenadeHandHandlerOverride;

            if (throwManager == null)
                return null;

            Transform root = throwManager.transform.root;
            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < transforms.Length; i++)
            {
                Transform candidate = transforms[i];
                if (candidate != null && candidate.name == grenadeHandlerObjectName)
                    return candidate;
            }

            return null;
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
                if (DMIGrenadeHotbarUtility.TryConsumeOneFromHotbar(
                        inventory, grenadeItem, DMUiToolkitHotCross.GrenadeLocalIndex))
                    grenadeItem.TryGrantConfiguredXp();

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
                if (GrenadesEnabled && inventory != null && grenadeItem != null
                    && LevelUnlockUtility.PassesUseGate(grenadeItem, showToast: false)
                    && DMIGrenadeHotbarUtility.TryGetHotbarStackAtLocal(
                        inventory,
                        DMUiToolkitHotCross.GrenadeLocalIndex,
                        grenadeItem,
                        out int hotbarAmount,
                        out _))
                    count = Mathf.Clamp(hotbarAmount, 0, Mathf.Max(1, maxCarry));

                var list = throwManager.Throwables;
                if (list == null)
                    return;

                int matchedIndex = -1;
                for (int i = 0; i < list.Count; i++)
                {
                    vThrowManager.Throwable entry = list[i];
                    if (entry == null)
                        continue;

                    if (!string.Equals(entry.name, throwableName, System.StringComparison.OrdinalIgnoreCase))
                        continue;

                    entry.maxAmount = Mathf.Max(1, maxCarry);
                    entry.amount = count;
                    matchedIndex = i;
                    break;
                }

                EnsureThrowableIndexSelected(matchedIndex);

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

        /// <summary>
        /// Invector tracks one "current" throwable index. Amount sync alone left index on a stale entry,
        /// so <see cref="vThrowManager.CurrentThrowAmount"/> stayed 0 and G-hold arm silently failed.
        /// </summary>
        private void EnsureThrowableIndexSelected(int matchedListIndex)
        {
            if (throwManager == null || matchedListIndex < 0)
                return;

            if (throwManager.indexOfCurrentThrowable == matchedListIndex)
                return;

            throwManager.SelectThrowable(matchedListIndex);
        }
    }
}
