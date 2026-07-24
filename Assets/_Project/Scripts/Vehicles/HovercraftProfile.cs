using Project.Data;
using UnityEngine;

namespace Project.Vehicles
{
    [CreateAssetMenu(
        fileName = "HovercraftProfile",
        menuName = "Survival Pioneer/Vehicles/Hovercraft Profile")]
    public class HovercraftProfile : ScriptableObject
    {
        [Header("Interaction")]
        public float enterRange = 3.5f;
        public string enterPrompt = "Press E to board hovercraft";
        public string exitPrompt = "Press E to exit hovercraft";

        [Header("Hover Physics")]
        public float hoverHeight = 2f;
        public float minAltitudeAboveGround = 2f;
        public float maxAltitudeAboveGround = 10f;
        [Tooltip("Altitude above ground when unoccupied so the player can board again.")]
        public float parkedAltitudeAboveGround = 0.35f;
        [Tooltip("How close to parked altitude before the player is released on exit.")]
        public float exitAltitudeTolerance = 0.15f;
        [Tooltip("Max seconds to wait for descent before forcing dismount.")]
        public float exitDescentTimeout = 4f;
        public float verticalAdjustSpeed = 3.5f;
        public float springStrength = 50000f;
        public float springDamping = 8000f;
        public LayerMask groundMask = ~0;
        public float rayLength = 16f;

        [Header("Thrust")]
        public float forwardThrust = 8500f;
        public float reverseThrust = 4200f;
        public float strafeThrust = 6200f;
        public float yawTorque = 6400f;
        public float maxForwardSpeed = 22f;
        public float maxReverseSpeed = 10f;
        public float maxStrafeSpeed = 14f;
        [Tooltip("W/S held above this magnitude switches A/D from strafe to yaw steering.")]
        public float forwardSteerThreshold = 0.15f;
        public float boosterMultiplier = 1.75f;
        public float linearDrag = 1.2f;
        public float angularDrag = 2.5f;

        [Header("Turbulence")]
        [Tooltip("Base camera/craft jiggle amplitude at low speed.")]
        public float turbulenceBaseAmplitude = 0.015f;
        [Tooltip("Max turbulence multiplier at full speed (1.5 = 50% stronger than base).")]
        public float turbulenceMaxMultiplier = 1.5f;
        public float turbulenceFrequency = 2.4f;
        public float visualTurbulenceRotation = 0.35f;

        [Header("Drive Tilt (Visual)")]
        public float drivePitchMax = 6f;
        public float driveRollMax = 22f;
        public float maxTotalBank = 32f;
        public float drivePitchSettleSeconds = 2.5f;
        public float driveTiltSmooth = 6f;
        public float yawRollFactor = 12f;

        [Header("Follow Camera")]
        public float followDistanceDefault = 14f;
        public float followDistanceMin = 5f;
        public float followDistanceMax = 36f;
        public float followHeight = 4.5f;
        public float followLookAhead = 6f;
        public float scrollDistanceStep = 2f;
        [Tooltip("How quickly the chase cam orbits when the craft yaws. Lower = subtler aircraft-style lag.")]
        public float followHeadingSmooth = 2.2f;
        [Tooltip("How quickly the chase cam position catches the desired orbit point.")]
        public float followPositionSmooth = 3.5f;
        [Tooltip("How quickly the chase cam look catches the look target. Keeps horizon level (no bank).")]
        public float followLookSmooth = 4f;
        [Tooltip("Scales lateral turbulence on the chase cam only (0 = none). Vertical still uses full sample.")]
        [Range(0f, 1f)] public float followLateralTurbulenceScale = 0.12f;

        [Header("Turret")]
        [Tooltip("Max yaw/pitch offset from craft forward (degrees).")]
        public float turretArcDegrees = 35f;
        public Vector2 turretSensitivity = new Vector2(0.06f, 0.05f);
        public float reticleSmoothSpeed = 8f;
        public float turretFireCooldown = 0.18f;
        [Tooltip("Turret fire one-shot. Drag an imported MP3/WAV/OGG AudioClip from the Project window.")]
        public AudioClip turretFireClip;
        [Tooltip("Optional second turret fire clip. Falls back to turretFireClip when empty.")]
        public AudioClip turretFireClip2;

        [Header("Engine Audio")]
        [Tooltip("Looping engine hum while occupied. Drag an imported MP3/WAV/OGG AudioClip from the Project window.")]
        public AudioClip engineRunningClip;
        [Range(0f, 1f)] public float engineVolume = 0.55f;
        public Vector2 enginePitchRange = new Vector2(0.85f, 1.35f);

        [Header("Vehicle Audio")]
        [Tooltip("One-shot played when boarding.")]
        public AudioClip boardClip;
        [Range(0f, 1f)] public float boardVolume = 0.75f;
        [Tooltip("One-shot played when exiting.")]
        public AudioClip exitClip;
        [Range(0f, 1f)] public float exitVolume = 0.75f;
        [Tooltip("One-shot played when boost (Shift) starts while moving forward.")]
        public AudioClip boostAudioClip;
        [Range(0f, 1f)] public float boostVolume = 0.85f;

        [Header("Weapon")]
        public ItemData weaponItem;
        public ItemData ammoItem;
    }
}
