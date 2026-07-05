using System;
using System.Collections;
using System.Collections.Generic;
using Invector.vCharacterController;
using Invector.vMelee;
using Invector.vShooter;
using Project.Data;
using Project.Pioneers;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Companions.Invector
{
    /// <summary>
    /// Binds roster loadout to pre-authored Drawn/Holstered slots on the companion prefab.
    /// </summary>
    [DisallowMultipleComponent]
    public class CompanionInvectorLoadoutBridge : MonoBehaviour
    {
        [SerializeField] private bool hideUnusedSlotsAtRuntime = true;

        private vShooterManager _shooterManager;
        private vMeleeManager _meleeManager;
        private vThirdPersonController _controller;
        private Animator _animator;

        private readonly List<PreloadedSlotPair> _preloadedSlots = new List<PreloadedSlotPair>(16);
        private readonly HashSet<string> _keptSlotKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        private string _weaponItemId = string.Empty;
        private ItemData _activeItem;
        private GameObject _activeDrawnInstance;
        private GameObject _activeHolsteredInstance;
        private bool _drawn;
        private bool _slotsDiscovered;
        private string _prunedRecordId;
        private GameObject _fallbackDrawn;
        private GameObject _fallbackHolstered;
        private string _fallbackWeaponId = string.Empty;
        private int _onlyArmsLayer = -1;
        private int _shotLayer = -1;

        public ItemData ActiveItem => _activeItem;
        public bool IsDrawn => _drawn;

        private static readonly int UpperBodyIdHash = Animator.StringToHash("UpperBody_ID");
        private static readonly int MoveSetIdHash = Animator.StringToHash("MoveSet_ID");
        private static readonly int IsAimingHash = Animator.StringToHash("IsAiming");
        private static readonly int CanAimHash = Animator.StringToHash("CanAim");

        private sealed class PreloadedSlotPair
        {
            public string slotKey;
            public GameObject drawnInstance;
            public GameObject holsteredInstance;
        }

        private void Awake()
        {
            _shooterManager = GetComponent<vShooterManager>();
            _meleeManager = GetComponent<vMeleeManager>();
            _controller = GetComponent<vThirdPersonController>();
            _animator = _controller != null ? _controller.animator : GetComponentInChildren<Animator>(true);
            if (_animator != null)
            {
                _onlyArmsLayer = _animator.GetLayerIndex("OnlyArms");
                _shotLayer = _animator.GetLayerIndex("Shot");
            }

            DiscoverPreloadedSlots();
            HideAllPreloadedSlots();
            ClearInvectorWeapons();
            ResetUnarmedAnimatorState();
        }

        private void Start()
        {
            HideAllPreloadedSlots();
            ClearInvectorWeapons();
            ResetUnarmedAnimatorState();
            StartCoroutine(RediscoverSlotsAfterInit());
        }

        private IEnumerator RediscoverSlotsAfterInit()
        {
            yield return null;
            InvalidateSlotCache();
            if (_activeItem != null)
                RefreshWeaponVisualState();
        }

        public void ApplyLoadout(SkilledPioneerRecord record, bool drawn)
        {
            if (record == null)
                return;

            EnsureSlotsDiscovered();
            _weaponItemId = record.weaponItemId ?? string.Empty;
            _activeItem = ItemRegistry.Resolve(_weaponItemId);
            _drawn = drawn;

            if (hideUnusedSlotsAtRuntime && _prunedRecordId != record.id)
            {
                PruneUnusedSlots(record);
                _prunedRecordId = record.id;
            }

            RefreshWeaponVisualState();
        }

        public void ApplyWeapon(string weaponItemId, bool? drawnOverride = null)
        {
            EnsureSlotsDiscovered();
            bool weaponChanged = !string.Equals(_weaponItemId, weaponItemId, StringComparison.OrdinalIgnoreCase);
            if (weaponChanged)
                ClearFallbackVisuals();

            _weaponItemId = weaponItemId ?? string.Empty;
            _activeItem = ItemRegistry.Resolve(_weaponItemId);
            if (drawnOverride.HasValue)
                _drawn = drawnOverride.Value;
            else if (weaponChanged)
                _drawn = false;
            RefreshWeaponVisualState();
        }

        public void SetDrawn(bool drawn)
        {
            if (_drawn == drawn)
                return;

            _drawn = drawn;
            RefreshWeaponVisualState();
        }

        /// <summary>
        /// Deactivates preloaded slot objects not referenced by the pioneer's current loadout.
        /// </summary>
        public void PruneUnusedSlots(SkilledPioneerRecord record)
        {
            if (record == null)
                return;

            EnsureSlotsDiscovered();
            BuildKeptSlotKeys(record);

            for (int i = _preloadedSlots.Count - 1; i >= 0; i--)
            {
                PreloadedSlotPair slot = _preloadedSlots[i];
                if (slot == null || _keptSlotKeys.Contains(slot.slotKey))
                    continue;

                if (slot.drawnInstance != null)
                    slot.drawnInstance.SetActive(false);
                if (slot.holsteredInstance != null)
                    slot.holsteredInstance.SetActive(false);
            }
        }

        private void EnsureSlotsDiscovered()
        {
            if (_slotsDiscovered)
                return;

            DiscoverPreloadedSlots();
        }

        private void DiscoverPreloadedSlots()
        {
            _preloadedSlots.Clear();

            var drawnByKey = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);
            var holsteredByKey = new Dictionary<string, GameObject>(StringComparer.OrdinalIgnoreCase);

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform child = children[i];
                if (child == null)
                    continue;

                string name = child.name;
                if (name.StartsWith("Drawn_", StringComparison.Ordinal))
                {
                    string key = name.Substring("Drawn_".Length);
                    if (!string.IsNullOrWhiteSpace(key))
                        drawnByKey[key] = child.gameObject;
                }
                else if (name.StartsWith("Holstered_", StringComparison.Ordinal))
                {
                    string key = name.Substring("Holstered_".Length);
                    if (!string.IsNullOrWhiteSpace(key))
                        holsteredByKey[key] = child.gameObject;
                }
            }

            var allKeys = new HashSet<string>(drawnByKey.Keys, StringComparer.OrdinalIgnoreCase);
            foreach (string key in holsteredByKey.Keys)
                allKeys.Add(key);

            foreach (string key in allKeys)
            {
                drawnByKey.TryGetValue(key, out GameObject drawn);
                holsteredByKey.TryGetValue(key, out GameObject holstered);
                if (drawn == null && holstered == null)
                    continue;

                _preloadedSlots.Add(new PreloadedSlotPair
                {
                    slotKey = key,
                    drawnInstance = drawn,
                    holsteredInstance = holstered
                });
            }

            _slotsDiscovered = true;
        }

        private void BuildKeptSlotKeys(SkilledPioneerRecord record)
        {
            _keptSlotKeys.Clear();
            AddKeptSlotKeyForItem(ItemRegistry.Resolve(record.weaponItemId));
            AddKeptSlotKeyForItem(ItemRegistry.Resolve(record.toolItemId));

            if (record.assignedSkillIds != null)
            {
                for (int i = 0; i < record.assignedSkillIds.Length; i++)
                    AddKeptSlotKeyForItem(ItemRegistry.Resolve(record.assignedSkillIds[i]));
            }
        }

        private void AddKeptSlotKeyForItem(ItemData item)
        {
            if (item == null)
                return;

            _keptSlotKeys.Add(PioneerInvectorWeaponBridge.MakeSafeSlotName(item.itemName, item.name));
        }

        private void RefreshWeaponVisualState()
        {
            HideAllPreloadedSlots();
            ClearInvectorWeapons();

            _activeDrawnInstance = null;
            _activeHolsteredInstance = null;

            if (_activeItem == null)
            {
                ClearFallbackVisuals();
                ResetUnarmedAnimatorState();
                return;
            }

            _activeDrawnInstance = ResolveDrawnSlot(_activeItem);
            _activeHolsteredInstance = ResolveHolsteredSlot(_activeItem);

            if (_drawn)
                ShowDrawnSlot(_activeItem, _activeDrawnInstance, _activeHolsteredInstance);
            else
                ShowHolsteredSlot(_activeItem, _activeDrawnInstance, _activeHolsteredInstance);
        }

        private void ShowDrawnSlot(ItemData item, GameObject drawn, GameObject holstered)
        {
            if (item == null)
                return;

            if (holstered != null)
                holstered.SetActive(false);

            if (drawn == null)
            {
                drawn = EnsureFallbackVisual(item, drawn: true);
                if (drawn == null)
                {
                    ResetUnarmedAnimatorState();
                    return;
                }

                _activeDrawnInstance = drawn;
            }

            if (item.itemType == ItemType.MeleeWeapon)
                PioneerInvectorWeaponBridge.PrepareDrawnMeleeSlot(drawn, item);
            else if (item.IsRangedWeapon)
                PioneerInvectorWeaponBridge.PrepareDrawnRangedSlot(drawn, item);

            drawn.SetActive(true);
            PioneerInvectorWeaponBridge.ApplyItemStatsToInstance(item, drawn);

            if (item.itemType == ItemType.MeleeWeapon)
                EquipMelee(drawn);
            else if (item.IsRangedWeapon)
                EquipRanged(drawn);
        }

        private GameObject ResolveDrawnSlot(ItemData item)
        {
            if (item == null)
                return null;

            GameObject found = TryFindPreloadedDrawn(item);
            if (found != null)
                return found;

            InvalidateSlotCache();
            return TryFindPreloadedDrawn(item);
        }

        private GameObject ResolveHolsteredSlot(ItemData item)
        {
            if (item == null)
                return null;

            GameObject found = TryFindPreloadedHolstered(item);
            if (found != null)
                return found;

            InvalidateSlotCache();
            return TryFindPreloadedHolstered(item);
        }

        private GameObject TryFindPreloadedDrawn(ItemData item)
        {
            PreloadedSlotPair pair = FindSlotPair(item);
            if (pair?.drawnInstance != null)
                return pair.drawnInstance;

            return PioneerInvectorWeaponBridge.FindPreloadedDrawnSlot(transform, item);
        }

        private GameObject TryFindPreloadedHolstered(ItemData item)
        {
            PreloadedSlotPair pair = FindSlotPair(item);
            if (pair?.holsteredInstance != null)
                return pair.holsteredInstance;

            return PioneerInvectorWeaponBridge.FindPreloadedHolsteredSlot(transform, item);
        }

        private void InvalidateSlotCache()
        {
            _slotsDiscovered = false;
            _preloadedSlots.Clear();
            DiscoverPreloadedSlots();
        }

        private PreloadedSlotPair FindSlotPair(ItemData item)
        {
            if (item == null)
                return null;

            string key = PioneerInvectorWeaponBridge.MakeSafeSlotName(item.itemName, item.name);
            for (int i = 0; i < _preloadedSlots.Count; i++)
            {
                PreloadedSlotPair slot = _preloadedSlots[i];
                if (slot != null && string.Equals(slot.slotKey, key, StringComparison.OrdinalIgnoreCase))
                    return slot;
            }

            return null;
        }

        private void ShowHolsteredSlot(ItemData item, GameObject drawn, GameObject holstered)
        {
            if (item == null)
                return;

            if (drawn != null)
                drawn.SetActive(false);

            ClearInvectorWeapons();
            ResetUnarmedAnimatorState();

            if (holstered == null)
            {
                holstered = EnsureFallbackVisual(item, drawn: false);
                if (holstered == null)
                    return;

                _activeHolsteredInstance = holstered;
            }

            PioneerInvectorWeaponBridge.PrepareHolsteredVisualSlot(holstered, item);
            holstered.SetActive(true);
        }

        private void HideAllPreloadedSlots()
        {
            for (int i = 0; i < _preloadedSlots.Count; i++)
            {
                PreloadedSlotPair slot = _preloadedSlots[i];
                if (slot == null)
                    continue;

                if (slot.drawnInstance != null)
                    slot.drawnInstance.SetActive(false);
                if (slot.holsteredInstance != null)
                    slot.holsteredInstance.SetActive(false);
            }
        }

        private void EquipRanged(GameObject instance)
        {
            if (_meleeManager != null)
            {
                _meleeManager.SetRightWeapon((GameObject)null);
                _meleeManager.SetLeftWeapon((GameObject)null);
            }

            ApplyInfiniteAmmo(instance);
            EnsureFriendlyFireIgnoreRules(instance);

            if (_shooterManager != null)
                _shooterManager.SetRightWeapon(instance);

            ApplyRangedArmsPose();
        }

        /// <summary>
        /// Companions never manage ammo pools; their ranged weapons fire and reload freely.
        /// </summary>
        private static void ApplyInfiniteAmmo(GameObject instance)
        {
            if (instance == null)
                return;

            foreach (vShooterWeapon weapon in instance.GetComponentsInChildren<vShooterWeapon>(true))
            {
                if (weapon == null)
                    continue;

                weapon.isInfinityAmmo = true;
                weapon.dontUseReload = true;
                weapon.ammo = weapon.clipSize > 0 ? weapon.clipSize : 999;
            }
        }

        private void EnsureFriendlyFireIgnoreRules(GameObject instance)
        {
            if (_shooterManager != null && !_shooterManager.ignoreTags.Contains("Player"))
                _shooterManager.ignoreTags.Add("Player");

            if (instance == null)
                return;

            foreach (vShooterWeapon weapon in instance.GetComponentsInChildren<vShooterWeapon>(true))
            {
                if (weapon == null || weapon.ignoreTags.Contains("Player"))
                    continue;

                weapon.ignoreTags.Add("Player");
            }
        }

        private void EquipMelee(GameObject instance)
        {
            if (_shooterManager != null)
                _shooterManager.SetRightWeapon((GameObject)null);

            if (_meleeManager != null)
                _meleeManager.SetRightWeapon(instance);

            ClearShooterArmsPose();
        }

        /// <summary>
        /// The ShooterMelee animator ships with OnlyArms/Shot layers at weight 1; the player's
        /// input script manages them per-frame, companions must set them explicitly.
        /// </summary>
        private void ApplyRangedArmsPose()
        {
            SyncRangedAimPose(aiming: false);
        }

        /// <summary>
        /// Keeps the upper-body aim pose active while a companion holds a drawn ranged weapon in combat.
        /// </summary>
        public void SyncRangedAimPose(bool aiming)
        {
            if (_animator == null)
                return;

            if (_onlyArmsLayer >= 0)
                _animator.SetLayerWeight(_onlyArmsLayer, aiming ? 1f : 0f);

            if (_shotLayer >= 0)
                _animator.SetLayerWeight(_shotLayer, 0f);

            if (!aiming)
            {
                _animator.SetFloat(UpperBodyIdHash, 0f);
                _animator.SetBool(CanAimHash, false);
                _animator.SetBool(IsAimingHash, false);
                return;
            }

            if (_shooterManager != null)
            {
                _animator.SetFloat(UpperBodyIdHash, _shooterManager.GetUpperBodyID());
                _animator.SetFloat(MoveSetIdHash, _shooterManager.GetMoveSetID());
            }

            _animator.SetBool(CanAimHash, true);
            _animator.SetBool(IsAimingHash, true);
        }

        /// <summary>
        /// Briefly enables the Shot layer for the fire animation blend.
        /// </summary>
        public void PulseRangedFirePose()
        {
            if (_animator == null)
                return;

            SyncRangedAimPose(aiming: true);

            if (_shotLayer >= 0)
                _animator.SetLayerWeight(_shotLayer, 1f);

            if (_shooterManager != null)
                _animator.SetFloat(Animator.StringToHash("Shot_ID"), _shooterManager.GetShotID());
        }

        private void ClearShooterArmsPose()
        {
            if (_animator == null)
                return;

            if (_onlyArmsLayer >= 0)
                _animator.SetLayerWeight(_onlyArmsLayer, 0f);
            if (_shotLayer >= 0)
                _animator.SetLayerWeight(_shotLayer, 0f);
        }

        private void ClearInvectorWeapons()
        {
            if (_shooterManager != null)
                _shooterManager.SetRightWeapon((GameObject)null);

            if (_meleeManager != null)
            {
                _meleeManager.SetRightWeapon((GameObject)null);
                _meleeManager.SetLeftWeapon((GameObject)null);
            }
        }

        private void ResetUnarmedAnimatorState()
        {
            _controller?.ResetInputAnimatorParameters();

            if (_animator == null)
                return;

            int upperBodyId = Animator.StringToHash("UpperBody_ID");
            _animator.SetFloat(upperBodyId, 0f);
            _animator.SetBool(IsAimingHash, false);
            _animator.SetBool(CanAimHash, false);
            ClearShooterArmsPose();
        }

        private void ClearFallbackVisuals()
        {
            if (_fallbackDrawn != null)
                Destroy(_fallbackDrawn);
            if (_fallbackHolstered != null)
                Destroy(_fallbackHolstered);

            _fallbackDrawn = null;
            _fallbackHolstered = null;
            _fallbackWeaponId = string.Empty;
        }

        private GameObject EnsureFallbackVisual(ItemData item, bool drawn)
        {
            if (item == null)
                return null;

            string weaponId = item.name ?? string.Empty;
            if (!string.Equals(_fallbackWeaponId, weaponId, StringComparison.OrdinalIgnoreCase))
            {
                ClearFallbackVisuals();
                _fallbackWeaponId = weaponId;
            }

            ref GameObject cache = ref (drawn ? ref _fallbackDrawn : ref _fallbackHolstered);
            if (cache != null)
                return cache;

            GameObject prefab = ResolveFallbackPrefab(item);
            if (prefab == null)
                return null;

            Transform socket = ResolveFallbackSocket(item, drawn);
            if (socket == null)
                return null;

            cache = Instantiate(prefab, socket);
            ApplyFallbackTransform(cache.transform, item, drawn);

            if (drawn)
            {
                if (item.itemType == ItemType.MeleeWeapon)
                    PioneerInvectorWeaponBridge.PrepareDrawnMeleeSlot(cache, item);
                else if (item.IsRangedWeapon)
                    PioneerInvectorWeaponBridge.PrepareDrawnRangedSlot(cache, item);
            }
            else
            {
                PioneerInvectorWeaponBridge.PrepareHolsteredVisualSlot(cache, item);
            }

            cache.SetActive(false);
            return cache;
        }

        private static GameObject ResolveFallbackPrefab(ItemData item)
        {
            if (item == null)
                return null;

            if (item.invectorWeaponPrefab != null)
                return item.invectorWeaponPrefab;

            if (item.heldPrefab != null)
                return item.heldPrefab;

            return item.worldPrefab;
        }

        private Transform ResolveFallbackSocket(ItemData item, bool drawn)
        {
            Transform modelRoot = transform.Find("ProjectUnityCharacter");
            if (modelRoot == null)
                modelRoot = transform;

            string socketName = drawn
                ? (string.IsNullOrWhiteSpace(item.equipSocketName) ? "RightHand" : item.equipSocketName)
                : PioneerInvectorWeaponBridge.ResolveHolsterSocketName(item);

            Transform socket = FindDeepChild(modelRoot, socketName);
            return socket != null ? socket : modelRoot;
        }

        private static void ApplyFallbackTransform(Transform instance, ItemData item, bool drawn)
        {
            if (instance == null || item == null)
                return;

            if (drawn)
            {
                Vector3 scale = item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale;
                instance.localPosition = item.heldLocalPosition;
                instance.localRotation = item.useHeldLocalRotation
                    ? item.heldLocalRotation
                    : Quaternion.Euler(item.heldLocalEuler);
                instance.localScale = scale;
                return;
            }

            Vector3 holsterScale = item.sheathedLocalScale == Vector3.zero ? Vector3.one : item.sheathedLocalScale;
            instance.localPosition = item.sheathedLocalPosition;
            instance.localRotation = item.useSheathedLocalRotation
                ? item.sheathedLocalRotation
                : Quaternion.Euler(item.sheathedLocalEuler);
            instance.localScale = holsterScale;
        }

        private static Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null || string.IsNullOrWhiteSpace(childName))
                return null;

            if (parent.name.Equals(childName, StringComparison.OrdinalIgnoreCase))
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
