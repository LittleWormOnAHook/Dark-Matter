using UnityEngine;

namespace Project.Player
{
    /// <summary>
    /// Shared third-person camera tuning for Pioneer zoom + wall collision push.
    /// Edited from Genesis Studio → Player → Camera.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DM_CameraProfile",
        menuName = "Dark Matter Genesis/Player/Camera Profile")]
    public sealed class DMCameraProfile : ScriptableObject
    {
        public const string ResourcesPath = "Player/DM_CameraProfile";

        [Header("Mouse Zoom (follow distance)")]
        [Tooltip("Closest free-look follow distance (scroll all the way in). Keep >= Collision Min Follow.")]
        public float minDistance = 2.25f;
        [Tooltip("Farthest free-look follow distance (scroll all the way out).")]
        public float maxDistance = 12f;
        [Tooltip("Follow distance applied when an expedition starts.")]
        public float defaultDistance = 2.25f;
        [Tooltip("Discrete mouse-wheel stops between min and max.")]
        [Range(2, 24)]
        public int zoomClickLevels = 10;

        [Header("Aim / Sprint Zoom")]
        [Tooltip("How much closer Aiming pulls vs free-look preferred zoom.")]
        public float aimZoomPullInMeters = 0.55f;
        [Tooltip("Extra follow distance while sprinting (slight pull-out only).")]
        public float sprintZoomOutMeters = 0.85f;
        [Tooltip("Closest follow distance allowed while aiming (ADS). Can be below scroll min.")]
        public float aimMinCameraDistance = 0.78f;

        [Header("Collision Push (walls / terrain)")]
        [Tooltip("Never pull the lens closer than this while colliding (meters from pivot).")]
        public float minFollow = 2.15f;
        public float climbMinFollow = 2.55f;
        public float mantleMinFollow = 2.9f;
        public float pullSpeed = 16f;
        public float releaseSpeed = 5.5f;
        public float collisionHysteresis = 0.12f;
        public float climbPullSpeed = 5.5f;
        public float climbReleaseSpeed = 2.8f;
        public float mantlePullSpeed = 3.0f;
        public float mantleReleaseSpeed = 2.2f;
        public float climbNearRadius = 4.25f;
        public float sphereRadius = 0.2f;
        public float extraSkin = 0.08f;
        public float floorProbe = 3.0f;

        [Header("Lens")]
        [Tooltip("Gameplay camera near clip plane. Too small + max zoom-in can make world UI swim.")]
        public float nearClipPlane = 0.05f;

        public static DMCameraProfile LoadOrNull()
        {
            return Resources.Load<DMCameraProfile>(ResourcesPath);
        }

        private void OnValidate()
        {
            maxDistance = Mathf.Max(maxDistance, minDistance + 0.05f);
            defaultDistance = Mathf.Clamp(defaultDistance, minDistance, maxDistance);
            aimMinCameraDistance = Mathf.Max(0.05f, aimMinCameraDistance);
            zoomClickLevels = Mathf.Clamp(zoomClickLevels, 2, 24);
            minFollow = Mathf.Max(0.5f, minFollow);
            // Keep scroll-min from fighting collision hold.
            if (minDistance + 0.01f < minFollow)
                minDistance = minFollow;
            if (defaultDistance + 0.01f < minFollow)
                defaultDistance = minFollow;
            nearClipPlane = Mathf.Clamp(nearClipPlane, 0.01f, 1f);
        }
    }
}
