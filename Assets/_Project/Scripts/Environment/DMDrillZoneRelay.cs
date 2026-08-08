using UnityEngine;

namespace Project.Environment
{
    public enum DrillZoneKind
    {
        Approach,
        Interior,
        OuterBoundary
    }

    /// <summary>
    /// Optional child marker for approach / interior / outer boundary volumes.
    /// Presence is sampled by <see cref="DMDrillController"/>; relays stay for tooling.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Collider))]
    public class DMDrillZoneRelay : MonoBehaviour
    {
        [SerializeField] private DMDrillController owner;
        [SerializeField] private DrillZoneKind zoneKind;

        public DrillZoneKind ZoneKind => zoneKind;

        public void Configure(DMDrillController controller, DrillZoneKind kind)
        {
            owner = controller;
            zoneKind = kind;
        }
    }
}
