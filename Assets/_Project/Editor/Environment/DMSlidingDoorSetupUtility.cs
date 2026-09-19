using System.Text;
using Project.EditorTools;
using Project.Environment.Doors;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Environment
{
    public static class DMSlidingDoorSetupUtility
    {
        private const string DefaultProfileAssetPath =
            "Assets/_Project/Resources/Environment/SlidingDoors/DM_SlidingDoor_SciFiBig.asset";

        private const string ResourcesFolder = "Assets/_Project/Resources/Environment/SlidingDoors";

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Wire Selected Sliding Door (A + trigger)")]
        public static void WireSelectedSlidingDoor()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Sliding Door",
                    "Select a Sci-Fi kit door root (e.g. DOOR_Horz_ThinFrame_) in the Hierarchy.",
                    "OK");
                return;
            }

            EnsureDefaultProfileAsset();
            string message = WireDoorRoot(selected);
            EditorUtility.DisplayDialog("Sliding Door", message, "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Wire Additional Sliding Door Sets (B/C/D)")]
        public static void WireAdditionalSlidingDoorSets()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Sliding Door",
                    "Select a door with DMSlidingDoorController (ThinFrame root).",
                    "OK");
                return;
            }

            DMSlidingDoorController controller = selected.GetComponentInParent<DMSlidingDoorController>();
            if (controller == null)
                controller = selected.GetComponent<DMSlidingDoorController>();

            if (controller == null)
            {
                EditorUtility.DisplayDialog("Sliding Door", "No DMSlidingDoorController on selection.", "OK");
                return;
            }

            controller.EditorRebindAndRecache(wireAdditionalSets: true);
            EditorUtility.SetDirty(controller);
            EditorUtility.DisplayDialog(
                "Sliding Door",
                "Additional panel sets (B/C/D) auto-wired where present under DOOR Horizontal.",
                "OK");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.World + "Create Default Sliding Door Profile")]
        public static void CreateDefaultProfileMenu()
        {
            DMSlidingDoorProfile profile = EnsureDefaultProfileAsset();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = profile;
            EditorUtility.DisplayDialog(
                "Sliding Door Profile",
                "Created or refreshed:\n" + DefaultProfileAssetPath,
                "OK");
        }

        public static DMSlidingDoorProfile EnsureDefaultProfileAsset()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Environment"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Environment");

            if (!AssetDatabase.IsValidFolder(ResourcesFolder))
                AssetDatabase.CreateFolder("Assets/_Project/Resources/Environment", "SlidingDoors");

            DMSlidingDoorProfile existing =
                AssetDatabase.LoadAssetAtPath<DMSlidingDoorProfile>(DefaultProfileAssetPath);
            if (existing != null)
                return existing;

            var profile = ScriptableObject.CreateInstance<DMSlidingDoorProfile>();
            profile.profileId = DMSlidingDoorProfileRegistry.DefaultProfileId;
            profile.motionMode = DMSlidingDoorMotionMode.Slide;
            profile.localAxis = DMSlidingDoorLocalAxis.Z;
            profile.openDirection = DMSlidingDoorOpenDirection.PanelSymmetric;
            profile.slideDistanceMeters = 1.45f;
            profile.moveDurationSeconds = 0.75f;
            profile.closeDelaySeconds = 2f;
            profile.playerTag = "Player";
            profile.vfxLifetimeSeconds = 2f;
            profile.sfxVolume = 1f;

            AssetDatabase.CreateAsset(profile, DefaultProfileAssetPath);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static string WireDoorRoot(GameObject selected)
        {
            var log = new StringBuilder();
            GameObject root = ResolveDoorRoot(selected);
            if (root != selected)
                log.AppendLine("Door root: " + root.name + " (selected was " + selected.name + ")");

            DMSlidingDoorController controller = root.GetComponent<DMSlidingDoorController>();
            if (controller == null && selected != root)
                controller = selected.GetComponent<DMSlidingDoorController>();

            if (controller != null && controller.gameObject != root)
            {
                Undo.DestroyObjectImmediate(controller);
                controller = null;
            }

            if (controller == null)
                controller = Undo.AddComponent<DMSlidingDoorController>(root);

            RemoveDuplicateControllers(root, controller);
            RemoveControllersOnApproachTriggers(root, controller);
            controller.EditorRebindAndRecache(wireAdditionalSets: false);

            SerializedObject so = new SerializedObject(controller);
            SerializedProperty profileIdProp = so.FindProperty("profileId");
            if (profileIdProp != null && string.IsNullOrWhiteSpace(profileIdProp.stringValue))
            {
                profileIdProp.stringValue = DMSlidingDoorProfileRegistry.DefaultProfileId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            log.AppendLine("Wired: " + root.name);
            log.AppendLine("Profile id: " + DMSlidingDoorProfileRegistry.DefaultProfileId);
            log.AppendLine("Assign open/close VFX and audio on the profile or controller instance.");
            log.AppendLine("Tune slide distance / axis / direction in Genesis Studio → World → Sliding Doors.");

            EditorUtility.SetDirty(root);
            return log.ToString();
        }

        private static GameObject ResolveDoorRoot(GameObject selected)
        {
            if (selected == null)
                return null;

            if (selected.GetComponentInChildren<Transform>(true) != null
                && HasHorizLeafInHierarchy(selected))
                return selected;

            Transform walk = selected.transform;
            while (walk != null)
            {
                if (HasHorizLeafInHierarchy(walk.gameObject))
                    return walk.gameObject;

                walk = walk.parent;
            }

            return selected;
        }

        private static void RemoveControllersOnApproachTriggers(GameObject root, DMSlidingDoorController keep)
        {
            DMSlidingDoorController[] all = root.GetComponentsInChildren<DMSlidingDoorController>(true);
            for (int i = 0; i < all.Length; i++)
            {
                DMSlidingDoorController other = all[i];
                if (other == null || other == keep)
                    continue;

                string n = other.gameObject.name;
                if (n.IndexOf("Door_Big_TRIGGER", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || n.IndexOf("TRIGGER", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    Undo.DestroyObjectImmediate(other);
                }
            }
        }

        private static void RemoveDuplicateControllers(GameObject root, DMSlidingDoorController keep)
        {
            DMSlidingDoorController[] all = root.GetComponentsInChildren<DMSlidingDoorController>(true);
            for (int i = 0; i < all.Length; i++)
            {
                DMSlidingDoorController other = all[i];
                if (other == null || other == keep)
                    continue;

                Undo.DestroyObjectImmediate(other);
            }
        }

        private static bool HasHorizLeafInHierarchy(GameObject root)
        {
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            bool hasLeft = false;
            bool hasTrigger = false;
            for (int i = 0; i < all.Length; i++)
            {
                string n = all[i].name;
                if (n == "DoorHoriz_A" || n.StartsWith("DoorHoriz_A "))
                    hasLeft = true;
                if (n.IndexOf("Door_Big_TRIGGER", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    hasTrigger = true;
            }

            return hasLeft && hasTrigger;
        }
    }
}
