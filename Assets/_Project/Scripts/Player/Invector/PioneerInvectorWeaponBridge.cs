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
            return FindHolsterSocket(transform, item);
        }

        /// <summary>
        /// Resolves the transform that should own a holstered weapon instance.
        /// Prefers HandgunHolder/RifleHolder under the skinned VBOT_ bone when present —
        /// BodySnaps/RightUpLeg holders use ~0.01 scale and break ItemData sheathed TRS.
        /// </summary>
        public static Transform FindHolsterSocket(Transform root, ItemData item)
        {
            if (root == null)
                return null;

            string socketName = ResolveHolsterSocketName(item);
            Transform bone = FindPreferredSheatheBone(root, socketName);
            string holderName = ResolveHolsterHolderName(item);

            if (bone != null && !string.IsNullOrEmpty(holderName))
            {
                Transform holder = FindChildTransformByName(bone, holderName);
                if (holder != null)
                    return holder;
            }

            if (bone != null)
                return bone;

            if (!string.IsNullOrEmpty(holderName))
            {
                Transform holder = FindPreferredHolsterHolder(root, holderName);
                if (holder != null)
                    return holder;
            }

            return FindChildTransformByName(root, socketName);
        }

        public static string ResolveHolsterSocketName(ItemData item)
        {
            if (item == null)
                return "Spine2";

            // Explicit Spine2 on one-handed pistols is legacy — prefer hip holster.
            if (!string.IsNullOrWhiteSpace(item.sheatheSocketName) &&
                !item.sheatheSocketName.Equals("Spine", StringComparison.OrdinalIgnoreCase) &&
                !item.sheatheSocketName.Equals("Spine2", StringComparison.OrdinalIgnoreCase))
            {
                return item.sheatheSocketName;
            }

            return ResolveDefaultHolsterSocketName(item);
        }

        /// <summary>
        /// Invector holder under the sheathe bone. One-handed hip → HandgunHolder; two-handed → RifleHolder.
        /// </summary>
        public static string ResolveHolsterHolderName(ItemData item)
        {
            if (item == null)
                return null;

            // Mining pistol holsters on the hip like other one-handed ranged weapons.
            if (item.weaponGrip == WeaponGrip.TwoHanded || item.IsTwoHanded)
                return "RifleHolder";

            string socketName = ResolveHolsterSocketName(item);
            if (socketName.Equals("RightUpLeg", StringComparison.OrdinalIgnoreCase) ||
                socketName.Equals("LeftUpLeg", StringComparison.OrdinalIgnoreCase))
                return "HandgunHolder";

            return item.IsRangedWeapon ? "HandgunHolder" : null;
        }

        private static Transform FindPreferredSheatheBone(Transform root, string socketName)
        {
            if (root == null || string.IsNullOrWhiteSpace(socketName))
                return null;

            // Skinned armature bones are named VBOT_:RightUpLeg; BodySnaps/RightUpLeg is the Invector proxy.
            Transform vbot = FindChildTransformByName(root, "VBOT_:" + socketName);
            if (vbot != null)
                return vbot;

            return FindChildTransformByName(root, socketName);
        }

        private static Transform FindPreferredHolsterHolder(Transform root, string holderName)
        {
            if (root == null || string.IsNullOrWhiteSpace(holderName))
                return null;

            Transform best = null;
            FindPreferredHolsterHolderRecursive(root, holderName, ref best);
            return best;
        }

        private static void FindPreferredHolsterHolderRecursive(Transform current, string holderName, ref Transform best)
        {
            if (current == null)
                return;

            if (current.name.Equals(holderName, StringComparison.OrdinalIgnoreCase))
            {
                // Prefer holders under VBOT_ bones (local scale ~1) over BodySnaps (~0.01).
                bool underVbot = IsUnderVbotBone(current);
                if (best == null || (underVbot && !IsUnderVbotBone(best)))
                    best = current;
            }

            for (int i = 0; i < current.childCount; i++)
                FindPreferredHolsterHolderRecursive(current.GetChild(i), holderName, ref best);
        }

        private static bool IsUnderVbotBone(Transform t)
        {
            for (Transform c = t; c != null; c = c.parent)
            {
                if (c.name.StartsWith("VBOT_:", StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        public static string ResolveDefaultMeleeHolsterSocketName(ItemData item)
        {
            return ResolveDefaultHolsterSocketName(item);
        }

        /// <summary>
        /// One-handed pistols / melee hip holster on RightUpLeg; two-handed / other on Spine2.
        /// </summary>
        public static string ResolveDefaultHolsterSocketName(ItemData item)
        {
            if (item == null)
                return "Spine2";

            if (item.itemType == ItemType.MeleeWeapon && !item.IsTwoHanded)
                return "RightUpLeg";

            if (item.IsRangedWeapon && item.weaponGrip != WeaponGrip.TwoHanded)
                return "RightUpLeg";

            return "Spine2";
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

        public static void ApplySheathedTransformToInstance(ItemData item, GameObject instance)
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
            _equipment?.HolsterWeapon();
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

            // World pickups / inventory adds must NOT tear down and re-equip a drawn gun.
            // Re-equipping Invector weapons with isInfinityAmmo retriggers reload SFX even for
            // mushrooms, scrap, etc. Only refresh when the drawn/holstered weapon identity changed.
            if (!NeedsWeaponVisualRefreshAfterInventoryChange())
                return;

            RefreshEquippedWeapon();
        }

        /// <summary>
        /// True when hotbar weapon identity no longer matches what the bridge is showing.
        /// Unrelated stack adds (loot, scrap, ammo credit) return false.
        /// </summary>
        private bool NeedsWeaponVisualRefreshAfterInventoryChange()
        {
            if (_equipment == null)
                return false;

            if (_holsterPreviewActive)
                return true;

            if (_equipment.IsWeaponDrawn)
            {
                ItemData drawn = _equipment.DrawnWeaponItem;
                if (drawn != _activeItem)
                    return true;

                return drawn != null && !IsDrawnWeaponPresentationActive(drawn);
            }

            // Holstered: refresh if we still think a drawn weapon is active, or holster target changed.
            if (_activeItem != null)
                return true;

            ItemData equipped = _equipment.EquippedItem;
            if (equipped == null || !equipped.IsEquippable)
                return false;

            return !IsHolsteredWeaponPresentationActive(equipped);
        }

        private bool IsDrawnWeaponPresentationActive(ItemData item)
        {
            if (item == null)
                return false;

            if (item.IsRangedWeapon && TryGetRangedSlot(item, out RangedWeaponSlot ranged))
            {
                return ranged.drawnInstance != null
                    && ranged.drawnInstance.activeInHierarchy
                    && _shooterManager != null
                    && _shooterManager.CurrentWeapon != null
                    && _shooterManager.CurrentWeapon.transform.IsChildOf(ranged.drawnInstance.transform);
            }

            if (item.itemType == ItemType.MeleeWeapon && TryGetMeleeSlot(item, out MeleeWeaponSlot melee))
            {
                return melee.drawnInstance != null
                    && melee.drawnInstance.activeInHierarchy
                    && _meleeManager != null
                    && _meleeManager.rightWeapon != null
                    && _meleeManager.rightWeapon.transform.IsChildOf(melee.drawnInstance.transform);
            }

            if (_spawnedInstances.TryGetValue(item, out GameObject instance))
                return instance != null && instance.activeInHierarchy;

            return false;
        }

        private bool IsHolsteredWeaponPresentationActive(ItemData item)
        {
            if (item == null)
                return true;

            if (item.IsRangedWeapon && TryGetRangedSlot(item, out RangedWeaponSlot ranged))
                return ranged.holsteredInstance != null && ranged.holsteredInstance.activeInHierarchy;

            if (item.itemType == ItemType.MeleeWeapon && TryGetMeleeSlot(item, out MeleeWeaponSlot melee))
                return melee.holsteredInstance != null && melee.holsteredInstance.activeInHierarchy;

            return true;
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

            if (_equipment == null || !_equipment.IsWeaponDrawn)
            {
                // Already holstered with the correct visual — skip teardown (avoids reload SFX).
                if (_activeItem == null)
                {
                    ItemData holsterTarget = _equipment != null ? _equipment.EquippedItem : null;
                    if (holsterTarget == null || !holsterTarget.IsEquippable || IsHolsteredWeaponPresentationActive(holsterTarget))
                        return;
                }

                HideAllSpawnedWeapons();
                _activeItem = null;
                ClearInvectorWeapons();
                ShowHolsteredWeaponIfNeeded();
                return;
            }

            _holsterPreviewActive = false;
            _holsterPreviewItem = null;

            ItemData item = _equipment.DrawnWeaponItem;
            // Same drawn weapon already live on the shooter/melee manager — do not unequip/re-equip.
            // Inventory pickups used to hit this path every time and play pistol reload audio.
            if (item != null && item == _activeItem && IsDrawnWeaponPresentationActive(item))
                return;

            HideAllSpawnedWeapons();

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

        /// <summary>
        /// Resolves the muzzle transform on the active drawn ranged slot for the given item.
        /// Prefers the authored barrel <c>muzzle</c> / <c>Muzzle</c> on the drawn visual.
        /// </summary>
        public bool TryGetActiveDrawnMuzzle(ItemData item, out Transform muzzle)
        {
            muzzle = null;
            if (!TryGetRangedSlot(item, out RangedWeaponSlot slot) || slot?.drawnInstance == null)
                return false;

            if (!slot.drawnInstance.activeInHierarchy)
                return false;

            Transform[] children = slot.drawnInstance.GetComponentsInChildren<Transform>(true);

            // Prefer authored muzzle that owns a Laser LineRenderer stack (Sci-Fi Pistol / Survival Rifle / Mining Tool).
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (!t.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase) &&
                    !t.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase))
                    continue;

                Transform laser = t.Find("Laser");
                if (laser == null)
                {
                    for (int c = 0; c < t.childCount; c++)
                    {
                        Transform child = t.GetChild(c);
                        if (child != null && child.name.Equals("Laser", StringComparison.OrdinalIgnoreCase))
                        {
                            laser = child;
                            break;
                        }
                    }
                }

                if (laser != null && laser.GetComponent<LineRenderer>() != null)
                {
                    muzzle = t;
                    return true;
                }
            }

            // Prefer the authored barrel muzzle the artist placed (exact name match).
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (t.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase) ||
                    t.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase))
                {
                    muzzle = t;
                    return true;
                }
            }

            // Legacy runtime tip created for mining when no authored muzzle exists.
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (t.name.Equals("MiningBeamMuzzle", StringComparison.OrdinalIgnoreCase))
                {
                    muzzle = t;
                    return true;
                }
            }

            // Next: any transform with "uzzle" under PioneerVisual_* (authored mesh).
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (t.name.IndexOf("uzzle", StringComparison.OrdinalIgnoreCase) < 0)
                    continue;

                bool underVisual = false;
                Transform p = t;
                while (p != null && p != slot.drawnInstance.transform)
                {
                    if (p.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                    {
                        underVisual = true;
                        break;
                    }

                    p = p.parent;
                }

                if (!underVisual)
                    continue;

                muzzle = t;
                return true;
            }

            muzzle = slot.drawnInstance.transform;
            return true;
        }

        /// <summary>
        /// World-space barrel tip sampled from the active Pioneer mining visual (post-aim / IK safe).
        /// </summary>
        public bool TryGetActiveDrawnMiningTip(ItemData item, out Vector3 tipWorld)
        {
            tipWorld = default;
            if (!TryGetRangedSlot(item, out RangedWeaponSlot slot) || slot?.drawnInstance == null)
                return false;

            if (!slot.drawnInstance.activeInHierarchy)
                return false;

            Transform weaponRoot = slot.drawnInstance.transform;
            Transform visual = null;
            Transform[] children = slot.drawnInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t != null && t.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                {
                    visual = t;
                    break;
                }
            }

            if (visual == null)
                return false;

            tipWorld = ResolveMiningMeshTipWorld(visual, weaponRoot);
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
                EnableRenderersUnder(existingVisual.transform);
                StripAuthoredVisualForEquippedWeapon(existingVisual.gameObject, item);
                HideVendorRenderers(invectorInstance, existingVisual.transform);
                AlignMiningToolAimAxis(invectorInstance, existingVisual.transform, item);
                return;
            }

            GameObject visual = Instantiate(visualPrefab, invectorInstance.transform, false);
            visual.name = $"PioneerVisual_{visualPrefab.name}";
            visual.SetActive(true);
            EnableRenderersUnder(visual.transform);
            StripAuthoredVisualForEquippedWeapon(visual, item);
            AlignMiningToolAimAxis(invectorInstance, visual.transform, item);

            if (HasRenderer(visual))
                HideVendorRenderers(invectorInstance, visual.transform);
        }

        /// <summary>
        /// Re-applies ItemData held/invector visuals on an existing Drawn_/Holstered_ slot and hides
        /// leftover VBOT / GreatSword vendor meshes so the visible mesh matches the item.
        /// </summary>
        public static void SyncPreloadedSlotVisuals(GameObject slotInstance, ItemData item, GameObject invectorPrefab, bool holstered)
        {
            if (slotInstance == null || item == null)
                return;

            if (holstered)
            {
                EnsureHolsteredMeshMatchesItem(slotInstance, item);
                PrepareHolsteredVisualSlot(slotInstance, item);
                EnableRenderersUnder(slotInstance.transform);
                return;
            }

            RemoveOrphanPioneerVisuals(slotInstance, item);

            if (item.itemType == ItemType.MeleeWeapon)
                PrepareDrawnMeleeSlot(slotInstance, item, invectorPrefab);
            else if (item.IsRangedWeapon)
                PrepareDrawnRangedSlot(slotInstance, item, invectorPrefab);
            else
                PreparePreloadedWeaponInstance(slotInstance, item, invectorPrefab);

            // Prefer authored PioneerVisual; keep VBOT template meshes component-disabled.
            EquippedVisualMarker pioneer = FindMountedVisual(slotInstance, item);
            if (pioneer != null)
            {
                pioneer.gameObject.SetActive(true);
                EnableRenderersUnder(pioneer.transform);
                HideVendorRenderers(slotInstance, pioneer.transform);
            }
            else
            {
                EnableRenderersUnder(slotInstance.transform);
            }
        }

        /// <summary>
        /// Older mounts left unmarked PioneerVisual_* siblings; HideVendor then disables them as
        /// "vendor" leftovers. Keep the ItemData-marked visual (or a single survivor) only.
        /// </summary>
        private static void RemoveOrphanPioneerVisuals(GameObject slotInstance, ItemData item)
        {
            if (slotInstance == null)
                return;

            EquippedVisualMarker keep = FindMountedVisual(slotInstance, item);
            Transform keepTransform = keep != null ? keep.transform : null;

            if (keepTransform == null)
            {
                for (int i = 0; i < slotInstance.transform.childCount; i++)
                {
                    Transform child = slotInstance.transform.GetChild(i);
                    if (child == null || !child.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                        continue;

                    keepTransform = child;
                    EquippedVisualMarker marker = child.GetComponent<EquippedVisualMarker>();
                    if (marker == null)
                        marker = child.gameObject.AddComponent<EquippedVisualMarker>();
                    if (item != null)
                        marker.BindItem(item);
                    break;
                }
            }

            List<GameObject> toDestroy = new List<GameObject>(4);
            for (int i = 0; i < slotInstance.transform.childCount; i++)
            {
                Transform child = slotInstance.transform.GetChild(i);
                if (child == null || !child.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                    continue;
                if (keepTransform != null && child == keepTransform)
                    continue;
                toDestroy.Add(child.gameObject);
            }

            for (int i = 0; i < toDestroy.Count; i++)
            {
                if (toDestroy[i] != null)
                    DestroyUnityObject(toDestroy[i]);
            }
        }

        private static void EnsureHolsteredMeshMatchesItem(GameObject holstered, ItemData item)
        {
            GameObject visualPrefab = item.heldPrefab != null ? item.heldPrefab : item.worldPrefab;
            if (visualPrefab == null || holstered == null)
                return;

            if (HolsterMeshesMatchReference(holstered, visualPrefab))
                return;

            // Strip leftover VBOT / wrong meshes, then mount the ItemData held/world visual.
            Transform root = holstered.transform;
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                Transform child = root.GetChild(i);
                if (child == null)
                    continue;
                if (child.GetComponent<MeshFilter>() != null ||
                    child.GetComponent<MeshRenderer>() != null ||
                    child.GetComponentInChildren<Renderer>(true) != null)
                    DestroyUnityObject(child.gameObject);
            }

            MeshFilter selfFilter = holstered.GetComponent<MeshFilter>();
            MeshRenderer selfRenderer = holstered.GetComponent<MeshRenderer>();
            if (selfFilter != null)
                DestroyUnityObject(selfFilter);
            if (selfRenderer != null)
                DestroyUnityObject(selfRenderer);

            GameObject visual = UnityEngine.Object.Instantiate(visualPrefab, holstered.transform, false);
            visual.name = $"PioneerVisual_{visualPrefab.name}";
            visual.SetActive(true);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = Vector3.one;
            StripAuthoredVisualForEquippedWeapon(visual, item);
            EnableRenderersUnder(visual.transform);
        }

        private static bool HolsterMeshesMatchReference(GameObject holstered, GameObject reference)
        {
            HashSet<string> referenceMeshes = CollectMeshNames(reference);
            if (referenceMeshes.Count == 0)
                return true;

            HashSet<string> holsterMeshes = CollectMeshNames(holstered);
            if (holsterMeshes.Count == 0)
                return false;

            foreach (string meshName in holsterMeshes)
            {
                if (referenceMeshes.Contains(meshName))
                    return true;
            }

            return false;
        }

        private static HashSet<string> CollectMeshNames(GameObject root)
        {
            HashSet<string> names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (root == null)
                return names;

            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i] != null && filters[i].sharedMesh != null)
                    names.Add(filters[i].sharedMesh.name);
            }

            SkinnedMeshRenderer[] skinned = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int i = 0; i < skinned.Length; i++)
            {
                if (skinned[i] != null && skinned[i].sharedMesh != null)
                    names.Add(skinned[i].sharedMesh.name);
            }

            return names;
        }

        private static void EnableRenderersUnder(Transform root)
        {
            if (root == null)
                return;

            Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || IsResourceScanCone(renderer.transform))
                    continue;

                renderer.enabled = true;
                if (!renderer.gameObject.activeSelf)
                    renderer.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// Authored mining scan VFX under Drawn_DM_Mining_Tool/renderer — stays off until DMIMiningResourceScanner enables it.
        /// </summary>
        private static bool IsResourceScanCone(Transform t)
        {
            while (t != null)
            {
                if (t.name.Equals("Scan Cone", StringComparison.OrdinalIgnoreCase))
                    return true;
                t = t.parent;
            }

            return false;
        }

        /// <summary>
        /// DM Mining Tool mesh is authored along local +X, while Invector aims along weapon +Z.
        /// Rotate the Pioneer visual so barrel shares the aim axis, keep a stable MiningBeamMuzzle
        /// bound to vShooterWeapon.muzzle (never leave a destroyed muzzle ref - that freezes aim camera),
        /// and seat the mesh / leftHandIK on the grip.
        /// </summary>
        private static void AlignMiningToolAimAxis(GameObject invectorInstance, Transform visual, ItemData item)
        {
            if (invectorInstance == null || visual == null || item == null || !item.isMiningTool)
                return;

            Transform weaponRoot = invectorInstance.transform;

            // Keep the authored visual pose — do not rewrite localPosition/euler (that moved the
            // mining pistol in-hand). Only ensure the beam muzzle exists and is bound.
            EnsureMiningBeamMuzzle(invectorInstance, visual, weaponRoot);

            if (item.weaponGrip == WeaponGrip.TwoHanded)
                AlignMiningLeftHandIk(invectorInstance, visual, weaponRoot);
        }

        /// <summary>
        /// Runtime-safe: rebind muzzle / visual even when PrepareDrawnRangedSlot is called without an invector prefab.
        /// </summary>
        private static void EnsureMiningToolRuntimeBindings(GameObject instance, ItemData item)
        {
            if (instance == null || item == null || !item.isMiningTool)
                return;

            Transform visual = null;
            Transform[] children = instance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t != null && t.name.StartsWith("PioneerVisual_", StringComparison.Ordinal))
                {
                    visual = t;
                    break;
                }
            }

            if (visual != null)
                AlignMiningToolAimAxis(instance, visual, item);
            else
                EnsureMiningBeamMuzzle(instance, null, instance.transform);
        }

        private static Vector3 ResolveMiningVisualBarrelEuler(Transform visual)
        {
            Bounds localBounds = GetMiningVisualLocalBounds(visual);
            Vector3 size = localBounds.size;
            if (size.sqrMagnitude < 0.000001f)
                return Vector3.zero;

            // Bulky legacy tool authored along +X; pistol meshes are usually along +Z already.
            if (size.x > size.z * 1.15f && size.x >= size.y)
                return new Vector3(0f, 270f, 0f);

            return Vector3.zero;
        }

        private static Vector3 ResolveMiningVisualGripOffset(Transform visual)
        {
            Vector3 offset = new Vector3(0f, -0.01f, 0.06f);
            Bounds localBounds = GetMiningVisualLocalBounds(visual);
            if (localBounds.size.sqrMagnitude < 0.000001f)
                return offset;

            float halfLength = Mathf.Max(localBounds.extents.x, localBounds.extents.z);
            offset.z = Mathf.Clamp(halfLength * 0.25f, 0.04f, 0.2f);
            return offset;
        }

        private static Vector3 ResolveMiningMeshTipWorld(Transform visual, Transform weaponRoot)
        {
            Vector3 tipWorld = weaponRoot.position + weaponRoot.forward * 0.35f;
            float best = float.NegativeInfinity;

            // Mining Pistol mesh assets can report zero MeshFilter/localBounds while Renderer.bounds
            // is valid — build tip candidates from world bounds remapped into each renderer.
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || !renderer.enabled)
                    continue;

                Bounds worldBounds = renderer.bounds;
                if (worldBounds.size.sqrMagnitude < 0.000001f)
                    continue;

                Vector3 c = worldBounds.center;
                Vector3 e = worldBounds.extents;
                // True directional tip of an AABB is always a corner, not a face midpoint.
                Vector3[] candidates =
                {
                    c + new Vector3( e.x,  e.y,  e.z),
                    c + new Vector3( e.x,  e.y, -e.z),
                    c + new Vector3( e.x, -e.y,  e.z),
                    c + new Vector3( e.x, -e.y, -e.z),
                    c + new Vector3(-e.x,  e.y,  e.z),
                    c + new Vector3(-e.x,  e.y, -e.z),
                    c + new Vector3(-e.x, -e.y,  e.z),
                    c + new Vector3(-e.x, -e.y, -e.z)
                };

                for (int n = 0; n < candidates.Length; n++)
                {
                    float score = Vector3.Dot(candidates[n] - weaponRoot.position, weaponRoot.forward);
                    if (score > best)
                    {
                        best = score;
                        tipWorld = candidates[n];
                    }
                }
            }

            return tipWorld;
        }

        private static Bounds GetMiningVisualLocalBounds(Transform visual)
        {
            if (visual == null)
                return new Bounds(Vector3.zero, Vector3.zero);

            // Prefer remapping live renderer world bounds into visual space — authored Mining Pistol
            // meshes often have empty MeshFilter.bounds / localBounds.
            bool has = false;
            Bounds local = new Bounds(Vector3.zero, Vector3.zero);
            Renderer[] renderers = visual.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                    continue;

                Bounds wb = renderer.bounds;
                if (wb.size.sqrMagnitude < 0.000001f)
                    continue;

                for (int x = -1; x <= 1; x += 2)
                for (int y = -1; y <= 1; y += 2)
                for (int z = -1; z <= 1; z += 2)
                {
                    Vector3 corner = wb.center + Vector3.Scale(wb.extents, new Vector3(x, y, z));
                    Vector3 inVisual = visual.InverseTransformPoint(corner);
                    if (!has)
                    {
                        local = new Bounds(inVisual, Vector3.zero);
                        has = true;
                    }
                    else
                    {
                        local.Encapsulate(inVisual);
                    }
                }
            }

            if (has)
                return local;

            MeshRenderer meshRenderer = visual.GetComponentInChildren<MeshRenderer>(true);
            if (meshRenderer != null && meshRenderer.localBounds.size.sqrMagnitude > 0.000001f)
                return meshRenderer.localBounds;

            MeshFilter filter = visual.GetComponentInChildren<MeshFilter>(true);
            if (filter != null && filter.sharedMesh != null && filter.sharedMesh.bounds.size.sqrMagnitude > 0.000001f)
                return filter.sharedMesh.bounds;

            return new Bounds(Vector3.zero, Vector3.zero);
        }

        private static void EnsureMiningBeamMuzzle(GameObject invectorInstance, Transform visual, Transform weaponRoot)
        {
            if (invectorInstance == null || weaponRoot == null)
                return;

            Transform tip = null;
            Transform[] children = invectorInstance.GetComponentsInChildren<Transform>(true);

            // Prefer the authored barrel muzzle (artist-placed) over any runtime tip.
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null)
                    continue;

                if (child.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase) ||
                    child.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase))
                {
                    tip = child;
                    break;
                }
            }

            if (tip == null)
            {
                for (int i = 0; i < children.Length; i++)
                {
                    Transform child = children[i];
                    if (child != null && child.name.Equals("MiningBeamMuzzle", StringComparison.OrdinalIgnoreCase))
                    {
                        tip = child;
                        break;
                    }
                }
            }

            // Only create a runtime tip when no authored muzzle exists. Never rewrite an authored muzzle pose.
            bool createdTip = false;
            bool authoredMuzzle = tip != null &&
                (tip.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase) ||
                 tip.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase));

            if (tip == null)
            {
                GameObject tipGo = new GameObject("MiningBeamMuzzle");
                tip = tipGo.transform;
                createdTip = true;
            }

            if (createdTip)
            {
                Transform tipParent = visual != null ? visual : weaponRoot;
                if (tip.parent != tipParent)
                    tip.SetParent(tipParent, true);

                Vector3 tipWorld = visual != null
                    ? ResolveMiningMeshTipWorld(visual, weaponRoot)
                    : weaponRoot.position + weaponRoot.forward * 0.35f;
                tip.position = tipWorld;
                tip.rotation = Quaternion.LookRotation(weaponRoot.forward, weaponRoot.up);
            }
            else if (!authoredMuzzle && tip.parent == null)
            {
                Transform tipParent = visual != null ? visual : weaponRoot;
                tip.SetParent(tipParent, true);
            }

            foreach (vShooterWeapon shooter in invectorInstance.GetComponentsInChildren<vShooterWeapon>(true))
            {
                if (shooter != null)
                    shooter.muzzle = tip;
            }
        }

        private static void AlignMiningLeftHandIk(GameObject invectorInstance, Transform visual, Transform weaponRoot)
        {
            if (invectorInstance == null || weaponRoot == null)
                return;

            Transform leftIk = null;
            Transform[] children = invectorInstance.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t != null && t.name.Equals("leftHandIK", StringComparison.OrdinalIgnoreCase))
                {
                    leftIk = t;
                    break;
                }
            }

            if (leftIk == null)
                return;

            // Seat support hand on the underside / mid body of the mining tool (weapon local space).
            Vector3 targetLocal = new Vector3(0.02f, -0.03f, 0.18f);
            if (visual != null)
            {
                Renderer renderer = visual.GetComponentInChildren<Renderer>(true);
                if (renderer != null)
                {
                    Vector3 localCenter = weaponRoot.InverseTransformPoint(renderer.bounds.center);
                    targetLocal = new Vector3(localCenter.x + 0.02f, localCenter.y - 0.04f, Mathf.Max(0.12f, localCenter.z * 0.55f));
                }
            }

            leftIk.position = weaponRoot.TransformPoint(targetLocal);
            // Keep a natural palm-up support pose relative to the weapon.
            leftIk.rotation = weaponRoot.rotation * Quaternion.Euler(280f, 40f, 200f);

            foreach (vShooterWeapon shooter in invectorInstance.GetComponentsInChildren<vShooterWeapon>(true))
            {
                if (shooter != null)
                    shooter.handIKTarget = leftIk;
            }
        }

        public static void PreparePreloadedWeaponInstance(GameObject instance, ItemData item, GameObject invectorPrefab = null)
        {
            if (instance == null)
                return;

            StripInvectorPickupUi(instance);
            if (item != null && invectorPrefab != null)
                MountAuthoredWeaponVisual(instance, item, invectorPrefab);
            EnsureMiningToolRuntimeBindings(instance, item);
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
        /// true so Invector's native reserve never gates the player — finite ammo is enforced only
        /// by WeaponAmmoState (Standard/Gunpowder are no longer treated as infinite for the player).
        /// Companions/enemies keep true infinite fire via their own loadout bridges. dontUseReload
        /// stays false so Invector's reload animation/timing is still available —
        /// PioneerInvectorAmmoBridge decides when a round is available and when a reload should
        /// start, purely from WeaponAmmoState. weapon.ammo is left for
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
                weapon.autoReload = false;
                // Mining uses continuous beam audio while charged; empty plasma uses the same
                // shared dry-fire click as other guns (never the handgun fireClip).
                ApplySharedEmptyClickClip(weapon);
                EnsureReloadAudioSource(weapon);
                PioneerInvectorRecoilUtility.ApplyWeaponRecoilTuning(weapon, weaponItem);
            }
        }

        private static AudioClip _sharedEmptyClickClip;

        private static void ApplySharedEmptyClickClip(vShooterWeapon weapon)
        {
            if (weapon == null)
                return;

            if (_sharedEmptyClickClip == null)
            {
#if UNITY_EDITOR
                _sharedEmptyClickClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(
                    "Assets/Invector-3rdPersonController/Shooter/Audio/Weapons/EmptyClip_A.mp3");
#endif
            }

            if (_sharedEmptyClickClip != null)
                weapon.emptyClip = _sharedEmptyClickClip;
        }

        /// <summary>
        /// Invector FinishReloadEffect calls PlayOneShot on reloadSource (defaults to the fire
        /// AudioSource). Holstered/preloaded copies often leave that source disabled, which logs
        /// "Can not play a disabled audio source" when a reload finishes. Re-enable for the drawn
        /// weapon so Invector reload SFX can play safely.
        /// </summary>
        private static void EnsureReloadAudioSource(vShooterWeapon weapon)
        {
            if (weapon == null)
                return;

            AudioSource reloadSource = weapon.reloadSource != null ? weapon.reloadSource : weapon.source;
            if (reloadSource == null)
                return;

            if (weapon.gameObject.activeInHierarchy && !reloadSource.enabled)
                reloadSource.enabled = true;
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