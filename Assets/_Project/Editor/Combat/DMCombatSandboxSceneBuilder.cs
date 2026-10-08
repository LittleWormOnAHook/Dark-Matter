#if UNITY_EDITOR
using Project.Combat;
using Project.EditorTools;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools.Combat
{
    public static class DMCombatSandboxSceneBuilder
    {
        private const string ScenePath = "Assets/_Project/Scenes/Combat/Combat_Sandbox.unity";
        private const string ProfilePath = "Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset";
        private const string DummyPath = "Assets/_Project/Prefabs/Combat/Enemies/TrainingDummy.prefab";
        private const string HumanoidPath = "Assets/_Project/Prefabs/Combat/Enemies/HumanoidEnemy_Invector.prefab";
        private const string PlayerVariantPath = "Assets/_Project/Prefabs/Players/Player_v7 Variant.prefab";

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Create Combat Sandbox Scene", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Create_Combat_Sandbox_Scene)]
        public static void CreateCombatSandboxScene()
        {
            EnsureCombatCoreProfile();

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Scenes/Combat"))
                AssetDatabase.CreateFolder("Assets/_Project/Scenes", "Combat");

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerVariantPath);
            GameObject dummyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(DummyPath);
            GameObject humanoidPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(HumanoidPath);

            GameObject player = playerPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab)
                : new GameObject("Player_v7 Variant");

            player.transform.position = new Vector3(0f, 0f, 0f);

            GameObject dummy = dummyPrefab != null
                ? (GameObject)PrefabUtility.InstantiatePrefab(dummyPrefab)
                : new GameObject("TrainingDummy");
            dummy.transform.position = new Vector3(0f, 0f, 4f);
            if (dummy.GetComponent<CombatPoise>() == null)
                dummy.AddComponent<CombatPoise>();

            GameObject spawnRoot = new GameObject("CombatSandbox");
            GameObject spawnPoint = new GameObject("EnemySpawnPoint");
            spawnPoint.transform.SetParent(spawnRoot.transform);
            spawnPoint.transform.position = new Vector3(8f, 0f, 4f);

            DMCombatSandboxSpawner spawner = spawnRoot.AddComponent<DMCombatSandboxSpawner>();
            SerializedObject so = new SerializedObject(spawner);
            so.FindProperty("humanoidPrefab").objectReferenceValue = humanoidPrefab;
            so.FindProperty("spawnPoint").objectReferenceValue = spawnPoint.transform;
            so.FindProperty("spawnIntervalSeconds").floatValue = 12f;
            so.FindProperty("maxAlive").intValue = 1;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log($"Combat sandbox saved to {ScenePath}. Assign terrain/lighting as needed.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "Ensure Combat Core Profile", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Ensure_Combat_Core_Profile)]
        public static void EnsureCombatCoreProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Combat"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Combat");
            }

            DM_CombatCoreProfile existing = AssetDatabase.LoadAssetAtPath<DM_CombatCoreProfile>(ProfilePath);
            if (existing != null)
                return;

            DM_CombatCoreProfile profile = ScriptableObject.CreateInstance<DM_CombatCoreProfile>();
            AssetDatabase.CreateAsset(profile, ProfilePath);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
