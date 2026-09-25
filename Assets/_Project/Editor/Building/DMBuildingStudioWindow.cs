using Project.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Building
{
    /// <summary>
    /// Small Building Studio surface: ghost color and alpha for the placement hologram.
    /// </summary>
    public sealed class DMBuildingStudioWindow : EditorWindow
    {
        const string AssetPath = DMBuildingGhostProfile.AssetPath;

        DMBuildingGhostProfile profile;

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
            EditorGUILayout.HelpBox(
                "Play reads this asset. A blocked seat stays Deep Magenta. Unbuilt ghosts still disappear, with a refund, when build mode ends.",
                MessageType.Info);

            EditorGUI.BeginChangeCheck();
            Undo.RecordObject(profile, "Edit Building Profile");

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            profile.ghostColor = EditorGUILayout.ColorField(
                new GUIContent("Ghost Color"),
                profile.ghostColor,
                true,
                false,
                false);
            profile.ghostAlpha = EditorGUILayout.Slider("Ghost Alpha", profile.ghostAlpha, 0f, 1f);
            profile.finishedColor = EditorGUILayout.ColorField(
                new GUIContent("Finished Color"),
                profile.finishedColor,
                true,
                false,
                false);
            profile.glassColor = EditorGUILayout.ColorField(
                new GUIContent("Glass Color"),
                profile.glassColor,
                true,
                false,
                false);
            profile.glassAlpha = EditorGUILayout.Slider("Glass Alpha", profile.glassAlpha, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Snap", EditorStyles.boldLabel);
            profile.yawStepDegrees = EditorGUILayout.FloatField("Yaw Degrees / Notch", profile.yawStepDegrees);
            profile.heightStepMeters = EditorGUILayout.FloatField("Height Step (m)", profile.heightStepMeters);
            profile.maxHeightOffsetMeters = EditorGUILayout.FloatField("Max Height Offset (m)", profile.maxHeightOffsetMeters);
            profile.largeModuleMeters = EditorGUILayout.FloatField("Large Module (m)", profile.largeModuleMeters);
            profile.smallModuleMeters = EditorGUILayout.FloatField("Small Module (m)", profile.smallModuleMeters);
            profile.edgeSnapRangeMeters = EditorGUILayout.FloatField("Edge Snap Range (m)", profile.edgeSnapRangeMeters);
            profile.topSnapRangeMeters = EditorGUILayout.FloatField("Top Snap Range (m)", profile.topSnapRangeMeters);
            profile.buildLookUpDegrees = EditorGUILayout.Slider("Build Look Up (deg)", profile.buildLookUpDegrees, 0f, 75f);
            profile.doorFrameRangeMeters = EditorGUILayout.FloatField("Door Frame Range (m)", profile.doorFrameRangeMeters);
            profile.edgeFacingDot = EditorGUILayout.Slider("Edge Facing Dot", profile.edgeFacingDot, 0f, 1f);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Placement", EditorStyles.boldLabel);
            profile.buildSeconds = EditorGUILayout.FloatField("Hold To Build (s)", profile.buildSeconds);
            profile.destroyHoldSeconds = EditorGUILayout.FloatField("Hold To Destroy (s)", profile.destroyHoldSeconds);
            profile.aimDistanceMeters = EditorGUILayout.FloatField("Aim Distance (m)", profile.aimDistanceMeters);
            profile.doorSeatDropMeters = EditorGUILayout.FloatField("Door Seat Drop (m)", profile.doorSeatDropMeters);
            profile.overlapPaddingMeters = EditorGUILayout.FloatField("Overlap Padding (m)", profile.overlapPaddingMeters);

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Materials", EditorStyles.boldLabel);
            DMBuildingMaterialLibrary materialLibrary = DMBuildingMaterialLibrary.Live;
            if (materialLibrary == null || materialLibrary.variants == null || materialLibrary.variants.Count == 0)
                EditorGUILayout.HelpBox("No material variants yet. Create the two Stone finishes, then drag other materials onto the entries.", MessageType.None);
            else
            {
                for (int i = 0; i < materialLibrary.variants.Count; i++)
                {
                    DMBuildingMaterialVariant variant = materialLibrary.variants[i];
                    if (variant == null)
                        continue;
                    EditorGUILayout.LabelField(variant.displayName + " (" + variant.tier + ")");
                    variant.finishedMaterial = (Material)EditorGUILayout.ObjectField(
                        "Finished Material",
                        variant.finishedMaterial,
                        typeof(Material),
                        false);
                }
            }

            if (GUILayout.Button("Create Stone Finishes"))
                DMBuildingMaterialLibraryBuilder.EnsureStoneFinishes();

            if (EditorGUI.EndChangeCheck())
            {
                profile.ghostAlpha = Mathf.Clamp01(profile.ghostAlpha);
                profile.glassAlpha = Mathf.Clamp01(profile.glassAlpha);
                profile.edgeFacingDot = Mathf.Clamp01(profile.edgeFacingDot);
                EditorUtility.SetDirty(profile);
                if (materialLibrary != null)
                    EditorUtility.SetDirty(materialLibrary);
                AssetDatabase.SaveAssets();
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
