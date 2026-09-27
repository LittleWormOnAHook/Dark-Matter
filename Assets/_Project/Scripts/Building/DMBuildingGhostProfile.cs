using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// Live building placement profile. Building Studio and Genesis Studio edit this asset.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Building/Ghost Profile")]
    public sealed class DMBuildingGhostProfile : ScriptableObject
    {
        public const string ResourcePath = "Building/DM_BuildingGhostProfile";
        public const string AssetPath = "Assets/_Project/Resources/Building/DM_BuildingGhostProfile.asset";

        [Header("Preview — Snap & build (valid seat)")]
        [Tooltip("Optional HDRP material for the green build-ready hologram. Color and alpha still tint each frame.")]
        public Material validGhostMaterial;

        public Color ghostColor = new Color(0.18f, 0.78f, 0.36f, 1f);

        [Range(0f, 1f)]
        public float ghostAlpha = 0.45f;

        [Header("Preview — Blocked seat")]
        [Tooltip("Optional material when the seat is invalid or unaffordable.")]
        public Material blockedGhostMaterial;

        public Color blockedGhostColor = new Color(0.561f, 0.118f, 0.369f, 1f);

        [Range(0f, 1f)]
        public float blockedGhostAlpha = 0.45f;

        [Header("Built piece tints")]
        public Color finishedColor = new Color(0.62f, 0.58f, 0.52f, 1f);

        public Color glassColor = new Color(0.85f, 0.92f, 0.95f, 1f);

        [Range(0f, 1f)]
        public float glassAlpha = 0.35f;

        [Tooltip("Material for built pieces when their style has no finish. Tinted by Finished mesh. Empty = plain lit surface.")]
        public Material builtMaterial;

        [Header("Snap")]
        [Tooltip("Edge-to-edge spacing for 4 m footprints (foundations, walls, floors).")]
        public float largeModuleMeters = 4f;

        [Tooltip("Edge-to-edge spacing for smaller footprints.")]
        public float smallModuleMeters = 2f;

        [Tooltip("Alt + scroll step for the first foundation. Pieces on the building grid turn in 90 degree steps.")]
        public float yawStepDegrees = 45f;
        public float heightStepMeters = 0.25f;
        public float maxHeightOffsetMeters = 4f;
        public float edgeSnapRangeMeters = 4f;
        public float topSnapRangeMeters = 7f;

        [Tooltip("Extra degrees the camera may look up while build mode is on. Applied to the look-up limit only, then restored.")]
        public float buildLookUpDegrees = 45f;

        [Tooltip("Pull camera back in build mode: saved distance × this, plus extra meters. Keep low indoors to avoid wall clip fight.")]
        public float buildModeCameraDistanceMultiplier = 1.35f;

        public float buildModeCameraExtraMeters = 1.25f;
        public float doorFrameRangeMeters = 5f;

        [Range(0f, 1f)]
        public float edgeFacingDot = 0.5f;

        [Header("Placement")]
        public float buildSeconds = 2f;
        public float destroyHoldSeconds = 2f;
        public float aimDistanceMeters = 80f;
        public float doorSeatDropMeters = 0.4f;
        public float overlapPaddingMeters = 0.08f;

        [Tooltip("A foundation may stand alone (start a new base) only when no foundation is within this many meters. Closer than this it must connect edge to edge to another foundation. 0 = only the very first foundation in the world may stand alone.")]
        public float newBaseClearanceMeters = 30f;

        [Header("Base power (0926-generator)")]
        [Tooltip("Side of the square power area around a base, centred on its first placed foundation, in meters. Equipment can be placed on the ground inside it.")]
        public float basePowerSquareMeters = 100f;
        [Tooltip("Generator refuel reaches Plasma Fuel in storage crates within this many meters.")]
        public float generatorFuelRangeMeters = 200f;
        [Tooltip("Generator tank size in units. 1 Plasma Fuel item = 1 unit.")]
        public int generatorTankUnits = 100;
        [Tooltip("Minutes of gameplay one fuel unit lasts.")]
        public float generatorMinutesPerUnit = 5f;
        [Tooltip("Load period in minutes: every powered item and force field in the base adds its units once per period on top of the base burn.")]
        public float powerLoadPeriodMinutes = 30f;
        [Tooltip("Extra Plasma Fuel units per load period for each powered item (lights, equipment).")]
        public float poweredItemUnitsPerPeriod = 1f;
        [Tooltip("Extra Plasma Fuel units per load period for each force field.")]
        public float forceFieldUnitsPerPeriod = 2f;
        [Tooltip("Force fields shut off (anyone can walk through) while their base has no powered generator.")]
        public bool forceFieldsNeedPower = true;
        [Tooltip("Lights on built pieces go dark while their base has no powered generator.")]
        public bool lightsNeedPower = true;

        [Header("Build Hub zone (0926-build-hub)")]
        [Tooltip("Every piece except the Build Hub must stand fully inside a Build Hub zone. Off = build anywhere (testing).")]
        public bool requireBuildHub = true;
        [Tooltip("Width and depth (X and Z) of a level 0 Build Hub zone, centred on the hub, in meters.")]
        public float buildHubZoneMeters = 50f;
        [Tooltip("Height (Y) of a level 0 Build Hub zone, centred on the hub (half above, half below), in meters.")]
        public float buildHubZoneHeightMeters = 50f;
        [Tooltip("Meters added to the zone width and depth per upgrade level.")]
        public float buildHubZoneStepMeters = 25f;
        [Tooltip("Meters added to the zone height per upgrade level.")]
        public float buildHubZoneHeightStepMeters = 25f;
        [Tooltip("Highest Build Hub upgrade level.")]
        public int buildHubMaxLevel = 10;
        [Tooltip("A Build Hub's whole zone must be free of colliders except terrain (characters, creatures, items and built pieces are ignored; triggers count). Also checked when upgrading.")]
        public bool buildHubZoneMustBeClear = true;

        [Header("Patrol paths (0926-build-hub)")]
        [Tooltip("Pieces may not be built on or across creature / pet patrol paths (DMIPathFollowProvider).")]
        public bool blockPatrolPaths = true;
        [Tooltip("Clearance around a patrol path polyline, in meters. The ghost bounds are grown by this much before the test.")]
        public float patrolPathClearanceMeters = 1.5f;
        [Tooltip("Extra vertical slack for the patrol path test, in meters (path points rarely sit exactly on the ground).")]
        public float patrolPathHeightToleranceMeters = 2f;

        [Header("Build crosshair")]
        [Tooltip("Centre dot shown while build mode is on, in pixels. 0 hides it.")]
        public float crosshairDotPixels = 6f;
        public Color crosshairDotColor = new Color(1f, 1f, 1f, 0.9f);
        public Color crosshairDotOutline = new Color(0f, 0f, 0f, 0.6f);

        [Header("Layers")]
        [Tooltip("Layer every placed piece lives on. Rebuild Stone Kit creates it when missing.")]
        public string buildingLayerName = "Building";

        [Tooltip("Crosshair ray that finds built pieces for snapping and destroy. The Building layer is always included. Nothing = Building + Climbable.")]
        public LayerMask builtAimLayers;

        [Tooltip("Ground ray for the first foundation. The Building layer is never included. Nothing = Default + Terrain.")]
        public LayerMask groundLayers;

        [Tooltip("What blocks a seat besides other pieces. The Building layer is always included. Nothing = Default, Climbable, Resource, Pushable and the PW object layers.")]
        public LayerMask blockerLayers;

        [Header("Door swing (E)")]
        public float doorSwingDegrees = 90f;
        public float doorSwingSeconds = 0.35f;
        public float doorInteractRangeMeters = 2.4f;
        [Tooltip("Kit phase 3: both leaves of an 8 m gate swing this far.")]
        public float gateSwingDegrees = 90f;
        [Tooltip("Kit phase 3: gates are heavy, so they swing slower than doors.")]
        public float gateSwingSeconds = 1.4f;

        static DMBuildingGhostProfile live;

        public static DMBuildingGhostProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DMBuildingGhostProfile>(ResourcePath);
                return live;
            }
        }

        public static Color ResolveGhostColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.ghostColor : new Color(0.18f, 0.78f, 0.36f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.ghostAlpha : 0.45f);
            return color;
        }

        public static Color ResolveBlockedGhostColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.blockedGhostColor : new Color(0.561f, 0.118f, 0.369f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.blockedGhostAlpha : 0.45f);
            return color;
        }

        public static Material ValidGhostMaterialTemplate => Live != null ? Live.validGhostMaterial : null;
        public static Material BlockedGhostMaterialTemplate => Live != null ? Live.blockedGhostMaterial : null;
        public static Material BuiltMaterialTemplate => Live != null ? Live.builtMaterial : null;

        public static Color ResolveFinishedColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.finishedColor : new Color(0.62f, 0.58f, 0.52f, 1f);
            color.a = 1f;
            return color;
        }

        public static Color ResolveGlassColor()
        {
            DMBuildingGhostProfile profile = Live;
            Color color = profile != null ? profile.glassColor : new Color(0.85f, 0.92f, 0.95f, 1f);
            color.a = Mathf.Clamp01(profile != null ? profile.glassAlpha : 0.35f);
            return color;
        }

        public static float LargeModuleMeters => Positive(Live != null ? Live.largeModuleMeters : 4f, 4f);
        public static float SmallModuleMeters => Positive(Live != null ? Live.smallModuleMeters : 2f, 2f);
        public static float YawStepDegrees => Positive(Live != null ? Live.yawStepDegrees : 90f, 90f);
        public static float HeightStepMeters => Positive(Live != null ? Live.heightStepMeters : 0.25f, 0.25f);
        public static float MaxHeightOffsetMeters => Positive(Live != null ? Live.maxHeightOffsetMeters : 4f, 4f);
        public static float EdgeSnapRangeMeters => Positive(Live != null ? Live.edgeSnapRangeMeters : 4f, 4f);
        public static float TopSnapRangeMeters => Positive(Live != null ? Live.topSnapRangeMeters : 7f, 7f);
        public static float BuildLookUpDegrees => Mathf.Clamp(Live != null ? Live.buildLookUpDegrees : 45f, 0f, 75f);
        public static float BuildModeCameraDistanceMultiplier =>
            Positive(Live != null ? Live.buildModeCameraDistanceMultiplier : 1.35f, 1.35f);
        public static float BuildModeCameraExtraMeters =>
            Mathf.Max(0f, Live != null ? Live.buildModeCameraExtraMeters : 1.25f);
        public static float DoorFrameRangeMeters => Positive(Live != null ? Live.doorFrameRangeMeters : 5f, 5f);
        public static float EdgeFacingDot => Mathf.Clamp(Live != null ? Live.edgeFacingDot : 0.5f, -1f, 1f);
        public static float BuildSeconds => Positive(Live != null ? Live.buildSeconds : 2f, 2f);
        public static float CrosshairDotPixels => Mathf.Max(0f, Live != null ? Live.crosshairDotPixels : 6f);
        public static Color CrosshairDotColor => Live != null ? Live.crosshairDotColor : new Color(1f, 1f, 1f, 0.9f);
        public static Color CrosshairDotOutline => Live != null ? Live.crosshairDotOutline : new Color(0f, 0f, 0f, 0.6f);
        public static float DestroyHoldSeconds => Positive(Live != null ? Live.destroyHoldSeconds : 2f, 2f);
        public static float AimDistanceMeters => Positive(Live != null ? Live.aimDistanceMeters : 80f, 80f);
        public static float DoorSeatDropMeters => Mathf.Max(0f, Live != null ? Live.doorSeatDropMeters : 0.4f);
        public static float NewBaseClearanceMeters => Mathf.Max(0f, Live != null ? Live.newBaseClearanceMeters : 30f);
        public static float BasePowerSquareMeters => Positive(Live != null ? Live.basePowerSquareMeters : 100f, 100f);
        public static float GeneratorFuelRangeMeters => Mathf.Max(0f, Live != null ? Live.generatorFuelRangeMeters : 200f);
        public static int GeneratorTankUnits => Mathf.Max(1, Live != null ? Live.generatorTankUnits : 100);
        public static float GeneratorMinutesPerUnit => Positive(Live != null ? Live.generatorMinutesPerUnit : 5f, 5f);
        public static float PowerLoadPeriodMinutes => Positive(Live != null ? Live.powerLoadPeriodMinutes : 30f, 30f);
        public static float PoweredItemUnitsPerPeriod => Mathf.Max(0f, Live != null ? Live.poweredItemUnitsPerPeriod : 1f);
        public static float ForceFieldUnitsPerPeriod => Mathf.Max(0f, Live != null ? Live.forceFieldUnitsPerPeriod : 2f);
        public static bool ForceFieldsNeedPower => Live == null || Live.forceFieldsNeedPower;
        public static bool LightsNeedPower => Live == null || Live.lightsNeedPower;
        public static bool RequireBuildHub => Live == null || Live.requireBuildHub;
        public static float BuildHubZoneMeters => Positive(Live != null ? Live.buildHubZoneMeters : 50f, 50f);
        public static float BuildHubZoneHeightMeters => Positive(Live != null ? Live.buildHubZoneHeightMeters : 50f, 50f);
        public static float BuildHubZoneStepMeters => Mathf.Max(0f, Live != null ? Live.buildHubZoneStepMeters : 25f);
        public static float BuildHubZoneHeightStepMeters => Mathf.Max(0f, Live != null ? Live.buildHubZoneHeightStepMeters : 25f);
        public static int BuildHubMaxLevel => Mathf.Max(0, Live != null ? Live.buildHubMaxLevel : 10);
        public static bool BuildHubZoneMustBeClear => Live == null || Live.buildHubZoneMustBeClear;
        public static bool BlockPatrolPaths => Live == null || Live.blockPatrolPaths;
        public static float PatrolPathClearanceMeters => Mathf.Max(0f, Live != null ? Live.patrolPathClearanceMeters : 1.5f);
        public static float PatrolPathHeightToleranceMeters => Mathf.Max(0f, Live != null ? Live.patrolPathHeightToleranceMeters : 2f);
        public static float OverlapPaddingMeters => Mathf.Max(0f, Live != null ? Live.overlapPaddingMeters : 0.08f);
        public static float DoorSwingDegrees => Positive(Live != null ? Live.doorSwingDegrees : 90f, 90f);
        public static float DoorSwingSeconds => Positive(Live != null ? Live.doorSwingSeconds : 0.35f, 0.35f);
        public static float DoorInteractRangeMeters => Positive(Live != null ? Live.doorInteractRangeMeters : 2.4f, 2.4f);
        public static float GateSwingDegrees => Positive(Live != null ? Live.gateSwingDegrees : 90f, 90f);
        public static float GateSwingSeconds => Positive(Live != null ? Live.gateSwingSeconds : 1.4f, 1.4f);

        public static string BuildingLayerName => Live != null && !string.IsNullOrWhiteSpace(Live.buildingLayerName) ? Live.buildingLayerName.Trim() : "Building";
        public static int BuiltAimLayersRaw => Live != null ? Live.builtAimLayers.value : 0;
        public static int GroundLayersRaw => Live != null ? Live.groundLayers.value : 0;
        public static int BlockerLayersRaw => Live != null ? Live.blockerLayers.value : 0;

        static float Positive(float value, float fallback)
        {
            return value > 0.001f ? value : fallback;
        }
    }
}
