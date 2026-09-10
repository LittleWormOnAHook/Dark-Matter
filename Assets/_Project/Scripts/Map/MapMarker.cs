using Project.AI;
using Project.Creatures;
using Project.Data;
using Project.Interaction;
using Project.Quests;
using Project.UI;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
        /// Optional world marker shown on the minimap and full map.
        /// Fog-gated, live-position POIs. Enemies also require combat aggro.
    /// </summary>
    [DefaultExecutionOrder(200)]
    public class MapMarker : MonoBehaviour
    {
        [SerializeField] private string label = "Point of Interest";
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private bool showOnMinimap = true;
        [SerializeField] private bool showOnFullMap = true;
        [SerializeField] private bool keepRegisteredWhenDisabled;
        [Tooltip("When true, marker icons stay hidden until a scanner sweep discovers this id.")]
        [SerializeField] private bool requiresScanDiscovery = true;
        [Tooltip("Stable save id. Auto-built from label + xz if empty.")]
        [SerializeField] private string discoveryId;
        [Tooltip("Legacy: fog-gated markers. Ignored when requiresScanDiscovery is true.")]
        [SerializeField] private bool requiresFogReveal = true;
        [Tooltip("When true, WorldPosition follows the host transform.")]
        [SerializeField] private bool useLiveWorldPosition = true;
        [Tooltip("Enemy/creature dots: 3s flash on aggro, solid after, flash while attacking, solid 5s after lose-aggro.")]
        [SerializeField] private bool requiresCombatAggro;

        private const float CombatLingerSeconds = 5f;
        private const float AggroFlashSeconds = 3f;
        private const float CombatFlashHz = 3f;

        private bool wasCombatActive;
        private float combatLingerUntil;
        private float aggroFlashUntil;

        public string Label => label;
        public Color Color => color;
        public Sprite IconSprite => iconSprite;
        public bool ShowOnMinimap => showOnMinimap;
        public bool ShowOnFullMap => showOnFullMap;
        public bool RequiresScanDiscovery => requiresScanDiscovery;
        public bool RequiresFogReveal => requiresFogReveal;
        public bool UsesLiveWorldPosition => useLiveWorldPosition;
        public bool RequiresCombatAggro => requiresCombatAggro;
        public Vector3 WorldPosition => transform.position;

        public string DiscoveryId
        {
            get
            {
                if (string.IsNullOrWhiteSpace(discoveryId))
                    discoveryId = BuildDefaultDiscoveryId();
                return discoveryId;
            }
        }

        public bool IsRevealedOnMap
        {
            get
            {
                if (requiresScanDiscovery)
                    return ScannerDiscoveryRegistry.IsDiscovered(DiscoveryId);

                if (!requiresFogReveal)
                    return true;

                MapFogOfWar fog = MapFogOfWar.Instance ?? MapFogOfWar.EnsureExists();
                return fog != null && fog.IsWorldRevealed(WorldPosition);
            }
        }

        public bool IsResourceMarker
        {
            get
            {
                if (TryGetComponent(out ResourceNode _))
                    return true;

                return TryGetComponent(out ItemPickup pickup)
                    && pickup.itemData != null
                    && !pickup.IsPickedUp;
            }
        }

        public string GetInteractionHintText()
        {
            if (TryGetComponent(out ItemPickup pickup)
                && pickup.itemData != null
                && !pickup.IsPickedUp)
            {
                return $"{pickup.promptText} {pickup.itemData.itemName}";
            }

            ResourceNode node = GetComponent<ResourceNode>();
            if (node != null && node.resourceItem != null)
            {
                string itemName = node.resourceItem.itemName;
                if (node.interactionMode == ResourceNodeInteractionMode.HoldHarvest)
                    return string.IsNullOrWhiteSpace(itemName) ? "Harvest" : itemName;

                return $"Hit to gather {itemName}";
            }

            return string.IsNullOrWhiteSpace(label) ? null : label;
        }

        public void ConfigureForResource(ItemData item)
        {
            if (item == null)
                return;

            label = string.IsNullOrWhiteSpace(item.itemName) ? "Resource" : item.itemName;
            iconSprite = item.icon;
            color = MapUiSprites.GetMapPoiColor(item.itemType);
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = true;
            discoveryId = string.Empty;
            ApplyCanonicalFlags();
        }

        public void ConfigureScannedPoi(string displayName, Color markerColor)
        {
            label = string.IsNullOrWhiteSpace(displayName) ? "Point of Interest" : displayName;
            color = DarkMatterGenesisUiPalette.ToMapPoiColor(markerColor);
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = true;
            discoveryId = string.Empty;
            ApplyCanonicalFlags();
        }

        public void ConfigureQuestGiver(string npcDisplayName)
        {
            label = string.IsNullOrWhiteSpace(npcDisplayName) ? "Quest Giver" : npcDisplayName;
            color = DarkMatterGenesisUiPalette.MapPoiBlue;
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = false;
            ApplyCanonicalFlags();
        }

        public void SetRequiresScanDiscovery(bool required)
        {
            requiresScanDiscovery = required;
        }

        public void SetKeepRegisteredWhenDisabled(bool keepRegistered)
        {
            keepRegisteredWhenDisabled = keepRegistered;
        }

        public static MapMarker EnsureForEnemy(EnemyHealth enemy)
        {
            if (enemy == null)
                return null;

            MapMarker marker = enemy.GetComponent<MapMarker>();
            if (marker == null)
                marker = enemy.gameObject.AddComponent<MapMarker>();

            marker.ConfigureForEnemy(enemy.name);
            return marker;
        }

        public void ConfigureForEnemy(string enemyName)
        {
            label = string.IsNullOrWhiteSpace(enemyName) ? "Threat" : enemyName;
            color = DarkMatterGenesisUiPalette.MapPoiRed;
            iconSprite = null;
            showOnMinimap = true;
            showOnFullMap = false;
            requiresScanDiscovery = false;
            discoveryId = string.Empty;
            ApplyCanonicalFlags();
        }

        /// <summary>
        /// Fog reveal + live world position on every marker.
        /// Combat-aggro only on enemy/creature hosts.
        /// </summary>
        public void ApplyCanonicalFlags()
        {
            requiresFogReveal = true;
            useLiveWorldPosition = true;
            requiresCombatAggro = IsEnemyHost();
        }

        public void RefreshFromHost()
        {
            if (TryGetComponent(out QuestGiverNpc questGiver))
                ConfigureQuestGiver(questGiver.DisplayName);
            else if (TryGetComponent(out ResourceNode node) && node.resourceItem != null)
                ConfigureForResource(node.resourceItem);
            else if (TryGetComponent(out ItemPickup pickup) && pickup.itemData != null)
                ConfigureForResource(pickup.itemData);
            else if (TryGetComponent(out EnemyHealth enemy))
                ConfigureForEnemy(enemy.name);
            else if (TryGetComponent(out DMICreatureAiController creature))
                ConfigureForEnemy(creature.name);
            else
                ApplyCanonicalFlags();
        }

        public bool IsEnemyHost()
        {
            return TryGetComponent(out EnemyHealth _)
                || TryGetComponent(out EnemyAiController _)
                || TryGetComponent(out DMICreatureAiController _)
                || TryGetComponent(out DMICreatureHealth _);
        }

        public const float CompassAggroPreviewPadding = 20f;

        public bool ShouldDrawOnMinimap(Vector3 playerWorld)
        {
            if (!showOnMinimap || !isActiveAndEnabled)
                return false;

            if (TryGetComponent(out EnemyHealth health) && health.IsDead)
            {
                ClearCombatPulse();
                return false;
            }

            if (requiresCombatAggro)
                return EvaluateCombatVisibility();

            return IsRevealedOnMap;
        }

        public bool ShouldDrawOnCompass(Vector3 playerWorld)
        {
            if (!showOnMinimap || !isActiveAndEnabled)
                return false;

            if (TryGetComponent(out EnemyHealth health) && health.IsDead)
            {
                ClearCombatPulse();
                return false;
            }

            if (requiresCombatAggro || IsEnemyHost())
            {
                if (EvaluateCombatVisibility())
                    return true;

                return IsWithinCompassAggroPreview(playerWorld);
            }

            return IsRevealedOnMap;
        }

        public bool IsWithinCompassAggroPreview(Vector3 playerWorld)
        {
            Vector3 delta = transform.position - playerWorld;
            delta.y = 0f;
            float limit = GetOuterAggroRange() + CompassAggroPreviewPadding;
            return delta.sqrMagnitude <= limit * limit;
        }

        public float GetOuterAggroRange()
        {
            if (TryGetComponent(out EnemySenses senses))
                return Mathf.Max(1f, senses.OuterSenseRange);

            if (TryGetComponent(out DMICreatureAiController creature))
                return Mathf.Max(1f, creature.OuterAggroRange);

            return 16f;
        }

        public bool IsCombatFlashing => requiresCombatAggro && ShouldFlashCombatDot();

        public float GetMinimapDisplayAlpha()
        {
            if (!IsCombatFlashing)
                return 1f;

            return Mathf.Repeat(Time.unscaledTime * CombatFlashHz, 1f) < 0.5f ? 1f : 0f;
        }

        private bool EvaluateCombatVisibility()
        {
            if (IsHostAggroed())
            {
                if (!wasCombatActive)
                    aggroFlashUntil = Time.unscaledTime + AggroFlashSeconds;

                wasCombatActive = true;
                combatLingerUntil = 0f;
                return true;
            }

            if (wasCombatActive)
            {
                wasCombatActive = false;
                aggroFlashUntil = 0f;
                combatLingerUntil = Time.unscaledTime + CombatLingerSeconds;
            }

            return combatLingerUntil > 0f && Time.unscaledTime < combatLingerUntil;
        }

        private bool ShouldFlashCombatDot()
        {
            if (IsHostAttacking())
                return true;

            return IsHostAggroed() && Time.unscaledTime < aggroFlashUntil;
        }

        private bool IsHostAggroed()
        {
            if (TryGetComponent(out EnemyAiController ai) && ai.IsAggroed)
                return true;

            if (TryGetComponent(out DMICreatureAiController creature) && creature.IsAggroed)
                return true;

            return false;
        }

        private bool IsHostAttacking()
        {
            if (TryGetComponent(out EnemyCombat combat) && combat.IsAttacking)
                return true;

            if (TryGetComponent(out DMICreatureAiController creature) && creature.IsAttacking)
                return true;

            return false;
        }

        private void ClearCombatPulse()
        {
            wasCombatActive = false;
            combatLingerUntil = 0f;
            aggroFlashUntil = 0f;
        }

        private void LateUpdate()
        {
            if (!requiresCombatAggro)
                return;

            if (TryGetComponent(out EnemyHealth health) && health.IsDead)
            {
                ClearCombatPulse();
                return;
            }

            EvaluateCombatVisibility();
        }

        public void CaptureHostLocation()
        {
            MapRegistry.NotifyUpdated(this);
        }

        private void Awake()
        {
            ApplyCanonicalFlags();
        }

        private void Start()
        {
            ApplyCanonicalFlags();
            CaptureHostLocation();
        }

        private string BuildDefaultDiscoveryId()
        {
            Vector3 p = WorldPosition;
            string safeLabel = string.IsNullOrWhiteSpace(label) ? gameObject.name : label.Trim();
            safeLabel = safeLabel.Replace(' ', '_');
            return $"scan_{safeLabel}_{p.x:F1}_{p.z:F1}";
        }

        private void OnEnable()
        {
            CaptureHostLocation();
            MapRegistry.Register(this);
        }

        private void OnDisable()
        {
            if (!keepRegisteredWhenDisabled)
                MapRegistry.Unregister(this);
        }
    }
}
