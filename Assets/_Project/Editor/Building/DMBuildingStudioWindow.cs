using Project.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Building Studio: placement profile, preview ghost materials, and finished-piece library.
    /// </summary>
    public sealed class DMBuildingStudioWindow : EditorWindow
    {
        const string AssetPath = DMBuildingGhostProfile.AssetPath;

        DMBuildingGhostProfile profile;
        Vector2 scrollPosition;

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Building Studio")]
        public static void Open()
        {
            GetWindow<DMBuildingStudioWindow>("Building Studio");
        }

        void OnEnable()
        {
            profile = LoadOrCreate();
        }

        void OnGUI()
        {
            if (profile == null)
                profile = LoadOrCreate();

            EditorGUILayout.LabelField("Building profile", EditorStyles.boldLabel);
            EditorGUILayout.ObjectField("Asset", profile, typeof(DMBuildingGhostProfile), false);
            EditorGUILayout.HelpBox(
                "Play reads DM_BuildingGhostProfile from Resources. "
                + "Valid snap preview uses Snap & Build fields; invalid seats use Blocked. "
                + "Unbuilt ghosts still refund when build mode ends.",
                MessageType.Info);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Building Profile");

            EditorGUILayout.Space();
            DrawPreviewGhostSection();

            EditorGUILayout.Space();
            DrawBuiltTintSection();

            EditorGUILayout.Space();
            DrawSnapSection();

            EditorGUILayout.Space();
            DrawPlacementSection();

            EditorGUILayout.Space();
            DrawDoorSection();

            EditorGUILayout.Space();
            DrawFinishedMaterialLibrary();

            if (GUILayout.Button("Create Stone Finishes"))
                DMBuildingMaterialLibraryBuilder.EnsureStoneFinishes();

            EditorGUILayout.EndScrollView();

            if (EditorGUI.EndChangeCheck())
            {
                profile.ghostAlpha = Mathf.Clamp01(profile.ghostAlpha);
                profile.blockedGhostAlpha = Mathf.Clamp01(profile.blockedGhostAlpha);
                profile.glassAlpha = Mathf.Clamp01(profile.glassAlpha);
                profile.edgeFacingDot = Mathf.Clamp01(profile.edgeFacingDot);
                EditorUtility.SetDirty(profile);
                DMBuildingMaterialLibrary materialLibrary = DMBuildingMaterialLibrary.Live;
                if (materialLibrary != null)
                    EditorUtility.SetDirty(materialLibrary);
                AssetDatabase.SaveAssets();
            }
        }

        void DrawPreviewGhostSection()
        {
            EditorGUILayout.LabelField("Preview holograms", EditorStyles.boldLabel);

            EditorGUILayout.LabelField("Snap & build (valid seat)", EditorStyles.miniBoldLabel);
            profile.validGhostMaterial = (Material)EditorGUILayout.ObjectField(
                "Material (optional)",
                profile.validGhostMaterial,
                typeof(Material),
                false);
            profile.ghostColor = EditorGUILayout.ColorField(
                new GUIContent("Color"),
                profile.ghostColor,
                true,
                false,
                false);
            profile.ghostAlpha = EditorGUILayout.Slider("Alpha", profile.ghostAlpha, 0f, 1f);

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Blocked (invalid / unaffordable)", EditorStyles.miniBoldLabel);
            profile.blockedGhostMaterial = (Material)EditorGUILayout.ObjectField(
                "Material (optional)",
                profile.blockedGhostMaterial,
                typeof(Material),
                false);
            profile.blockedGhostColor = EditorGUILayout.ColorField(
                new GUIContent("Color"),
                profile.blockedGhostColor,
                true,
                false,
                false);
            profile.blockedGhostAlpha = EditorGUILayout.Slider("Alpha", profile.blockedGhostAlpha, 0f, 1f);
        }

        void DrawBuiltTintSection()
        {
            EditorGUILayout.LabelField("Built piece tints", EditorStyles.boldLabel);
            profile.finishedColor = EditorGUILayout.ColorField(
                new GUIContent("Finished mesh"),
                profile.finishedColor,
                true,
                false,
                false);
            profile.glassColor = EditorGUILayout.ColorField(
                new GUIContent("Window glass"),
                profile.glassColor,
                true,
                false,
                false);
            profile.glassAlpha = EditorGUILayout.Slider("Glass alpha", profile.glassAlpha, 0f, 1f);
        }

        void DrawSnapSection()
        {
            EditorGUILayout.LabelField("Snap & grid", EditorStyles.boldLabel);
            profile.yawStepDegrees = EditorGUILayout.FloatField("Yaw degrees / notch", profile.yawStepDegrees);
            profile.heightStepMeters = EditorGUILayout.FloatField("Height step (m)", profile.heightStepMeters);
            profile.maxHeightOffsetMeters = EditorGUILayout.FloatField("Max height offset (m)", profile.maxHeightOffsetMeters);
            profile.largeModuleMeters = EditorGUILayout.FloatField("Large module (m)", profile.largeModuleMeters);
            profile.smallModuleMeters = EditorGUILayout.FloatField("Small module (m)", profile.smallModuleMeters);
            profile.edgeSnapRangeMeters = EditorGUILayout.FloatField("Edge snap range (m)", profile.edgeSnapRangeMeters);
            profile.topSnapRangeMeters = EditorGUILayout.FloatField("Top snap range (m)", profile.topSnapRangeMeters);
            profile.buildLookUpDegrees = EditorGUILayout.Slider("Build look up (deg)", profile.buildLookUpDegrees, 0f, 75f);
            profile.buildModeCameraDistanceMultiplier = EditorGUILayout.Slider(
                "Build camera distance ×",
                profile.buildModeCameraDistanceMultiplier,
                1f,
                3f);
            profile.buildModeCameraExtraMeters = EditorGUILayout.FloatField(
                "Build camera extra (m)",
                profile.buildModeCameraExtraMeters);
            profile.doorFrameRangeMeters = EditorGUILayout.FloatField("Door frame range (m)", profile.doorFrameRangeMeters);
            profile.edgeFacingDot = EditorGUILayout.Slider("Edge facing dot", profile.edgeFacingDot, 0f, 1f);
        }

        void DrawPlacementSection()
        {
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            profile.buildSeconds = EditorGUILayout.FloatField("Hold to build (s)", profile.buildSeconds);
            profile.destroyHoldSeconds = EditorGUILayout.FloatField("Hold to destroy (s)", profile.destroyHoldSeconds);
            profile.aimDistanceMeters = EditorGUILayout.FloatField("Aim distance (m)", profile.aimDistanceMeters);
            profile.doorSeatDropMeters = EditorGUILayout.FloatField("Door seat drop (m)", profile.doorSeatDropMeters);
            profile.overlapPaddingMeters = EditorGUILayout.FloatField("Overlap padding (m)", profile.overlapPaddingMeters);
        }

        void DrawDoorSection()
        {
            EditorGUILayout.LabelField("Doors (E swing)", EditorStyles.boldLabel);
            profile.doorSwingDegrees = EditorGUILayout.FloatField("Swing degrees", profile.doorSwingDegrees);
            profile.doorSwingSeconds = EditorGUILayout.FloatField("Swing duration (s)", profile.doorSwingSeconds);
            profile.doorInteractRangeMeters = EditorGUILayout.FloatField("Interact range (m)", profile.doorInteractRangeMeters);
        }

        void DrawFinishedMaterialLibrary()
        {
            EditorGUILayout.LabelField("Finished materials (M key)", EditorStyles.boldLabel);
            DMBuildingMaterialLibrary materialLibrary = DMBuildingMaterialLibrary.Live;
            if (materialLibrary == null || materialLibrary.variants == null || materialLibrary.variants.Count == 0)
            {
                EditorGUILayout.HelpBox(
                    "No material variants yet. Use Create Stone Finishes, then assign HDRP materials per tier.",
                    MessageType.None);
                return;
            }

            for (int i = 0; i < materialLibrary.variants.Count; i++)
            {
                DMBuildingMaterialVariant variant = materialLibrary.variants[i];
                if (variant == null)
                    continue;
                EditorGUILayout.LabelField(variant.displayName + " (" + variant.tier + ")");
                variant.finishedMaterial = (Material)EditorGUILayout.ObjectField(
                    "Finished material",
                    variant.finishedMaterial,
                    typeof(Material),
                    false);
            }
        }

        static DMBuildingGhostProfile LoadOrCreate()
        {
            var existing = AssetDatabase.LoadAssetAtPath<DMBuildingGhostProfile>(AssetPath);
            if (existing != null)
                return existing;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Building"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Building");

            var created = CreateInstance<DMBuildingGhostProfile>();
            AssetDatabase.CreateAsset(created, AssetPath);
            AssetDatabase.SaveAssets();
            return created;
        }
    }
}
