using System;
using System.Collections;
using System.Collections.Generic;
using Invector;
using Invector.vCharacterController;
using Invector.vCharacterController.vActions;
using Invector.vMelee;
using Invector.vShooter;
using Project.Core;
using Project.Data;
using Project.Interaction;
using Project.Inventory;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Maps Pioneer hotbar/equipment to Invector shooter and melee weapon instances.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PioneerInvectorBootstrap))]
    [RequireComponent(typeof(EquipmentController))]
    public class PioneerInvectorWeaponBridge : MonoBehaviour
    {
        public const string PreloadedMeleeWeaponSlotsRootName = "PreloadedMeleeWeaponSlots";
        public const string PreloadedRangedWeaponSlotsRootName = "PreloadedRangedWeaponSlots";

        [Serializable]
        public sealed class MeleeWeaponSlot
        {
            public string slotId;
            public ItemData item;
            public GameObject drawnInstance;
            public GameObject holsteredInstance;
        }

        [Serializable]
        public sealed class RangedWeaponSlot
        {
            public string slotId;
            public ItemData item;
            public GameObject drawnInstance;
            public GameObject holsteredInstance;
        }

        [Header("Default Invector Prefabs")]
        [SerializeField] private GameObject defaultPistolPrefab;
        [SerializeField] private GameObject defaultRiflePrefab;
        [SerializeField] private GameObject defaultMeleeSwordPrefab;
        [SerializeField] private GameObject defaultMeleeTwoHandPrefab;

        [Header("Preloaded Melee Slots")]
        [SerializeField] private List<MeleeWeaponSlot> meleeWeaponSlots = new();

        [Header("Preloaded Ranged Slots")]
        [SerializeField] private List<RangedWeaponSlot> rangedWeaponSlots = new();

        private PioneerInvectorBootstrap _bootstrap;
        private EquipmentController _equipment;
        private InventorySystem _inventory;
        private vShooterManager _shooterManager;
        private vMeleeManager _meleeManager;
        private vCollectMeleeControl _collectControl;

        private readonly Dictionary<ItemData, GameObject> _spawnedInstances = new();
        private readonly Dictionary<ItemData, MeleeWeaponSlot> _meleeSlotLookup = new();
        private readonly Dictionary<ItemData, RangedWeaponSlot> _rangedSlotLookup = new();
        private readonly Dictionary<GameObject, AuthoredSlotTransform> _authoredSlotTransforms = new();
        private ItemData _activeItem;
        private bool _startupWeaponLayoutReady;

        private struct AuthoredSlotTransform
        {
            public Transform Parent;
            public Vector3 LocalPosition;
            public Quaternion LocalRotation;
            public Vector3 LocalScale;
        }

        public ItemData ActiveEquippedItem => _activeItem;

        public bool IsHolsterPreviewActive => _holsterPreviewActive;

        public GameObject TryGetWeaponInstance(ItemData item)
        {
            if (item == null)
                return null;

            if (item.itemType == ItemType.MeleeWeapon && TryGetMeleeSlot(item, out MeleeWeaponSlot slot))
                return slot.drawnInstance;

            if (item.IsRangedWeapon && TryGetRangedSlot(item, out RangedWeaponSlot rangedSlot))
                return rangedSlot.drawnInstance;

            return _spawnedInstances.TryGetValue(item, out GameObject instance) ? instance : null;
        }

        public GameObject TryGetHolsteredWeaponInstance(ItemData item)
        {
            if (item != null && item.itemType == ItemType.MeleeWeapon && TryGetMeleeSlot(item, out MeleeWeaponSlot slot))
                return slot.holsteredInstance;

            if (item != null && item.IsRangedWeapon && TryGetRangedSlot(item, out RangedWeaponSlot rangedSlot))
                return rangedSlot.holsteredInstance;

            return TryGetWeaponInstance(item);
        }

        public Transform FindHolsterSocket(ItemData item)
        {
            string socketName = ResolveHolsterSocketName(item);

            return FindChildTransformByName(transform, socketName);
        }

        public static string ResolveHolsterSocketName(ItemData item)
        {
            if (item == null)
                return "Spine2";

            if (!string.IsNullOrWhiteSpace(item.sheatheSocketName) &&
                !item.sheatheSocketName.Equals("Spine", StringComparison.OrdinalIgnoreCase))
            {
                return item.sheatheSocketName;
            }

            return ResolveDefaultMeleeHolsterSocketName(item);
        }

        public static string ResolveDefaultMeleeHolsterSocketName(ItemData item)
        {
            return item != null && item.itemType == ItemType.MeleeWeapon && !item.IsTwoHanded
                ? "RightUpLeg"
                : "Spine2";
        }

        public void BeginHolsterPreview(ItemData item)
        {
            if (item == null || !_bootstrap.IsActive)
                return;

            GameObject prefab = ResolveWeaponPrefab(item);
            if (prefab == null)
                return;

            _holsterPreviewActive = true;
            _holsterPreviewItem = item;
            HideAllSpawnedWeapons();
            ClearInvectorWeapons();

            if (item.itemType == ItemType.MeleeWeapon)
            {
                if (TryGetMeleeSlot(item, out MeleeWeaponSlot slot) && slot.holsteredInstance != null)
                    ShowMeleeHolsteredSlot(item, slot);
                else
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Holstered_ slot for '{item.name}'.");
                return;
            }

            if (item.IsRangedWeapon)
            {
                if (TryGetRangedSlot(item, out RangedWeaponSlot slot) && slot.holsteredInstance != null)
                {
                    ShowRangedHolsteredSlot(item, slot);
                }
                else
                {
                    HideRuntimeGeneratedWeaponsForItem(item);
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Holstered_ slot for '{item.name}'.");
                }
                return;
            }

            GameObject instance = GetOrCreateInstance(item, prefab);
            Transform socket = FindHolsterSocket(item);
            if (socket == null)
                return;

            instance.transform.SetParent(socket, false);
            ApplySheathedTransform(instance, item);
            StripEquippedWeaponPhysics(instance);
            instance.SetActive(true);
        }

        public void EndHolsterPreview()
        {
            if (!_holsterPreviewActive)
                return;

            _holsterPreviewActive = false;
            _holsterPreviewItem = null;
            RefreshEquippedWeapon();
        }

        public void ApplySheathedTransformToInstance(ItemData item, GameObject instance)
        {
            ApplySheathedTransform(instance, item);
        }

        private bool _holsterPreviewActive;
        private ItemData _holsterPreviewItem;

        private void Awake()
        {
            _bootstrap = GetComponent<PioneerInvectorBootstrap>();
            _equipment = GetComponent<EquipmentController>();
            _inventory = GetComponent<InventorySystem>();
            _shooterManager = GetComponent<vShooterManager>();
            _meleeManager = GetComponent<vMeleeManager>();
            _collectControl = GetComponent<vCollectMeleeControl>();
            BindPreloadedMeleeSlots();
            BindPreloadedRangedSlots();
        }

        public void RebindPreloadedMeleeSlots()
        {
            BindPreloadedMeleeSlots();
        }

        public void RebindPreloadedRangedSlots()
        {
            BindPreloadedRangedSlots();
        }

        public void PrepareForVehicleBoarding()
        {
            ForceAuthoredSlotCaptures();
            ClearInvectorWeapons();
            RestoreAuthoredWeaponSlotTransforms();
            HideAllSpawnedWeapons();
        }

        public void ScheduleRestoreAfterVehicleExit()
        {
            StopCoroutine(nameof(RestoreAfterVehicleExitRoutine));
            StartCoroutine(RestoreAfterVehicleExitRoutine());
        }

        private IEnumerator RestoreAfterVehicleExitRoutine()
        {
            yield return null;
            RestoreAfterVehicleExit();
        }

        public void RestoreAfterVehicleExit()
        {
            ClearInvectorWeapons();
            RefreshEquippedWeapon();
        }

        public void EnsureAuthoredSlotCaptures()
        {
            CaptureAuthoredSlots(meleeWeaponSlots);
            CaptureAuthoredSlots(rangedWeaponSlots);
        }

        private void ForceAuthoredSlotCaptures()
        {
            ForceCaptureAuthoredSlots(meleeWeaponSlots);
            ForceCaptureAuthoredSlots(rangedWeaponSlots);
        }

        private void OnEnable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged += HandleEquipmentChanged;
            if (_inventory != null)
                _inventory.OnInventoryChanged += HandleInventoryChanged;
            GameSession.GameStarted += HandleGameStarted;
        }

        private void OnDisable()
        {
            if (_equipment != null)
                _equipment.OnSelectedHotbarChanged -= HandleEquipmentChanged;
            if (_inventory != null)
                _inventory.OnInventoryChanged -= HandleInventoryChanged;
            GameSession.GameStarted -= HandleGameStarted;
        }

        private void Start()
        {
            StartCoroutine(FinalizeStartupWeaponLayoutRoutine());
        }

        private IEnumerator FinalizeStartupWeaponLayoutRoutine()
        {
            // vSnapToBody / vShooterManager Start run at default order 0, after this bridge's Start.
            // Refreshing equipped visuals before they run leaves weapons on unstaged handlers.
            yield return null;
            yield return null;
            FinalizeStartupWeaponLayout();
        }

        private void FinalizeStartupWeaponLayout()
        {
            if (_startupWeaponLayoutReady || !_bootstrap.IsActive)
                return;

            _startupWeaponLayoutReady = true;
            HideAllSpawnedWeapons();
            ClearInvectorWeapons();
            ForceAuthoredSlotCaptures();
            RefreshEquippedWeapon();
        }

        private void HandleEquipmentChanged(int _)
        {
            if (!_bootstrap.IsActive || !_startupWeaponLayoutReady)
                return;

            RefreshEquippedWeapon();
        }

        private void HandleInventoryChanged()
        {
            if (!_bootstrap.IsActive || !_startupWeaponLayoutReady)
                return;

            RefreshEquippedWeapon();
        }

        private void HandleGameStarted()
        {
            if (!_bootstrap.IsActive)
                return;

            if (!_startupWeaponLayoutReady)
                FinalizeStartupWeaponLayout();
            else
                RefreshEquippedWeapon();
        }

        public void RefreshEquippedWeapon()
        {
            if (_holsterPreviewActive && _holsterPreviewItem != null)
            {
                BeginHolsterPreview(_holsterPreviewItem);
                return;
            }

            HideAllSpawnedWeapons();

            if (_equipment == null || !_equipment.IsWeaponDrawn)
            {
                _activeItem = null;
                ClearInvectorWeapons();
                ShowHolsteredWeaponIfNeeded();
                return;
            }

            _holsterPreviewActive = false;
            _holsterPreviewItem = null;

            ItemData item = _equipment.DrawnWeaponItem;
            _activeItem = item;
            if (item == null)
            {
                ClearInvectorWeapons();
                return;
            }

            GameObject prefab = ResolveWeaponPrefab(item);
            if (prefab == null)
            {
                ClearInvectorWeapons();
                return;
            }

            if (item.itemType == ItemType.MeleeWeapon)
            {
                if (TryGetMeleeSlot(item, out MeleeWeaponSlot slot) && slot.drawnInstance != null)
                    ShowMeleeDrawnSlot(item, slot);
                else
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Drawn_ slot for '{item.name}'.");
                return;
            }

            if (item.IsRangedWeapon)
            {
                if (TryGetRangedSlot(item, out RangedWeaponSlot slot) && slot.drawnInstance != null)
                {
                    ShowRangedDrawnSlot(item, slot);
                }
                else
                {
                    HideRuntimeGeneratedWeaponsForItem(item);
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Drawn_ slot for '{item.name}'.");
                }
                return;
            }

            GameObject instance = GetOrCreateInstance(item, prefab);
            AttachWeaponToHandler(instance, item);
            instance.SetActive(true);

            if (item.IsRangedWeapon)
                EquipRanged(instance);
            else if (item.itemType == ItemType.MeleeWeapon)
                EquipMelee(instance);

            ApplyItemStats(item, instance);
            StripEquippedWeaponPhysics(instance);
        }

        private void ShowMeleeDrawnSlot(ItemData item, MeleeWeaponSlot slot)
        {
            if (item == null || slot == null || slot.drawnInstance == null)
                return;

            if (slot.holsteredInstance != null)
                slot.holsteredInstance.SetActive(false);

            ClearInvectorWeapons();
            GameObject instance = slot.drawnInstance;

            PrepareDrawnMeleeSlot(instance, item);
            instance.SetActive(true);
            EquipMelee(instance);
            ApplyItemStats(item, instance);
        }

        private void ShowMeleeHolsteredSlot(ItemData item, MeleeWeaponSlot slot)
        {
            if (item == null || slot == null || slot.holsteredInstance == null)
                return;

            if (slot.drawnInstance != null)
                slot.drawnInstance.SetActive(false);

            GameObject instance = slot.holsteredInstance;

            PrepareHolsteredVisualSlot(instance, item);
            instance.SetActive(true);
        }

        private void ShowRangedDrawnSlot(ItemData item, RangedWeaponSlot slot)
        {
            if (item == null || slot == null || slot.drawnInstance == null)
                return;

            if (slot.holsteredInstance != null)
                slot.holsteredInstance.SetActive(false);

            ClearInvectorWeapons();
            GameObject instance = slot.drawnInstance;

            PrepareDrawnRangedSlot(instance, item);
            instance.SetActive(true);
            EquipRanged(instance);
            ApplyItemStats(item, instance);
        }

        private void ShowRangedHolsteredSlot(ItemData item, RangedWeaponSlot slot)
        {
            if (item == null || slot == null || slot.holsteredInstance == null)
                return;

            if (slot.drawnInstance != null)
                slot.drawnInstance.SetActive(false);

            GameObject instance = slot.holsteredInstance;

            // Holstered ranged slots are visual-only; drawn slots keep the live vShooterWeapon.
            HideRuntimeGeneratedWeaponsForItem(item);
            PrepareHolsteredVisualSlot(instance, item);
            instance.SetActive(true);
        }

        private bool TryGetMeleeSlot(ItemData item, out MeleeWeaponSlot slot)
        {
            slot = null;
            if (item == null)
                return false;

            if (!_meleeSlotLookup.TryGetValue(item, out slot) || slot == null)
            {
                slot = FindSerializedMeleeSlot(item) ?? new MeleeWeaponSlot
                {
                    slotId = MakeSafeSlotName(item.itemName, item.name),
                    item = item
                };
            }

            if (slot.drawnInstance == null)
                slot.drawnInstance = FindNamedSlotInstance(transform, "Drawn_", item);
            if (slot.holsteredInstance == null)
                slot.holsteredInstance = FindNamedSlotInstance(transform, "Holstered_", item);

            // Ranged weapons must have both authored slots. If either one is missing,
            // do not fall back to the old generated InvectorWeapon_* holster flow.
            if (slot.drawnInstance == null || slot.holsteredInstance == null)
            {
                slot = null;
                return false;
            }

            _meleeSlotLookup[item] = slot;
            return true;
        }

        private MeleeWeaponSlot FindSerializedMeleeSlot(ItemData item)
        {
            if (item == null || meleeWeaponSlots == null)
                return null;

            for (int i = 0; i < meleeWeaponSlots.Count; i++)
            {
                MeleeWeaponSlot slot = meleeWeaponSlots[i];
                if (slot != null && slot.item == item)
                    return slot;
            }

            return null;
        }

        private bool TryGetRangedSlot(ItemData item, out RangedWeaponSlot slot)
        {
            slot = null;
            if (item == null)
                return false;

            if (!_rangedSlotLookup.TryGetValue(item, out slot) || slot == null)
            {
                slot = FindSerializedRangedSlot(item) ?? new RangedWeaponSlot
                {
                    slotId = MakeSafeSlotName(item.itemName, item.name),
                    item = item
                };
            }

            if (slot.drawnInstance == null)
                slot.drawnInstance = FindNamedSlotInstance(transform, "Drawn_", item);
            if (slot.holsteredInstance == null)
                slot.holsteredInstance = FindNamedSlotInstance(transform, "Holstered_", item);

            if (slot.drawnInstance == null && slot.holsteredInstance == null)
            {
                slot = null;
                return false;
            }

            _rangedSlotLookup[item] = slot;
            return true;
        }

        private RangedWeaponSlot FindSerializedRangedSlot(ItemData item)
        {
            if (item == null || rangedWeaponSlots == null)
                return null;

            for (int i = 0; i < rangedWeaponSlots.Count; i++)
            {
                RangedWeaponSlot slot = rangedWeaponSlots[i];
                if (slot != null && slot.item == item)
                    return slot;
            }

            return null;
        }

        private static GameObject FindNamedSlotInstance(Transform root, string prefix, ItemData item)
        {
            if (item == null || string.IsNullOrEmpty(prefix))
                return null;

            string itemName = MakeSafeSlotName(item.itemName, item.name);
            string assetName = MakeSafeSlotName(item.name, item.itemName);
            Transform match = FindChildTransformByName(root, prefix + itemName);
            if (match == null && assetName != itemName)
                match = FindChildTransformByName(root, prefix + assetName);
            if (match == null && !string.IsNullOrWhiteSpace(item.itemName))
                match = FindChildTransformByName(root, prefix + item.itemName);
            if (match == null && !string.IsNullOrWhiteSpace(item.name))
                match = FindChildTransformByName(root, prefix + item.name);

            return match != null ? match.gameObject : null;
        }

        public static void ApplyItemStatsToInstance(ItemData item, GameObject instance)
        {
            ApplyItemStats(item, instance);
        }

        public static GameObject FindPreloadedDrawnSlot(Transform root, ItemData item)
        {
            return FindNamedSlotInstance(root, "Drawn_", item);
        }

        public static GameObject FindPreloadedHolsteredSlot(Transform root, ItemData item)
        {
            return FindNamedSlotInstance(root, "Holstered_", item);
        }

        public static string MakeSafeSlotName(string preferred, string fallback)
        {
            string raw = string.IsNullOrWhiteSpace(preferred) ? fallback : preferred;
            if (string.IsNullOrWhiteSpace(raw))
                raw = "MeleeWeapon";

            char[] chars = raw.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                    chars[i] = '_';
            }

            return new string(chars);
        }

        private void BindPreloadedMeleeSlots()
        {
            _meleeSlotLookup.Clear();
            if (meleeWeaponSlots == null)
                return;

            for (int i = 0; i < meleeWeaponSlots.Count; i++)
            {
                MeleeWeaponSlot slot = meleeWeaponSlots[i];
                if (slot == null || slot.item == null)
                    continue;

                _meleeSlotLookup[slot.item] = slot;
                ResolveMeleeSlotInstances(slot);
                if (slot.drawnInstance != null)
                {
                    PrepareDrawnMeleeSlot(slot.drawnInstance, slot.item);
                    slot.drawnInstance.SetActive(false);
                }

                if (slot.holsteredInstance != null)
                {
                    PrepareHolsteredVisualSlot(slot.holsteredInstance, slot.item);
                    slot.holsteredInstance.SetActive(false);
                }
            }
        }

        private void BindPreloadedRangedSlots()
        {
            _rangedSlotLookup.Clear();
            if (rangedWeaponSlots == null)
                return;

            for (int i = 0; i < rangedWeaponSlots.Count; i++)
            {
                RangedWeaponSlot slot = rangedWeaponSlots[i];
                if (slot == null || slot.item == null)
                    continue;

                _rangedSlotLookup[slot.item] = slot;
                ResolveRangedSlotInstances(slot);
                if (slot.drawnInstance != null)
                {
                    PrepareDrawnRangedSlot(slot.drawnInstance, slot.item);
                    slot.drawnInstance.SetActive(false);
                }

                if (slot.holsteredInstance != null)
                {
                    PrepareHolsteredVisualSlot(slot.holsteredInstance, slot.item);
                    slot.holsteredInstance.SetActive(false);
                }
            }
        }

        private void ResolveMeleeSlotInstances(MeleeWeaponSlot slot)
        {
            if (slot?.item == null)
                return;

            if (slot.drawnInstance == null)
                slot.drawnInstance = FindNamedSlotInstance(transform, "Drawn_", slot.item);
            if (slot.holsteredInstance == null)
                slot.holsteredInstance = FindNamedSlotInstance(transform, "Holstered_", slot.item);
        }

        private void ResolveRangedSlotInstances(RangedWeaponSlot slot)
        {
            if (slot?.item == null)
                return;

            if (slot.drawnInstance == null)
                slot.drawnInstance = FindNamedSlotInstance(transform, "Drawn_", slot.item);
            if (slot.holsteredInstance == null)
                slot.holsteredInstance = FindNamedSlotInstance(transform, "Holstered_", slot.item);
        }

        private void CaptureAuthoredSlots<TSlot>(IReadOnlyList<TSlot> slots)
            where TSlot : class
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                switch (slots[i])
                {
                    case MeleeWeaponSlot meleeSlot:
                        ResolveMeleeSlotInstances(meleeSlot);
                        CaptureAuthoredSlotTransform(meleeSlot.drawnInstance);
                        CaptureAuthoredSlotTransform(meleeSlot.holsteredInstance);
                        break;
                    case RangedWeaponSlot rangedSlot:
                        ResolveRangedSlotInstances(rangedSlot);
                        CaptureAuthoredSlotTransform(rangedSlot.drawnInstance);
                        CaptureAuthoredSlotTransform(rangedSlot.holsteredInstance);
                        break;
                }
            }
        }

        private void CaptureAuthoredSlotTransform(GameObject instance)
        {
            if (instance == null || _authoredSlotTransforms.ContainsKey(instance))
                return;

            StoreAuthoredSlotTransform(instance);
        }

        private void ForceCaptureAuthoredSlotTransform(GameObject instance)
        {
            if (instance == null)
                return;

            StoreAuthoredSlotTransform(instance);
        }

        private void StoreAuthoredSlotTransform(GameObject instance)
        {
            Transform slotTransform = instance.transform;
            _authoredSlotTransforms[instance] = new AuthoredSlotTransform
            {
                Parent = slotTransform.parent,
                LocalPosition = slotTransform.localPosition,
                LocalRotation = slotTransform.localRotation,
                LocalScale = slotTransform.localScale
            };
        }

        private void ForceCaptureAuthoredSlots<TSlot>(IReadOnlyList<TSlot> slots)
            where TSlot : class
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                switch (slots[i])
                {
                    case MeleeWeaponSlot meleeSlot:
                        ResolveMeleeSlotInstances(meleeSlot);
                        ForceCaptureAuthoredSlotTransform(meleeSlot.drawnInstance);
                        ForceCaptureAuthoredSlotTransform(meleeSlot.holsteredInstance);
                        break;
                    case RangedWeaponSlot rangedSlot:
                        ResolveRangedSlotInstances(rangedSlot);
                        ForceCaptureAuthoredSlotTransform(rangedSlot.drawnInstance);
                        ForceCaptureAuthoredSlotTransform(rangedSlot.holsteredInstance);
                        break;
                }
            }
        }

        private void RestoreAuthoredWeaponSlotTransforms()
        {
            RestorePreloadedSlotTransforms(meleeWeaponSlots);
            RestorePreloadedSlotTransforms(rangedWeaponSlots);
        }

        private void RestorePreloadedSlotTransforms<TSlot>(IReadOnlyList<TSlot> slots)
            where TSlot : class
        {
            if (slots == null)
                return;

            for (int i = 0; i < slots.Count; i++)
            {
                switch (slots[i])
                {
                    case MeleeWeaponSlot meleeSlot:
                        RestoreAuthoredSlotTransform(meleeSlot.drawnInstance);
                        RestoreAuthoredSlotTransform(meleeSlot.holsteredInstance);
                        break;
                    case RangedWeaponSlot rangedSlot:
                        RestoreAuthoredSlotTransform(rangedSlot.drawnInstance);
                        RestoreAuthoredSlotTransform(rangedSlot.holsteredInstance);
                        break;
                }
            }
        }

        private void RestoreAuthoredSlotTransform(GameObject instance)
        {
            if (instance == null)
                return;

            if (!_authoredSlotTransforms.TryGetValue(instance, out AuthoredSlotTransform authored))
                return;

            Transform slotTransform = instance.transform;
            if (authored.Parent != null)
                slotTransform.SetParent(authored.Parent, false);

            slotTransform.localPosition = authored.LocalPosition;
            slotTransform.localRotation = authored.LocalRotation;
            slotTransform.localScale = authored.LocalScale;
        }

        private GameObject ResolveWeaponPrefab(ItemData item)
        {
            if (item.invectorWeaponPrefab != null)
                return item.invectorWeaponPrefab;

            if (item.IsRangedWeapon)
                return item.weaponGrip == WeaponGrip.TwoHanded ? defaultRiflePrefab : defaultPistolPrefab;

            if (item.itemType == ItemType.MeleeWeapon)
                return item.IsTwoHanded ? defaultMeleeTwoHandPrefab : defaultMeleeSwordPrefab;

            return null;
        }

        private GameObject GetOrCreateInstance(ItemData item, GameObject prefab)
        {
            if (_spawnedInstances.TryGetValue(item, out GameObject existing) && existing != null)
                return existing;

            GameObject instance = Instantiate(prefab);
            instance.name = $"InvectorWeapon_{item.itemName}";
            PreparePreloadedWeaponInstance(instance, item, prefab);
            instance.SetActive(false);
            _spawnedInstances[item] = instance;
            return instance;
        }

        private static void MountAuthoredWeaponVisual(GameObject invectorInstance, ItemData item, GameObject invectorPrefab)
        {
            if (invectorInstance == null || item == null)
                return;

            GameObject visualPrefab = item.heldPrefab != null ? item.heldPrefab : item.worldPrefab;
            if (visualPrefab == null || visualPrefab == invectorPrefab)
                return;

            EquippedVisualMarker existingVisual = FindMountedVisual(invectorInstance, item);
            if (existingVisual != null)
            {
                existingVisual.gameObject.SetActive(true);
                StripAuthoredVisualForEquippedWeapon(existingVisual.gameObject, item);
                HideVendorRenderers(invectorInstance, existingVisual.transform);
                return;
            }

            GameObject visual = Instantiate(visualPrefab, invectorInstance.transform, false);
            visual.name = $"PioneerVisual_{visualPrefab.name}";
            visual.SetActive(true);
            StripAuthoredVisualForEquippedWeapon(visual, item);

            if (HasRenderer(visual))
                HideVendorRenderers(invectorInstance, visual.transform);
        }

        public static void PreparePreloadedWeaponInstance(GameObject instance, ItemData item, GameObject invectorPrefab = null)
        {
            if (instance == null)
                return;

            StripInvectorPickupUi(instance);
            if (item != null && invectorPrefab != null)
                MountAuthoredWeaponVisual(instance, item, invectorPrefab);
            StripEquippedWeaponPhysics(instance);
        }

        public static void PrepareDrawnMeleeSlot(GameObject instance, ItemData item, GameObject invectorPrefab = null)
        {
            PreparePreloadedWeaponInstance(instance, item, invectorPrefab);
        }

        public static void PrepareDrawnRangedSlot(GameObject instance, ItemData item, GameObject invectorPrefab = null)
        {
            PreparePreloadedWeaponInstance(instance, item, invectorPrefab);
        }

        public static void PrepareHolsteredVisualSlot(GameObject instance, ItemData item)
        {
            if (instance == null)
                return;

            foreach (vMeleeWeapon weapon in instance.GetComponentsInChildren<vMeleeWeapon>(true))
                DestroyUnityObject(weapon);

            foreach (vShooterWeapon weapon in instance.GetComponentsInChildren<vShooterWeapon>(true))
                DestroyUnityObject(weapon);

            foreach (vHitBox hitBox in instance.GetComponentsInChildren<vHitBox>(true))
                DestroyUnityObject(hitBox);

            StripAuthoredVisualForEquippedWeapon(instance, item);
        }

        private static EquippedVisualMarker FindMountedVisual(GameObject root, ItemData item)
        {
            EquippedVisualMarker[] markers = root.GetComponentsInChildren<EquippedVisualMarker>(true);
            for (int i = 0; i < markers.Length; i++)
            {
                EquippedVisualMarker marker = markers[i];
                if (marker != null && marker.SourceItem == item)
                    return marker;
            }

            return null;
        }

        private static bool HasRenderer(GameObject root)
        {
            return root != null && root.GetComponentInChildren<Renderer>(true) != null;
        }

        private static void HideVendorRenderers(GameObject root, Transform ignoredRoot)
        {
            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || (ignoredRoot != null && renderer.transform.IsChildOf(ignoredRoot)))
                    continue;

                renderer.enabled = false;
            }
        }

        private static void StripAuthoredVisualForEquippedWeapon(GameObject visual, ItemData item)
        {
            if (visual == null)
                return;

            EquippedVisualMarker marker = visual.GetComponent<EquippedVisualMarker>();
            if (marker == null)
                marker = visual.AddComponent<EquippedVisualMarker>();
            marker.BindItem(item);

            SetLayerRecursively(visual, 0);

            foreach (ItemPickup pickup in visual.GetComponentsInChildren<ItemPickup>(true))
                DestroyUnityObject(pickup);

            foreach (ResourceNode node in visual.GetComponentsInChildren<ResourceNode>(true))
                DestroyUnityObject(node);

            foreach (vCollectableStandalone collectable in visual.GetComponentsInChildren<vCollectableStandalone>(true))
                DestroyUnityObject(collectable);

            foreach (Collider collider in visual.GetComponentsInChildren<Collider>(true))
                collider.enabled = false;

            foreach (Rigidbody body in visual.GetComponentsInChildren<Rigidbody>(true))
            {
                DestroyUnityObject(body);
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            Transform root = obj.transform;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i).gameObject, layer);
        }

        private static void StripInvectorPickupUi(GameObject instance)
        {
            if (instance == null)
                return;

            Transform root = instance.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child.name.StartsWith("vActionText", StringComparison.OrdinalIgnoreCase))
                    DestroyUnityObject(child.gameObject);
            }

            vCollectableStandalone[] collectables = instance.GetComponentsInChildren<vCollectableStandalone>(true);
            for (int i = 0; i < collectables.Length; i++)
            {
                vCollectableStandalone collectable = collectables[i];
                if (collectable == null)
                    continue;

                collectable.enabled = false;
            }

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                if (collider.GetComponentInParent<vHitBox>() != null)
                    continue;

                collider.enabled = false;
            }
        }

        private void AttachWeaponToHandler(GameObject instance, ItemData item)
        {
            if (instance == null)
                return;

            bool useLeftHand = false;
            vShooterWeapon shooterWeapon = instance.GetComponent<vShooterWeapon>();
            if (shooterWeapon != null)
                useLeftHand = shooterWeapon.isLeftWeapon;

            string equipPointName = ResolveEquipPointName(item, instance);

            Transform socket = ResolveEquipSocket(useLeftHand, equipPointName);
            if (socket == null)
                return;

            instance.transform.SetParent(socket, false);
            ApplyHeldTransform(instance, item);
            StripEquippedWeaponPhysics(instance);
        }

        private static string ResolveEquipPointName(ItemData item, GameObject instance)
        {
            if (item != null &&
                !string.IsNullOrEmpty(item.equipSocketName) &&
                !item.equipSocketName.Equals("RightHand", StringComparison.OrdinalIgnoreCase))
            {
                return item.equipSocketName;
            }

            vCollectableStandalone collectable = instance.GetComponentInChildren<vCollectableStandalone>(true);
            if (collectable != null && !string.IsNullOrEmpty(collectable.targetEquipPoint))
                return collectable.targetEquipPoint;

            return null;
        }

        private static void ApplyHeldTransform(GameObject instance, ItemData item)
        {
            if (item == null)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                return;
            }

            instance.transform.localPosition = item.heldLocalPosition;
            instance.transform.localScale = item.heldLocalScale;
            if (item.useHeldLocalRotation)
                instance.transform.localRotation = item.heldLocalRotation;
            else
                instance.transform.localEulerAngles = item.heldLocalEuler;
        }

        private static void ApplySheathedTransform(GameObject instance, ItemData item)
        {
            if (instance == null)
                return;

            if (item == null)
            {
                instance.transform.localPosition = Vector3.zero;
                instance.transform.localRotation = Quaternion.identity;
                instance.transform.localScale = Vector3.one;
                return;
            }

            instance.transform.localPosition = item.sheathedLocalPosition;
            instance.transform.localScale = item.sheathedLocalScale == Vector3.zero
                ? Vector3.one
                : item.sheathedLocalScale;

            if (item.useSheathedLocalRotation)
                instance.transform.localRotation = item.sheathedLocalRotation;
            else
                instance.transform.localEulerAngles = item.sheathedLocalEuler;
        }

        private static Transform FindChildTransformByName(Transform root, string name)
        {
            if (root == null || string.IsNullOrEmpty(name))
                return null;

            if (root.name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return root;

            for (int i = 0; i < root.childCount; i++)
            {
                Transform found = FindChildTransformByName(root.GetChild(i), name);
                if (found != null)
                    return found;
            }

            return null;
        }

        private Transform ResolveEquipSocket(bool useLeftHand, string equipPointName)
        {
            if (_collectControl == null)
                return null;

            vHandler handler = useLeftHand ? _collectControl.leftHandler : _collectControl.rightHandler;
            Transform socket = handler.defaultHandler;
            if (string.IsNullOrEmpty(equipPointName) || handler.customHandlers == null)
                return socket;

            for (int i = 0; i < handler.customHandlers.Count; i++)
            {
                Transform custom = handler.customHandlers[i];
                if (custom != null && custom.name.Equals(equipPointName))
                    return custom;
            }

            return socket;
        }

        private void EquipRanged(GameObject instance)
        {
            if (_meleeManager != null)
            {
                _meleeManager.SetRightWeapon((GameObject)null);
                _meleeManager.SetLeftWeapon((GameObject)null);
            }

            PrepareUnifiedProjectileWeapon(instance, _activeItem);

            if (_shooterManager != null)
                _shooterManager.SetRightWeapon(instance);
        }

        /// <summary>
        /// Fully hands ranged shot-effects ownership to our own ammo-driven pipeline. Invector's
        /// vShooterWeaponBase.ShotEffect() independently spawns its own physical bullet+trail
        /// (projectile), plays its own bundled gunshot (fireClip), fires its own muzzle-flash
        /// particle emitters (emittShurykenParticle), and flashes its own light (lightOnShot) — all
        /// on top of whatever CombatProjectileSpawner/CombatHitResolver do for the equipped ammo.
        /// Clearing all of them here (at equip time, mirroring CompanionInvectorLoadoutBridge) means
        /// every visual/audio effect comes from our own ammoItem-driven system. isInfinityAmmo stays
        /// true so Invector's own (unfed) native ammo/reload reserve system can never gate or
        /// interfere; dontUseReload stays false so Invector's reload animation/timing is still
        /// available — PioneerInvectorAmmoBridge decides when a round is available and when a
        /// reload should start, purely from WeaponAmmoState. weapon.ammo is left for
        /// PioneerInvectorAmmoBridge.SyncMagazineFromPioneer to set correctly on the next sync tick,
        /// rather than being force-filled here.
        /// </summary>
        private static void PrepareUnifiedProjectileWeapon(GameObject instance, ItemData weaponItem)
        {
            if (instance == null)
                return;

            foreach (vShooterWeapon weapon in instance.GetComponentsInChildren<vShooterWeapon>(true))
            {
                if (weapon == null)
                    continue;

                weapon.projectile = null;
                weapon.fireClip = null;
                weapon.emittShurykenParticle = null;
                weapon.lightOnShot = null;
                weapon.isInfinityAmmo = true;
                weapon.dontUseReload = false;
                PioneerInvectorRecoilUtility.ApplyWeaponRecoilTuning(weapon, weaponItem);
            }
        }

        private void EquipMelee(GameObject instance)
        {
            if (_shooterManager != null)
                _shooterManager.SetRightWeapon((GameObject)null);

            if (_meleeManager != null)
                _meleeManager.SetRightWeapon(instance);
        }

        private void ShowHolsteredWeaponIfNeeded()
        {
            if (_holsterPreviewActive)
                return;

            ItemData item = _equipment.EquippedItem;
            if (item == null || !item.IsEquippable)
                return;

            if (item.itemType == ItemType.MeleeWeapon)
            {
                if (TryGetMeleeSlot(item, out MeleeWeaponSlot slot) && slot.holsteredInstance != null)
                    ShowMeleeHolsteredSlot(item, slot);
                else
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Holstered_ slot for '{item.name}'.");
                return;
            }

            if (item.IsRangedWeapon)
            {
                if (TryGetRangedSlot(item, out RangedWeaponSlot slot) && slot.holsteredInstance != null)
                {
                    ShowRangedHolsteredSlot(item, slot);
                }
                else
                {
                    HideRuntimeGeneratedWeaponsForItem(item);
                    Debug.LogWarning($"PioneerInvectorWeaponBridge: missing Holstered_ slot for '{item.name}'.");
                }
                return;
            }

            GameObject prefab = ResolveWeaponPrefab(item);
            if (prefab == null)
                return;

            GameObject instance = GetOrCreateInstance(item, prefab);
            Transform socket = FindHolsterSocket(item);
            if (socket == null)
                return;

            instance.transform.SetParent(socket, false);
            ApplySheathedTransform(instance, item);
            StripEquippedWeaponPhysics(instance);
            instance.SetActive(true);
        }

        private static void StripEquippedWeaponPhysics(GameObject instance)
        {
            if (instance == null)
                return;

            Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null)
                    continue;

                // vHitBox triggers are controlled by vMeleeManager during attack windows.
                if (collider.GetComponentInParent<vHitBox>() != null)
                    continue;

                collider.enabled = false;
            }

            Rigidbody[] bodies = instance.GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < bodies.Length; i++)
            {
                Rigidbody body = bodies[i];
                if (body != null)
                    DestroyUnityObject(body);
            }
        }

        private static void DestroyUnityObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

#if UNITY_EDITOR
            // Prefab contents opened via PrefabUtility.LoadPrefabContents live in a preview
            // scene. Deferred Destroy would run after SaveAsPrefabAsset, so strip immediately.
            GameObject go = obj as GameObject;
            if (go == null && obj is Component component)
                go = component.gameObject;

            bool inPreviewScene = go != null &&
                UnityEditor.SceneManagement.EditorSceneManager.IsPreviewScene(go.scene);

            if (!Application.isPlaying || inPreviewScene)
            {
                DestroyImmediate(obj, true);
                return;
            }
#endif
            Destroy(obj);
        }

        private void ClearInvectorWeapons()
        {
            if (_shooterManager != null)
                _shooterManager.SetRightWeapon((GameObject)null);

            if (_meleeManager != null)
                _meleeManager.SetRightWeapon((GameObject)null);
        }

        private void HideAllSpawnedWeapons()
        {
            foreach (KeyValuePair<ItemData, GameObject> pair in _spawnedInstances)
            {
                if (pair.Value != null)
                    pair.Value.SetActive(false);
            }

            if (meleeWeaponSlots != null)
            {
                for (int i = 0; i < meleeWeaponSlots.Count; i++)
                {
                    MeleeWeaponSlot slot = meleeWeaponSlots[i];
                    if (slot == null)
                        continue;

                    if (slot.drawnInstance != null)
                        slot.drawnInstance.SetActive(false);
                    if (slot.holsteredInstance != null)
                        slot.holsteredInstance.SetActive(false);
                }
            }

            if (rangedWeaponSlots != null)
            {
                for (int i = 0; i < rangedWeaponSlots.Count; i++)
                {
                    RangedWeaponSlot slot = rangedWeaponSlots[i];
                    if (slot == null)
                        continue;

                    if (slot.drawnInstance != null)
                        slot.drawnInstance.SetActive(false);
                    if (slot.holsteredInstance != null)
                        slot.holsteredInstance.SetActive(false);
                }
            }
        }

        private void HideRuntimeGeneratedWeaponsForItem(ItemData item)
        {
            if (item == null)
                return;

            string displayName = item.itemName ?? string.Empty;
            string assetName = item.name ?? string.Empty;
            Transform root = transform;
            for (int i = root.childCount - 1; i >= 0; i--)
                HideRuntimeGeneratedWeaponsForItem(root.GetChild(i), displayName, assetName);
        }

        private static void HideRuntimeGeneratedWeaponsForItem(Transform node, string displayName, string assetName)
        {
            if (node == null)
                return;

            for (int i = node.childCount - 1; i >= 0; i--)
                HideRuntimeGeneratedWeaponsForItem(node.GetChild(i), displayName, assetName);

            string name = node.name;
            if (!name.StartsWith("InvectorWeapon_", StringComparison.Ordinal))
                return;

            bool matchesDisplayName = !string.IsNullOrWhiteSpace(displayName) &&
                                      name.IndexOf(displayName, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchesAssetName = !string.IsNullOrWhiteSpace(assetName) &&
                                    name.IndexOf(assetName, StringComparison.OrdinalIgnoreCase) >= 0;
            if (matchesDisplayName || matchesAssetName)
                node.gameObject.SetActive(false);
        }

        private static void ApplyItemStats(ItemData item, GameObject instance)
        {
            if (item == null || instance == null)
                return;

            vShooterWeapon shooterWeapon = instance.GetComponent<vShooterWeapon>();
            if (shooterWeapon != null)
            {
                shooterWeapon.maxDamage = Mathf.RoundToInt(item.GetAverageRangedDamage());
                shooterWeapon.minDamage = Mathf.RoundToInt(Mathf.Max(1f, item.rangedDamage));
            }

            vMeleeWeapon meleeWeapon = instance.GetComponent<vMeleeWeapon>();
            if (meleeWeapon != null)
            {
                meleeWeapon.damage.damageValue = Mathf.RoundToInt(item.GetAverageMeleeDamage());
            }
        }
    }
}