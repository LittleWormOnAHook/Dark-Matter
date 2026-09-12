using ECM2;
using Project.Core;
using UnityEngine;

namespace Project.Audio
{
    public class LandingAudioController : MonoBehaviour
    {
        [SerializeField] private float groundCheckDistance = 1.4f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Character character;
        private bool wasGrounded = true;
        private bool initialized;
        private float peakFallSpeed;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || character == null)
                return;

            bool grounded = character.IsGrounded();

            if (!initialized)
            {
                wasGrounded = grounded;
                initialized = true;
                return;
            }

            if (!grounded)
            {
                float fallSpeed = Mathf.Max(0f, -character.GetVelocity().y);
                if (fallSpeed > peakFallSpeed)
                    peakFallSpeed = fallSpeed;
            }
            else if (!wasGrounded)
            {
                SampleGround(out string surfaceTag, out int terrainLayerIndex);
                GameAudioManager.Instance?.PlayLanding(transform.position, surfaceTag, peakFallSpeed, terrainLayerIndex);
                peakFallSpeed = 0f;
            }
            else
            {
                peakFallSpeed = 0f;
            }

            wasGrounded = grounded;
        }

        private void SampleGround(out string surfaceTag, out int terrainLayerIndex)
        {
            surfaceTag = "Default";
            terrainLayerIndex = -1;

            Vector3 origin = transform.position + Vector3.up * 0.15f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
                return;

            FootstepSurface surface = hit.collider.GetComponentInParent<FootstepSurface>();
            surfaceTag = surface != null && !string.IsNullOrEmpty(surface.SurfaceTag)
                ? surface.SurfaceTag
                : hit.collider.tag;

            Terrain terrain = hit.collider.GetComponent<Terrain>();
            if (terrain != null)
                terrainLayerIndex = Project.Player.DMFootstepManager.SampleTerrainLayerIndex(terrain, hit.point);
        }
    }
}
