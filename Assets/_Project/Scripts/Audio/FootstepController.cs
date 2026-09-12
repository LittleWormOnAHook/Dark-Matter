using ECM2;
using Project.Core;
using Project.Features.Dash;
using UnityEngine;

namespace Project.Audio
{
    public class FootstepController : MonoBehaviour
    {
        [SerializeField] private float runSpeedThreshold = 7f;
        [SerializeField] private float groundCheckDistance = 1.4f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Character character;
        private float distanceSinceLastStep;
        private DMDashController dash;

        private void Awake()
        {
            character = GetComponent<Character>();
            dash = GetComponent<DMDashController>();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || character == null)
                return;

            if (dash == null)
                dash = GetComponent<DMDashController>();

            if (dash != null && dash.IsDashing)
            {
                distanceSinceLastStep = 0f;
                return;
            }

            if (!character.IsGrounded())
            {
                distanceSinceLastStep = 0f;
                return;
            }

            float speed = character.GetSpeed();
            if (speed < 0.12f)
            {
                distanceSinceLastStep = 0f;
                return;
            }

            bool isRunning = speed >= runSpeedThreshold;
            GameAudioProfile profile = GameAudioManager.Instance != null ? GameAudioManager.Instance.Profile : null;
            FootstepSurfaceSet set = profile != null ? profile.defaultFootsteps : null;
            float stepDistance = isRunning
                ? (set != null ? set.runStepDistance : 2.8f)
                : (set != null ? set.walkStepDistance : 2.1f);

            distanceSinceLastStep += speed * Time.deltaTime;
            if (distanceSinceLastStep < stepDistance)
                return;

            distanceSinceLastStep = 0f;
            SampleGround(out string surfaceTag, out int terrainLayerIndex);
            GameAudioManager.Instance?.PlayFootstep(transform.position, surfaceTag, isRunning, terrainLayerIndex);
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
