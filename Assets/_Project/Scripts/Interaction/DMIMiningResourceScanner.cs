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
        private const string ScanConeObjectName = "Scan Cone";
        private const string ScanConePrefabResourcesPath = "VFX/Scan Cone";
        private const string ScanLoopResourcesPath = "Audio/Mining_Resource_Scan_Loop";
        private const string ScanLoopAssetPath =
            "Assets/_Project/Resources/Audio/Mining_Resource_Scan_Loop.wav";

        private const float ScanConeMeshHeight = 1.47f;
        private const float ScanLockBreakDegrees = 18f;
        private static readonly Vector3 ScanConeBaseScale = new Vector3(1.41f, 1.41f, 1.41f);

        private static Mesh cachedScanConeMesh;

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
                    PlayDeniedFeedback("Step back to scan");
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
                UpdateScanVisuals();
                UpdateProgressUi();

                if (scanProgress >= ScanDurationSeconds)
                    CompleteScan();
            }
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

            if (Vector3.Distance(transform.position, node.GetNodeCenter()) > DMIMiningController.MaxScanDistance)
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

            Vector3 delta = node.GetNodeCenter() - transform.position;
            delta.y = 0f;
            return delta.sqrMagnitude >= DMIMiningController.MinScanStandoffDistance * DMIMiningController.MinScanStandoffDistance;
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
                    return false;
                }

                found = true;
            }

            node = bestHit.collider.GetComponentInParent<ResourceNode>();
            if (node == null || node.resourceItem == null)
                return false;

            if (Vector3.Distance(transform.position, bestHit.point) > DMIMiningController.MaxScanDistance
                && Vector3.Distance(transform.position, node.GetNodeCenter()) > DMIMiningController.MaxScanDistance)
            {
                return false;
            }

            point = bestHit.point;
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
            // Return cached cone if still valid and active in the hierarchy.
            if (scanCone != null)
            {
                if (scanCone.activeInHierarchy || !scanCone.activeSelf)
                {
                    if (IsPreferredScanConeTransform(scanCone.transform))
                        return scanCone;

                    scanCone = null;
                }
                else
                {
                    scanCone = null;
                }
            }

            Transform drawnTool = FindDrawnMiningTool();
            if (drawnTool != null)
            {
                Transform muzzleParent = ResolveScanConeParent(drawnTool);
                if (muzzleParent != null)
                {
                    Transform onMuzzle = FindChildNamed(muzzleParent, ScanConeObjectName);
                    if (onMuzzle != null)
                    {
                        scanCone = onMuzzle.gameObject;
                        PrepareScanConeInstance(scanCone);
                        return scanCone;
                    }
                }

                Transform anywhereUnderTool = FindChildNamed(drawnTool, ScanConeObjectName);
                if (anywhereUnderTool != null && IsPreferredScanConeTransform(anywhereUnderTool))
                {
                    scanCone = anywhereUnderTool.gameObject;
                    PrepareScanConeInstance(scanCone);
                    return scanCone;
                }
            }

            Transform fallback = FindChildNamed(transform, ScanConeObjectName);
            if (fallback != null)
            {
                scanCone = fallback.gameObject;
                PrepareScanConeInstance(scanCone);
                return scanCone;
            }

            // Last resort: spawn from Resources under the drawn tool muzzle (or this player).
            GameObject prefab = Resources.Load<GameObject>(ScanConePrefabResourcesPath);
            if (prefab != null)
            {
                Transform parent = ResolveScanConeParent(drawnTool);
                scanCone = Instantiate(prefab, parent);
                scanCone.name = ScanConeObjectName;
                scanCone.transform.localPosition = Vector3.zero;
                scanCone.transform.localRotation = Quaternion.identity;
                PrepareScanConeInstance(scanCone);
                scanCone.SetActive(false);
                return scanCone;
            }

            return null;
        }

        private bool IsPreferredScanConeTransform(Transform coneTransform)
        {
            if (coneTransform == null)
                return false;

            ResolveMuzzle();
            if (muzzleTransform != null && coneTransform.IsChildOf(muzzleTransform))
                return true;

            Transform parent = coneTransform.parent;
            if (parent == null)
                return false;

            return parent.name.Equals("MiningBeamMuzzle", StringComparison.OrdinalIgnoreCase)
                   || parent.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase)
                   || parent.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase);
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

        private Transform ResolveScanConeParent(Transform drawnTool)
        {
            ResolveMuzzle();
            if (muzzleTransform != null)
                return muzzleTransform;

            if (drawnTool != null)
            {
                Transform renderer = FindChildNamed(drawnTool, "renderer");
                if (renderer != null)
                    return renderer;

                return drawnTool;
            }

            return transform;
        }

        private void UpdateScanVisuals()
        {
            GameObject cone = ResolveScanCone();
            if (cone == null)
                return;

            SetScanConeVisible(true);
            AimScanConeAtTarget(cone);
        }

        private void AimScanConeAtTarget(GameObject cone)
        {
            if (cone == null || scanTarget == null)
                return;

            ResolveMuzzle();
            Transform coneTransform = cone.transform;
            Vector3 origin = muzzleTransform != null ? muzzleTransform.position : coneTransform.position;
            Vector3 toTarget = scanPoint - origin;
            float distance = toTarget.magnitude;
            if (distance < 0.05f)
                return;

            Vector3 direction = toTarget / distance;
            coneTransform.rotation = Quaternion.FromToRotation(Vector3.up, direction);

            float lengthScale = Mathf.Max(0.35f, distance / ScanConeMeshHeight);
            coneTransform.localScale = new Vector3(
                ScanConeBaseScale.x,
                ScanConeBaseScale.y * lengthScale,
                ScanConeBaseScale.z);
        }

        private void SetScanConeVisible(bool visible)
        {
            GameObject cone = ResolveScanCone();
            if (cone == null)
                return;

            PrepareScanConeInstance(cone);

            if (cone.activeSelf != visible)
                cone.SetActive(visible);

            Renderer[] renderers = cone.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer != null)
                    renderer.enabled = visible;
            }

            if (visible)
            {
                DMIMaterialPulseScroll pulse = cone.GetComponent<DMIMaterialPulseScroll>();
                if (pulse != null)
                    pulse.enabled = true;
            }
        }

        private static void PrepareScanConeInstance(GameObject cone)
        {
            if (cone == null)
                return;

            MeshFilter meshFilter = cone.GetComponent<MeshFilter>();
            if (meshFilter != null && meshFilter.sharedMesh == null)
            {
                Mesh mesh = ResolveScanConeMesh();
                if (mesh != null)
                    meshFilter.sharedMesh = mesh;
            }

            Collider[] colliders = cone.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider != null)
                    collider.enabled = false;
            }
        }

        private static Mesh ResolveScanConeMesh()
        {
            if (cachedScanConeMesh != null)
                return cachedScanConeMesh;

            GameObject prefab = Resources.Load<GameObject>(ScanConePrefabResourcesPath);
            if (prefab != null)
            {
                MeshFilter meshFilter = prefab.GetComponent<MeshFilter>();
                if (meshFilter != null)
                    cachedScanConeMesh = meshFilter.sharedMesh;
            }

            return cachedScanConeMesh;
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
