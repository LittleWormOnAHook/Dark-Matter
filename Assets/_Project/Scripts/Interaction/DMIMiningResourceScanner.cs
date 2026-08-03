using Project.Audio;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.Player;
using Project.Player.Invector;
using Project.Progression;
using Project.UI;
using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Secondary mining multi-tool scanner. While the mining tool is drawn and in aim mode,
    /// hold F on a locked ResourceNode within mine range for 5 seconds to identify it
    /// (gated by Mining / Harvesting skill rank). Already-identified types cannot be re-scanned.
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
        private const string ScanConeObjectName = "Scan Cone";
        private const string ScanLoopResourcesPath = "Audio/Mining_Resource_Scan_Loop";
        private const string ScanLoopAssetPath =
            "Assets/_Project/Resources/Audio/Mining_Resource_Scan_Loop.wav";

        private static readonly Color ScanOutlineColor = new Color(0.45f, 0.85f, 1f, 1f);

        [SerializeField] private LayerMask resourceLayer = ~0;
        [SerializeField] private float acquireRayRadius = 0.45f;
        [Tooltip("Optional. If empty, finds Drawn_*Mining_Tool/renderer/Scan Cone.")]
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
            CancelScan(clearProgressUi: true);
        }

        private void Update()
        {
            ItemData tool = ResolveDrawnMiningTool();
            if (tool == null || IsInputBlocked() || !IsAimModeActive())
            {
                CancelScan(clearProgressUi: true);
                return;
            }

            bool holdF = IsScanHeld();
            if (!holdF)
            {
                CancelScan(clearProgressUi: true);
                return;
            }

            if (!TryAcquireTarget(out ResourceNode node, out Vector3 point))
            {
                CancelScan(clearProgressUi: true);
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
                scanPoint = point;
                isScanning = true;
                scanProgress += Time.deltaTime;
                UpdateScanVisuals();
                UpdateProgressUi();

                if (scanProgress >= ScanDurationSeconds)
                    CompleteScan();
            }
        }

        private void BeginScan(ResourceNode node, Vector3 point)
        {
            CancelScan(clearProgressUi: false);
            scanTarget = node;
            scanPoint = point;
            scanProgress = 0f;
            isScanning = true;
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
            if (isScanning || scanTarget != null)
                ApplyHighlight(scanTarget, false);

            isScanning = false;
            scanTarget = null;
            scanProgress = 0f;
            lastProgressPercentShown = -1;
            lastProgressLabelNode = null;
            SetScanConeVisible(false);
            StopScanAudio();

            if (clearProgressUi)
                SetProgressUiVisible(false);
        }

        private bool TryAcquireTarget(out ResourceNode node, out Vector3 point)
        {
            node = null;
            point = default;

            Vector3 aimDir = ResolveAimDirection(out Vector3 aimOrigin);
            // Collide so HoldHarvest plant trigger colliders are scannable (same as WorldUse).
            if (!Physics.Raycast(
                    aimOrigin,
                    aimDir,
                    out RaycastHit hit,
                    DMIMiningController.MaxMineDistance,
                    resourceLayer,
                    QueryTriggerInteraction.Collide))
            {
                if (!Physics.SphereCast(
                        aimOrigin,
                        Mathf.Max(0.05f, acquireRayRadius * 0.35f),
                        aimDir,
                        out hit,
                        DMIMiningController.MaxMineDistance,
                        resourceLayer,
                        QueryTriggerInteraction.Collide))
                {
                    return false;
                }
            }

            node = hit.collider.GetComponentInParent<ResourceNode>();
            if (node == null || node.resourceItem == null)
                return false;

            if (Vector3.Distance(transform.position, hit.point) > DMIMiningController.MaxMineDistance
                && Vector3.Distance(transform.position, node.GetNodeCenter()) > DMIMiningController.MaxMineDistance)
            {
                return false;
            }

            point = hit.point;
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

        private bool IsAimModeActive()
        {
            if (inputBridge == null)
                inputBridge = GetComponent<PioneerInvectorInputBridge>();

            return inputBridge != null && inputBridge.IsAiming;
        }

        private bool IsInputBlocked()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            return player != null && player.BlocksCombatInput;
        }

        private static bool IsScanHeld()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.fKey.isPressed;
        }

        private static bool WasScanPressedThisFrame()
        {
            Keyboard kb = Keyboard.current;
            return kb != null && kb.fKey.wasPressedThisFrame;
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
            if (weaponBridge != null && equipment != null && equipment.EquippedItem != null)
            {
                if (weaponBridge.TryGetActiveDrawnMuzzle(equipment.EquippedItem, out Transform drawnMuzzle)
                    && drawnMuzzle != null
                    && drawnMuzzle.gameObject.activeInHierarchy)
                {
                    muzzleTransform = drawnMuzzle;
                    return;
                }
            }

            Transform[] children = GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t == null || !t.gameObject.activeInHierarchy)
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

        private void ApplyHighlight(ResourceNode node, bool on)
        {
            if (highlightedOutline != null)
            {
                highlightedOutline.ClearResourceScanHighlight();
                highlightedOutline = null;
            }

            if (!on || node == null)
                return;

            highlightedOutline = node.GetComponentInChildren<OutlineController>();
            if (highlightedOutline == null)
                highlightedOutline = node.gameObject.AddComponent<OutlineController>();

            highlightedOutline.SetResourceScanHighlight(true, ScanOutlineColor, ScanHighlightAlpha);
        }

        private GameObject ResolveScanCone()
        {
            // Drop destroyed / missing refs so a replaced authored cone is rediscovered.
            if (scanCone == null)
                scanCone = null;
            else
                return scanCone;

            Transform drawnTool = FindDrawnMiningTool();
            if (drawnTool != null)
            {
                Transform renderer = FindChildNamed(drawnTool, "renderer");
                if (renderer != null)
                {
                    Transform underRenderer = FindChildNamed(renderer, ScanConeObjectName);
                    if (underRenderer != null)
                    {
                        scanCone = underRenderer.gameObject;
                        return scanCone;
                    }
                }

                Transform anywhereUnderTool = FindChildNamed(drawnTool, ScanConeObjectName);
                if (anywhereUnderTool != null)
                {
                    scanCone = anywhereUnderTool.gameObject;
                    return scanCone;
                }
            }

            Transform fallback = FindChildNamed(transform, ScanConeObjectName);
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
            // Authored beam under Drawn_*Mining_Tool/renderer/Scan Cone — toggle only; keep local pose.
            SetScanConeVisible(true);
        }

        private void SetScanConeVisible(bool visible)
        {
            GameObject cone = ResolveScanCone();
            if (cone != null && cone.activeSelf != visible)
                cone.SetActive(visible);
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
                AudioSource.PlayClipAtPoint(denied, transform.position, 0.8f);
            else
                GameAudioManager.Instance?.PlayInventoryItemClick();

            PickupToastUI.Show(message);
        }

        private void PlaySuccessFeedback()
        {
            AudioClip success = ResolveScanSuccessClip(ResolveDrawnMiningTool());
            if (success != null)
                AudioSource.PlayClipAtPoint(success, transform.position, 0.85f);
            else
                GameAudioManager.Instance?.PlayItemPickup();
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
