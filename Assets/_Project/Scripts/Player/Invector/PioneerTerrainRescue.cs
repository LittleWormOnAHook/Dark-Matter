using Project.Vehicles;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Safety net against physics tunneling: if the player's capsule ends up below the
    /// terrain surface, snap them back on top. Samples the tile under the player, not
    /// Terrain.activeTerrain (that can be a leftover origin tile).
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerTerrainRescue : MonoBehaviour
    {
        [Tooltip("How far below the terrain surface counts as fallen through.")]
        [SerializeField] private float fallThroughTolerance = 2.5f;

        [Tooltip("Seconds between checks.")]
        [SerializeField] private float checkInterval = 0.5f;

        private float nextCheckTime;
        private Rigidbody body;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
        }

        private void LateUpdate()
        {
            if (!isActiveAndEnabled)
                return;

            if (PlayerVehicleState.IsMounted)
                return;

            if (transform.parent != null && transform.parent.name == "HiddenCrewHolder")
                return;

            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + checkInterval;

            Vector3 position = transform.position;
            if (!TrySampleSurfaceUnderPlayer(position, out float surfaceY))
                return;

            if (position.y >= surfaceY - fallThroughTolerance)
                return;

            position.y = surfaceY + 0.25f;
            transform.position = position;

            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            Debug.LogWarning($"[TerrainRescue] {name} fell below terrain — snapped back to surface at y={position.y:0.##}");
        }

        private static bool TrySampleSurfaceUnderPlayer(Vector3 position, out float surfaceY)
        {
            Terrain[] terrains = Terrain.activeTerrains;
            for (int i = 0; i < terrains.Length; i++)
            {
                Terrain terrain = terrains[i];
                if (terrain == null || !terrain.enabled || terrain.terrainData == null)
                    continue;

                Vector3 origin = terrain.transform.position;
                Vector3 size = terrain.terrainData.size;
                if (position.x < origin.x || position.x > origin.x + size.x)
                    continue;
                if (position.z < origin.z || position.z > origin.z + size.z)
                    continue;

                surfaceY = terrain.SampleHeight(position) + origin.y;
                return true;
            }

            surfaceY = 0f;
            return false;
        }
    }
}
