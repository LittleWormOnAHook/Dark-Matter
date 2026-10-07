using System;
using UnityEngine;

namespace Project.Events
{
    /// <summary>
    /// Chest / crate lid driver that runs on unscaled time (loot plan 7.4), so the lid keeps moving while
    /// the loot or storage window holds the game at timeScale 0. Two modes:
    /// <list type="bullet">
    /// <item><b>LegacyClip</b>: samples a legacy <see cref="Animation"/> clip (e.g. <c>Cache-Lid-Open</c>) by hand,
    /// forward to open and in reverse to close, resuming from the current pose when interrupted.</item>
    /// <item><b>ProceduralHinge</b>: rotates / lifts a lid transform from its closed pose (no clip needed).</item>
    /// </list>
    /// Open / close durations default to <see cref="DMLootChestProfile"/> lid times; a positive local override wins.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Loot/DM Chest Lid")]
    public sealed class DMChestLid : MonoBehaviour
    {
        public enum LidMode
        {
            LegacyClip = 0,
            ProceduralHinge = 1
        }

        [SerializeField] private LidMode mode = LidMode.LegacyClip;

        [Header("Legacy clip")]
        [SerializeField] private Animation chestAnimation;
        [SerializeField] private string clipName = "Cache-Lid-Open";

        [Header("Procedural hinge")]
        [SerializeField] private Transform lidTransform;
        [Tooltip("Local euler rotation added to the closed pose when fully open.")]
        [SerializeField] private Vector3 openEulerOffset = new Vector3(-80f, 0f, 0f);
        [Tooltip("Local position offset added to the closed pose when fully open.")]
        [SerializeField] private Vector3 openLocalOffset = Vector3.zero;

        [Header("Timing (<= 0 uses DM_LootChestProfile)")]
        [SerializeField, Min(-1f)] private float openSecondsOverride = -1f;
        [SerializeField, Min(-1f)] private float closeSecondsOverride = -1f;

        private float progress;
        private float target;
        private Action pendingCallback;
        private bool closedPoseCaptured;
        private Vector3 closedLocalPosition;
        private Quaternion closedLocalRotation = Quaternion.identity;

        /// <summary>0 = closed, 1 = fully open.</summary>
        public float Progress => progress;
        public bool IsMoving => !Mathf.Approximately(progress, target);
        public bool IsFullyOpen => progress >= 0.999f && target >= 0.999f;
        public bool IsFullyClosed => progress <= 0.001f && target <= 0.001f;
        public bool IsOpeningOrOpen => target >= 0.999f;
        public LidMode Mode => mode;

        public bool HasDriver =>
            mode == LidMode.ProceduralHinge
                ? lidTransform != null
                : chestAnimation != null && ResolveState() != null;

        /// <summary>
        /// Returns the lid on the chest's Animation object, adding a LegacyClip lid at runtime when missing.
        /// Null when there is no Animation (nothing to drive).
        /// </summary>
        public static DMChestLid EnsureForLegacyClip(Animation animation, string clip)
        {
            if (animation == null)
                return null;

            DMChestLid lid = animation.GetComponent<DMChestLid>();
            if (lid == null)
            {
                // Never add components to scene objects from edit-mode tools (no scene dirtying).
                if (!Application.isPlaying)
                    return null;
                lid = animation.gameObject.AddComponent<DMChestLid>();
            }
            if (!lid.HasDriver)
                lid.ConfigureLegacyClip(animation, clip);
            return lid;
        }

        /// <summary>Runtime setup for chests that carry a legacy lid clip but no lid component yet.</summary>
        public void ConfigureLegacyClip(Animation animation, string clip)
        {
            mode = LidMode.LegacyClip;
            chestAnimation = animation;
            if (!string.IsNullOrWhiteSpace(clip))
                clipName = clip;
            else if (animation != null && animation.clip != null)
                clipName = animation.clip.name;
        }

        /// <summary>Runtime setup for a hinged lid transform (no clip).</summary>
        public void ConfigureProcedural(Transform lid, Vector3 openEuler, Vector3 openOffset, float openSeconds = -1f, float closeSeconds = -1f)
        {
            mode = LidMode.ProceduralHinge;
            lidTransform = lid;
            openEulerOffset = openEuler;
            openLocalOffset = openOffset;
            openSecondsOverride = openSeconds;
            closeSecondsOverride = closeSeconds;
            closedPoseCaptured = false;
            CaptureClosedPose();
        }

        /// <summary>Starts (or resumes) opening. <paramref name="onComplete"/> runs once fully open.</summary>
        public void Open(Action onComplete = null)
        {
            MoveTo(1f, onComplete);
        }

        /// <summary>Starts (or resumes) closing. <paramref name="onComplete"/> runs once fully closed.</summary>
        public void Close(Action onComplete = null)
        {
            MoveTo(0f, onComplete);
        }

        /// <summary>Jumps straight to the closed pose (no callback of a pending move fires).</summary>
        public void SnapClosed()
        {
            pendingCallback = null;
            target = 0f;
            progress = 0f;
            ApplyPose(0f);
        }

        /// <summary>Jumps straight to the open pose.</summary>
        public void SnapOpen()
        {
            pendingCallback = null;
            target = 1f;
            progress = 1f;
            ApplyPose(1f);
        }

        private void Awake()
        {
            CaptureClosedPose();
        }

        private void OnDisable()
        {
            // An inactive lid cannot animate: settle at the target so callers never wait forever.
            if (!IsMoving)
                return;

            progress = target;
            ApplyPose(progress);
            FireCallback();
        }

        private void MoveTo(float destination, Action onComplete)
        {
            CaptureClosedPose();
            target = Mathf.Clamp01(destination);
            pendingCallback = onComplete;

            if (!HasDriver || !isActiveAndEnabled)
            {
                progress = target;
                ApplyPose(progress);
                FireCallback();
                return;
            }

            if (!IsMoving)
            {
                ApplyPose(progress);
                FireCallback();
            }
        }

        private void Update()
        {
            if (!IsMoving)
                return;

            float seconds = target > progress ? ResolveOpenSeconds() : ResolveCloseSeconds();
            float step = seconds <= 0.0001f ? 1f : Time.unscaledDeltaTime / seconds;
            progress = Mathf.MoveTowards(progress, target, step);
            ApplyPose(progress);
            if (!IsMoving)
                FireCallback();
        }

        private void FireCallback()
        {
            Action callback = pendingCallback;
            pendingCallback = null;
            callback?.Invoke();
        }

        private float ResolveOpenSeconds()
        {
            if (openSecondsOverride > 0f)
                return openSecondsOverride;
            DMLootChestProfile profile = DMLootChestProfile.Live;
            return profile != null ? Mathf.Max(0.05f, profile.lidOpenSeconds) : 1f;
        }

        private float ResolveCloseSeconds()
        {
            if (closeSecondsOverride > 0f)
                return closeSecondsOverride;
            DMLootChestProfile profile = DMLootChestProfile.Live;
            return profile != null ? Mathf.Max(0.05f, profile.lidCloseSeconds) : 0.8f;
        }

        private void CaptureClosedPose()
        {
            if (closedPoseCaptured || lidTransform == null)
                return;

            closedLocalPosition = lidTransform.localPosition;
            closedLocalRotation = lidTransform.localRotation;
            closedPoseCaptured = true;
        }

        private AnimationState ResolveState()
        {
            if (chestAnimation == null)
                return null;

            if (!string.IsNullOrWhiteSpace(clipName))
            {
                AnimationState named = chestAnimation[clipName];
                if (named != null)
                    return named;
            }

            AnimationClip fallback = chestAnimation.clip;
            return fallback != null ? chestAnimation[fallback.name] : null;
        }

        private void ApplyPose(float t)
        {
            if (mode == LidMode.ProceduralHinge)
            {
                if (lidTransform == null)
                    return;

                CaptureClosedPose();
                float eased = Mathf.SmoothStep(0f, 1f, t);
                lidTransform.localRotation = closedLocalRotation * Quaternion.Euler(openEulerOffset * eased);
                lidTransform.localPosition = closedLocalPosition + openLocalOffset * eased;
                return;
            }

            AnimationState state = ResolveState();
            if (state == null)
                return;

            // Hold the sampled pose: enabled state at speed 0 with clamped wrap, sampled by hand on unscaled time.
            state.wrapMode = WrapMode.ClampForever;
            state.enabled = true;
            state.weight = 1f;
            state.speed = 0f;
            state.time = Mathf.Clamp01(t) * state.length;
            chestAnimation.Sample();
        }
    }
}
