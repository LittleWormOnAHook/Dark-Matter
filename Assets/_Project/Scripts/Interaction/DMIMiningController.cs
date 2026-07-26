using Invector.vShooter;
using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Project.Interaction
{
    /// <summary>
    /// Hold Fire to mine ResourceNodes with a continuous soft-locked red laser, muzzle sparks, and pass-based grants.
    /// Mining tools use infinite Laser Tool power once loaded. Sustained fire overheats after 20s (3s cooloff).
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(600)]
    public class DMIMiningController : MonoBehaviour
    {
        private const float ProgressRetainSeconds = 4f;
        private const float MaxMineRayDistance = 80f;
        private const float OverheatSeconds = 20f;
        private const float CooloffSeconds = 3f;
        private const string HitSparksPrefabPath = "Assets/_Project/Prefabs/SparksLong.prefab";
        private static readonly Color LaserRed = new Color(1f, 0.18f, 0.12f, 0.95f);
        private static readonly Color OverheatTint = new Color(1f, 0.08f, 0.05f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [SerializeField] private LayerMask resourceLayer = ~0;
        [SerializeField] private float acquireRayRadius = 0.45f;
        [Tooltip("Impact sparks spawned at the laser LineRenderer world hit point (SparksLong).")]
        [SerializeField] private GameObject hitSparksPrefab;

        private EquipmentController equipment;
        private ResourceGatherer gatherer;
        private PlayerController player;
        private PioneerInvectorWeaponBridge weaponBridge;
        private PioneerInvectorAmmoBridge ammoBridge;
        private Camera gameplayCamera;

        private ResourceNode lockedNode;
        private Vector3 lockPoint;
        private Vector3 lockDirection;
        private bool hasLock;

        private LineRenderer laserLine;
        private Transform laserRoot;
        private Transform laserSightSprite;
        private vLaserSight boundLaserSight;
        private bool usingWeaponLaserStack;
        private Transform muzzleTransform;
        private GameObject impactGlow;
        private GameObject hitSparksInstance;
        private ParticleSystem hitSparksParticles;
        private bool hitSparksAuthored;

        private Canvas progressCanvas;
        private RectTransform progressRoot;
        private TextMeshProUGUI progressLabel;
        private Image progressFill;

        private AudioSource continuousAudio;
        private bool continuousAudioPlaying;
        private ItemData continuousAudioAmmo;
        private AudioClip continuousLoopSourceClip;
        private static readonly Dictionary<int, AudioClip> trimmedContinuousLoopCache = new Dictionary<int, AudioClip>(8);
        private const float ContinuousLoopTrimFraction = 0.15f;
        private WeaponAmmoState ammoState;
        private float powerDrainAccumulator;

        private float heatSeconds;
        private float cooloffRemaining;
        private bool isOverheated;
        private GameObject heatVisualRoot;
        private Renderer[] heatRenderers;
        private Color[] heatBaseColors;
        private MaterialPropertyBlock heatPropertyBlock;

        private void Awake()
        {
            equipment = GetComponent<EquipmentController>();
            gatherer = GetComponent<ResourceGatherer>();
            player = GetComponent<PlayerController>();
            weaponBridge = GetComponent<PioneerInvectorWeaponBridge>();
            ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();
            ammoState = GetComponent<WeaponAmmoState>();
            EnsureVisuals();
            EnsureProgressUi();
            EnsureContinuousAudio();
            SetMiningFxActive(false);
            SetProgressUiVisible(false);
        }

        private void OnDisable()
        {
            StopContinuousLaserAudio(playStopSound: false);
            StopHitSparks();
            RestoreHeatTint();
        }

        private void OnDestroy()
        {
            if (hitSparksInstance != null && !hitSparksAuthored)
                Destroy(hitSparksInstance);
        }

        private void Update()
        {
            ItemData tool = equipment != null ? equipment.EquippedItem : null;
            bool miningTool = tool != null && tool.isMiningTool && tool.IsRangedWeapon;
            bool fireHeld = miningTool && IsFireHeld();

            if (miningTool)
                TickOverheatState(tool, fireHeld);

            if (!miningTool || !fireHeld || isOverheated)
            {
                if (hasLock && lockedNode != null)
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);

                ClearLock();
                SetMiningFxActive(false);
                SetProgressUiVisible(false);
                StopContinuousLaserAudio(playStopSound: true);

                if (miningTool && isOverheated && fireHeld)
                    PlayOverheatClick();
            }

            if (!miningTool)
            {
                heatSeconds = 0f;
                cooloffRemaining = 0f;
                isOverheated = false;
                RestoreHeatTint();
            }
        }

        private void LateUpdate()
        {
            // Laser / muzzle FX must run after Invector aim + hand IK (LateUpdate), otherwise the
            // beam origin freezes at the pre-aim pose and floats off the barrel when looking around.
            ItemData tool = equipment != null ? equipment.EquippedItem : null;
            bool miningTool = tool != null && tool.isMiningTool && tool.IsRangedWeapon;
            bool fireHeld = miningTool && IsFireHeld() && !isOverheated;
            if (!miningTool || !fireHeld)
            {
                powerDrainAccumulator = 0f;
                return;
            }

            if (!TryDrainMiningPower(tool))
            {
                if (hasLock && lockedNode != null)
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);

                ClearLock();
                SetMiningFxActive(false);
                SetProgressUiVisible(false);
                StopContinuousLaserAudio(playStopSound: true);
                return;
            }

            ResolveMuzzle();
            TryBindWeaponLaserStack();
            // Prefer the authored Laser transform (under muzzle) so the beam starts on that stack.
            Vector3 origin = laserRoot != null
                ? laserRoot.position
                : ResolveMuzzleWorldPosition();
            float range = MaxMineRayDistance;
            if (tool != null)
                range = Mathf.Max(8f, tool.rangedRange);

            // Soft-lock uses the camera reticle ray; beam visuals use muzzle → aim raycast.
            Vector3 camAimDir = ResolveAimDirection(out Vector3 aimOrigin);
            UpdateSoftLock(tool, aimOrigin, camAimDir);

            Camera cam = ResolveCamera();
            Vector3 aimDirection = cam != null
                ? RangedFireSolver.ResolveMuzzleToReticleDirection(cam, origin, range, out _)
                : camAimDir;

            Vector3 endPoint = origin + aimDirection * range;
            if (Physics.Raycast(origin, aimDirection, out RaycastHit hit, range, ~0, QueryTriggerInteraction.Ignore))
                endPoint = hit.point;

            UpdateLaserVisuals(origin, endPoint);
            SetMiningFxActive(true);
            UpdateContinuousLaserAudio(tool, origin);

            if (hasLock && lockedNode != null)
            {
                TickMining(tool);
                UpdateProgressUi(tool);
            }
            else
            {
                SetProgressUiVisible(false);
            }
        }

        private void TickOverheatState(ItemData tool, bool fireHeld)
        {
            if (isOverheated)
            {
                cooloffRemaining -= Time.deltaTime;
                float coolT = CooloffSeconds > 0.01f
                    ? Mathf.Clamp01(cooloffRemaining / CooloffSeconds)
                    : 0f;
                ApplyHeatTint(tool, coolT);

                if (cooloffRemaining <= 0f)
                {
                    isOverheated = false;
                    heatSeconds = 0f;
                    cooloffRemaining = 0f;
                    ApplyHeatTint(tool, 0f);
                }

                return;
            }

            if (fireHeld && HasMiningPower(tool))
            {
                heatSeconds += Time.deltaTime;
                float heatT = Mathf.Clamp01(heatSeconds / OverheatSeconds);
                ApplyHeatTint(tool, heatT);

                if (heatSeconds >= OverheatSeconds)
                {
                    isOverheated = true;
                    cooloffRemaining = CooloffSeconds;
                    heatSeconds = OverheatSeconds;
                    ApplyHeatTint(tool, 1f);
                }
            }
            else if (heatSeconds > 0f)
            {
                // Bleed heat while not firing so pulsed use can avoid a full overheat.
                heatSeconds = Mathf.Max(0f, heatSeconds - Time.deltaTime * (OverheatSeconds / CooloffSeconds));
                ApplyHeatTint(tool, Mathf.Clamp01(heatSeconds / OverheatSeconds));
            }
        }

        private bool HasMiningPower(ItemData tool)
        {
            if (ammoState == null)
                ammoState = GetComponent<WeaponAmmoState>();
            if (ammoState == null)
                return true;

            if (ammoState.GetActiveLoadedAmmo() > 0)
                return true;

            int slot = equipment != null ? equipment.ActiveWeaponHotbarSlot : -1;
            if (slot >= 0)
                ammoState.EnsureWeaponInitialized(slot, tool);

            return ammoState.GetActiveLoadedAmmo() > 0;
        }

        private void PlayOverheatClick()
        {
            if (ammoBridge == null)
                ammoBridge = GetComponent<PioneerInvectorAmmoBridge>();

            if (ammoBridge != null)
                ammoBridge.PlayDryFireClick();
        }

        private void EnsureHeatRenderers(ItemData tool)
        {
            GameObject visual = weaponBridge != null ? weaponBridge.TryGetWeaponInstance(tool) : null;
            if (visual == null)
                visual = weaponBridge != null ? weaponBridge.TryGetHolsteredWeaponInstance(tool) : null;

            if (visual == heatVisualRoot && heatRenderers != null && heatRenderers.Length > 0)
                return;

            heatVisualRoot = visual;
            heatPropertyBlock ??= new MaterialPropertyBlock();

            if (visual == null)
            {
                heatRenderers = System.Array.Empty<Renderer>();
                heatBaseColors = System.Array.Empty<Color>();
                return;
            }

            Renderer[] found = visual.GetComponentsInChildren<Renderer>(true);
            var list = new System.Collections.Generic.List<Renderer>(found.Length);
            var colors = new System.Collections.Generic.List<Color>(found.Length);
            for (int i = 0; i < found.Length; i++)
            {
                Renderer renderer = found[i];
                if (renderer == null || renderer is ParticleSystemRenderer)
                    continue;

                list.Add(renderer);
                colors.Add(ReadRendererBaseColor(renderer));
            }

            heatRenderers = list.ToArray();
            heatBaseColors = colors.ToArray();
        }

        private Color ReadRendererBaseColor(Renderer renderer)
        {
            if (renderer == null)
                return Color.white;

            Material mat = renderer.sharedMaterial;
            if (mat == null)
                return Color.white;

            if (mat.HasProperty(BaseColorId))
                return mat.GetColor(BaseColorId);
            if (mat.HasProperty(ColorId))
                return mat.GetColor(ColorId);
            return Color.white;
        }

        private void ApplyHeatTint(ItemData tool, float heat01)
        {
            EnsureHeatRenderers(tool);
            if (heatRenderers == null || heatRenderers.Length == 0)
                return;

            heatPropertyBlock ??= new MaterialPropertyBlock();
            heat01 = Mathf.Clamp01(heat01);

            for (int i = 0; i < heatRenderers.Length; i++)
            {
                Renderer renderer = heatRenderers[i];
                if (renderer == null)
                    continue;

                Color baseColor = i < heatBaseColors.Length ? heatBaseColors[i] : Color.white;
                Color tinted = Color.Lerp(baseColor, OverheatTint, heat01);
                heatPropertyBlock.SetColor(BaseColorId, tinted);
                heatPropertyBlock.SetColor(ColorId, tinted);
                renderer.SetPropertyBlock(heatPropertyBlock);
            }
        }

        private void RestoreHeatTint()
        {
            if (heatRenderers == null)
                return;

            heatPropertyBlock ??= new MaterialPropertyBlock();
            for (int i = 0; i < heatRenderers.Length; i++)
            {
                Renderer renderer = heatRenderers[i];
                if (renderer == null)
                    continue;

                Color baseColor = i < heatBaseColors.Length ? heatBaseColors[i] : Color.white;
                heatPropertyBlock.SetColor(BaseColorId, baseColor);
                heatPropertyBlock.SetColor(ColorId, baseColor);
                renderer.SetPropertyBlock(heatPropertyBlock);
            }

            heatVisualRoot = null;
            heatRenderers = null;
            heatBaseColors = null;
        }

        private bool TryDrainMiningPower(ItemData tool)
        {
            if (ammoState == null)
                ammoState = GetComponent<WeaponAmmoState>();

            if (ammoState == null || tool == null)
                return true; // Fail open if ammo wiring is missing.

            if (ammoState.GetActiveLoadedAmmo() <= 0)
            {
                int slot = equipment != null ? equipment.ActiveWeaponHotbarSlot : -1;
                if (slot >= 0)
                    ammoState.EnsureWeaponInitialized(slot, tool);

                if (ammoState.GetActiveLoadedAmmo() <= 0)
                    return false;
            }

            // Infinite Laser Tool power: keep the mag topped and never deplete.
            int activeSlot = equipment != null ? equipment.ActiveWeaponHotbarSlot : -1;
            if (activeSlot >= 0 && ammoState.IsInfiniteAmmoForSlot(activeSlot))
                return true;

            float rate = Mathf.Max(1f, tool.fireRate);
            powerDrainAccumulator += rate * Time.deltaTime;
            while (powerDrainAccumulator >= 1f)
            {
                powerDrainAccumulator -= 1f;
                if (!ammoState.TryConsumeActiveRound())
                    return false;
            }

            return true;
        }

        private bool IsFireHeld()
        {
            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            if (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed)
                return true;

            return false;
        }

        private Vector3 ResolveAimDirection(out Vector3 origin)
        {
            Camera cam = ResolveCamera();
            if (cam != null)
            {
                // Always aim at screen-center reticle (same as RangedCombatHud crosshair).
                Ray ray = cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                origin = ray.origin;
                return ray.direction.normalized;
            }

            origin = muzzleTransform != null ? muzzleTransform.position : transform.position;
            return transform.forward;
        }

        private void ResolveMuzzle()
        {
            muzzleTransform = null;

            // Prefer authored barrel muzzle under the active drawn mining slot.
            if (weaponBridge != null && equipment != null && equipment.EquippedItem != null)
            {
                if (weaponBridge.TryGetActiveDrawnMuzzle(equipment.EquippedItem, out Transform drawnMuzzle) &&
                    drawnMuzzle != null &&
                    drawnMuzzle.gameObject.activeInHierarchy)
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

                if (t.name.Equals("muzzle", StringComparison.OrdinalIgnoreCase) ||
                    t.name.Equals("Muzzle", StringComparison.OrdinalIgnoreCase) ||
                    t.name.Equals("MiningBeamMuzzle", StringComparison.OrdinalIgnoreCase))
                {
                    muzzleTransform = t;
                    return;
                }
            }

            if (muzzleTransform == null)
                muzzleTransform = transform;
        }

        /// <summary>
        /// Live authored muzzle world position after aim IK. Do not rewrite tip from mesh AABB.
        /// </summary>
        private Vector3 ResolveMuzzleWorldPosition()
        {
            return muzzleTransform != null ? muzzleTransform.position : transform.position;
        }

        private Camera ResolveCamera()
        {
            if (gameplayCamera != null)
                return gameplayCamera;

            if (player != null && player.GameplayCamera != null)
                gameplayCamera = player.GameplayCamera;
            else
                gameplayCamera = Camera.main;

            return gameplayCamera;
        }

        private void UpdateSoftLock(ItemData tool, Vector3 aimOrigin, Vector3 aimDir)
        {
            float breakDegrees = Mathf.Max(5f, tool.miningLockBreakDegrees);

            if (hasLock && lockedNode != null)
            {
                Vector3 toLock = (lockPoint - aimOrigin).normalized;
                float liveAngle = Vector3.Angle(aimDir, toLock);
                if (liveAngle > breakDegrees)
                {
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);
                    ClearLock();
                }
                else
                {
                    lockPoint = ResolveNodePoint(lockedNode);
                    lockDirection = (lockPoint - aimOrigin).normalized;
                    return;
                }
            }

            if (Physics.SphereCast(
                    aimOrigin,
                    acquireRayRadius,
                    aimDir,
                    out RaycastHit hit,
                    MaxMineRayDistance,
                    resourceLayer,
                    QueryTriggerInteraction.Ignore))
            {
                ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
                if (node != null && node.resourceItem != null)
                {
                    lockedNode = node;
                    lockPoint = hit.point;
                    lockDirection = aimDir.normalized;
                    hasLock = true;
                }
            }
        }

        private static Vector3 ResolveNodePoint(ResourceNode node)
        {
            Renderer rend = node.GetComponentInChildren<Renderer>();
            if (rend != null)
                return rend.bounds.center;
            return node.transform.position;
        }

        private void TickMining(ItemData tool)
        {
            if (lockedNode == null || gatherer == null)
                return;

            bool passCompleted = lockedNode.TickMining(
                gatherer,
                Time.deltaTime,
                tool.miningPassDuration,
                tool.miningPassesRequired,
                tool.miningDropMin,
                tool.miningDropMax,
                ProgressRetainSeconds,
                out bool finishedNode,
                out _);

            if (passCompleted)
                DMIMiningChunkVfx.Spawn(lockPoint, muzzleTransform, tool.miningChunkVfxPrefab);

            if (finishedNode || lockedNode == null)
            {
                ClearLock();
                SetProgressUiVisible(false);
            }
        }

        private void ClearLock()
        {
            hasLock = false;
            lockedNode = null;
        }

        private void EnsureVisuals()
        {
            EnsureHitSparksPrefab();
            TryBindWeaponLaserStack();

            if (laserLine == null)
            {
                GameObject laserGo = new GameObject("MiningLaserBeam");
                laserGo.transform.SetParent(transform, false);
                laserLine = laserGo.AddComponent<LineRenderer>();
                laserLine.positionCount = 2;
                laserLine.useWorldSpace = true;
                laserLine.widthCurve = AnimationCurve.Linear(0f, 0.06f, 1f, 0.02f);
                laserLine.widthMultiplier = 1f;
                laserLine.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                laserLine.receiveShadows = false;
                laserLine.numCapVertices = 4;
                Shader shader = Shader.Find("Universal Render Pipeline/Unlit") ?? Shader.Find("Unlit/Color");
                laserLine.material = new Material(shader) { color = LaserRed };
                laserLine.startColor = LaserRed;
                laserLine.endColor = new Color(LaserRed.r, LaserRed.g, LaserRed.b, 0.55f);
                laserLine.enabled = false;
                usingWeaponLaserStack = false;
            }

            EnsureHitSparksInstance();
        }

        private void EnsureHitSparksPrefab()
        {
            if (hitSparksPrefab != null)
                return;

#if UNITY_EDITOR
            hitSparksPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(HitSparksPrefabPath);
#endif
        }

        private void EnsureHitSparksInstance()
        {
            if (hitSparksInstance != null && hitSparksInstance)
                return;

            EnsureHitSparksPrefab();
            if (hitSparksPrefab == null)
                return;

            hitSparksAuthored = false;
            hitSparksInstance = Instantiate(hitSparksPrefab);
            hitSparksInstance.name = "MiningHitSparks_SparksLong";
            // World-space FX host — never parent under laserSight (tiny scale warps emission).
            hitSparksInstance.transform.SetParent(null, true);
            hitSparksInstance.transform.localScale = Vector3.one;

            hitSparksParticles = hitSparksInstance.GetComponent<ParticleSystem>();
            if (hitSparksParticles == null)
                hitSparksParticles = hitSparksInstance.GetComponentInChildren<ParticleSystem>(true);

            hitSparksInstance.SetActive(false);
        }

        /// <summary>
        /// Prefer Drawn_DM_Mining_Tool/renderer/muzzle/Laser (+ laserSight) over the runtime MiningLaserBeam.
        /// </summary>
        private void TryBindWeaponLaserStack()
        {
            ItemData tool = equipment != null ? equipment.EquippedItem : null;
            if (tool == null || !tool.isMiningTool || weaponBridge == null)
                return;

            GameObject drawn = weaponBridge.TryGetWeaponInstance(tool);
            if (drawn == null || !drawn.activeInHierarchy)
                return;

            // Already bound to this drawn instance's Laser.
            if (usingWeaponLaserStack &&
                laserRoot != null &&
                laserRoot.IsChildOf(drawn.transform) &&
                laserLine != null)
                return;

            Transform laser = FindChildRecursive(drawn.transform, "Laser");
            if (laser == null && muzzleTransform != null)
                laser = muzzleTransform.Find("Laser");

            LineRenderer line = laser != null ? laser.GetComponent<LineRenderer>() : null;
            if (line == null)
                return;

            // Drop runtime fallback beam if we previously created one.
            if (laserLine != null && !usingWeaponLaserStack && laserLine.gameObject.name == "MiningLaserBeam")
            {
                Destroy(laserLine.gameObject);
                laserLine = null;
            }

            laserRoot = laser;
            laserLine = line;
            usingWeaponLaserStack = true;

            boundLaserSight = laser.GetComponent<vLaserSight>();
            if (boundLaserSight != null)
                boundLaserSight.enabled = false;

            laserSightSprite = FindChildRecursive(laser, "laserSight");
            if (laserSightSprite == null && boundLaserSight != null && boundLaserSight.aimSprite != null)
                laserSightSprite = boundLaserSight.aimSprite.transform;

            laserLine.useWorldSpace = true;
            laserLine.positionCount = 2;
            laserLine.enabled = false;
        }

        private static Transform FindChildRecursive(Transform root, string exactName)
        {
            if (root == null || string.IsNullOrEmpty(exactName))
                return null;

            Transform[] children = root.GetComponentsInChildren<Transform>(true);
            for (int i = 0; i < children.Length; i++)
            {
                Transform t = children[i];
                if (t != null && t.name.Equals(exactName, StringComparison.OrdinalIgnoreCase))
                    return t;
            }

            return null;
        }

        private void UpdateLaserVisuals(Vector3 muzzlePos, Vector3 endPoint)
        {
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.useWorldSpace = true;
                laserLine.positionCount = 2;
                laserLine.SetPosition(0, muzzlePos);
                laserLine.SetPosition(1, endPoint);
            }

            if (laserSightSprite != null)
            {
                laserSightSprite.gameObject.SetActive(true);
                laserSightSprite.position = endPoint;
            }

            UpdateHitSparks(endPoint, endPoint - muzzlePos);
        }

        private void UpdateHitSparks(Vector3 endPoint, Vector3 beamDelta)
        {
            EnsureHitSparksInstance();
            if (hitSparksInstance == null)
                return;

            hitSparksInstance.SetActive(true);
            hitSparksInstance.transform.SetParent(null, true);
            hitSparksInstance.transform.position = endPoint;
            hitSparksInstance.transform.localScale = Vector3.one;
            if (beamDelta.sqrMagnitude > 0.0001f)
                hitSparksInstance.transform.rotation = Quaternion.LookRotation(beamDelta.normalized, Vector3.up);

            if (hitSparksParticles != null && !hitSparksParticles.isPlaying)
                hitSparksParticles.Play(true);
        }

        private void StopHitSparks()
        {
            if (hitSparksParticles != null)
                hitSparksParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            if (hitSparksInstance != null)
                hitSparksInstance.SetActive(false);
        }

        private void EnsureContinuousAudio()
        {
            if (continuousAudio != null)
                return;

            // Dedicated child — never reuse/move an AudioSource on the player root (that teleports the player).
            Transform existing = transform.Find("MiningLaserAudio");
            GameObject host = existing != null ? existing.gameObject : new GameObject("MiningLaserAudio");
            if (existing == null)
                host.transform.SetParent(transform, false);

            continuousAudio = host.GetComponent<AudioSource>();
            if (continuousAudio == null)
                continuousAudio = host.AddComponent<AudioSource>();

            continuousAudio.playOnAwake = false;
            continuousAudio.loop = true;
            continuousAudio.spatialBlend = 1f;
            continuousAudio.rolloffMode = AudioRolloffMode.Linear;
            continuousAudio.minDistance = 1.5f;
            continuousAudio.maxDistance = 28f;
            continuousAudio.volume = 0.7f;
        }

        private static ItemData ResolveContinuousLaserAmmo(ItemData tool)
        {
            if (tool == null)
                return null;

            if (tool.defaultAmmoItem != null &&
                (tool.defaultAmmoItem.isContinuousLaser || tool.defaultAmmoItem.isHitscanBeam))
                return tool.defaultAmmoItem;

            return tool;
        }

        private void UpdateContinuousLaserAudio(ItemData tool, Vector3 muzzlePos)
        {
            EnsureContinuousAudio();
            ItemData ammo = ResolveContinuousLaserAmmo(tool);
            continuousAudioAmmo = ammo;

            AudioClip sourceLoop = ammo != null ? ammo.continuousLoopSound : null;
            if (sourceLoop == null && ammo != null)
                sourceLoop = ammo.projectileTravelSound; // legacy fallback

            if (continuousAudio == null)
                return;

            continuousAudio.transform.position = muzzlePos;

            if (sourceLoop == null)
            {
                if (continuousAudio.isPlaying)
                    continuousAudio.Stop();
                continuousAudioPlaying = false;
                continuousLoopSourceClip = null;
                return;
            }

            // Trim 15% off front + end so the loop seam is continuous (no click/skip).
            AudioClip loop = GetOrCreateTrimmedContinuousLoop(sourceLoop);

            if (!continuousAudioPlaying || continuousLoopSourceClip != sourceLoop)
            {
                if (ammo != null && ammo.continuousStartSound != null)
                    AudioSource.PlayClipAtPoint(ammo.continuousStartSound, muzzlePos);

                continuousLoopSourceClip = sourceLoop;
                continuousAudio.clip = loop;
                continuousAudio.loop = true;
                continuousAudio.time = 0f;
                continuousAudio.Play();
                continuousAudioPlaying = true;
            }
        }

        /// <summary>
        /// Builds a cached loop clip with <see cref="ContinuousLoopTrimFraction"/> removed from
        /// both ends so looping does not hitch on attack/release transients.
        /// </summary>
        private static AudioClip GetOrCreateTrimmedContinuousLoop(AudioClip source)
        {
            if (source == null)
                return null;

            int key = source.GetEntityId();
            if (trimmedContinuousLoopCache.TryGetValue(key, out AudioClip cached) && cached != null)
                return cached;

            AudioClip trimmed = CreateTrimmedLoopClip(source, ContinuousLoopTrimFraction);
            if (trimmed == null)
                trimmed = source;

            trimmedContinuousLoopCache[key] = trimmed;
            return trimmed;
        }

        private static AudioClip CreateTrimmedLoopClip(AudioClip source, float trimFraction)
        {
            if (source == null)
                return null;

            trimFraction = Mathf.Clamp(trimFraction, 0f, 0.45f);
            int channels = source.channels;
            int frequency = source.frequency;
            int totalSamples = source.samples;
            if (channels <= 0 || frequency <= 0 || totalSamples <= 0)
                return source;

            int trimSamples = Mathf.FloorToInt(totalSamples * trimFraction);
            int keepSamples = totalSamples - (trimSamples * 2);
            if (keepSamples < Mathf.Max(256, frequency / 10))
                return source; // Too short after trim — keep original.

            float[] raw;
            try
            {
                raw = new float[totalSamples * channels];
                if (!source.GetData(raw, 0))
                    return source;
            }
            catch
            {
                // Compressed/non-readable clips can't be sampled at runtime.
                return source;
            }

            float[] trimmedData = new float[keepSamples * channels];
            int srcOffset = trimSamples * channels;
            Array.Copy(raw, srcOffset, trimmedData, 0, trimmedData.Length);

            AudioClip trimmed = AudioClip.Create(
                source.name + "_LoopTrim15",
                keepSamples,
                channels,
                frequency,
                false);
            trimmed.SetData(trimmedData, 0);
            return trimmed;
        }

        private void StopContinuousLaserAudio(bool playStopSound)
        {
            if (!continuousAudioPlaying && (continuousAudio == null || !continuousAudio.isPlaying))
            {
                continuousAudioPlaying = false;
                continuousLoopSourceClip = null;
                return;
            }

            Vector3 pos = continuousAudio != null ? continuousAudio.transform.position : transform.position;
            if (playStopSound && continuousAudioAmmo != null && continuousAudioAmmo.continuousStopSound != null)
                AudioSource.PlayClipAtPoint(continuousAudioAmmo.continuousStopSound, pos);

            if (continuousAudio != null && continuousAudio.isPlaying)
                continuousAudio.Stop();

            continuousAudioPlaying = false;
            continuousAudioAmmo = null;
            continuousLoopSourceClip = null;
        }

        private void SetMiningFxActive(bool active)
        {
            if (laserLine != null)
                laserLine.enabled = active;

            if (laserSightSprite != null)
                laserSightSprite.gameObject.SetActive(active);

            if (impactGlow != null)
                impactGlow.SetActive(false);

            if (!active)
                StopHitSparks();
        }

        private void EnsureProgressUi()
        {
            if (progressCanvas != null)
                return;

            GameObject canvasGo = new GameObject("MiningProgressCanvas");
            canvasGo.transform.SetParent(transform, false);
            progressCanvas = canvasGo.AddComponent<Canvas>();
            progressCanvas.renderMode = RenderMode.WorldSpace;
            canvasGo.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 10f;
            RectTransform canvasRect = canvasGo.GetComponent<RectTransform>();
            canvasRect.sizeDelta = new Vector2(2.4f, 0.55f);
            progressRoot = canvasRect;

            GameObject bg = new GameObject("Bg");
            bg.transform.SetParent(progressRoot, false);
            Image bgImage = bg.AddComponent<Image>();
            bgImage.color = new Color(0.08f, 0.08f, 0.1f, 0.82f);
            RectTransform bgRect = bg.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            GameObject fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(progressRoot, false);
            progressFill = fillGo.AddComponent<Image>();
            progressFill.color = SurvivalPioneerUiPalette.Gold;
            progressFill.type = Image.Type.Filled;
            progressFill.fillMethod = Image.FillMethod.Horizontal;
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = new Vector2(0.05f, 0.12f);
            fillRect.anchorMax = new Vector2(0.95f, 0.42f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            GameObject labelGo = new GameObject("Label");
            labelGo.transform.SetParent(progressRoot, false);
            progressLabel = labelGo.AddComponent<TextMeshProUGUI>();
            progressLabel.fontSize = 0.22f;
            progressLabel.alignment = TextAlignmentOptions.Center;
            progressLabel.color = SurvivalPioneerUiPalette.WarmOffWhite;
            RectTransform labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0.45f);
            labelRect.anchorMax = new Vector2(1f, 1f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        private void UpdateProgressUi(ItemData tool)
        {
            if (lockedNode == null || progressRoot == null)
            {
                SetProgressUiVisible(false);
                return;
            }

            SetProgressUiVisible(true);
            Vector3 pos = ResolveNodePoint(lockedNode) + Vector3.up * 1.15f;
            progressRoot.position = pos;
            Camera cam = ResolveCamera();
            if (cam != null)
                progressRoot.rotation = Quaternion.LookRotation(progressRoot.position - cam.transform.position);

            string name = lockedNode.resourceItem != null ? lockedNode.resourceItem.itemName : "Resource";
            int pass = lockedNode.MiningPassIndex + 1;
            int total = Mathf.Max(1, tool.miningPassesRequired);
            progressLabel.text = $"{name}  {pass}/{total}";
            progressFill.fillAmount = lockedNode.OverallMiningProgress01(tool.miningPassesRequired);
        }

        private void SetProgressUiVisible(bool visible)
        {
            if (progressCanvas != null)
                progressCanvas.gameObject.SetActive(visible);
        }
    }
}
