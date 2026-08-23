using Project.Audio;
using Project.Core;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.Player;
using Project.Player.Invector;
using Project.Progression;
using Project.Rendering;
using Project.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Secondary mining multi-tool scanner. While the mining tool is drawn, hold F (or LB) to
    /// force aim and scan a ResourceNode within <see cref="DMIMiningController.MaxScanDistance"/>
    /// for 5 seconds to identify it (gated by Mining / Harvesting skill rank).
    /// Already-identified types cannot be re-scanned.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(610)]
    public class DMIMiningResourceScanner : MonoBehaviour
    {
        private const float ScanDurationSeconds = 5f;
        private const float ScanHighlightAlpha = 0.32f;
        private const float ToastCooldownSeconds = 1.25f;
        private const float ScanLoopVolume = 0.4f;
        private const float ScanLoopPitch = 1.18f;
        private const string ScanConeFieldObjectName = "Scan Cone Field";
        private const string ScanLoopResourcesPath = "Audio/Mining_Resource_Scan_Loop";
        private const string ScanLoopAssetPath =
            "Assets/_Project/Resources/Audio/Mining_Resource_Scan_Loop.wav";

        private const float ScanLockBreakDegrees = 18f;

        private static readonly Color ScanOutlineColor = new Color(0.45f, 0.85f, 1f, 1f);

        [SerializeField] private LayerMask resourceLayer = ~0;
        [SerializeField] private float acquireRayRadius = 0.45f;
        [Tooltip("Optional. If empty, uses Scan Cone Field under Drawn_DM_Mining_Tool/renderer/muzzle.")]
        [SerializeField] private GameObject scanCone;
        [Tooltip("Optional override. Prefer Mining Resource Scan Audio on DM_Mining_Tool ItemData. Else Resources/Audio/Mining_Resource_Scan_Loop.")]
        [SerializeField] private AudioClip scanLoopClip;
        [Tooltip("Optional override. Prefer miningScanSuccessSound on the drawn mining tool ItemData.")]
        [SerializeField] private AudioClip scanSuccessClip;
        [Tooltip("Optional override. Prefer miningScanDeniedSound on the drawn mining tool ItemData.")]
        [SerializeField] private AudioClip scanDeniedClip;

        private EquipmentController equipment;
        private PlayerController player;
        private PioneerInvectorWeaponBridge weaponBridge;
        private PioneerInvectorInputBridge inputBridge;
        private Camera gameplayCamera;

        private ResourceNode scanTarget;
        private Vector3 scanPoint;
        private float scanProgress;
        private bool isScanning;
        private OutlineController highlightedOutline;
        private OutlineController addedOutlineController;
        private WorldNodeProgressBar progressBar;
        private Transform muzzleTransform;
        private AudioSource scanAudio;
        private AudioClip cachedDefaultScanLoop;
        private float nextToastTime;
        private string cachedProgressLabel = "Scanning…  0%";
        private int lastProgressPercentShown = -1;
        private ResourceNode lastProgressLabelNode;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            player = GetComponent<PlayerController>();
            weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
            inputBridge = GetComponent<PioneerInvectorInputBridge>();
            SetScanConeVisible(false);
        }

        private void OnDisable()
        {
            SetMiningScanAimHold(false);
            CancelScan(clearProgressUi: true);
            MiningToolResourceCollisionUtility.ClearIgnoredResource();
        }

        private void Update()
        {
            ItemData tool = ResolveDrawnMiningTool();
            if (tool == null || IsInputBlocked())
            {
                SetMiningScanAimHold(false);
                CancelScan(clearProgressUi: true);
                return;
            }

            bool holdF = IsScanHeld();
            if (!holdF)
            {
                SetMiningScanAimHold(false);
                CancelScan(clearProgressUi: true);
                return;
            }

            // F / LB forces aim on the drawn mining tool — no need to be aiming first.
            SetMiningScanAimHold(true);

            if (!TryAcquireTarget(out ResourceNode node, out Vector3 point))
            {
                if (isScanning && scanTarget != null && TryMaintainScanLock(out point))
                {
                    node = scanTarget;
                }
                else
                {
                    CancelScan(clearProgressUi: true);
                    return;
                }
            }

            if (!IsScanStandoffOk(node))
            {
                CancelScan(clearProgressUi: true);
                if (WasScanPressedThisFrame() || holdF)
                    PlayDeniedFeedback("Move into scan range (2–6 m)");
                return;
            }

            if (ResourceIdentificationRegistry.IsIdentified(node.resourceItem))
            {
                CancelScan(clearProgressUi: true);
                if (WasScanPressedThisFrame())
                    PlayDeniedFeedback("Already identified");
                return;
            }

            if (scanTarget != node)
            {
                BeginScan(node, point);
            }
            else
            {
                RefreshScanLockPoint(node);
                isScanning = true;
                scanProgress += Time.deltaTime;
                EnsureScanAudio();
                UpdateProgressUi();

                if (scanProgress >= ScanDurationSeconds)
                    CompleteScan();
            }
        }

        private void LateUpdate()
        {
            if (!isScanning || scanTarget == null)
                return;

            UpdateScanVisuals();
        }

        private void RefreshScanLockPoint(ResourceNode node)
        {
            if (node == null)
                return;

            Vector3 aimDir = ResolveAimDirection(out Vector3 aimOrigin);
            if (DMIMiningController.TryGetLockPointOnNode(
                    node,
                    aimOrigin,
                    aimDir,
                    DMIMiningController.MaxScanDistance,
                    out Vector3 point))
            {
                scanPoint = point;
            }
        }

        private bool TryMaintainScanLock(out Vector3 point)
        {
            point = scanPoint;
            ResourceNode node = scanTarget;
            if (node == null || !IsScanStandoffOk(node))
                return false;

            if (Vector3.Distance(transform.position, node.GetClosestPoint(transform.position)) > DMIMiningController.MaxScanDistance)
                return false;

            Vector3 aimDir = ResolveAimDirection(out Vector3 aimOrigin);
            if (DMIMiningController.TryGetLockPointOnNode(
                    node,
                    aimOrigin,
                    aimDir,
                    DMIMiningController.MaxScanDistance,
                    out point))
            {
                scanPoint = point;
                return true;
            }

            Vector3 toNode = node.GetNodeCenter() - aimOrigin;
            if (toNode.sqrMagnitude < 0.0001f)
                return false;

            if (Vector3.Angle(aimDir, toNode.normalized) <= ScanLockBreakDegrees)
            {
                scanPoint = node.GetNodeCenter();
                point = scanPoint;
                return true;
            }

            return false;
        }

        private bool IsScanStandoffOk(ResourceNode node)
        {
            if (node == null)
                return false;

            // Closest-point distance: 2m means "do not stand inside" the volume, 6m is max lock.
            float dist = Vector3.Distance(transform.position, node.GetClosestPoint(transform.position));
            return dist >= DMIMiningController.MinScanStandoffDistance
                && dist <= DMIMiningController.MaxScanDistance;
        }

        private void BeginScan(ResourceNode node, Vector3 point)
        {
            // Deny immediately if skill rank is insufficient — don't wait for the full 5s timer.
            ItemData item = node.resourceItem;
            if (item != null)
            {
                MineHarvestGatherKind gatherKind = ResolveGatherKind(item, node);
                int requiredRank = ResolveRequiredRank(item);
                int playerRank = PlayerSkillAllocator.GetGatherSkillRank(gatherKind);
                if (playerRank < requiredRank)
                {
                    string skillName = gatherKind == MineHarvestGatherKind.Harvest ? "Harvesting" : "Mining";
                    PlayDeniedFeedback($"Requires {skillName} rank {requiredRank}");
                    return;
                }
            }

            CancelScan(clearProgressUi: false);
            scanTarget = node;
            scanPoint = point;
            scanProgress = 0f;
            isScanning = true;
            MiningToolResourceCollisionUtility.PushIgnoredResource(node, transform);
            RefreshScanLockPoint(node);
            ApplyHighlight(node, true);
            EnsureScanAudio();
            UpdateScanVisuals();
            UpdateProgressUi();
        }

        private void CompleteScan()
        {
            ResourceNode node = scanTarget;
            ItemData item = node != null ? node.resourceItem : null;
            CancelScan(clearProgressUi: true);

            if (node == null || item == null)
                return;

            if (ResourceIdentificationRegistry.IsIdentified(item))
            {
                PlayDeniedFeedback("Already identified");
                return;
            }

            MineHarvestGatherKind gatherKind = ResolveGatherKind(item, node);
            int requiredRank = ResolveRequiredRank(item);
            int playerRank = PlayerSkillAllocator.GetGatherSkillRank(gatherKind);
            string skillName = gatherKind == MineHarvestGatherKind.Harvest ? "Harvesting" : "Mining";

            if (playerRank < requiredRank)
            {
                PlayDeniedFeedback($"Requires {skillName} rank {requiredRank}");
                return;
            }

            ResourceIdentificationRegistry.Identify(item);
            PlaySuccessFeedback();
            ResourceScanResultUI.Show(
                item,
                gatherKind == MineHarvestGatherKind.Harvest ? "Harvesting" : "Mining",
                BuildYieldText(node, item));
        }

        private void CancelScan(bool clearProgressUi)
        {
            ResourceNode previousTarget = scanTarget;
            if (isScanning || scanTarget != null)
                ApplyHighlight(scanTarget, false);

            isScanning = false;
            scanTarget = null;
            scanProgress = 0f;
            lastProgressPercentShown = -1;
            lastProgressLabelNode = null;
            if (previousTarget != null)
                MiningToolResourceCollisionUtility.PopIgnoredResource(previousTarget);
            SetScanConeVisible(false);
            StopScanAudio();

            if (clearProgressUi)
                SetProgressUiVisible(false);
        }

        private void SetMiningScanAimHold(bool held)
        {
            if (inputBridge == null)
                inputBridge = GetComponent<PioneerInvectorInputBridge>();

            inputBridge?.SetMiningScanAimHold(held);
        }

        private bool TryAcquireTarget(out ResourceNode node, out Vector3 point)
        {
            node = null;
            point = default;

            // Always exclude layer 2 (Ignore Raycast) regardless of the authored resourceLayer mask.
            LayerMask effectiveMask = resourceLayer & ~(1 << 2);

            Vector3 aimDir = ResolveAimDirection(out Vector3 aimOrigin);

            // Use RaycastAll so we can skip non-ResourceNode hits (e.g. player colliders) instead
            // of stopping at the first geometry collision.
            RaycastHit[] hits = Physics.RaycastAll(
                aimOrigin, aimDir, DMIMiningController.MaxScanDistance,
                effectiveMask, QueryTriggerInteraction.Collide);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            RaycastHit bestHit = default;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                ResourceNode candidate = hits[i].collider.GetComponentInParent<ResourceNode>();
                if (candidate != null && candidate.resourceItem != null)
                {
                    bestHit = hits[i];
                    found = true;
                    break;
                }
            }

            // Sphere-cast fallback for wider acquisition when straight raycast misses.
            if (!found)
            {
                if (!Physics.SphereCast(
                        aimOrigin,
                        Mathf.Max(0.05f, acquireRayRadius * 0.35f),
                        aimDir,
                        out bestHit,
                        DMIMiningController.MaxScanDistance,
                        effectiveMask,
                        QueryTriggerInteraction.Collide))
                {
                    return TryAcquireFromNearbyNodes(aimOrigin, aimDir, out node, out point);
                }

                found = true;
            }

            node = bestHit.collider.GetComponentInParent<ResourceNode>();
            if (node == null || node.resourceItem == null)
                return false;

            float surfaceDist = Vector3.Distance(transform.position, node.GetClosestPoint(transform.position));
            if (Vector3.Distance(transform.position, bestHit.point) > DMIMiningController.MaxScanDistance
                && surfaceDist > DMIMiningController.MaxScanDistance)
            {
                return false;
            }

            point = bestHit.point;
            return true;
        }

        private bool TryAcquireFromNearbyNodes(Vector3 aimOrigin, Vector3 aimDir, out ResourceNode node, out Vector3 point)
        {
            node = null;
            point = default;

            Collider[] nearby = Physics.OverlapSphere(
                transform.position,
                DMIMiningController.MaxScanDistance,
                resourceLayer & ~(1 << 2),
                QueryTriggerInteraction.Collide);
            if (nearby == null || nearby.Length == 0)
                return false;

            float bestAngle = 25f;
            ResourceNode bestNode = null;
            Vector3 bestPoint = default;

            for (int i = 0; i < nearby.Length; i++)
            {
                if (nearby[i] == null)
                    continue;

                ResourceNode candidate = nearby[i].GetComponentInParent<ResourceNode>();
                if (candidate == null || candidate.resourceItem == null)
                    continue;
                if (Vector3.Distance(transform.position, candidate.GetClosestPoint(transform.position))
                    > DMIMiningController.MaxScanDistance)
                    continue;
                if (!DMIMiningController.TryGetLockPointOnNode(
                        candidate, aimOrigin, aimDir, DMIMiningController.MaxScanDistance, out Vector3 lockPoint))
                    continue;

                Vector3 toPoint = lockPoint - aimOrigin;
                if (toPoint.sqrMagnitude < 0.0001f)
                    continue;

                float angle = Vector3.Angle(aimDir, toPoint);
                if (angle > bestAngle)
                    continue;

                bestAngle = angle;
                bestNode = candidate;
                bestPoint = lockPoint;
            }

            if (bestNode == null)
                return false;

            node = bestNode;
            point = bestPoint;
            return true;
        }

        private ItemData ResolveDrawnMiningTool()
        {
            if (equipment == null || !equipment.IsWeaponDrawn)
                return null;

            ItemData tool = equipment.DrawnWeaponItem;
            if (tool == null || !tool.isMiningTool || !tool.IsRangedWeapon)
                return null;

            return tool;
        }

        private bool IsInputBlocked()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            return player != null && player.BlocksCombatInput;
        }

        // Left Shoulder is the gamepad scan binding — held to scan (avoids conflict with West / harvest).
        private static bool IsScanHeld()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.fKey.isPressed)
                return true;

            Gamepad gp = Gamepad.current;
            return gp != null && gp.leftShoulder.isPressed;
        }

        private static bool WasScanPressedThisFrame()
        {
            Keyboard kb = Keyboard.current;
            if (kb != null && kb.fKey.wasPressedThisFrame)
                return true;

            Gamepad gp = Gamepad.current;
            return gp != null && gp.leftShoulder.wasPressedThisFrame;
        }

        private Vector3 ResolveAimDirection(out Vector3 origin)
        {
            Camera cam = ResolveCamera();
            if (cam != null)
            {
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                origin = ray.origin;
                return ray.direction.normalized;
            }

            ResolveMuzzle();
            origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
            return transform.forward;
        }

        private Camera ResolveCamera()
        {
            if (gameplayCamera != null && gameplayCamera.isActiveAndEnabled)
                return gameplayCamera;

            gameplayCamera = Camera.main;
            return gameplayCamera;
        }

        private void ResolveMuzzle()
        {
            muzzleTransform = null;
            ItemData drawnTool = ResolveDrawnMiningTool();
            if (weaponBridge != null && drawnTool != null)
            {
                if (weaponBridge.TryGetActiveDrawnMuzzle(drawnTool, out Transform drawnMuzzle)
                    && drawnMuzzle != null
                    && drawnMuzzle.gameObject.activeInHierarchy)
                {
                    muzzleTransform = drawnMuzzle;
                    return;
                }
            }

            Transform miningToolRoot = FindDrawnMiningTool();
            if (miningToolRoot != null)
            {
                Transform miningMuzzle = FindChildNamed(miningToolRoot, "MiningBeamMuzzle");
                if (miningMuzzle == null)
                    miningMuzzle = FindChildNamed(miningToolRoot, "muzzle");

                if (miningMuzzle != null && miningMuzzle.gameObject.activeInHierarchy)
                {
                    muzzleTransform = miningMuzzle;
                    return;
                }
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null || !t.gameObject.activeInHierarchy)
                    continue;

                if (!IsUnderDrawnMiningTool(t))
                    continue;

                if (t.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase)
                    || t.name.Equals("MiningBeamMuzzle", StringComparison.OrdinalIgnoreCase))
                {
                    muzzleTransform = t;
                    return;
                }
            }

            muzzleTransform = transform;
        }

        private static bool IsUnderDrawnMiningTool(Transform node)
        {
            Transform walk = node;
            while (walk != null)
            {
                if (walk.name.Equals("Drawn_DM_Mining_Tool", StringComparison.OrdinalIgnoreCase)
                    || walk.name.Equals("Drawn_Mining_Tool", StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }

                walk = walk.parent;
            }

            return false;
        }

        private void ApplyHighlight(ResourceNode node, bool on)
        {
            if (highlightedOutline != null)
            {
                highlightedOutline.ClearResourceScanHighlight();
                // Destroy the OutlineController if we added it dynamically, so we don't litter components.
                if (addedOutlineController != null && highlightedOutline == addedOutlineController)
                {
                    Destroy(addedOutlineController);
                    addedOutlineController = null;
                }

                highlightedOutline = null;
            }

            if (!on || node == null)
                return;

            highlightedOutline = node.GetComponentInChildren<OutlineController>();
            if (highlightedOutline == null)
            {
                highlightedOutline = node.gameObject.AddComponent<OutlineController>();
                addedOutlineController = highlightedOutline;
            }

            highlightedOutline.SetResourceScanHighlight(true, ScanOutlineColor, ScanHighlightAlpha);
        }

        private GameObject ResolveScanCone()
        {
            if (scanCone != null)
                return scanCone;

            Transform drawnTool = FindDrawnMiningTool();
            if (drawnTool != null)
            {
                Transform onTool = FindChildNamed(drawnTool, ScanConeFieldObjectName);
                if (onTool != null)
                {
                    scanCone = onTool.gameObject;
                    return scanCone;
                }
            }

            Transform fallback = FindChildNamed(transform, ScanConeFieldObjectName);
            if (fallback != null)
            {
                scanCone = fallback.gameObject;
                return scanCone;
            }

            return null;
        }

        private Transform FindDrawnMiningTool()
        {
            Transform[] children = GetComponentsInChildren<Transform>(true);
            Transform inactiveFallback = null;
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null)
                    continue;

                if (!t.name.Equals("Drawn_DM_Mining_Tool", StringComparison.OrdinalIgnoreCase)
                    && !t.name.Equals("Drawn_Mining_Tool", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (t.gameObject.activeInHierarchy)
                    return t;

                if (inactiveFallback == null)
                    inactiveFallback = t;
            }

            return inactiveFallback;
        }

        private static Transform FindChildNamed(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrEmpty(objectName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t != null && t.name.Equals(objectName, StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }

        private void UpdateScanVisuals()
        {
            SetScanConeVisible(true);
        }

        private void SetScanConeVisible(bool visible)
        {
            GameObject cone = ResolveScanCone();
            if (cone == null)
                return;

            if (cone.activeSelf != visible)
                cone.SetActive(visible);

            MeshRenderer renderer = cone.GetComponent<MeshRenderer>();
            if (renderer != null)
                renderer.enabled = visible;

            DMIMaterialPulseScroll pulse = cone.GetComponent<DMIMaterialPulseScroll>();
            if (pulse != null)
                pulse.enabled = visible;
        }

        /// <summary>
        /// Resolve order: component inspector override → drawn mining tool ItemData → Resources default.
        /// </summary>
        private AudioClip ResolveScanLoopClip(ItemData tool)
        {
            if (scanLoopClip != null)
                return scanLoopClip;

            if (tool != null && tool.miningScanLoopSound != null)
                return tool.miningScanLoopSound;

            if (cachedDefaultScanLoop == null)
            {
                cachedDefaultScanLoop = Resources.Load<AudioClip>(ScanLoopResourcesPath);
#if UNITY_EDITOR
                if (cachedDefaultScanLoop == null)
                    cachedDefaultScanLoop = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(ScanLoopAssetPath);
#endif
            }

            return cachedDefaultScanLoop;
        }

        private AudioClip ResolveScanSuccessClip(ItemData tool)
        {
            if (scanSuccessClip != null)
                return scanSuccessClip;
            return tool != null ? tool.miningScanSuccessSound : null;
        }

        private AudioClip ResolveScanDeniedClip(ItemData tool)
        {
            if (scanDeniedClip != null)
                return scanDeniedClip;
            return tool != null ? tool.miningScanDeniedSound : null;
        }

        private void EnsureScanAudio()
        {
            AudioClip loop = ResolveScanLoopClip(ResolveDrawnMiningTool());
            if (loop == null)
                return;

            if (scanAudio == null)
            {
                scanAudio = gameObject.AddComponent<AudioSource>();
                scanAudio.playOnAwake = false;
                scanAudio.loop = true;
                scanAudio.spatialBlend = 0.35f;
                scanAudio.priority = 128;
            }

            scanAudio.volume = ScanLoopVolume;
            scanAudio.pitch = ScanLoopPitch;

            if (!scanAudio.isPlaying || scanAudio.clip != loop)
            {
                scanAudio.clip = loop;
                scanAudio.Play();
            }
        }

        private void StopScanAudio()
        {
            if (scanAudio != null && scanAudio.isPlaying)
                scanAudio.Stop();
        }

        private void EnsureProgressUi()
        {
            if (progressBar != null)
                return;

            progressBar = WorldNodeProgressBar.Create(transform);
        }

        private void UpdateProgressUi()
        {
            if (scanTarget == null)
            {
                SetProgressUiVisible(false);
                return;
            }

            EnsureProgressUi();
            if (progressBar == null)
                return;

            float pct = Mathf.Clamp01(scanProgress / ScanDurationSeconds);
            int percent = Mathf.RoundToInt(pct * 100f);
            // Avoid per-frame string alloc churn when the displayed percent hasn't changed.
            if (percent != lastProgressPercentShown || scanTarget != lastProgressLabelNode)
            {
                lastProgressPercentShown = percent;
                lastProgressLabelNode = scanTarget;
                cachedProgressLabel = "Scanning…  " + percent + "%";
            }

            Vector3 anchor = scanPoint + Vector3.up * 0.75f;
            progressBar.UpdateBar(anchor, pct, cachedProgressLabel, ResolveCamera());
        }

        private void SetProgressUiVisible(bool visible)
        {
            if (progressBar != null)
                progressBar.SetVisible(visible);
        }

        private void PlayDeniedFeedback(string message)
        {
            if (Time.unscaledTime < nextToastTime)
                return;

            nextToastTime = Time.unscaledTime + ToastCooldownSeconds;
            AudioClip denied = ResolveScanDeniedClip(ResolveDrawnMiningTool());
            if (denied != null)
            {
                float vol = GameSettings.SfxVolume * 0.8f;
                if (scanAudio != null)
                    scanAudio.PlayOneShot(denied, vol);
                else
                    AudioSource.PlayClipAtPoint(denied, transform.position, vol);
            }
            else
            {
                GameAudioManager.Instance?.PlayInventoryItemClick();
            }

            PickupToastUI.Show(message);
        }

        private void PlaySuccessFeedback()
        {
            AudioClip success = ResolveScanSuccessClip(ResolveDrawnMiningTool());
            if (success != null)
            {
                float vol = GameSettings.SfxVolume * 0.85f;
                if (scanAudio != null)
                    scanAudio.PlayOneShot(success, vol);
                else
                    AudioSource.PlayClipAtPoint(success, transform.position, vol);
            }
            else
            {
                GameAudioManager.Instance?.PlayItemPickup();
            }
        }

        private static MineHarvestGatherKind ResolveGatherKind(ItemData item, ResourceNode node)
        {
            if (item is MineHarvestItemData lean)
                return lean.gatherKind;

            return node.interactionMode == ResourceNodeInteractionMode.HoldHarvest
                ? MineHarvestGatherKind.Harvest
                : MineHarvestGatherKind.Mining;
        }

        private static int ResolveRequiredRank(ItemData item)
        {
            if (item is MineHarvestItemData lean)
                return Mathf.Max(1, lean.requiredGatherSkillRank);

            return 1;
        }

        private static string BuildYieldText(ResourceNode node, ItemData item)
        {
            string itemName = string.IsNullOrEmpty(item.itemName) ? item.name : item.itemName;
            node.ResolveDropRange(node.amountPerGather, node.amountPerGather, out int min, out int max);
            min = Mathf.Max(1, Mathf.Min(min, max));
            max = Mathf.Max(min, max);
            return min == max ? $"{itemName} × {min}" : $"{itemName} × {min}–{max}";
        }
    }
}
