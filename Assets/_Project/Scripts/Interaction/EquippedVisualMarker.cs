using Project.Data;
using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Marks weapon/tool meshes spawned on the player so world-use pickup logic ignores them.
    /// </summary>
    public sealed class EquippedVisualMarker : MonoBehaviour
    {
        [SerializeField] private ItemData sourceItem;

        public ItemData SourceItem => sourceItem;

        public void BindItem(ItemData item)
        {
            sourceItem = item;
        }
    }
}
