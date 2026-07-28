using Project.Data;
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
        [Header("Resource")]
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
        public string holdPromptText = "Hold E — Harvest";
        public float holdInteractRange = 3.5f;

        [Header("Loot Attract")]
        public GameObject lootAttractPrefab;
        public Color lootTint = new Color(0.82f, 0.72f, 0.35f, 1f);

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
            if (holdProgressBar != null)
                Destroy(holdProgressBar.gameObject);
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

            float distance = Vector3.Distance(context.PlayerPosition, transform.position);
            if (distance > holdInteractRange)
                return -1f;

            if (context.AimHit.HasValue && context.AimHit.Value.collider != null)
            {
                ResourceNode hitNode = context.AimHit.Value.collider.GetComponentInParent<ResourceNode>();
                if (hitNode == this)
                    return 92f - distance;
            }

            float aimDist = WorldUseController.GetViewRayDistance(context.ViewRay, GetNodeCenter());
            if (aimDist > 1.1f)
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
            return interactionMode == ResourceNodeInteractionMode.HoldHarvest
                && resourceItem != null
                && context.Gatherer != null
                && Vector3.Distance(context.PlayerPosition, transform.position) <= holdInteractRange;
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

            if (Vector3.Distance(context.PlayerPosition, transform.position) > holdInteractRange)
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
            float retained = Mathf.Clamp01(holdProgress);
            if (retained > 0.01f)
                UpdateHoldProgressBar(context, retained);
            else
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
            if (interactionMode == ResourceNodeInteractionMode.LaserMine)
                PlayBreakStoneSound(from);

            ResourceLootAttractVfx.Spawn(
                from,
                player,
                gatherer,
                resourceItem,
                amount,
                lootAttractPrefab,
                lootTint);
        }

        private static AudioClip s_breakStoneClip;
        private const string BreakStoneClipPath = "Assets/Audio/Others/Break Stone.wav";

        private static void PlayBreakStoneSound(Vector3 position)
        {
            if (s_breakStoneClip == null)
            {
#if UNITY_EDITOR
                s_breakStoneClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(BreakStoneClipPath);
#endif
            }

            if (s_breakStoneClip != null)
                AudioSource.PlayClipAtPoint(s_breakStoneClip, position, 0.9f);
        }

        public Vector3 GetNodeCenter()
        {
            Renderer rend = GetComponentInChildren<Renderer>();
            if (rend != null)
                return rend.bounds.center;
            return transform.position + Vector3.up * 0.4f;
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
            string name = resourceItem != null ? resourceItem.itemName : "Harvest";
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

            if (holdProgressBar != null)
            {
                Destroy(holdProgressBar.gameObject);
                holdProgressBar = null;
            }

            Destroy(gameObject);
        }
    }
}
