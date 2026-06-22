#if UNITY_EDITOR
using Project.EditorTools;
using UnityEditor;
using UnityEngine;

namespace Project.Audio.Editor
{
    [CustomEditor(typeof(AmbientAudioZone))]
    public class AmbientAudioZoneEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            DrawDefaultInspector();

            AmbientAudioZone zone = (AmbientAudioZone)target;

            EditorGUILayout.Space(12f);
            EditorGUILayout.LabelField("Layer Presets", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Add Birds"))
                    AddLayer(zone, AmbientZoneLayer.CreateBirdsLayer());
                if (GUILayout.Button("Add Insects"))
                    AddLayer(zone, AmbientZoneLayer.CreateInsectsLayer());
                if (GUILayout.Button("Add Creaking Trees"))
                    AddLayer(zone, AmbientZoneLayer.CreateTreesLayer());
            }

            if (GUILayout.Button("Add All Forest Layers"))
            {
                AddLayer(zone, AmbientZoneLayer.CreateBirdsLayer());
                AddLayer(zone, AmbientZoneLayer.CreateInsectsLayer());
                AddLayer(zone, AmbientZoneLayer.CreateTreesLayer());
            }

            serializedObject.ApplyModifiedProperties();
        }

        private static void AddLayer(AmbientAudioZone zone, AmbientZoneLayer layer)
        {
            AmbientAudioZoneEditorHelpers.AddLayer(zone, layer);
        }
    }

    internal static class AmbientAudioZoneEditorHelpers
    {
        public static void CopyLayer(SerializedProperty property, AmbientZoneLayer layer)
        {
            property.FindPropertyRelative("layerName").stringValue = layer.layerName;
            property.FindPropertyRelative("minInterval").floatValue = layer.minInterval;
            property.FindPropertyRelative("maxInterval").floatValue = layer.maxInterval;
            property.FindPropertyRelative("volume").floatValue = layer.volume;
            property.FindPropertyRelative("spatialBlend").floatValue = layer.spatialBlend;
            property.FindPropertyRelative("pitchMin").floatValue = layer.pitchMin;
            property.FindPropertyRelative("pitchMax").floatValue = layer.pitchMax;
            property.FindPropertyRelative("playAtRandomPointInZone").boolValue = layer.playAtRandomPointInZone;
        }

        public static void AddLayer(AmbientAudioZone zone, AmbientZoneLayer layer)
        {
            SerializedObject serializedZone = new SerializedObject(zone);
            SerializedProperty layersProperty = serializedZone.FindProperty("layers");
            layersProperty.arraySize++;
            SerializedProperty newLayer = layersProperty.GetArrayElementAtIndex(layersProperty.arraySize - 1);
            CopyLayer(newLayer, layer);
            serializedZone.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(zone);
        }
    }

    public static class GameAudioMenuItems
    {
        public const string ResourcesProfilePath = "Assets/_Project/Resources/GameAudioProfile.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Audio + "Create Game Audio Profile")]
        public static void CreateGameAudioProfile()
        {
            GameAudioProfile asset = ScriptableObject.CreateInstance<GameAudioProfile>();
            System.IO.Directory.CreateDirectory("Assets/_Project/Audio");
            string path = AssetDatabase.GenerateUniqueAssetPath("Assets/_Project/Audio/GameAudioProfile.asset");
            AssetDatabase.CreateAsset(asset, path);
            AssetDatabase.SaveAssets();
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Audio + "Open Game Audio Profile")]
        public static void OpenGameAudioProfile()
        {
            GameAudioProfile profile = GameAudioProfileAssetSetup.EnsureResourcesProfile();
            if (profile != null)
            {
                Selection.activeObject = profile;
                EditorGUIUtility.PingObject(profile);
            }
        }

        [MenuItem(SurvivalPioneerEditorMenus.Audio + "Create Ambient Audio Zone")]
        public static void CreateAmbientAudioZone()
        {
            GameObject zoneObject = new GameObject("AmbientAudioZone");
            BoxCollider collider = zoneObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(20f, 8f, 20f);
            AmbientAudioZone zone = zoneObject.AddComponent<AmbientAudioZone>();

            SerializedObject serializedZone = new SerializedObject(zone);
            SerializedProperty layersProperty = serializedZone.FindProperty("layers");
            AmbientZoneLayer[] presetLayers =
            {
                AmbientZoneLayer.CreateBirdsLayer(),
                AmbientZoneLayer.CreateInsectsLayer(),
                AmbientZoneLayer.CreateTreesLayer()
            };

            layersProperty.arraySize = presetLayers.Length;
            for (int i = 0; i < presetLayers.Length; i++)
                AmbientAudioZoneEditorHelpers.CopyLayer(layersProperty.GetArrayElementAtIndex(i), presetLayers[i]);

            serializedZone.ApplyModifiedPropertiesWithoutUndo();
            Selection.activeGameObject = zoneObject;
            Undo.RegisterCreatedObjectUndo(zoneObject, "Create Ambient Audio Zone");
        }
    }
}
#endif
