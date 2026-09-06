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
        [SerializeField] private float fallThroughTolerance = 0.45f;

        [Tooltip("Seconds between checks.")]
        [SerializeField] private float checkInterval = 0.1f;

        private float nextCheckTime;
        private Rigidbody body;
        private Project.Player.DMLandingDirector landing;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            landing = GetComponent<Project.Player.DMLandingDirector>();
        }

        private void OnEnable()
        {
            nextCheckTime = 0f;
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

            nextCheckTime = Time.time + Mathf.Max(0.05f, checkInterval);

            Vector3 position = transform.position;
            if (!TrySampleSurfaceUnderPlayer(position, out float surfaceY))
                return;

            float sink = Mathf.Clamp(fallThroughTolerance, 0.2f, 0.6f);
            if (position.y >= surfaceY - sink)
                return;

            float drop = surfaceY - position.y;
            position.y = surfaceY + 0.25f;
            transform.position = position;

            if (body != null && !body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (landing == null)
                landing = GetComponent<Project.Player.DMLandingDirector>();
            if (landing != null)
                landing.NotifyTerrainRescue();

            // Spawn often sits a few centimetres low. Only warn for a real tunnel.
            if (drop >= 2f)
                Debug.LogWarning($"[TerrainRescue] {name} fell below terrain — snapped back to surface at y={position.y:0.##}");
        }

        private static bool TrySampleSurfaceUnderPlayer(Vector3 position, out float surfaceY)
        {
            surfaceY = 0f;
            Terrain[] terrains = Terrain.activeTerrains;
            if (terrains == null || terrains.Length == 0)
                return false;

            bool any = false;
            float closestAbs = float.PositiveInfinity;
            float highest = float.NegativeInfinity;
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

                float sampled = terrain.SampleHeight(position) + origin.y;
                if (sampled > highest)
                    highest = sampled;

                float abs = Mathf.Abs(position.y - sampled);
                if (!any || abs < closestAbs)
                {
                    closestAbs = abs;
                    surfaceY = sampled;
                    any = true;
                }
            }

            if (!any)
                return false;

            // Overlapping tiles: if we are clearly under every nearby surface, use the highest.
            if (position.y < surfaceY - 2f && highest > surfaceY)
                surfaceY = highest;

            return true;
        }
    }
}
