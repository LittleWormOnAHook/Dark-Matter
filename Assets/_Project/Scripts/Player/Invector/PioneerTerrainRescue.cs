using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Safety net against physics tunneling: if the player's capsule ends up below the
    /// terrain surface (e.g. shoved through the floor by a kinematic AI collider or a
    /// depenetration spike during companion respawns), snap them back on top.
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
            if (Time.time < nextCheckTime)
                return;

            nextCheckTime = Time.time + checkInterval;

            Terrain terrain = Terrain.activeTerrain;
            if (terrain == null)
                return;

            Vector3 position = transform.position;
            float surfaceY = terrain.SampleHeight(position) + terrain.transform.position.y;

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
    }
}
