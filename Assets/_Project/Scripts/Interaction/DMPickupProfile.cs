using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// Global world-pickup tunables: hold-E time, prompt range, focus cone, default prompt, and pickup toast timing.
    /// Edited in Genesis Studio > Player > Pickup Items. Per-pickup values (amount, respawn, prompt, stem) stay on each prefab.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter Genesis/Interaction/Pickup Profile", fileName = "DM_PickupProfile")]
    public sealed class DMPickupProfile : ScriptableObject
    {
        public const string ResourcesPath = "Interaction/DM_PickupProfile";
        public const string AssetPath = "Assets/_Project/Resources/Interaction/DM_PickupProfile.asset";
        const string DefaultPrompt = "Hold E to Take";

        [Header("Hold E")]
        [Tooltip("Seconds E must be held to take a focused item or blueprint.")]
        [Min(0.05f)] public float holdSeconds = 0.5f;

        [Header("Prompt & Focus")]
        [Tooltip("Planar distance (m) at which the yellow dot becomes the Hold-E prompt and pickup is allowed.")]
        [Min(0.1f)] public float closePromptPlanarMeters = 1.25f;
        [Tooltip("View cone (degrees) used to choose the focused pickup.")]
        [Range(1f, 179f)] public float pickupConeFovDegrees = 70f;
        [Tooltip("Prompt shown when a pickup prefab has no prompt text of its own.")]
        public string defaultPromptText = DefaultPrompt;

        [Header("Pickup Toast")]
        [Min(0.01f)] public float toastSlideInSeconds = 0.35f;
        [Min(0f)] public float toastHoldSeconds = 2.3f;
        [Min(0.01f)] public float toastFadeOutSeconds = 0.35f;
        public float toastSlideDistance = 28f;

        static DMPickupProfile live;
        static bool searchedThisPlay;

        public static DMPickupProfile Live
        {
            get
            {
                if (live != null)
                    return live;
                if (Application.isPlaying && searchedThisPlay)
                    return null;
                live = Resources.Load<DMPickupProfile>(ResourcesPath);
                searchedThisPlay = Application.isPlaying;
                return live;
            }
        }

        public static float HoldSeconds => Live != null ? Mathf.Max(0.05f, Live.holdSeconds) : 0.5f;
        public static float ClosePromptMeters => Live != null ? Mathf.Max(0.1f, Live.closePromptPlanarMeters) : 1.25f;
        public static float ConeFovDegrees(float fallback) => Live != null ? Live.pickupConeFovDegrees : fallback;
        public static float ToastSlideInSeconds => Live != null ? Mathf.Max(0.01f, Live.toastSlideInSeconds) : 0.35f;
        public static float ToastHoldSeconds => Live != null ? Mathf.Max(0f, Live.toastHoldSeconds) : 2.3f;
        public static float ToastFadeOutSeconds => Live != null ? Mathf.Max(0.01f, Live.toastFadeOutSeconds) : 0.35f;
        public static float ToastSlideDistance => Live != null ? Live.toastSlideDistance : 28f;

        public static string PromptText(string own)
        {
            if (!string.IsNullOrEmpty(own))
                return own;
            return Live != null && !string.IsNullOrEmpty(Live.defaultPromptText) ? Live.defaultPromptText : DefaultPrompt;
        }
    }
}
