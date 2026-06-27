using Project.Data;
using Project.Interaction;
using Project.UI;
using UnityEngine;

namespace Project.Map
{
    /// <summary>
    /// Optional world marker shown on the minimap and full map.
    /// </summary>
    public class MapMarker : MonoBehaviour
    {
        [SerializeField] private string label = "Point of Interest";
        [SerializeField] private Color color = new Color(1f, 0.85f, 0.2f, 1f);
        [SerializeField] private Sprite iconSprite;
        [SerializeField] private bool showOnMinimap = true;
        [SerializeField] private bool showOnFullMap = true;

        public string Label => label;
        public Color Color => color;
        public Sprite IconSprite => iconSprite;
        public bool ShowOnMinimap => showOnMinimap;
        public bool ShowOnFullMap => showOnFullMap;
        public Vector3 WorldPosition => transform.position;

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
                return $"Hit to gather {node.resourceItem.itemName}";

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
        }

        private void OnEnable() => MapRegistry.Register(this);
        private void OnDisable() => MapRegistry.Unregister(this);
    }
}
