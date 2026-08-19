using Invector.vShooter;
using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Project.Interaction
{
    /// <summary>
    /// Hold Fire to mine ResourceNodes with a continuous soft-locked red laser, muzzle sparks, and pass-based grants.
    /// Mining tools drain a 0–100% Plasma Fuel charge tank while Fire is held.
    /// Sustained fire overheats after 10s (red base-color heat tint + smoke puff), then 3s cooloff before mining resumes.
    /// </summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(600)]
    public class DMIMiningController : MonoBehaviour
    {
        private const float ProgressRetainSeconds = 4f;
        /// <summary>Resource soft-lock / mining interaction range (meters).</summary>
        public const float MaxMineDistance = 6f;
        /// <summary>F-scan identify range (meters). Wider than mining so scan works before colliders push the tool away.</summary>
        public const float MaxScanDistance = 10f;
        /// <summary>Minimum horizontal distance (meters) from a resource center required to start/maintain F-scan.</summary>
        public const float MinScanStandoffDistance = 3f;
        /// <summary>Visual laser + hit FX range when not locked on a resource (meters).</summary>
        private const float MaxBeamVisualDistance = 50f;
        private const float OverheatSeconds = 10f;
        private const float CooloffSeconds = 3f;
        private const float OverheatBaseMapBlend = 0.92f;
        private const float OverheatVisualIntensityMultiplier = 2.5f;
        private const float HdrpOverheatTintBoost = 1.35f;
        private const string HitSparksPrefabPath = "Assets/_Project/Prefabs/Combat/VFX/SparksLong.prefab";
        private const string DefaultHitEffectPrefabPath = "Assets/_Project/Prefabs/Particles/Hit Effect Laser.prefab";
        private const string OverheatSmokePrefabPath = "Assets/PolygonNature/FX/FX_Prefabs/Smoke_Light_FX.prefab";
        private const string DefaultContinuousLoopPath =
            "Assets/Laser Weapons Sound Pack/Free/continuous_beam_1.wav";
        private const string DefaultContinuousLoopResourcesPath = "Audio/continuous_beam_1";
        private const float EmptyChargeSoundCooldown = 0.45f;
        private static readonly Color LaserRed = new Color(1f, 0.18f, 0.12f, 0.95f);
        private static readonly Color OverheatTint = new Color(1f, 0.22f, 0.08f, 1f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Acquire")]
        [Tooltip("Layers used when acquiring / soft-locking ResourceNodes for mining.")]
        [SerializeField] private LayerMask resourceLayer = ~0;
        [Tooltip("Sphere-cast radius for mining acquire ray (soft lock).")]
        [SerializeField] private float acquireRayRadius = 0.45f;

        [Header("FX / Audio Fallbacks (prefer DM_Mining_Tool ItemData)")]
        [Tooltip("Fallback impact FX only when ItemData.impactVfxPrefab is empty. Primary edit: DM_Mining_Tool → Impact VFX Prefab.")]
        [SerializeField] private GameObject hitSparksPrefab;
        [Tooltip("Optional smoke puff at the mining tool model center on overheat.")]
        [SerializeField] private GameObject overheatSmokePrefab;
        [Tooltip("Optional override for empty-plasma dry-fire. When null, uses the shared pistol empty-click.")]
        [SerializeField] private AudioClip emptyChargeClip;
        [Tooltip("Fallback continuous beam loop if ItemData.continuousLoopSound is empty. Primary edit: DM_Mining_Tool.")]
        [SerializeField] private AudioClip continuousLoopFallback;
        [Tooltip("Fallback start one-shot if ItemData.continuousStartSound is empty.")]
        [SerializeField] private AudioClip continuousStartFallback;
        [Tooltip("Fallback stop one-shot if ItemData.continuousStopSound is empty.")]
        [SerializeField] private AudioClip continuousStopFallback;

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
        private GameObject hitSparksSourcePrefab;

        private WorldNodeProgressBar progressBar;

        private AudioSource continuousAudio;
        private bool continuousAudioPlaying;
        private ItemData continuousAudioAmmo;
        private AudioClip continuousLoopSourceClip;
        private static readonly Dictionary<int, AudioClip> trimmedContinuousLoopCache = new Dictionary<int, AudioClip>(8);
        private const float ContinuousLoopTrimFraction = 0.15f;
        private WeaponAmmoState ammoState;
        private float powerDrainAccumulator;
        private float nextEmptyChargeSoundTime;
        private float nextScanRequiredToastTime;

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
            StopAllMiningFx(playStopSound: false);
            RestoreHeatTint();
            MiningToolResourceCollisionUtility.ClearIgnoredResource();
        }

        private void OnDestroy()
        {
            if (hitSparksInstance != null && !hitSparksAuthored)
                Destroy(hitSparksInstance);
        }

        private void Update()
        {
            ItemData tool = ResolveDrawnMiningTool();
            bool miningTool = tool != null;
            bool fireHeld = miningTool && IsFireHeld();

            if (miningTool)
                TickOverheatState(tool, fireHeld);

            if (!miningTool || !fireHeld || isOverheated)
            {
                if (hasLock && lockedNode != null)
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);

                StopAllMiningFx(playStopSound: true);

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
            ItemData tool = ResolveDrawnMiningTool();
            bool fireHeld = tool != null && IsFireHeld() && !isOverheated;
            if (tool == null || !fireHeld)
            {
                powerDrainAccumulator = 0f;
                return;
            }

            if (!TryDrainMiningPower(tool))
            {
                if (hasLock && lockedNode != null)
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);

                StopAllMiningFx(playStopSound: true);
                PlayEmptyChargeSound();
                return;
            }

            ResolveMuzzle();
            TryBindWeaponLaserStack();
            // Fallback beam must exist when the held mining mesh has no authored Laser stack
            // (DM Mining Tool / Mining Pistol has muzzle tip only). Rebuild if missing.
            EnsureVisuals();
            // Prefer the authored Laser transform (under muzzle) so the beam starts on that stack.
            Vector3 origin = laserRoot != null
                ? laserRoot.position
                : ResolveMuzzleWorldPosition();

            // Soft-lock uses the camera reticle ray; beam + laserSight snap to the lock / reticle point.
            Vector3 camAimDir = ResolveAimDirection(out Vector3 aimOrigin);
            UpdateSoftLock(tool, aimOrigin, camAimDir);

            Vector3 endPoint = ResolveMiningBeamEndPoint(origin, aimOrigin, camAimDir, out bool beamHitCollider);

            UpdateLaserVisuals(origin, endPoint, beamHitCollider, tool);
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

        /// <summary>
        /// When soft-locked, beam + laserSight stick to the mineral hit (within mine range).
        /// Otherwise the visual beam tracks the reticle out to <see cref="MaxBeamVisualDistance"/>
        /// with impact sparks on collider hits. Resource acquire/mine still use <see cref="MaxMineDistance"/>.
        /// </summary>
        private Vector3 ResolveMiningBeamEndPoint(
            Vector3 muzzleOrigin,
            Vector3 aimOrigin,
            Vector3 camAimDir,
            out bool hitCollider)
        {
            hitCollider = false;

            if (hasLock && lockedNode != null)
            {
                // Keep lockPoint glued to the node surface under (or nearest) the reticle.
                if (!TryGetLockPointOnNode(lockedNode, aimOrigin, camAimDir, out lockPoint))
                    lockPoint = ResolveNodePoint(lockedNode);

                hitCollider = true;
                return lockPoint;
            }

            // Unlocked: visual beam / hit FX use the longer beam range (not mining acquire range).
            Vector3 reticlePoint = aimOrigin + camAimDir * MaxBeamVisualDistance;
            bool camHitSurface = Physics.Raycast(
                aimOrigin,
                camAimDir,
                out RaycastHit camHit,
                MaxBeamVisualDistance,
                ~0,
                QueryTriggerInteraction.Ignore);
            if (camHitSurface)
                reticlePoint = camHit.point;

            Vector3 toReticle = reticlePoint - muzzleOrigin;
            float dist = toReticle.magnitude;
            if (dist < 0.001f)
            {
                hitCollider = camHitSurface;
                return reticlePoint;
            }

            Vector3 beamDir = toReticle / dist;
            if (Physics.Raycast(
                    muzzleOrigin,
                    beamDir,
                    out RaycastHit muzzleHit,
                    dist,
                    ~0,
                    QueryTriggerInteraction.Ignore))
            {
                hitCollider = true;
                return muzzleHit.point;
            }

            hitCollider = camHitSurface;
            return reticlePoint;
        }

        private void TickOverheatState(ItemData tool, bool fireHeld)
        {
            if (isOverheated)
            {
                cooloffRemaining -= Time.deltaTime;
                // coolT: 1 at overheat start → 0 when cooloff ends (glow fades back to normal).
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
                    SpawnOverheatSmokePuff(tool);
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

        private void PlayEmptyChargeSound()
        {
            if (Time.unscaledTime < nextEmptyChargeSoundTime)
                return;

            nextEmptyChargeSoundTime = Time.unscaledTime + EmptyChargeSoundCooldown;

            // Same empty-mag click as pistols/rifles — not a gunshot and not the old electricity SFX.
            if (emptyChargeClip != null)
            {
                Vector3 pos = ResolveMiningToolCenter(ResolveDrawnMiningTool());
                AudioSource.PlayClipAtPoint(emptyChargeClip, pos, 0.85f);
                return;
            }

            PlayDryFireClick();
        }

        private void PlayOverheatClick()
        {
            PlayDryFireClick();
        }

        private void PlayDryFireClick()
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
                if (renderer == null || renderer is ParticleSystemRenderer || renderer is LineRenderer)
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

        private static bool IsHdrpShader(Shader shader)
        {
            if (shader == null)
                return false;

            string name = shader.name;
            return name.StartsWith("HDRP/", StringComparison.Ordinal)
                   || name.StartsWith("Hidden/HDRP", StringComparison.Ordinal);
        }

        private void ApplyHeatTint(ItemData tool, float heat01)
        {
            EnsureHeatRenderers(tool);
            if (heatRenderers == null || heatRenderers.Length == 0)
                return;

            heatPropertyBlock ??= new MaterialPropertyBlock();
            heat01 = Mathf.Clamp01(heat01);
            float baseMapBlend = Mathf.Clamp01(heat01 * OverheatBaseMapBlend * OverheatVisualIntensityMultiplier);

            for (int i = 0; i < heatRenderers.Length; i++)
            {
                Renderer renderer = heatRenderers[i];
                if (renderer == null)
                    continue;

                Color baseColor = i < heatBaseColors.Length ? heatBaseColors[i] : Color.white;
                Color tintTarget = OverheatTint;
                Material sharedMat = renderer.sharedMaterial;
                if (sharedMat != null && IsHdrpShader(sharedMat.shader))
                    tintTarget *= HdrpOverheatTintBoost;

                Color tinted = Color.Lerp(baseColor, tintTarget, baseMapBlend);

                int materialCount = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                    ? renderer.sharedMaterials.Length
                    : 1;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                {
                    renderer.GetPropertyBlock(heatPropertyBlock, materialIndex);
                    heatPropertyBlock.SetColor(BaseColorId, tinted);
                    heatPropertyBlock.SetColor(ColorId, tinted);
                    renderer.SetPropertyBlock(heatPropertyBlock, materialIndex);
                }
            }
        }

        private void RestoreHeatTint()
        {
            if (heatRenderers == null)
                return;

            for (int i = 0; i < heatRenderers.Length; i++)
            {
                Renderer renderer = heatRenderers[i];
                if (renderer == null)
                    continue;

                int materialCount = renderer.sharedMaterials != null && renderer.sharedMaterials.Length > 0
                    ? renderer.sharedMaterials.Length
                    : 1;
                for (int materialIndex = 0; materialIndex < materialCount; materialIndex++)
                    renderer.SetPropertyBlock(null, materialIndex);
            }

            heatVisualRoot = null;
            heatRenderers = null;
            heatBaseColors = null;
        }

        /// <summary>
        /// One-shot smoke puff attached to the drawn mining tool model (not the mineral node).
        /// </summary>
        private void SpawnOverheatSmokePuff(ItemData tool)
        {
            EnsureOverheatSmokePrefab();

            GameObject toolVisual = weaponBridge != null ? weaponBridge.TryGetWeaponInstance(tool) : null;
            if (toolVisual == null)
                toolVisual = heatVisualRoot;

            if (toolVisual == null)
                return;

            Vector3 center = ResolveMiningToolCenter(tool);

            GameObject puff;
            if (overheatSmokePrefab != null)
            {
                puff = Instantiate(overheatSmokePrefab);
                puff.name = "MiningOverheatSmoke";
            }
            else
            {
                puff = CreateRuntimeSmokePuff(center);
            }

            if (puff == null)
                return;

            // Parent to the tool so the puff rides the weapon, not the mined node.
            puff.transform.SetParent(toolVisual.transform, false);
            puff.transform.position = center;
            puff.transform.rotation = Quaternion.identity;
            // Keep a small world-ish scale even if the weapon hierarchy is scaled oddly.
            Vector3 lossy = toolVisual.transform.lossyScale;
            puff.transform.localScale = new Vector3(
                0.45f / Mathf.Max(0.05f, Mathf.Abs(lossy.x)),
                0.45f / Mathf.Max(0.05f, Mathf.Abs(lossy.y)),
                0.45f / Mathf.Max(0.05f, Mathf.Abs(lossy.z)));

            ParticleSystem[] systems = puff.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                if (ps == null)
                    continue;

                var main = ps.main;
                main.loop = false;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                if (!ps.isPlaying)
                    ps.Play(true);
            }

            Destroy(puff, 3.5f);
        }

        private Vector3 ResolveMiningToolCenter(ItemData tool)
        {
            EnsureHeatRenderers(tool);

            GameObject toolVisual = heatVisualRoot;
            if (toolVisual == null && weaponBridge != null && tool != null)
                toolVisual = weaponBridge.TryGetWeaponInstance(tool);

            if (toolVisual != null)
            {
                Renderer[] rends = toolVisual.GetComponentsInChildren<Renderer>(true);
                bool hasBounds = false;
                Bounds bounds = default;
                for (int i = 0; i < rends.Length; i++)
                {
                    Renderer r = rends[i];
                    if (r == null || r is ParticleSystemRenderer || !r.enabled || !r.gameObject.activeInHierarchy)
                        continue;

                    if (!hasBounds)
                    {
                        bounds = r.bounds;
                        hasBounds = true;
                    }
                    else
                    {
                        bounds.Encapsulate(r.bounds);
                    }
                }

                if (hasBounds)
                    return bounds.center;

                return toolVisual.transform.position;
            }

            if (muzzleTransform != null)
                return muzzleTransform.position;

            return transform.position + Vector3.up * 1.2f;
        }

        private void EnsureOverheatSmokePrefab()
        {
            if (overheatSmokePrefab != null)
                return;

#if UNITY_EDITOR
            overheatSmokePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(OverheatSmokePrefabPath);
#endif
        }

        private static GameObject CreateRuntimeSmokePuff(Vector3 worldPos)
        {
            GameObject go = new GameObject("MiningOverheatSmoke_Runtime");
            go.transform.position = worldPos;

            ParticleSystem ps = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.45f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 2.2f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.35f, 0.9f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.7f);
            main.startColor = new Color(0.55f, 0.55f, 0.58f, 0.65f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 48;
            main.gravityModifier = -0.15f;

            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, 18, 28) });

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.08f;

            var colorOverLifetime = ps.colorOverLifetime;
            colorOverLifetime.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new[]
                {
                    new GradientColorKey(new Color(0.7f, 0.7f, 0.72f), 0f),
                    new GradientColorKey(new Color(0.45f, 0.45f, 0.48f), 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.7f, 0f),
                    new GradientAlphaKey(0.25f, 0.55f),
                    new GradientAlphaKey(0f, 1f)
                });
            colorOverLifetime.color = grad;

            var sizeOverLifetime = ps.sizeOverLifetime;
            sizeOverLifetime.enabled = true;
            sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.6f, 1f, 1.6f));

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                Material mat = CreateHdrpSafeUnlitMaterial(
                    new Color(0.6f, 0.6f, 0.62f, 0.55f),
                    "DM_MiningSmoke (Runtime)");
                if (mat != null)
                    renderer.sharedMaterial = mat;
            }

            ps.Play(true);
            return go;
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

            float drainPerSecond = tool.miningChargeDrainPerSecond > 0f
                ? tool.miningChargeDrainPerSecond
                : Mathf.Max(1f, tool.fireRate);

            // Drain charge % over time while Fire is held (1 unit = 1%).
            powerDrainAccumulator += drainPerSecond * Time.deltaTime;
            while (powerDrainAccumulator >= 1f)
            {
                powerDrainAccumulator -= 1f;
                if (!ammoState.TryConsumeActiveRound())
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Mining only while the mining tool is physically drawn — EquippedItem stays valid when holstered.
        /// </summary>
        private ItemData ResolveDrawnMiningTool()
        {
            if (equipment == null || !equipment.IsWeaponDrawn)
                return null;

            ItemData tool = equipment.DrawnWeaponItem;
            if (tool == null || !tool.isMiningTool || !tool.IsRangedWeapon)
                return null;

            return tool;
        }

        /// <summary>
        /// Journal / inventory / map / pause must not drive mining from UI clicks or leftover Fire holds.
        /// </summary>
        private bool IsMiningInputBlocked()
        {
            if (player == null)
                player = GetComponent<PlayerController>();

            return player != null && player.BlocksCombatInput;
        }

        private bool IsFireHeld()
        {
            if (IsMiningInputBlocked())
                return false;

            if (Mouse.current != null && Mouse.current.leftButton.isPressed)
                return true;

            if (Gamepad.current != null && Gamepad.current.rightTrigger.isPressed)
                return true;

            return false;
        }

        private void StopAllMiningFx(bool playStopSound)
        {
            ClearLock();
            SetMiningFxActive(false);
            SetProgressUiVisible(false);
            StopContinuousLaserAudio(playStopSound);
            StopHitSparks();
            DMILaserBurnMarkSpawner.ResetMiningStampState();
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
                float nodeDist = Vector3.Distance(transform.position, ResolveNodePoint(lockedNode));
                if (nodeDist > MaxMineDistance)
                {
                    lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);
                    ClearLock();
                }
                else
                {
                    if (!TryGetLockPointOnNode(lockedNode, aimOrigin, aimDir, out Vector3 refreshed))
                        refreshed = ResolveNodePoint(lockedNode);

                    Vector3 toLock = (refreshed - aimOrigin).normalized;
                    float liveAngle = Vector3.Angle(aimDir, toLock);
                    if (liveAngle > breakDegrees)
                    {
                        lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);
                        ClearLock();
                    }
                    else
                    {
                        lockPoint = refreshed;
                        lockDirection = toLock;
                        return;
                    }
                }
            }

            // Non-convex MeshColliders on mineral nodes only respond to Raycast (not SphereCast).
            // Prefer a precise Raycast, then a thin SphereCast fallback for legacy box/convex rocks.
            RaycastHit hit;
            bool acquired = Physics.Raycast(
                aimOrigin,
                aimDir,
                out hit,
                MaxMineDistance,
                resourceLayer,
                QueryTriggerInteraction.Ignore);

            if (!acquired)
            {
                acquired = Physics.SphereCast(
                    aimOrigin,
                    Mathf.Max(0.05f, acquireRayRadius * 0.35f),
                    aimDir,
                    out hit,
                    MaxMineDistance,
                    resourceLayer,
                    QueryTriggerInteraction.Ignore);
            }

            if (acquired)
            {
                ResourceNode node = hit.collider.GetComponentInParent<ResourceNode>();
                ItemData drawnTool = ResolveDrawnMiningTool();
                if (node != null
                    && node.resourceItem != null
                    && node.interactionMode == ResourceNodeInteractionMode.LaserMine
                    && Vector3.Distance(transform.position, hit.point) <= MaxMineDistance
                    && node.AllowsMiningToolIgnoringIdentification(drawnTool))
                {
                    if (!node.IsResourceIdentified)
                    {
                        MaybeToastScanRequired();
                        return;
                    }

                    if (node.AllowsMiningTool(drawnTool))
                    {
                        lockedNode = node;
                        lockPoint = hit.point;
                        lockDirection = aimDir.normalized;
                        hasLock = true;
                        MiningToolResourceCollisionUtility.PushIgnoredResource(node, transform);
                    }
                }
            }
        }

        private void MaybeToastScanRequired()
        {
            if (Time.unscaledTime < nextScanRequiredToastTime)
                return;

            nextScanRequiredToastTime = Time.unscaledTime + 1.25f;
            PickupToastUI.Show("Scan required (Hold F)");
        }

        /// <summary>
        /// Finds the reticle ray hit on this specific mineral node, else a safe collider approximation.
        /// </summary>
        public static bool TryGetLockPointOnNode(
            ResourceNode node,
            Vector3 aimOrigin,
            Vector3 aimDir,
            float maxDistance,
            out Vector3 point)
        {
            point = default;
            if (node == null)
                return false;

            RaycastHit[] hits = Physics.RaycastAll(
                aimOrigin,
                aimDir,
                maxDistance,
                ~0,
                QueryTriggerInteraction.Ignore);

            float bestDist = float.MaxValue;
            bool found = false;
            for (int i = 0; i < hits.Length; i++)
            {
                ResourceNode hitNode = hits[i].collider.GetComponentInParent<ResourceNode>();
                if (hitNode != node)
                    continue;

                if (hits[i].distance >= bestDist)
                    continue;

                bestDist = hits[i].distance;
                point = hits[i].point;
                found = true;
            }

            if (found)
                return true;

            Vector3 fallbackReference = aimOrigin + aimDir * Mathf.Min(maxDistance, 2f);
            Collider[] colliders = node.GetComponentsInChildren<Collider>();
            float closestSqrDistance = float.MaxValue;
            bool hasColliderFallback = false;

            for (int i = 0; i < colliders.Length; i++)
            {
                Collider collider = colliders[i];
                if (collider == null || !collider.enabled || !collider.gameObject.activeInHierarchy)
                    continue;

                Vector3 candidate = SupportsClosestPoint(collider)
                    ? collider.ClosestPoint(fallbackReference)
                    : collider.bounds.ClosestPoint(fallbackReference);
                float sqrDistance = (candidate - fallbackReference).sqrMagnitude;
                if (sqrDistance >= closestSqrDistance)
                    continue;

                closestSqrDistance = sqrDistance;
                point = candidate;
                hasColliderFallback = true;
            }

            if (hasColliderFallback)
                return true;

            point = ResolveNodePoint(node);
            return true;
        }

        /// <summary>
        /// Finds the reticle ray hit on this specific mineral node, else a safe collider approximation.
        /// </summary>
        private static bool TryGetLockPointOnNode(
            ResourceNode node,
            Vector3 aimOrigin,
            Vector3 aimDir,
            out Vector3 point)
        {
            return TryGetLockPointOnNode(node, aimOrigin, aimDir, MaxMineDistance, out point);
        }

        private static bool SupportsClosestPoint(Collider collider)
        {
            if (collider is BoxCollider
                || collider is SphereCollider
                || collider is CapsuleCollider)
            {
                return true;
            }

            return collider is MeshCollider meshCollider && meshCollider.convex;
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

            if (Vector3.Distance(transform.position, ResolveNodePoint(lockedNode)) > MaxMineDistance)
            {
                lockedNode.NotifyMiningInterrupted(ProgressRetainSeconds);
                ClearLock();
                SetProgressUiVisible(false);
                return;
            }

            float duration = lockedNode.ResolvePassDuration(tool.miningPassDuration);
            int passes = lockedNode.ResolvePassCount(tool.miningPassesRequired);

            bool passCompleted = lockedNode.TickMining(
                gatherer,
                Time.deltaTime,
                duration,
                passes,
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
            ResourceNode previous = lockedNode;
            hasLock = false;
            lockedNode = null;
            if (previous != null)
                MiningToolResourceCollisionUtility.PopIgnoredResource(previous);
        }

        private void EnsureVisuals()
        {
            EnsureFallbackHitSparksPrefab();
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
                laserLine.startColor = LaserRed;
                laserLine.endColor = new Color(LaserRed.r, LaserRed.g, LaserRed.b, 0.55f);
                laserLine.enabled = false;
                usingWeaponLaserStack = false;
                laserRoot = null;
            }

            // HDRP player builds strip URP Unlit — never leave a null/invalid LineRenderer material.
            if (laserLine != null &&
                (laserLine.sharedMaterial == null ||
                 laserLine.sharedMaterial.shader == null ||
                 !laserLine.sharedMaterial.shader.isSupported))
            {
                Material mat = CreateHdrpSafeUnlitMaterial(LaserRed, "DM_MiningLaserLine (Runtime)");
                if (mat != null)
                    laserLine.material = mat;
            }

            EnsureHitSparksInstance();
        }

        /// <summary>
        /// LineRenderer / particle materials for this HDRP project. URP Unlit Shader.Find fails in
        /// player builds and leaves the mining beam invisible when plasma charge is draining.
        /// </summary>
        private static Material CreateHdrpSafeUnlitMaterial(Color color, string materialName)
        {
            Shader shader = Shader.Find("HDRP/Unlit")
                ?? Shader.Find("Sprites/Default")
                ?? Shader.Find("Unlit/Color")
                ?? Shader.Find("Legacy Shaders/Particles/Alpha Blended Premultiply");
            if (shader == null)
                return null;

            Material mat = new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave,
                color = color
            };

            if (mat.HasProperty("_UnlitColor"))
                mat.SetColor("_UnlitColor", color);
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            return mat;
        }

        /// <summary>
        /// Priority: tool.impactVfxPrefab → inspector hitSparksPrefab → Hit Effect Laser → SparksLong.
        /// Does not overwrite the inspector fallback when a tool supplies its own Impact VFX.
        /// </summary>
        private GameObject ResolveHitSparksPrefab(ItemData tool)
        {
            if (tool != null && tool.impactVfxPrefab != null)
                return tool.impactVfxPrefab;

            EnsureFallbackHitSparksPrefab();
            return hitSparksPrefab;
        }

        private void EnsureFallbackHitSparksPrefab()
        {
            if (hitSparksPrefab != null)
                return;

#if UNITY_EDITOR
            hitSparksPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultHitEffectPrefabPath);
            if (hitSparksPrefab == null)
                hitSparksPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(HitSparksPrefabPath);
#endif
        }

        private void EnsureHitSparksInstance(ItemData tool = null)
        {
            GameObject preferred = ResolveHitSparksPrefab(tool);

            // Rebuild if the resolved impact prefab changed (or first create).
            if (hitSparksInstance != null && hitSparksInstance
                && hitSparksSourcePrefab == preferred)
                return;

            if (hitSparksInstance != null && !hitSparksAuthored)
                Destroy(hitSparksInstance);

            hitSparksInstance = null;
            hitSparksParticles = null;
            hitSparksSourcePrefab = preferred;

            if (preferred == null)
                return;

            hitSparksAuthored = false;
            hitSparksInstance = Instantiate(preferred);
            hitSparksInstance.name = "MiningHitEffect";
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

        private void UpdateLaserVisuals(Vector3 muzzlePos, Vector3 endPoint, bool hitCollider, ItemData tool)
        {
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.useWorldSpace = true;
                laserLine.positionCount = 2;
                // Force world-space endpoints every frame so the tool LineRenderer stays
                // aligned with the reticle / soft-locked mineral (vLaserSight is disabled).
                laserLine.SetPosition(0, muzzlePos);
                laserLine.SetPosition(1, endPoint);
            }

            if (laserSightSprite != null)
            {
                laserSightSprite.gameObject.SetActive(true);
                // Soft-lock: snap the laser reticle onto the mineral surface.
                laserSightSprite.position = endPoint;
                Camera cam = ResolveCamera();
                if (cam != null)
                {
                    Vector3 toCam = cam.transform.position - endPoint;
                    if (toCam.sqrMagnitude > 0.0001f)
                        laserSightSprite.rotation = Quaternion.LookRotation(-toCam.normalized, Vector3.up);
                }
            }

            // Sparks / impact FX only when the beam lands on a collider (resource nodes included).
            if (hitCollider)
            {
                Vector3 beamDelta = endPoint - muzzlePos;
                UpdateHitSparks(endPoint, beamDelta, tool);
                // Parent mining burns to the soft-locked resource so deplete clears them with the node.
                Transform burnAttach = (hasLock && lockedNode != null) ? lockedNode.transform : null;
                DMILaserBurnMarkSpawner.TryStampMining(endPoint, beamDelta, burnAttach);
            }
            else
            {
                StopHitSparks();
            }
        }

        private void UpdateHitSparks(Vector3 endPoint, Vector3 beamDelta, ItemData tool)
        {
            EnsureHitSparksInstance(tool);
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

        /// <summary>
        /// Mining beam audio is authored on the mining tool ItemData (DM_Mining_Tool).
        /// Optional defaultAmmoItem continuous clips remain as a legacy secondary source.
        /// </summary>
        private static ItemData ResolveContinuousLaserAudioSource(ItemData tool)
        {
            if (tool == null)
                return null;

            // Prefer the tool itself — Plasma Fuel mining wires loop/start/stop on DM_Mining_Tool.
            if (tool.isMiningTool || tool.isContinuousLaser || tool.continuousLoopSound != null)
                return tool;

            if (tool.defaultAmmoItem != null &&
                (tool.defaultAmmoItem.isContinuousLaser ||
                 tool.defaultAmmoItem.continuousLoopSound != null ||
                 tool.defaultAmmoItem.isHitscanBeam))
                return tool.defaultAmmoItem;

            return tool;
        }

        private AudioClip ResolveContinuousLoopClip(ItemData audioSource)
        {
            if (audioSource != null)
            {
                if (audioSource.continuousLoopSound != null)
                    return audioSource.continuousLoopSound;
                if (audioSource.projectileTravelSound != null)
                    return audioSource.projectileTravelSound;
            }

            if (continuousLoopFallback != null)
                return continuousLoopFallback;

            EnsureDefaultContinuousLoopFallback();
            return continuousLoopFallback;
        }

        private AudioClip ResolveContinuousStartClip(ItemData audioSource)
        {
            if (audioSource != null && audioSource.continuousStartSound != null)
                return audioSource.continuousStartSound;
            return continuousStartFallback;
        }

        private AudioClip ResolveContinuousStopClip(ItemData audioSource)
        {
            if (audioSource != null && audioSource.continuousStopSound != null)
                return audioSource.continuousStopSound;
            return continuousStopFallback;
        }

        private void EnsureDefaultContinuousLoopFallback()
        {
            if (continuousLoopFallback != null)
                return;

            continuousLoopFallback = Resources.Load<AudioClip>(DefaultContinuousLoopResourcesPath);
#if UNITY_EDITOR
            if (continuousLoopFallback == null)
                continuousLoopFallback = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(DefaultContinuousLoopPath);
#endif
        }

        private void UpdateContinuousLaserAudio(ItemData tool, Vector3 muzzlePos)
        {
            EnsureContinuousAudio();
            ItemData audioSource = ResolveContinuousLaserAudioSource(tool);
            continuousAudioAmmo = audioSource;

            AudioClip sourceLoop = ResolveContinuousLoopClip(audioSource);

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
                AudioClip startClip = ResolveContinuousStartClip(audioSource);
                if (startClip != null)
                    AudioSource.PlayClipAtPoint(startClip, muzzlePos);

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

            int key = source.GetEntityId().GetHashCode();
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
            if (playStopSound)
            {
                AudioClip stopClip = ResolveContinuousStopClip(continuousAudioAmmo);
                if (stopClip != null)
                    AudioSource.PlayClipAtPoint(stopClip, pos);
            }

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
            if (progressBar != null)
                return;

            progressBar = WorldNodeProgressBar.Create(transform);
        }

        private void UpdateProgressUi(ItemData tool)
        {
            if (lockedNode == null)
            {
                SetProgressUiVisible(false);
                return;
            }

            EnsureProgressUi();
            if (progressBar == null)
                return;

            int total = lockedNode.ResolvePassCount(tool != null ? tool.miningPassesRequired : 1);
            string name = lockedNode.GetDisplayName();
            int pass = lockedNode.MiningPassIndex + 1;
            // Slider tracks hold time for the current mining wave (0→1 over passDuration).
            progressBar.UpdateBar(
                ResolveNodePoint(lockedNode) + Vector3.up * 0.75f,
                lockedNode.MiningPassProgress01,
                $"{name}  {pass}/{total}",
                ResolveCamera());
        }

        private void SetProgressUiVisible(bool visible)
        {
            if (progressBar != null)
                progressBar.SetVisible(visible);
        }
    }
}
