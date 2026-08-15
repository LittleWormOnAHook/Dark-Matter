using Project.Data;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Optional world marker shown on the minimap and full map.
    /// Items stay hidden until discovered by a scanner sweep.
    /// </summary>
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

        private Vector3 cachedWorldPosition;

        public string Label => label;
        public Color Color => color;
        public Sprite IconSprite => iconSprite;
        public bool ShowOnMinimap => showOnMinimap;
        public bool ShowOnFullMap => showOnFullMap;
        public bool RequiresScanDiscovery => requiresScanDiscovery;
        public bool RequiresFogReveal => requiresFogReveal;
        public Vector3 WorldPosition =>
            isActiveAndEnabled ? transform.position : cachedWorldPosition;

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
            color = MapUiSprites.GetResourceColor(item.itemType);
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = true;
            discoveryId = string.Empty;
        }

        public void ConfigureScannedPoi(string displayName, Color markerColor)
        {
            label = string.IsNullOrWhiteSpace(displayName) ? "Point of Interest" : displayName;
            color = markerColor;
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = true;
            discoveryId = string.Empty;
        }

        public void ConfigureQuestGiver(string npcDisplayName)
        {
            label = string.IsNullOrWhiteSpace(npcDisplayName) ? "Quest Giver" : npcDisplayName;
            color = DarkMatterGenesisUiPalette.Gold;
            showOnMinimap = true;
            showOnFullMap = true;
            requiresScanDiscovery = false;
            requiresFogReveal = false;
        }

        public void SetRequiresScanDiscovery(bool required)
        {
            requiresScanDiscovery = required;
        }

        public void SetKeepRegisteredWhenDisabled(bool keepRegistered)
        {
            keepRegisteredWhenDisabled = keepRegistered;
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
            cachedWorldPosition = transform.position;
            MapRegistry.Register(this);
        }

        private void OnDisable()
        {
            cachedWorldPosition = transform.position;
            if (!keepRegisteredWhenDisabled)
                MapRegistry.Unregister(this);
        }
    }
}
