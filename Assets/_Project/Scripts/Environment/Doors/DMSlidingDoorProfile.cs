using System;
using UnityEngine;

namespace Project.Environment.Doors
{
    [Serializable]
    public struct DMSlidingDoorAdditionalSetTiming
    {
        [Tooltip("Seconds after set 1 begins opening before this additional set starts.")]
        [Min(0f)]
        public float openDelaySeconds;
    }

    [CreateAssetMenu(
        menuName = "Dark Matter/Environment/Sliding Door Profile",
        fileName = "DM_SlidingDoorProfile")]
    public sealed class DMSlidingDoorProfile : ScriptableObject
    {
        [Header("Registry")]
        [Tooltip("Unique id for DMSlidingDoorProfileRegistry (e.g. scifi_big_horizontal).")]
        public string profileId = "default";

        [Header("Motion")]
        public DMSlidingDoorMotionMode motionMode = DMSlidingDoorMotionMode.Slide;
        public DMSlidingDoorLocalAxis localAxis = DMSlidingDoorLocalAxis.Z;
        public DMSlidingDoorOpenDirection openDirection = DMSlidingDoorOpenDirection.PanelSymmetric;

        [Tooltip("Meters along local axis when motionMode is Slide.")]
        [Min(0.01f)]
        public float slideDistanceMeters = 1.45f;

        [Tooltip("Degrees when motionMode is Rotate.")]
        [Range(1f, 175f)]
        public float rotateAngleDegrees = 90f;

        [Tooltip("Custom mode only. Multiplier on distance/angle for the left leaf.")]
        public float leftPanelSign = -1f;

        [Tooltip("Custom mode only. Multiplier on distance/angle for the right leaf.")]
        public float rightPanelSign = 1f;

        [Header("Timing")]
        [Min(0.05f)]
        public float moveDurationSeconds = 0.75f;

        [Min(0f)]
        public float closeDelaySeconds = 2f;

        [Header("Additional panel sets (timing defaults)")]
        [Tooltip("Open delay per extra set (B, C, …). Wires to controller Additional Panel Sets by index.")]
        public DMSlidingDoorAdditionalSetTiming[] additionalSetTimings;

        [Header("Trigger")]
        public string playerTag = "Player";

        [Header("VFX")]
        public GameObject openVfxPrefab;
        public GameObject closeVfxPrefab;
        [Min(0.05f)]
        public float vfxLifetimeSeconds = 2f;
        public Vector3 vfxLocalOffset;

        [Header("Audio")]
        public AudioClip openClip;
        public AudioClip closeClip;
        [Range(0f, 1f)]
        public float sfxVolume = 1f;

        public void ResolvePanelSigns(out float leftSign, out float rightSign)
        {
            switch (openDirection)
            {
                case DMSlidingDoorOpenDirection.SameDirection:
                    leftSign = 1f;
                    rightSign = 1f;
                    break;
                case DMSlidingDoorOpenDirection.Custom:
                    leftSign = leftPanelSign;
                    rightSign = rightPanelSign;
                    break;
                default:
                    leftSign = -1f;
                    rightSign = 1f;
                    break;
            }
        }

        public static Vector3 AxisVector(DMSlidingDoorLocalAxis axis)
        {
            switch (axis)
            {
                case DMSlidingDoorLocalAxis.X:
                    return Vector3.right;
                case DMSlidingDoorLocalAxis.Y:
                    return Vector3.up;
                default:
                    return Vector3.forward;
            }
        }
    }

    /// <summary>One left/right leaf pair. Set 1 uses the controller primary Left/Right Panel fields.</summary>
    [Serializable]
    public sealed class DMSlidingDoorPanelSet
    {
        [Tooltip("Left leaf for this set (e.g. DoorHoriz_B).")]
        public Transform leftPanel;

        [Tooltip("Right leaf for this set (e.g. DoorHoriz_B (1)).")]
        public Transform rightPanel;

        [Tooltip("Seconds after set 1 begins opening before this set starts. Ignored for the primary pair.")]
        [Min(0f)]
        public float openDelaySeconds;
    }
}
