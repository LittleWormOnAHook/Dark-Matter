using System.Collections;
using System.Collections.Generic;
using Project.Data;
using Project.Inventory;
using Project.Player.Invector;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Spawns equipped melee visuals in hand when drawn; sheathes on the back when holstered (press weapon key again).
    /// </summary>
    [RequireComponent(typeof(EquipmentController))]
    public class EquippedItemVisual : MonoBehaviour
    {
        [SerializeField] private string defaultSocketName = "RightHand";
        [SerializeField] private string defaultSheatheSocketName = "Spine";

        [Header("Dual Holster")]
        [SerializeField] private Vector3 secondaryBackHolsterOffset = new Vector3(0.12f, 0.04f, 0.08f);

        [Header("Power Hit Charge Pose")]
        [SerializeField] private Vector3 powerChargeLocalOffset = new Vector3(0.04f, 0.1f, 0.16f);
        [SerializeField] private Vector3 powerChargeEuler = new Vector3(-42f, 18f, 6f);
        [SerializeField] private float powerChargeBlendTime = 0.2f;

        private EquipmentController equipment;
        private InventorySystem inventory;
        private ItemData currentHandItem;
        private WeaponHitbox activeHandHitbox;
        private readonly List<ItemData> currentBackItems = new List<ItemData>(4);
        private readonly List<ItemData> pendingBackItems = new List<ItemData>(4);
        private GameObject handInstance;
        private RangedCombatController rangedCombat;
        private readonly List<GameObject> backInstances = new List<GameObject>(4);
        private Quaternion handRestRotation = Quaternion.identity;
        private Vector3 handRestLocalPosition = Vector3.zero;
        private Vector3 handRestLocalScale = Vector3.one;
        private Vector3 swingEuler = new Vector3(-120f, 0f, 0f);
        private Coroutine swingRoutine;
        private Coroutine poseRoutine;
        private bool isPowerCharging;
        private RangedWeaponPoseMode currentPoseMode = RangedWeaponPoseMode.Holstered;
        private Transform cachedMuzzle;

        public RangedWeaponPoseMode CurrentPoseMode => currentPoseMode;

        private bool UsesInvectorWeaponBridge => PioneerInvectorBootstrap.IsInvectorPlayer(this);

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            inventory = GetComponent<InventorySystem>();
            rangedCombat = GetComponent<RangedCombatController>();
        }

        private void LateUpdate()
        {
            if (UsesInvectorWeaponBridge)
                return;

            UpdateRangedPoseMode();
        }

        private void OnEnable()
        {
            if (UsesInvectorWeaponBridge)
                return;

            if (equipment != null)
            {
                equipment.OnSelectedHotbarChanged += HandleSelectionChanged;
                equipment.OnToolbarSelectionChanged += HandleSelectionChanged;
            }
            RefreshHeldItem(force: true);
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.OnSelectedHotbarChanged -= HandleSelectionChanged;
                equipment.OnToolbarSelectionChanged -= HandleSelectionChanged;
            }

            StopHandPoseCoroutines();
            isPowerCharging = false;
        }

        private void HandleSelectionChanged(int _)
        {
            RefreshHeldItem(force: true);
        }

        private void HandleSelectionChanged()
        {
            RefreshHeldItem(force: true);
        }

        public WeaponHitbox ActiveHandHitbox => activeHandHitbox;

        public Transform GetMuzzleTransform()
        {
            if (handInstance == null)
                return null;

            if (cachedMuzzle != null)
                return cachedMuzzle;

            ItemData item = currentHandItem;
            string socketName = item != null && !string.IsNullOrWhiteSpace(item.muzzleSocketName)
                ? item.muzzleSocketName
                : "Muzzle";

            Transform muzzle = FindDeepChild(handInstance.transform, socketName);
            cachedMuzzle = muzzle;
            return muzzle != null ? muzzle : handInstance.transform;
        }

        private RangedWeaponPoseMode ResolvePoseMode()
        {
            if (handInstance == null || currentHandItem == null || !currentHandItem.IsRangedWeapon)
                return RangedWeaponPoseMode.Holstered;

            bool aiming = rangedCombat != null && rangedCombat.IsAiming;
            return aiming ? RangedWeaponPoseMode.Aiming : RangedWeaponPoseMode.HipReady;
        }

        private void UpdateRangedPoseMode()
        {
            RangedWeaponPoseMode mode = ResolvePoseMode();
            if (mode == currentPoseMode)
                return;

#if UNITY_EDITOR
            // Leave the live transform alone while paused so grip edits can be baked.
            if (UnityEditor.EditorApplication.isPaused)
                return;
#endif

            currentPoseMode = mode;
            ApplyPose(mode);
        }

        private void ApplyPose(RangedWeaponPoseMode mode)
        {
            if (handInstance == null || currentHandItem == null)
                return;

            Transform t = handInstance.transform;
            if (mode == RangedWeaponPoseMode.Aiming && currentHandItem.useAimHeldGrip)
            {
                Vector3 scale = currentHandItem.aimHeldLocalScale == Vector3.zero
                    ? handRestLocalScale
                    : currentHandItem.aimHeldLocalScale;
                t.localPosition = currentHandItem.aimHeldLocalPosition;
                t.localRotation = currentHandItem.useAimHeldLocalRotation
                    ? currentHandItem.aimHeldLocalRotation
                    : Quaternion.Euler(currentHandItem.aimHeldLocalEuler);
                t.localScale = scale;
                return;
            }

            t.localPosition = handRestLocalPosition;
            t.localRotation = handRestRotation;
            t.localScale = handRestLocalScale;
        }

        public void ForceRefresh()
        {
            if (UsesInvectorWeaponBridge)
                return;

            RefreshHeldItem(force: true);
        }

        public void ApplyBakedHandGrip(
            ItemData item,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            if (item == null || handInstance == null || currentHandItem != item)
                return;

            Transform t = handInstance.transform;
            t.localPosition = localPosition;
            t.localRotation = localRotation;
            t.localScale = localScale == Vector3.zero ? Vector3.one : localScale;

            handRestLocalPosition = t.localPosition;
            handRestRotation = t.localRotation;
            swingEuler = item.swingEulerAngles == Vector3.zero
                ? new Vector3(-120f, 0f, 0f)
                : item.swingEulerAngles;
        }

        public void ApplyBakedSheathedGrip(
            ItemData item,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            if (item == null)
                return;

            Vector3 scale = localScale == Vector3.zero ? Vector3.one : localScale;
            for (int i = 0; i < backInstances.Count; i++)
            {
                if (backInstances[i] == null || i >= currentBackItems.Count || currentBackItems[i] != item)
                    continue;

                Transform t = backInstances[i].transform;
                t.localPosition = localPosition;
                t.localRotation = localRotation;
                t.localScale = scale;
            }
        }

        private void RefreshHeldItem(bool force = false)
        {
            if (UsesInvectorWeaponBridge)
                return;

            ItemData handItem = ResolveHandItem();
            pendingBackItems.Clear();
            CollectBackItems(pendingBackItems);

            if (!force && handItem == currentHandItem && BackItemsEqual(pendingBackItems, currentBackItems))
                return;

            ClearHandVisual();
            ClearBackVisual();
            currentHandItem = handItem;
            currentBackItems.Clear();
            currentBackItems.AddRange(pendingBackItems);

            if (handItem != null)
                handInstance = SpawnVisual(handItem, isHand: true);

            activeHandHitbox = handInstance != null ? handInstance.GetComponent<WeaponHitbox>() : null;

            for (int i = 0; i < currentBackItems.Count; i++)
            {
                GameObject instance = SpawnVisual(currentBackItems[i], isHand: false, backHolsterIndex: i);
                if (instance != null)
                    backInstances.Add(instance);
            }
        }

        private ItemData ResolveHandItem()
        {
            if (equipment == null)
                return null;

            ItemData selected = equipment.ActiveToolItem;
            if (selected != null && selected.itemType == ItemType.Tool)
                return selected;

            selected = equipment.SelectedHotbarItem;
            if (selected != null && selected.itemType == ItemType.Tool)
                return selected;

            if (!equipment.IsWeaponDrawn)
                return null;

            if (!equipment.IsWeaponHotbarSlot(equipment.SelectedHotbarSlot))
                return null;

            ItemData item = equipment.DrawnWeaponItem;
            return item != null && item.IsEquippable ? item : null;
        }

        private void CollectBackItems(List<ItemData> results)
        {
            if (inventory == null || equipment == null)
                return;

            int activeHotbar = equipment.ActiveWeaponHotbarSlot;
            bool activeWeaponSelected = equipment.IsWeaponHotbarSlot(equipment.SelectedHotbarSlot);

            equipment.ForEachWeaponHotbarSlot(hotbarIndex =>
            {
                if (equipment.IsWeaponDrawn && activeWeaponSelected && hotbarIndex == activeHotbar)
                    return;

                ItemData weapon = equipment.GetHotbarItem(hotbarIndex);
                if (weapon == null || !weapon.IsEquippable)
                    return;

                for (int i = 0; i < results.Count; i++)
                {
                    if (results[i] == weapon)
                        return;
                }

                results.Add(weapon);
            });
        }

        private static bool BackItemsEqual(List<ItemData> a, List<ItemData> b)
        {
            if (a.Count != b.Count)
                return false;

            for (int i = 0; i < a.Count; i++)
            {
                if (a[i] != b[i])
                    return false;
            }

            return true;
        }

        private GameObject SpawnVisual(ItemData item, bool isHand, int backHolsterIndex = 0)
        {
            if (item.heldPrefab == null)
                return null;

            string socketName = isHand
                ? (string.IsNullOrWhiteSpace(item.equipSocketName) ? defaultSocketName : item.equipSocketName)
                : (string.IsNullOrWhiteSpace(item.sheatheSocketName) ? defaultSheatheSocketName : item.sheatheSocketName);

            Transform socket = FindDeepChild(transform, socketName);
            if (socket == null)
            {
                Debug.LogWarning($"EquippedItemVisual: could not find socket '{socketName}' under {name}.");
                return null;
            }

            GameObject instance = Instantiate(item.heldPrefab, socket);
            StripForHeld(instance, isHand, item);
            if (isHand && item.itemType == ItemType.MeleeWeapon)
                EnsureWeaponHitbox(instance, item);

            Transform t = instance.transform;
            if (isHand)
            {
                Vector3 scale = item.heldLocalScale == Vector3.zero ? Vector3.one : item.heldLocalScale;
                t.localPosition = item.heldLocalPosition;
                t.localRotation = item.useHeldLocalRotation
                    ? item.heldLocalRotation
                    : Quaternion.Euler(item.heldLocalEuler);
                t.localScale = scale;
                handRestLocalPosition = t.localPosition;
                handRestRotation = t.localRotation;
                handRestLocalScale = t.localScale;
                swingEuler = item.swingEulerAngles == Vector3.zero ? new Vector3(-120f, 0f, 0f) : item.swingEulerAngles;

                if (item.itemType == ItemType.RangedWeapon)
                {
                    EnsureMuzzleSocket(instance, item);
                    cachedMuzzle = null;
                    currentPoseMode = RangedWeaponPoseMode.HipReady;
                }
            }
            else
            {
                Vector3 scale = item.sheathedLocalScale == Vector3.zero ? Vector3.one : item.sheathedLocalScale;
                Vector3 position = item.sheathedLocalPosition;
                if (backHolsterIndex > 0)
                    position += secondaryBackHolsterOffset * backHolsterIndex;

                t.localPosition = position;
                t.localRotation = item.useSheathedLocalRotation
                    ? item.sheathedLocalRotation
                    : Quaternion.Euler(item.sheathedLocalEuler);
                t.localScale = scale;
            }

            return instance;
        }

        private void ClearHandVisual()
        {
            StopHandPoseCoroutines();
            isPowerCharging = false;
            activeHandHitbox = null;
            cachedMuzzle = null;
            currentPoseMode = RangedWeaponPoseMode.Holstered;

            if (handInstance != null)
            {
                Destroy(handInstance);
                handInstance = null;
            }
        }

        private void ClearBackVisual()
        {
            for (int i = 0; i < backInstances.Count; i++)
            {
                if (backInstances[i] != null)
                    Destroy(backInstances[i]);
            }

            backInstances.Clear();
        }

        private void StopHandPoseCoroutines()
        {
            if (poseRoutine != null)
            {
                StopCoroutine(poseRoutine);
                poseRoutine = null;
            }

            if (swingRoutine != null)
            {
                StopCoroutine(swingRoutine);
                swingRoutine = null;
            }
        }

        private Transform FindDeepChild(Transform parent, string childName)
        {
            if (parent == null)
                return null;

            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindDeepChild(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void StripForHeld(GameObject instance, bool isHand, ItemData item)
        {
            EquippedVisualMarker marker = instance.GetComponent<EquippedVisualMarker>();
            if (marker == null)
                marker = instance.AddComponent<EquippedVisualMarker>();

            marker.BindItem(item);

            SetLayerRecursively(instance, 0);

            foreach (ItemPickup pickup in instance.GetComponentsInChildren<ItemPickup>(true))
                Destroy(pickup);

            foreach (ResourceNode node in instance.GetComponentsInChildren<ResourceNode>(true))
                Destroy(node);

            if (!isHand)
            {
                foreach (WeaponHitbox hitbox in instance.GetComponentsInChildren<WeaponHitbox>(true))
                    Destroy(hitbox);
            }

            foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            {
                if (isHand && collider.GetComponentInParent<WeaponHitbox>() != null)
                    continue;

                collider.enabled = false;
            }

            foreach (Rigidbody body in instance.GetComponentsInChildren<Rigidbody>(true))
            {
                if (isHand && body.GetComponentInParent<WeaponHitbox>() != null)
                    continue;

                body.isKinematic = true;
                body.detectCollisions = false;
            }
        }

        private static void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            Transform root = obj.transform;
            for (int i = 0; i < root.childCount; i++)
                SetLayerRecursively(root.GetChild(i).gameObject, layer);
        }

        private void AlignMuzzleToBarrelTip(GameObject instance, ItemData item)
        {
            if (instance == null)
                return;

            string socketName = item != null && !string.IsNullOrWhiteSpace(item.muzzleSocketName)
                ? item.muzzleSocketName
                : "Muzzle";

            Transform muzzle = FindDeepChild(instance.transform, socketName);
            if (muzzle == null)
            {
                GameObject muzzleObject = new GameObject(socketName);
                muzzleObject.transform.SetParent(instance.transform, false);
                muzzle = muzzleObject.transform;
            }

            muzzle.localPosition = ComputeBarrelTipLocalPosition(instance);
            muzzle.localRotation = Quaternion.identity;
        }

        private static Vector3 ComputeBarrelTipLocalPosition(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return new Vector3(0f, 0.05f, 0.35f);

            Transform root = instance.transform;
            float maxForward = float.NegativeInfinity;
            const float forwardPlaneTolerance = 0.03f;
            float sumX = 0f;
            float sumY = 0f;
            int forwardCount = 0;

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                for (int xi = -1; xi <= 1; xi += 2)
                {
                    for (int yi = -1; yi <= 1; yi += 2)
                    {
                        for (int zi = -1; zi <= 1; zi += 2)
                        {
                            Vector3 cornerWorld = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                            float localZ = root.InverseTransformPoint(cornerWorld).z;
                            maxForward = Mathf.Max(maxForward, localZ);
                        }
                    }
                }
            }

            if (maxForward <= float.NegativeInfinity)
                return new Vector3(0f, 0.05f, 0.35f);

            for (int r = 0; r < renderers.Length; r++)
            {
                Renderer renderer = renderers[r];
                if (renderer == null)
                    continue;

                Bounds bounds = renderer.bounds;
                Vector3 center = bounds.center;
                Vector3 extents = bounds.extents;

                for (int xi = -1; xi <= 1; xi += 2)
                {
                    for (int yi = -1; yi <= 1; yi += 2)
                    {
                        for (int zi = -1; zi <= 1; zi += 2)
                        {
                            Vector3 cornerWorld = center + Vector3.Scale(extents, new Vector3(xi, yi, zi));
                            Vector3 cornerLocal = root.InverseTransformPoint(cornerWorld);
                            if (cornerLocal.z < maxForward - forwardPlaneTolerance)
                                continue;

                            sumX += cornerLocal.x;
                            sumY += cornerLocal.y;
                            forwardCount++;
                        }
                    }
                }
            }

            if (forwardCount <= 0)
                return new Vector3(0f, 0.05f, maxForward + 0.02f);

            return new Vector3(sumX / forwardCount, sumY / forwardCount, maxForward + 0.02f);
        }

        private void EnsureMuzzleSocket(GameObject instance, ItemData item)
        {
            if (instance == null)
                return;

            string socketName = item != null && !string.IsNullOrWhiteSpace(item.muzzleSocketName)
                ? item.muzzleSocketName
                : "Muzzle";

            // Authored prefab socket is authoritative; only fall back to the
            // renderer-bounds heuristic for prefabs that were never set up.
            Transform authored = FindDeepChild(instance.transform, socketName);
            if (authored != null)
                return;

            AlignMuzzleToBarrelTip(instance, item);
        }

        private void EnsureWeaponHitbox(GameObject instance, ItemData item = null)
        {
            if (instance == null)
                return;

            WeaponHitbox hitbox = instance.GetComponent<WeaponHitbox>();
            if (hitbox == null)
                hitbox = instance.AddComponent<WeaponHitbox>();

            hitbox.Configure(transform, item);
        }

        public void PlaySwing(float duration)
        {
            if (!isActiveAndEnabled || handInstance == null)
                return;

            StopHandPoseCoroutines();
            isPowerCharging = false;
            ResetHandRestPoseImmediate();
            swingRoutine = StartCoroutine(SwingRoutine(Mathf.Clamp(duration, 0.15f, 0.6f)));
        }

        public void BeginPowerHitCharge()
        {
            if (!isActiveAndEnabled || handInstance == null || isPowerCharging)
                return;

            isPowerCharging = true;
            StopHandPoseCoroutines();

            Vector3 chargePosition = handRestLocalPosition + powerChargeLocalOffset;
            Quaternion chargeRotation = handRestRotation * Quaternion.Euler(powerChargeEuler);
            poseRoutine = StartCoroutine(BlendHandPose(
                handInstance.transform.localPosition,
                handInstance.transform.localRotation,
                chargePosition,
                chargeRotation,
                powerChargeBlendTime));
        }

        public void CancelPowerHitCharge()
        {
            if (!isPowerCharging || handInstance == null)
                return;

            isPowerCharging = false;
            RestoreHandRestPose(powerChargeBlendTime * 0.5f);
        }

        public void CancelPowerHitChargeImmediate()
        {
            isPowerCharging = false;
            StopHandPoseCoroutines();
            ResetHandRestPoseImmediate();
        }

        public void ReleasePowerHitCharge(float duration)
        {
            if (!isActiveAndEnabled || handInstance == null)
                return;

            isPowerCharging = false;
            StopHandPoseCoroutines();
            swingRoutine = StartCoroutine(PowerSwingFromChargeRoutine(Mathf.Clamp(duration, 0.2f, 0.9f)));
        }

        private void RestoreHandRestPose(float blendTime)
        {
            if (!isActiveAndEnabled || handInstance == null)
                return;

            if (poseRoutine != null)
                StopCoroutine(poseRoutine);

            Transform t = handInstance.transform;
            poseRoutine = StartCoroutine(BlendHandPose(
                t.localPosition,
                t.localRotation,
                handRestLocalPosition,
                handRestRotation,
                blendTime));
        }

        private void ResetHandRestPoseImmediate()
        {
            if (handInstance == null)
                return;

            Transform t = handInstance.transform;
            t.localPosition = handRestLocalPosition;
            t.localRotation = handRestRotation;
        }

        private IEnumerator BlendHandPose(
            Vector3 fromPosition,
            Quaternion fromRotation,
            Vector3 toPosition,
            Quaternion toRotation,
            float duration)
        {
            duration = Mathf.Max(0.01f, duration);
            float elapsed = 0f;

            while (elapsed < duration)
            {
                if (handInstance == null)
                    yield break;

                Transform t = handInstance.transform;
                elapsed += Time.deltaTime;
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / duration);
                t.localPosition = Vector3.Lerp(fromPosition, toPosition, blend);
                t.localRotation = Quaternion.Slerp(fromRotation, toRotation, blend);
                yield return null;
            }

            if (handInstance == null)
                yield break;

            handInstance.transform.localPosition = toPosition;
            handInstance.transform.localRotation = toRotation;
            poseRoutine = null;
        }

        private IEnumerator PowerSwingFromChargeRoutine(float duration)
        {
            if (handInstance == null)
                yield break;

            Transform t = handInstance.transform;
            Vector3 chargePosition = handRestLocalPosition + powerChargeLocalOffset;
            Quaternion chargeRotation = handRestRotation * Quaternion.Euler(powerChargeEuler);
            Vector3 strikePosition = handRestLocalPosition + new Vector3(0.02f, -0.03f, 0.08f);
            Quaternion strikeRotation = handRestRotation * Quaternion.Euler(Vector3.Scale(swingEuler, new Vector3(1.25f, 1f, 1f)));

            t.localPosition = chargePosition;
            t.localRotation = chargeRotation;

            float strikeTime = duration * 0.35f;
            float recoverTime = duration * 0.65f;

            float elapsed = 0f;
            while (elapsed < strikeTime)
            {
                if (handInstance == null)
                    yield break;

                elapsed += Time.deltaTime;
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / strikeTime);
                t.localPosition = Vector3.Lerp(chargePosition, strikePosition, blend);
                t.localRotation = Quaternion.Slerp(chargeRotation, strikeRotation, blend);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < recoverTime)
            {
                if (handInstance == null)
                    yield break;

                elapsed += Time.deltaTime;
                float blend = Mathf.SmoothStep(0f, 1f, elapsed / recoverTime);
                t.localPosition = Vector3.Lerp(strikePosition, handRestLocalPosition, blend);
                t.localRotation = Quaternion.Slerp(strikeRotation, handRestRotation, blend);
                yield return null;
            }

            if (handInstance != null)
            {
                t.localPosition = handRestLocalPosition;
                t.localRotation = handRestRotation;
            }

            swingRoutine = null;
        }

        private IEnumerator SwingRoutine(float duration)
        {
            if (handInstance == null)
                yield break;

            Transform t = handInstance.transform;
            Quaternion start = handRestRotation;
            Quaternion peak = handRestRotation * Quaternion.Euler(swingEuler);
            float half = Mathf.Max(0.05f, duration * 0.5f);

            float elapsed = 0f;
            while (elapsed < half)
            {
                if (handInstance == null)
                    yield break;

                elapsed += Time.deltaTime;
                t.localRotation = Quaternion.Slerp(start, peak, elapsed / half);
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < half)
            {
                if (handInstance == null)
                    yield break;

                elapsed += Time.deltaTime;
                t.localRotation = Quaternion.Slerp(peak, start, elapsed / half);
                yield return null;
            }

            if (handInstance != null)
                t.localRotation = handRestRotation;

            swingRoutine = null;
        }
    }
}
