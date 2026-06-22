using ECM2;
using Project.Core;
using UnityEngine;

namespace Project.Audio
{
    [RequireComponent(typeof(Character))]
    public class FootstepController : MonoBehaviour
    {
        [SerializeField] private float runSpeedThreshold = 7f;
        [SerializeField] private float groundCheckDistance = 1.4f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private Character character;
        private float distanceSinceLastStep;

        private void Awake()
        {
            character = GetComponent<Character>();
        }

        private void Update()
        {
            if (!GameSession.HasStarted || character == null)
                return;

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
            FootstepSurfaceSet set = profile != null
                ? profile.GetFootstepsForSurface(GetSurfaceTag())
                : null;

            float stepDistance = isRunning
                ? (set != null ? set.runStepDistance : 2.8f)
                : (set != null ? set.walkStepDistance : 2.1f);

            distanceSinceLastStep += speed * Time.deltaTime;
            if (distanceSinceLastStep < stepDistance)
                return;

            distanceSinceLastStep = 0f;
            GameAudioManager.Instance?.PlayFootstep(transform.position, GetSurfaceTag(), isRunning);
        }

        private string GetSurfaceTag()
        {
            Vector3 origin = transform.position + Vector3.up * 0.15f;
            if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, groundCheckDistance, groundLayers, QueryTriggerInteraction.Ignore))
                return "Default";

            FootstepSurface surface = hit.collider.GetComponentInParent<FootstepSurface>();
            if (surface != null && !string.IsNullOrEmpty(surface.SurfaceTag))
                return surface.SurfaceTag;

            return hit.collider.tag;
        }
    }
}
