using UnityEngine;

namespace Project.Features.Dash
{
    [CreateAssetMenu(menuName = "Dark Matter/Player/Dash Profile", fileName = "DMDashProfile")]
    public sealed class DMDashProfile : ScriptableObject
    {
        [Header("Input")]
        [Tooltip("Max seconds between two taps of the same WASD key.")]
        public float doubleTapWindow = 0.28f;

        [Header("Motion")]
        [Tooltip("How far the slide travels, in meters. Speed only changes how quickly that distance is covered.")]
        [Range(1f, 25f)]
        public float distance = 6.5f;
        [Tooltip("Dash move speed in meters per second. Does not change how far you travel.")]
        [Range(4f, 40f)]
        public float speed = 14f;
        [Tooltip("Fallback only. Live duration is Distance / Speed.")]
        public float duration = 0.18f;
        [Tooltip("Seconds before another dash can start.")]
        public float cooldown = 0.55f;
        [Tooltip("Flat SurvivalStats stamina spent when a dash starts.")]
        public float staminaCost = 22f;
        public bool allowAirDash = false;
        [Tooltip("0 freezes walk/run. 1 is normal speed.")]
        [Range(0f, 1f)]
        public float animationSpeed = 0f;

        [Header("Hologram")]
        public Color hologramColor = new Color(0.25f, 0.85f, 1f, 0.42f);
        public float hologramEmission = 4f;
        [Tooltip("Optional override. If empty, an HDRP unlit hologram is built at runtime.")]
        public Material hologramMaterial;

        [Header("Speed streaks")]
        public Color streakColor = new Color(0.55f, 0.95f, 1f, 0.85f);
        public int streakCount = 18;
        public float streakLifetime = 0.22f;
        public float streakSize = 0.08f;
        public float streakStretch = 3.5f;
        public float streakRadius = 0.55f;
        public Material streakMaterial;
        public GameObject streakPrefab;

        [Header("Smoke")]
        public Color smokeColor = new Color(0.55f, 0.62f, 0.7f, 0.45f);
        public int smokeCount = 14;
        public float smokeLifetime = 0.55f;
        public float smokeSize = 0.7f;
        public Material smokeMaterial;
        public GameObject smokePrefab;
    }
}