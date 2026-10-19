using System;
using FORGE3D;
using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>
    /// Per-prefab FORGE3D <see cref="F3DTurret"/> tracking tuning on <see cref="DMTurretRuntime"/>.
    /// When an override toggle is off, that field is not pushed — the linked F3DTurret keeps its prefab value.
    /// </summary>
    [Serializable]
    public sealed class DMTurretTrackingAuthority
    {
        [Header("Mount / swivel (optional)")]
        [Tooltip("When on, Mount / Swivel below replace forgeTurret refs (usually wired from hierarchy).")]
        public bool overrideMountSwivel;
        public GameObject mount;
        public GameObject swivel;

        [Header("Tracking speeds")]
        public bool overrideHeadingTrackingSpeed;
        public float headingTrackingSpeed = 40f;

        public bool overrideElevationTrackingSpeed;
        public float elevationTrackingSpeed = 30f;

        [Header("Limits (degrees)")]
        public bool overrideHeadingLimit;
        public Vector2 headingLimit = new Vector2(-180f, 180f);

        public bool overrideElevationLimit;
        public Vector2 elevationLimit = new Vector2(-15f, 45f);

        [Header("Motion")]
        public bool overrideSmoothControlling;
        public bool smoothControlling;

        [Header("Debug / animation")]
        [Tooltip("Scene view (Edit mode only): draw heading/elevation limit cones from the mount/barrel pivot (F3D TrackTarget axes). Off by default — drawing all turrets is expensive.")]
        public bool editorDrawVisionCones = false;

        public bool overrideDebugDraw;
        public bool debugDraw;

        public bool overrideDebugTarget;
        public Transform debugTarget;

        public bool overrideAnimators;
        public Animator[] animators;

        public bool Any =>
            overrideMountSwivel || overrideHeadingTrackingSpeed || overrideElevationTrackingSpeed
            || overrideHeadingLimit || overrideElevationLimit || overrideSmoothControlling
            || overrideDebugDraw || overrideDebugTarget || overrideAnimators;
    }
}
