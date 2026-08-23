using Project.Combat;
using Project.Data;
using Project.Inventory;
using Project.Map;
using Project.UI;
using UnityEngine;

namespace Project.Interaction
{
    public enum ResourceNodeInteractionMode
    {
        LaserMine = 0,
        HoldHarvest = 1
    }

    /// <summary>
    /// World resource that can be melee-gathered, laser-mined in waves, or hold-E harvested.
    /// Laser / hold paths grant on every completed wave (last wave uses a yield scale).
    /// </summary>
    public class ResourceNode : MonoBehaviour, IDamageable, IHoldWorldUsable
    {
        [Header("Resource / Loot")]
        [Tooltip("Item granted to the player inventory when this node finishes a mine/harvest wave.")]
        public ItemData resourceItem;
        public int amountPerGather = 1;
        public int maxHits = 3;

        [Header("Interaction Mode")]
        public ResourceNodeInteractionMode interactionMode = ResourceNodeInteractionMode.LaserMine;

        [Header("Mining / Harvest Waves")]
        [Tooltip("Seconds of continuous work per wave. Used when > 0 (overrides tool default).")]
        public float passDuration = 5f;
        [Tooltip("Number of waves / passes. Used when > 0 (overrides tool default).")]
        public int waves = 1;
        [Tooltip("Minimum items granted per completed wave (before last-wave scale).")]
        public int dropMin = 1;
        [Tooltip("Maximum items granted per completed wave (before last-wave scale).")]
        public int dropMax = 3;
        [Tooltip("Multiplies min/max on the final wave only (e.g. 0.6).")]
        [Range(0.1f, 1f)]
        public float lastWaveDropScale = 0.6f;

        [Header("Hold Harvest")]
        [Tooltip("Hold-E duration when interactionMode is HoldHarvest. Falls back to passDuration.")]
        public float holdDurationSeconds = 4f;
        [Tooltip("Legacy authoring string. Plant harvest uses proximity dots + map markers instead of Hold-E prompt UI.")]
        public string holdPromptText = "Hold E — Harvest";
        public float holdInteractRange = 3.5f;

        [Header("Tool Requirements")]
        [Tooltip("Optional specific tool. Null = any valid tool for this mode (drawn mining laser / bare hands).")]
        public ItemData requiredTool;
        [Tooltip("When true, laser-mine nodes only accept equipped isMiningTool weapons.")]
        public bool requireMiningLaser = true;

        [Header("Loot Attract (fly-to-player)")]
        [Tooltip("Visual that flies from the node to the player before inventory grant. " +
                 "If empty, uses resourceItem.worldPrefab, then a tinted orb fallback.")]
        public GameObject lootAttractPrefab;
        [Tooltip("Tint applied when using the procedural orb fallback (or materials that read vertex color).")]
        public Color lootTint = new Color(0.82f, 0.72f, 0.35f, 1f);
        [Tooltip("Optional yield SFX override. Empty falls back to MineHarvestItemData.lootYieldClip, then built-in defaults.")]
        public AudioClip lootYieldClipOverride;
        [Tooltip("Optional grant SFX override when loot reaches the player.")]
        public AudioClip lootGrantClipOverride;

        private int currentHits;
        private int miningPassIndex;
        private float miningPassProgress;
        private float miningProgressRetainUntil = -1f;

        private bool holdActive;
        private float holdProgress;
        private float holdRetainUntil = -1f;
        private WorldNodeProgressBar holdProgressBar;

        public ItemData ResourceItem => resourceItem;
        public int MiningPassIndex => miningPassIndex;
        public float MiningPassProgress01 => Mathf.Clamp01(miningPassProgress);
        public bool IsHoldActive => holdActive;

        /// <summary>True when this node's resource type has been identified by multi-tool F-scan.</summary>
        public bool IsResourceIdentified =>
            resourceItem != null && ResourceIdentificationRegistry.IsIdentified(resourceItem);

        public string GetDisplayName()
        {
            if (resourceItem == null)
                return "Resource";

            if (!IsResourceIdentified)
            {
                if (resourceItem is MineHarvestItemData lean
                    && !string.IsNullOrWhiteSpace(lean.unknownDisplayName))
                {
                    return lean.unknownDisplayName;
                }

                return "Unknown Resource";
            }

            return string.IsNullOrEmpty(resourceItem.itemName) ? resourceItem.name : resourceItem.itemName;
        }

        public string GetUnknownDisplayName()
        {
            if (resourceItem is MineHarvestItemData lean
                && !string.IsNullOrWhiteSpace(lean.unknownDisplayName))
            {
                return lean.unknownDisplayName;
            }

            return "Unknown Resource";
        }

        public float HoldDurationSeconds =>
            holdDurationSeconds > 0.05f ? holdDurationSeconds : Mathf.Max(0.05f, passDuration);

        public string HoldPromptText =>
            string.IsNullOrWhiteSpace(holdPromptText) ? "Hold E — Harvest" : holdPromptText;

        public float OverallMiningProgress01(int passesRequired)
        {
            int passes = Mathf.Max(1, passesRequired);
            return Mathf.Clamp01((miningPassIndex + Mathf.Clamp01(miningPassProgress)) / passes);
        }

        public float OverallProgress01 =>
            OverallMiningProgress01(ResolvePassCount(0));

        public bool IsFullyMined(int passesRequired) =>
            miningPassIndex >= Mathf.Max(1, passesRequired);

        public float ResolvePassDuration(float toolFallback)
        {
            if (passDuration > 0.05f)
                return passDuration;
            return Mathf.Max(0.05f, toolFallback);
        }

        public int ResolvePassCount(int toolFallback)
        {
            if (waves > 0)
                return waves;
            return Mathf.Max(1, toolFallback);
        }

        public void ResolveDropRange(int toolMin, int toolMax, out int min, out int max)
        {
            if (dropMin > 0 || dropMax > 0)
            {
                min = Mathf.Max(1, Mathf.Min(dropMin, dropMax > 0 ? dropMax : dropMin));
                max = Mathf.Max(min, Mathf.Max(dropMin, dropMax));
                return;
            }

            min = Mathf.Max(1, Mathf.Min(toolMin, toolMax));
            max = Mathf.Max(min, Mathf.Max(toolMin, toolMax));
        }

        private void OnEnable()
        {
            ResourceIdentificationRegistry.Changed += OnIdentificationChanged;
            EnsureInteractionVolume();

            if (interactionMode != ResourceNodeInteractionMode.HoldHarvest)
                return;

            EnsureHarvestMapMarker();
            PickupProximityDotUI.RegisterHarvestNode(this);
        }

        private void OnDisable()
        {
            ResourceIdentificationRegistry.Changed -= OnIdentificationChanged;
            PickupProximityDotUI.UnregisterHarvestNode(this);
        }

        private void OnIdentificationChanged()
        {
            // When an identification is added or removed, refresh the harvest map marker so the
            // display name switches between "Unknown Resource" and the identified item name.
            if (interactionMode == ResourceNodeInteractionMode.HoldHarvest)
                EnsureHarvestMapMarker();
        }

        private void Update()
        {
            if (miningProgressRetainUntil > 0f && Time.time > miningProgressRetainUntil)
            {
                miningPassProgress = 0f;
                miningProgressRetainUntil = -1f;
            }

            if (!holdActive && holdRetainUntil > 0f && Time.time > holdRetainUntil)
            {
                holdProgress = 0f;
                holdRetainUntil = -1f;
                SetHoldProgressBarVisible(false);
            }
        }

        private void OnDestroy()
        {
            PickupProximityDotUI.UnregisterHarvestNode(this);
            SetHoldProgressBarVisible(false);
        }

        private void EnsureHarvestMapMarker()
        {
            MapMarker marker = GetComponent<MapMarker>();
            if (marker == null)
                marker = gameObject.AddComponent<MapMarker>();

            if (resourceItem != null)
                marker.ConfigureForResource(resourceItem);
        }

        public void Gather(ResourceGatherer gatherer) => Gather(gatherer, 1);

        public void Gather(ResourceGatherer gatherer, int hitStrength)
        {
            if (interactionMode == ResourceNodeInteractionMode.HoldHarvest)
                return;

            currentHits++;
            currentHits += Mathf.Max(0, hitStrength - 1);
            if (currentHits >= maxHits && gatherer != null && resourceItem != null)
            {
                if (gatherer.TryGather(resourceItem, amountPerGather))
                    FinishGatherAndDestroy();
            }
        }

        public void TakeDamage(float damage, GameObject source, bool isCritical = false)
        {
            if (interactionMode == ResourceNodeInteractionMode.HoldHarvest)
                return;

            currentHits += Mathf.Max(1, Mathf.RoundToInt(damage));
            if (currentHits >= maxHits)
            {
                ResourceGatherer gatherer = source != null
                    ? source.GetComponentInParent<ResourceGatherer>()
                    : null;
                if (gatherer == null)
                    gatherer = FindAnyObjectByType<ResourceGatherer>();

                if (gatherer != null && resourceItem != null)
                    gatherer.TryGather(resourceItem, amountPerGather);

                FinishGatherAndDestroy();
            }
        }

        /// <summary>
        /// Advances mining while Fire is held. Grants loot attract on every completed wave.
        /// Destroys the node after the final wave grant is spawned.
        /// </summary>
        public bool TickMining(
            ResourceGatherer gatherer,
            float deltaTime,
            float passDurationFallback,
            int passesRequiredFallback,
            int dropMinFallback,
            int dropMaxFallback,
            float progressRetainSeconds,
            out bool finishedNode,
            out int grantedAmount)
        {
            finishedNode = false;
            grantedAmount = 0;

            if (interactionMode != ResourceNodeInteractionMode.LaserMine)
                return false;

            if (gatherer == null || resourceItem == null)
                return false;

            int passes = ResolvePassCount(passesRequiredFallback);
            float duration = ResolvePassDuration(passDurationFallback);
            ResolveDropRange(dropMinFallback, dropMaxFallback, out int baseMin, out int baseMax);
            miningProgressRetainUntil = Time.time + Mathf.Max(0.1f, progressRetainSeconds);

            if (miningPassIndex >= passes)
            {
                finishedNode = true;
                return false;
            }

            miningPassProgress += deltaTime / duration;
            if (miningPassProgress < 1f)
                return false;

            miningPassProgress = 0f;
            miningPassIndex++;

            bool isLastWave = miningPassIndex >= passes;
            grantedAmount = RollWaveYield(baseMin, baseMax, isLastWave);

            if (!CanReserveGrant(gatherer, grantedAmount))
            {
                miningPassIndex = Mathf.Max(0, miningPassIndex - 1);
                miningPassProgress = 0.99f;
                grantedAmount = 0;
                return true;
            }

            SpawnLootAttract(gatherer, grantedAmount);

            if (isLastWave)
            {
                finishedNode = true;
                FinishGatherAndDestroy();
            }

            return true;
        }

        public void NotifyMiningInterrupted(float progressRetainSeconds)
        {
            miningProgressRetainUntil = Time.time + Mathf.Max(0.1f, progressRetainSeconds);
        }

        public float GetUsePriority(WorldUseContext context)
        {
            if (interactionMode != ResourceNodeInteractionMode.HoldHarvest)
                return -1f;
            if (resourceItem == null || context.Gatherer == null)
                return -1f;

            float distance = DistanceToClosestPoint(context.PlayerPosition);
            if (distance > holdInteractRange)
                return -1f;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null)
            {
                ResourceNode hitNode = context.AimHit.Value.collider.GetComponentInParent<ResourceNode>();
                if (hitNode == this)
                    return 92f - distance;
            }

            // Reach is measured to the surface. Aim accepts a collider hit or a tight AABB graze
            // (no 1.1m center slop — scaled rocks were unusable or falsely selected).
            if (!IsViewAimedAtNode(context.ViewRay))
                return -1f;

            return 85f - distance;
        }

        public bool TryUse(WorldUseContext context)
        {
            // Hold harvest is driven by TickHold while E is held — press alone starts hold via controller.
            if (interactionMode != ResourceNodeInteractionMode.HoldHarvest)
                return false;

            if (!CanBeginHold(context))
                return false;

            BeginHold(context);
            return true;
        }

        public bool CanBeginHold(WorldUseContext context)
        {
            if (interactionMode != ResourceNodeInteractionMode.HoldHarvest
                || resourceItem == null
                || context.Gatherer == null
                || DistanceToClosestPoint(context.PlayerPosition) > holdInteractRange)
            {
                return false;
            }

            if (!IsResourceIdentified)
                return false;

            return AllowsHarvestWithoutSpecialTool() || MatchesRequiredTool(ResolveEquippedTool(context));
        }

        /// <summary>True when this laser-mine node accepts the drawn mining tool.</summary>
        public bool AllowsMiningTool(ItemData tool)
        {
            if (interactionMode != ResourceNodeInteractionMode.LaserMine || resourceItem == null)
                return false;

            if (!IsResourceIdentified)
                return false;

            if (requireMiningLaser && (tool == null || !tool.isMiningTool))
                return false;

            if (requiredTool != null && tool != requiredTool)
                return false;

            return true;
        }

        /// <summary>Tool/mode checks without requiring identification (used for scan-required feedback).</summary>
        public bool AllowsMiningToolIgnoringIdentification(ItemData tool)
        {
            if (interactionMode != ResourceNodeInteractionMode.LaserMine || resourceItem == null)
                return false;

            if (requireMiningLaser && (tool == null || !tool.isMiningTool))
                return false;

            if (requiredTool != null && tool != requiredTool)
                return false;

            return true;
        }

        /// <summary>Plant harvest with no requiredTool uses bare hands (Hold E).</summary>
        public bool AllowsHarvestWithoutSpecialTool() =>
            interactionMode == ResourceNodeInteractionMode.HoldHarvest && requiredTool == null;

        public bool MatchesRequiredTool(ItemData tool)
        {
            if (requiredTool == null)
                return true;
            return tool == requiredTool;
        }

        private static ItemData ResolveEquippedTool(WorldUseContext context)
        {
            if (context.Gatherer == null)
                return null;

            EquipmentController equipment = context.Gatherer.GetComponent<EquipmentController>();
            if (equipment == null)
                return null;

            return equipment.DrawnWeaponItem != null ? equipment.DrawnWeaponItem : equipment.EquippedItem;
        }

        public void BeginHold(WorldUseContext context)
        {
            holdActive = true;
            holdRetainUntil = -1f;
            EnsureHoldProgressBar();
            // Show the shared gold time-slider immediately at current hold progress.
            UpdateHoldProgressBar(context, Mathf.Clamp01(holdProgress));
        }

        public bool TickHold(WorldUseContext context, float deltaTime, out float progress01)
        {
            progress01 = Mathf.Clamp01(holdProgress);
            if (!holdActive || context.Gatherer == null || resourceItem == null)
                return false;

            if (DistanceToClosestPoint(context.PlayerPosition) > holdInteractRange)
            {
                CancelHold(context);
                progress01 = Mathf.Clamp01(holdProgress);
                return false;
            }

            float duration = HoldDurationSeconds;
            holdProgress += deltaTime / Mathf.Max(0.05f, duration);
            progress01 = Mathf.Clamp01(holdProgress);
            UpdateHoldProgressBar(context, progress01);

            if (holdProgress < 1f)
                return false;

            holdProgress = 0f;
            holdActive = false;

            ResolveDropRange(amountPerGather, amountPerGather, out int min, out int max);
            if (dropMin > 0 || dropMax > 0)
                ResolveDropRange(dropMin, dropMax, out min, out max);
            else
            {
                min = Mathf.Max(1, amountPerGather);
                max = min;
            }

            // Hold harvest uses the full authored range (no last-wave scale).
            int lo = Mathf.Max(1, Mathf.Min(min, max));
            int hi = Mathf.Max(lo, Mathf.Max(min, max));
            int amount = Random.Range(lo, hi + 1);
            if (!CanReserveGrant(context.Gatherer, amount))
            {
                holdProgress = 0.99f;
                holdActive = true;
                UpdateHoldProgressBar(context, holdProgress);
                return false;
            }

            SpawnLootAttract(context.Gatherer, amount);
            SetHoldProgressBarVisible(false);
            FinishGatherAndDestroy();
            return true;
        }

        public void CancelHold(WorldUseContext context)
        {
            holdActive = false;
            holdRetainUntil = Time.time + 4f;
            SetHoldProgressBarVisible(false);
        }

        private int RollWaveYield(int min, int max, bool isLastWave)
        {
            int lo = Mathf.Max(1, Mathf.Min(min, max));
            int hi = Mathf.Max(lo, Mathf.Max(min, max));
            if (isLastWave && lastWaveDropScale < 0.999f)
            {
                lo = Mathf.Max(1, Mathf.RoundToInt(lo * lastWaveDropScale));
                hi = Mathf.Max(lo, Mathf.RoundToInt(hi * lastWaveDropScale));
            }

            return Random.Range(lo, hi + 1);
        }

        private bool CanReserveGrant(ResourceGatherer gatherer, int amount)
        {
            if (gatherer == null || resourceItem == null || amount <= 0)
                return false;

            Inventory.InventorySystem inventory = gatherer.GetComponent<Inventory.InventorySystem>();
            if (inventory == null)
                return true;

            return inventory.HasSpaceFor(resourceItem, amount);
        }

        private void SpawnLootAttract(ResourceGatherer gatherer, int amount)
        {
            Transform player = gatherer != null ? gatherer.transform : null;
            Vector3 from = GetNodeCenter();

            ResolveLootYieldAudio(out AudioClip yieldClip, out float yieldVolume);
            if (yieldClip != null)
                AudioSource.PlayClipAtPoint(yieldClip, from, yieldVolume);

            // Explicit fly model → item world pickup mesh → tinted orb.
            GameObject flyModel = lootAttractPrefab;
            if (flyModel == null && resourceItem != null)
                flyModel = resourceItem.worldPrefab;

            ResolveLootGrantAudio(out AudioClip grantClip, out float grantVolume);

            ResourceLootAttractVfx.Spawn(
                from,
                player,
                gatherer,
                resourceItem,
                amount,
                flyModel,
                lootTint,
                grantClip,
                grantVolume);
        }

        private void ResolveLootYieldAudio(out AudioClip clip, out float volume)
        {
            clip = lootYieldClipOverride;
            volume = 0.9f;

            if (resourceItem is MineHarvestItemData lean)
            {
                if (clip == null)
                    clip = lean.lootYieldClip;
                volume = lean.lootYieldVolume;
            }

            if (clip != null)
                return;

            // Built-in defaults when item/node leave clips empty.
            clip = interactionMode == ResourceNodeInteractionMode.HoldHarvest
                ? LoadBuiltinClip(ref s_harvestYieldClip, BuiltinHarvestYieldClipPath)
                : LoadBuiltinClip(ref s_breakStoneClip, BuiltinBreakStoneClipPath);
            volume = interactionMode == ResourceNodeInteractionMode.HoldHarvest ? 0.85f : 0.9f;
        }

        private void ResolveLootGrantAudio(out AudioClip clip, out float volume)
        {
            clip = lootGrantClipOverride;
            volume = 0.95f;

            if (resourceItem is MineHarvestItemData lean)
            {
                if (clip == null)
                    clip = lean.lootGrantClip;
                volume = lean.lootGrantVolume;
            }
        }

        private static AudioClip s_breakStoneClip;
        private static AudioClip s_harvestYieldClip;
        private const string BuiltinBreakStoneClipPath = "Audio/Break Stone";
        private const string BuiltinHarvestYieldClipPath = "Audio/Break Wood Effect";

        private static AudioClip LoadBuiltinClip(ref AudioClip cache, string resourcesPath)
        {
            if (cache != null)
                return cache;

            cache = Resources.Load<AudioClip>(resourcesPath);
            return cache;
        }

        public Vector3 GetNodeCenter()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                return rend.bounds.center;
            return transform.position + Vector3.up * 0.4f;
        }

        /// <summary>
        /// Preferred interaction collider: the runtime-fitted root box, else any child collider.
        /// </summary>
        public Collider GetInteractionCollider()
        {
            BoxCollider rootBox = GetComponent<BoxCollider>();
            if (rootBox != null && rootBox.enabled)
                return rootBox;

            Collider[] cols = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col != null && col.enabled && col.gameObject.activeInHierarchy)
                    return col;
            }

            return null;
        }

        /// <summary>
        /// Closest world point on this node's colliders, else the renderer AABB.
        /// Use for reach / standoff so non-uniform scale and Visual-only plants work.
        /// </summary>
        public Vector3 GetClosestPoint(Vector3 worldPoint)
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            float bestSqr = float.MaxValue;
            Vector3 best = transform.position;
            bool found = false;

            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null || !col.enabled || !col.gameObject.activeInHierarchy)
                    continue;

                Vector3 candidate = SupportsClosestPoint(col)
                    ? col.ClosestPoint(worldPoint)
                    : col.bounds.ClosestPoint(worldPoint);
                float sqr = (candidate - worldPoint).sqrMagnitude;
                if (sqr >= bestSqr)
                    continue;

                bestSqr = sqr;
                best = candidate;
                found = true;
            }

            if (found)
                return best;

            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                return rend.bounds.ClosestPoint(worldPoint);

            return transform.position;
        }

        private float DistanceToClosestPoint(Vector3 worldPoint)
        {
            return Vector3.Distance(worldPoint, GetClosestPoint(worldPoint));
        }

        private const float AimAabbMarginMeters = 0.35f;

        private bool IsViewAimedAtNode(Ray viewRay)
        {
            Collider[] cols = GetComponentsInChildren<Collider>(true);
            float maxDist = holdInteractRange + 8f;
            for (int i = 0; i < cols.Length; i++)
            {
                Collider col = cols[i];
                if (col == null || !col.enabled)
                    continue;
                if (col.Raycast(viewRay, out _, maxDist))
                    return true;
            }

            Renderer rend = GetComponentInChildren<Renderer>();
            Bounds aabb = rend != null
                ? rend.bounds
                : new Bounds(GetNodeCenter(), Vector3.one * 0.5f);
            aabb.Expand(AimAabbMarginMeters * 2f);
            return aabb.IntersectRay(viewRay);
        }

        private static bool SupportsClosestPoint(Collider collider)
        {
            if (collider is BoxCollider || collider is SphereCollider || collider is CapsuleCollider)
                return true;
            return collider is MeshCollider mesh && mesh.convex;
        }

        private void EnsureInteractionVolume()
        {
            if (GetComponent<ResourceNodeInteractionVolume>() == null)
                gameObject.AddComponent<ResourceNodeInteractionVolume>();
        }

        private void EnsureHoldProgressBar()
        {
            if (holdProgressBar != null)
                return;
            holdProgressBar = WorldNodeProgressBar.Create(null);
        }

        private void UpdateHoldProgressBar(WorldUseContext context, float progress01)
        {
            EnsureHoldProgressBar();
            if (holdProgressBar == null)
                return;

            // Same small gold time-slider as laser mining (0→1 over HoldDurationSeconds).
            string name = GetDisplayName();
            Camera cam = context.ViewCamera != null ? context.ViewCamera : Camera.main;
            holdProgressBar.UpdateBar(
                GetNodeCenter() + Vector3.up * 0.75f,
                Mathf.Clamp01(progress01),
                name,
                cam);
        }

        private void SetHoldProgressBarVisible(bool visible)
        {
            if (holdProgressBar != null)
                holdProgressBar.SetVisible(visible);
        }

        private void FinishGatherAndDestroy()
        {
            ItemPickup pickup = GetComponent<ItemPickup>();
            if (pickup != null)
                PickupProximityDotUI.Unregister(pickup);

            PickupProximityDotUI.NotifyHarvested(this);

            SetHoldProgressBarVisible(false);

            // Return mining burn marks to the pool before Destroy so they are not orphaned in world space.
            DMILaserBurnMarkHost burnHost = GetComponent<DMILaserBurnMarkHost>();
            if (burnHost != null)
                burnHost.ReleaseAll();

            Destroy(gameObject);
        }
    }
}
