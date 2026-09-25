using Project.Data;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Built piece after a completed hold. Preview holograms live on the placement controller.
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
