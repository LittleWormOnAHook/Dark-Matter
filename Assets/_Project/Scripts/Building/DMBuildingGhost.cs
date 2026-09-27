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
        public int Cost;
        /// <summary>Surface items: the built piece this item sticks to. Destroying the host removes the item too.</summary>
        public DMBuildingGhost Host;
        public ItemData PaidItem;
        public bool Built;
        public Vector3 LocalHalfExtents;
        public string MaterialVariantId;
        /// <summary>0926-generator: placement order; the lowest built foundation of a base anchors its power square.</summary>
        public int BuildOrder;

        static int buildOrderCounter;

        /// <summary>Next placement order number (kept above anything restored from a save).</summary>
        public static int NextBuildOrder()
        {
            return ++buildOrderCounter;
        }

        // 0925-layers: live registry so placement never scans the scene for pieces.
        static readonly System.Collections.Generic.List<DMBuildingGhost> registry = new System.Collections.Generic.List<DMBuildingGhost>();

        /// <summary>Bumps whenever a piece is enabled or disabled.</summary>
        public static int RegistryVersion { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetRegistry()
        {
            registry.Clear();
            RegistryVersion++;
            buildOrderCounter = 0;
        }

        public static DMBuildingGhost[] Snapshot()
        {
            return registry.ToArray();
        }

        void OnEnable()
        {
            registry.Add(this);
            RegistryVersion++;
        }

        void OnDisable()
        {
            registry.Remove(this);
            RegistryVersion++;
        }
    }
}
