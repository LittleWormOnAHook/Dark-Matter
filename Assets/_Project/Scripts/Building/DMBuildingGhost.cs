using Project.Data;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Committed placement. Always the red ghost until a later slice materializes it.
    /// </summary>
    public sealed class DMBuildingGhost : MonoBehaviour
    {
        public string PieceId;
        public int StoneCost;
        public ItemData PaidItem;
        public bool Built;
        public Vector3 LocalHalfExtents;
        public string MaterialVariantId;
    }
}
