using Project.Inventory;
using UnityEngine;

namespace Project.Interaction
{
    public readonly struct WorldUseContext
    {
        public Transform PlayerTransform { get; }
        public Vector3 PlayerPosition { get; }
        public Camera ViewCamera { get; }
        public InventorySystem Inventory { get; }
        public ResourceGatherer Gatherer { get; }
        public float UseRange { get; }
        public Ray ViewRay { get; }
        public RaycastHit? AimHit { get; }

        public WorldUseContext(
            Transform playerTransform,
            Vector3 playerPosition,
            Camera viewCamera,
            InventorySystem inventory,
            ResourceGatherer gatherer,
            float useRange,
            Ray viewRay,
            RaycastHit? aimHit)
        {
            PlayerTransform = playerTransform;
            PlayerPosition = playerPosition;
            ViewCamera = viewCamera;
            Inventory = inventory;
            Gatherer = gatherer;
            UseRange = useRange;
            ViewRay = viewRay;
            AimHit = aimHit;
        }
    }
}
