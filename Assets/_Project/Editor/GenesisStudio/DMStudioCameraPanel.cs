#if UNITY_EDITOR
using Project.Player;
using Project.Player.Invector;
using Project.UI;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Player → Camera: shared DM_CameraProfile plus live prefab camera components.
    /// </summary>
    internal sealed class DMStudioCameraPanel
    {
        private const string ProfilePath = "Assets/_Project/Resources/Player/DM_CameraProfile.asset";
        private const string VariantPath = "Assets/_Project/Prefabs/Players/Player_v7 Variant.prefab";

        private SerializedObject profileSerialized;
        private Object profileAsset;
        private SerializedObject pioneerSerialized;
        private Object pioneerTarget;
        private SerializedObject playerSerialized;
        private Object playerTarget;

        public void Draw()
        {
            EditorGUILayout.HelpBox(
                "Scroll zoom min must stay at or above Collision Min Follow — otherwise max zoom-in fights wall push and world stems swim.",
                MessageType.Info);
            EditorGUILayout.Space(6f);

            DrawProfileBlock();
            EditorGUILayout.Space(14f);
            DrawAccent();
            EditorGUILayout.Space(10f);
            DrawPioneerBlock();
            EditorGUILayout.Space(14f);
            DrawAccent();
            EditorGUILayout.Space(10f);
            DrawPlayerControllerBlock();
            EditorGUILayout.Space(14f);
            DrawAccent();
            EditorGUILayout.Space(10f);
            DrawShakeLinks();
        }

        private void DrawProfileBlock()
        {
            EnsureProfileAsset();
            Object asset = AssetDatabase.LoadAssetAtPath<Object>(ProfilePath);
            if (asset == null)
            {
                EditorGUILayout.HelpBox("Missing camera profile:\n" + ProfilePath, MessageType.Error);
                if (GUILayout.Button("Create DM_CameraProfile", GUILayout.Height(28f)))
                {
                    EnsureProfileAsset();
                    asset = AssetDatabase.LoadAssetAtPath<Object>(ProfilePath);
                }

                if (asset == null)
                    return;
            }

            DrawBlockHeader(
                "Shared Camera Profile",
                "Mouse zoom, aim/sprint pull, collision push, near clip — used by Pioneer + DMCameraCollisionOverlay.",
                asset);

            Bind(ref profileAsset, ref profileSerialized, asset);
            profileSerialized.Update();
            DrawAllVisible(profileSerialized);
            if (profileSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(asset);

            DMCameraProfile profile = asset as DMCameraProfile;
            if (profile != null && profile.minDistance + 0.01f < profile.minFollow)
            {
                EditorGUILayout.HelpBox(
                    $"Zoom min ({profile.minDistance:0.##}m) is below collision min follow ({profile.minFollow:0.##}m). Raise zoom min or lower min follow.",
                    MessageType.Warning);
            }
        }

        private void DrawPioneerBlock()
        {
            PioneerShooterMeleeInput pioneer = LoadPioneer();
            if (pioneer == null)
            {
                EditorGUILayout.HelpBox(
                    "PioneerShooterMeleeInput not found on Player_v7 Variant.",
                    MessageType.Warning);
                return;
            }

            DrawBlockHeader(
                "Pioneer Input (prefab)",
                "Optional profile override reference, Meshy aim snap, and any leftover local zoom fallbacks.",
                pioneer);

            Bind(ref pioneerTarget, ref pioneerSerialized, pioneer);
            pioneerSerialized.Update();
            DrawFilteredProperties(
                pioneerSerialized,
                "cameraProfile",
                "runtimeMinCameraDistance",
                "runtimeMaxCameraDistance",
                "runtimeDefaultCameraDistance",
                "aimZoomPullInMeters",
                "sprintZoomOutMeters",
                "aimMinCameraDistance",
                "meshySnapAim",
                "meshySnapAimRequiresVisual",
                "invertCameraInputHorizontal",
                "invertCameraInputVertical");
            if (pioneerSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(pioneer);
        }

        private void DrawPlayerControllerBlock()
        {
            PlayerController player = LoadPlayerController();
            if (player == null)
            {
                EditorGUILayout.HelpBox(
                    "PlayerController not found on Player_v7 Variant.",
                    MessageType.Warning);
                return;
            }

            DrawBlockHeader(
                "PlayerController (prefab)",
                "Legacy ECM2 follow distances, camera collision, optics eye, and ranged-aim follow.",
                player);

            Bind(ref playerTarget, ref playerSerialized, player);
            playerSerialized.Update();
            DrawFilteredProperties(
                playerSerialized,
                "cameraFollowTarget",
                "followDistance",
                "followMinDistance",
                "followMaxDistance",
                "cameraCollisionMask",
                "cameraCollisionRadius",
                "cameraCollisionPadding",
                "opticsZoomLerpSpeed",
                "opticsEyeOffset",
                "opticsFallbackHeadHeight",
                "opticsBodyTurnSpeed",
                "aimFollowDistance",
                "aimCameraLerpSpeed");
            if (playerSerialized.ApplyModifiedProperties())
                EditorUtility.SetDirty(player);
        }

        private static void DrawShakeLinks()
        {
            EditorGUILayout.LabelField("Camera Shake", DMStudioStyles.SectionTitle);
            EditorGUILayout.LabelField(
                "Impulse / continuous shake emitters live under Prefabs/Environment/CameraShake.",
                DMStudioStyles.HeroSubtitle);
            EditorGUILayout.Space(4f);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Ping Shake Folder", GUILayout.Height(26f)))
            {
                Object folder = AssetDatabase.LoadAssetAtPath<Object>(
                    "Assets/_Project/Prefabs/Environment/CameraShake");
                if (folder != null)
                    EditorGUIUtility.PingObject(folder);
            }

            if (GUILayout.Button("Open Shake Emitter Tool", GUILayout.Height(26f)))
                EditorApplication.ExecuteMenuItem("Tools/Dark Matter Genesis/Prefab Creator/Camera Shake Emitter Creator");
            EditorGUILayout.EndHorizontal();
        }

        private static void EnsureProfileAsset()
        {
            if (AssetDatabase.LoadAssetAtPath<DMCameraProfile>(ProfilePath) != null)
                return;

            string folder = "Assets/_Project/Resources/Player";
            if (!AssetDatabase.IsValidFolder(folder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Player");
            }

            DMCameraProfile created = ScriptableObject.CreateInstance<DMCameraProfile>();
            AssetDatabase.CreateAsset(created, ProfilePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static PioneerShooterMeleeInput LoadPioneer()
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            if (variant == null)
                return Object.FindAnyObjectByType<PioneerShooterMeleeInput>();
            return variant.GetComponentInChildren<PioneerShooterMeleeInput>(true);
        }

        private static PlayerController LoadPlayerController()
        {
            GameObject variant = AssetDatabase.LoadAssetAtPath<GameObject>(VariantPath);
            if (variant == null)
                return Object.FindAnyObjectByType<PlayerController>();
            return variant.GetComponentInChildren<PlayerController>(true);
        }

        private static void DrawBlockHeader(string title, string subtitle, Object asset)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();
            EditorGUILayout.LabelField(title, DMStudioStyles.SectionTitle);
            EditorGUILayout.LabelField(subtitle, DMStudioStyles.HeroSubtitle);
            EditorGUILayout.EndVertical();
            if (GUILayout.Button("Ping", GUILayout.Width(52f), GUILayout.Height(22f)))
                EditorGUIUtility.PingObject(asset);
            if (GUILayout.Button("Select", GUILayout.Width(56f), GUILayout.Height(22f)))
                Selection.activeObject = asset;
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4f);
        }

        private static void DrawAccent()
        {
            DMStudioStyles.DrawAccentLine(
                EditorGUILayout.GetControlRect(false, 2f),
                DarkMatterGenesisUiPalette.SlateGray);
        }

        private static void Bind(ref Object cached, ref SerializedObject serialized, Object asset)
        {
            if (cached == asset && serialized != null)
                return;
            cached = asset;
            serialized = new SerializedObject(asset);
        }

        private static void DrawAllVisible(SerializedObject serialized)
        {
            SerializedProperty iterator = serialized.GetIterator();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.name == "m_Script")
                    continue;
                EditorGUILayout.PropertyField(iterator, true);
            }
        }

        private static void DrawFilteredProperties(SerializedObject serialized, params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                SerializedProperty prop = serialized.FindProperty(names[i]);
                if (prop != null)
                    EditorGUILayout.PropertyField(prop, true);
            }
        }
    }
}
#endif
