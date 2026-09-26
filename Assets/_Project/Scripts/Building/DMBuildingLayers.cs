using Invector.vCamera;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.Building
{
    /// <summary>
    /// 0925-layers: placed pieces sit on a dedicated Building layer. Placement raycasts and overlap
    /// tests use these masks instead of every layer. The player, enemies and the third-person camera
    /// learn the Building layer at runtime so they still stand on and collide with pieces.
    /// </summary>
    public static class DMBuildingLayers
    {
        public const int IgnoreRaycastLayer = 2;

        static string cachedName;
        static int cachedLayer = -1;
        static int maskFrame = -1;
        static int aimMask;
        static int groundMask;
        static int overlapMask;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache()
        {
            cachedName = null;
            cachedLayer = -1;
            maskFrame = -1;
        }

        /// <summary>Building layer index, or -1 until the layer exists.</summary>
        public static int BuildingLayer
        {
            get
            {
                string name = DMBuildingGhostProfile.BuildingLayerName;
                if (name != cachedName)
                {
                    cachedName = name;
                    cachedLayer = LayerMask.NameToLayer(name);
                }

                return cachedLayer;
            }
        }

        public static int BuildingBit
        {
            get
            {
                int layer = BuildingLayer;
                return layer >= 0 ? 1 << layer : 0;
            }
        }

        /// <summary>Bit for the layer pieces actually use: Building, or Default until that layer exists.</summary>
        public static int PieceBit
        {
            get
            {
                int bit = BuildingBit;
                return bit != 0 ? bit : 1;
            }
        }

        public static int Bit(string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            return layer >= 0 ? 1 << layer : 0;
        }

        /// <summary>Crosshair ray against built pieces: Building + Climbable by default.</summary>
        public static int AimMask
        {
            get
            {
                Refresh();
                return aimMask;
            }
        }

        /// <summary>Ground ray for the first foundation: Default + Climbable + Terrain by default, never Building.</summary>
        public static int GroundMask
        {
            get
            {
                Refresh();
                return groundMask;
            }
        }

        /// <summary>Seat overlap test: Building plus blockers only.</summary>
        public static int OverlapMask
        {
            get
            {
                Refresh();
                return overlapMask;
            }
        }

        static void Refresh()
        {
            int frame = Time.frameCount;
            if (frame == maskFrame)
                return;

            maskFrame = frame;
            int pieces = PieceBit;
            int building = BuildingBit;
            int ignore = 1 << IgnoreRaycastLayer;

            int aim = DMBuildingGhostProfile.BuiltAimLayersRaw;
            if (aim == 0)
                aim = Bit("Climbable");
            aimMask = (aim | pieces) & ~ignore;

            int ground = DMBuildingGhostProfile.GroundLayersRaw;
            if (ground == 0)
                ground = 1 | Bit("Climbable") | Bit("Terrain"); // 0926: one terrain tile sits on Climbable
            groundMask = ground & ~building & ~ignore;
            if (groundMask == 0)
                groundMask = 1;

            int blockers = DMBuildingGhostProfile.BlockerLayersRaw;
            if (blockers == 0)
            {
                blockers = 1 | Bit("Climbable") | Bit("Resource") | Bit("Pushable")
                    | Bit("PW_Object_Small") | Bit("PW_Object_Medium") | Bit("PW_Object_Large");
            }

            overlapMask = (blockers | pieces) & ~ignore;
        }

        public static void SetLayer(GameObject root, int layer)
        {
            if (root == null || layer < 0 || layer > 31)
                return;

            foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
                part.gameObject.layer = layer;
        }

        /// <summary>Moves a placed piece and all its parts onto the Building layer.</summary>
        public static void Apply(GameObject root)
        {
            SetLayer(root, BuildingLayer);
        }

        /// <summary>
        /// Adds the Building layer to every character ground mask and camera culling mask that already
        /// includes Default, so pieces keep working as floors and camera blockers.
        /// </summary>
        public static void EnsureWorldMasks()
        {
            int bit = BuildingBit;
            if (bit == 0)
                return;

            vThirdPersonMotor[] motors = UnityEngine.Object.FindObjectsByType<vThirdPersonMotor>(FindObjectsInactive.Exclude);
            for (int i = 0; i < motors.Length; i++)
            {
                vThirdPersonMotor motor = motors[i];
                if (motor == null)
                    continue;
                int value = motor.groundLayer.value;
                if ((value & 1) != 0 && (value & bit) == 0)
                    motor.groundLayer = value | bit;
            }

            vThirdPersonCamera[] cameras = UnityEngine.Object.FindObjectsByType<vThirdPersonCamera>(FindObjectsInactive.Exclude);
            for (int i = 0; i < cameras.Length; i++)
            {
                vThirdPersonCamera cam = cameras[i];
                if (cam == null)
                    continue;
                int value = cam.cullingLayer.value;
                if ((value & 1) != 0 && (value & bit) == 0)
                    cam.cullingLayer = value | bit;
            }
        }
    }
}
