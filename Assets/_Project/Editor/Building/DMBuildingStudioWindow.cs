using Project.Building;
using Project.EditorTools.GenesisStudio;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Building Studio: Settings (placement profile, preview ghosts, snap, doors) and Library (styles, kits, parts).
    /// </summary>
    public sealed class DMBuildingStudioWindow : EditorWindow
    {
        const string AssetPath = DMBuildingGhostProfile.AssetPath;
        static readonly string[] Tabs = { "Settings", "Library" };

        DMBuildingGhostProfile profile;
        Vector2 scrollPosition;
        int tab;
        readonly DMBuildingLibraryPanel libraryPanel = new DMBuildingLibraryPanel();

        [MenuItem("Tools/Dark Matter Genesis/Buildings/Building Studio")]
        public static void Open()
        {
            GetWindow<DMBuildingStudioWindow>("Building Studio");
        }

        void OnEnable()
        {
            profile = LoadOrCreate();
            tab = EditorPrefs.GetInt("DM.BuildingStudio.Tab", 0);
        }

        void OnGUI()
        {
            if (profile == null)
                profile = LoadOrCreate();

            DMStudioStyles.DrawHeroHeader("Building Studio", "Stone, Iron and Silicate build kits - placement, snap, and the part library.");
            EditorGUILayout.BeginHorizontal(DMStudioStyles.SidebarPanel);
            for (int i = 0; i < Tabs.Length; i++)
            {
                if (DMStudioStyles.DrawCategoryTab(Tabs[i], tab == i, new Color(0.75f, 0.18f, 0.48f, 1f)) && tab != i)
                {
                    tab = i;
                    EditorPrefs.SetInt("DM.BuildingStudio.Tab", tab);
                    GUI.FocusControl(null);
                }
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (tab == 1)
                libraryPanel.Draw();
            else
                DrawSettings();
            EditorGUILayout.EndScrollView();
        }

        void DrawSettings()
        {
            DMStudioStyles.DrawSection("Building profile", DMStudioStyles.ContentPanel, () =>
            {
                EditorGUILayout.ObjectField("Asset", profile, typeof(DMBuildingGhostProfile), false);
                EditorGUILayout.HelpBox(
                    "Play reads DM_BuildingGhostProfile from Resources. "
                    + "Valid snap preview uses Snap & Build fields; invalid seats use Blocked. "
                    + "Unbuilt ghosts still refund when build mode ends. Kit shapes, finishes and parts live per style under Library.",
                    MessageType.Info);
            });

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Building Profile");
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawPreviewGhostSection);
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawBuiltTintSection);
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawSnapSection);
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawPlacementSection);
            DMStudioStyles.DrawSection(null, DMStudioStyles.ContentPanel, DrawDoorSection);

            if (EditorGUI.EndChangeCheck())
            {
                profile.ghostAlpha = Mathf.Clamp01(profile.ghostAlpha);
                profile.blockedGhostAlpha = Mathf.Clamp01(profile.blockedGhostAlpha);
                profile.glassAlpha = Mathf.Clamp01(profile.glassAlpha);
                profile.edgeFacingDot = Mathf.Clamp01(profile.edgeFacingDot);
                EditorUtility.SetDirty(profile);
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
            profile.builtMaterial = (Material)EditorGUILayout.ObjectField(
                new GUIContent("Built material", "Used on built pieces when their style has no finish. Tinted by Finished mesh."),
                profile.builtMaterial,
                typeof(Material),
                false);
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
