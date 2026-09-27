using UnityEngine;

namespace Project.Building
{
    /// <summary>0926-generator: Equipment goes on base floors or on the ground inside a base's power square.</summary>
    public sealed partial class DMBuildingPlacementController
    {
        static bool IsEquipmentPiece(DMBuildingPiece piece)
        {
            return piece != null && (piece.Category == DMBuildingCategory.Equipment || piece.HotbarId == DMBuildingCatalog.EquipmentId);
        }

        /// <summary>Ground fallback for Equipment when the crosshair is not on a built floor.</summary>
        static bool TrySeatEquipmentOnGround(Ray ray, DMBuildingPiece piece, Camera camera, out Vector3 position, out Quaternion rotation)
        {
            position = default;
            rotation = Quaternion.identity;
            if (!IsEquipmentPiece(piece) || camera == null)
                return false;
            if (!TryGroundHit(ray, out RaycastHit hit))
                return false;
            if (hit.normal.y < 0.7f)
                return false;
            if (hit.collider != null && hit.collider.GetComponentInParent<DMBuildingGhost>() != null)
                return false;
            // A built wall in front of the ground spot blocks it (the ground ray ignores building colliders).
            if (TryHitBuiltAlongRay(ray, Mathf.Max(0f, hit.distance - 0.05f), out _))
                return false;
            // 0926-build-hub: the Build Hub may stand on open ground anywhere (it starts a build zone).
            if (!DMBuildingCatalog.IsBuildHub(piece.Id) && !DMBasePower.IsInsideAnyBase(hit.point))
                return false;

            Vector3 forward = Vector3.ProjectOnPlane(camera.transform.forward, Vector3.up);
            if (forward.sqrMagnitude < 0.0001f)
                forward = Vector3.forward;
            float spin = instance != null ? instance.yawNotches * YawStep() : 0f;
            rotation = Quaternion.AngleAxis(spin, Vector3.up) * Quaternion.LookRotation(forward.normalized, Vector3.up);
            position = hit.point + Vector3.up * (piece.Size.y * 0.5f + Mathf.Max(0f, piece.SurfaceOffset));
            return true;
        }

        /// <summary>Generator and Build Hub logic on freshly built pieces (power-switched lights: EnsurePoweredLights).</summary>
        static void AttachPoweredParts(GameObject ghostObject, DMBuildingPiece piece)
        {
            if (ghostObject == null || piece == null)
                return;
            if (DMBuildingCatalog.IsGenerator(piece.Id) && ghostObject.GetComponent<DMBaseGenerator>() == null)
                ghostObject.AddComponent<DMBaseGenerator>();
            if (DMBuildingCatalog.IsBuildHub(piece.Id) && ghostObject.GetComponent<DMBuildHub>() == null)
                ghostObject.AddComponent<DMBuildHub>(); // 0926-build-hub
        }

        /// <summary>
        /// 0927-power-lights: every placed or save-restored piece with Light components, or with glowing materials (not
        /// Equipment or force fields, which run their own look), goes dark without a running generator and adds load.
        /// Runs after the finish is applied so the glow check sees the final materials.
        /// </summary>
        static void EnsurePoweredLights(GameObject ghostObject, DMBuildingPiece piece)
        {
            if (ghostObject == null || piece == null)
                return;
            bool glowCounts = !IsEquipmentPiece(piece)
                && !DMBuildingCatalog.IsForceField(piece.Id)
                && !DMBuildingCatalog.IsGenerator(piece.Id)
                && !DMBuildingCatalog.IsBuildHub(piece.Id);
            DMBasePoweredLights.Ensure(ghostObject, glowCounts);
        }

        /// <summary>A destroyed generator hands the whole units left in its tank back as Plasma Fuel.</summary>
        static void ReleaseGenerator(DMBuildingGhost ghost)
        {
            DMBaseGenerator generator = ghost != null ? ghost.GetComponent<DMBaseGenerator>() : null;
            if (generator != null)
                generator.ReturnFuelToInventory();
        }
    }
}
