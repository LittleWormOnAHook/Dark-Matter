using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Marker on catalog barrel instances spawned onto <c>*SOCKET_BARREL_*</c>.</summary>
    [DisallowMultipleComponent]
    public sealed class DMTurretSpawnedBarrel : MonoBehaviour
    {
        [SerializeField] private string catalogId;

        public string CatalogId
        {
            get => catalogId;
            set => catalogId = value;
        }
    }
}
