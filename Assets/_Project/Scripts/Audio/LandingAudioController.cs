using ECM2;
using Project.Core;
using UnityEngine;

namespace Project.Audio
{
    [RequireComponent(typeof(Character))]
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
                GameAudioManager.Instance?.PlayLanding(transform.position, GetSurfaceTag(), peakFallSpeed);
                peakFallSpeed = 0f;
            }
            else
            {
                peakFallSpeed = 0f;
            }

            wasGrounded = grounded;
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
