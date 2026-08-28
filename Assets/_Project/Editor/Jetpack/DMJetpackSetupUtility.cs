using System.Text;
using Project.Features.Jetpack;
using QFX.SFX;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Jetpack
{
    public static class DMJetpackSetupUtility
    {
        private const string PlayerV7PrefabPath = ProjectAssetPaths.PrefabsPlayers + "/Player_v7.prefab";
        private const string DefaultProfilePath = DMJetpackProfilePresets.SmoothPath;
        private const string JetpackModelPath = "Assets/_Project/Models/DM_Jetpack/DM_Jetpack.fbx";
        private const string EnginePrefabPath = "Assets/QFX/Sci-Fi VFX/Prefabs/Engine/Engine.prefab";
        private const string EngineInnerTemplatePath =
            "Assets/QFX/Sci-Fi VFX/Resources/Materials/Engine/GO_EngineInner 1.mat";
        private const string JetpackEngineInnerPath =
            "Assets/_Project/Models/DM_Jetpack/Materials/DM_Jetpack_EngineInner.mat";

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Setup Selected Player For Jetpack")]
        public static void SetupSelectedPlayer()
        {
            GameObject selected = Selection.activeGameObject;
            if (selected == null)
            {
                EditorUtility.DisplayDialog(
                    "Jetpack Setup",
                    "Select Player_v7 (root) in the Hierarchy or Project.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog("Jetpack Setup", WirePlayerRoot(selected), "OK");
        }

        [MenuItem("Tools/Dark Matter Genesis/Jetpack/Wire Player_v7 Prefab")]
        public static void WirePlayerV7PrefabMenu()
        {
            EditorUtility.DisplayDialog("Jetpack Prefab Wire", WirePlayerPrefabAtPath(PlayerV7PrefabPath), "OK");
        }

        public static string WirePlayerPrefabAtPath(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab"))
                return "Invalid prefab path.";

            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            if (root == null)
                return "Could not load prefab: " + prefabPath;

            try
            {
                string result = WirePlayerRoot(root);
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
                AssetDatabase.SaveAssets();
                return result + "\n\nSaved: " + prefabPath;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static string WirePlayerRoot(GameObject root)
        {
            var log = new StringBuilder();

            DMJetpackProfile profile = EnsureDefaultProfile();
            RuntimeAnimatorController animatorController =
                PlayerJetpackAnimatorSetup.BuildOrUpdateController(out string animatorMessage);
            Material engineInner = EnsureJetpackEngineInnerMaterial();

            log.AppendLine(animatorMessage);

            AddIfMissing<DMJetpackController>(root);
            AddIfMissing<DMJetpackInputBridge>(root);
            AddIfMissing<DMJetpackAnimatorDriver>(root);

            DMJetpackController jetpackController = root.GetComponent<DMJetpackController>();
            SerializedObject jetpackSo = new SerializedObject(jetpackController);
            jetpackSo.FindProperty("profile").objectReferenceValue = profile;
            jetpackSo.ApplyModifiedPropertiesWithoutUndo();

            Animator animator = root.GetComponent<Animator>();
            if (animator == null)
                animator = root.GetComponentInChildren<Animator>(true);

            if (animator != null && animatorController != null)
            {
                animator.runtimeAnimatorController = animatorController;
                animator.enabled = true;
                EditorUtility.SetDirty(animator);
                log.AppendLine("Assigned jetpack animator controller.");
            }
            else
            {
                log.AppendLine("Warning: Could not assign jetpack animator controller.");
            }

            Transform spine2 = FindChildRecursive(root.transform, "Spine2")
                               ?? FindChildRecursive(root.transform, "Spine02");
            if (spine2 == null)
            {
                log.AppendLine("Warning: Spine2 / Spine02 bone not found — jetpack model not attached.");
                return log.ToString();
            }

            Transform jetpackRoot = EnsureJetpackModel(spine2);
            EnsureEngineChild(jetpackRoot, "Engine_L", engineInner, new Vector3(-0.00446f, 0.00229f, -0.00875f));
            EnsureEngineChild(jetpackRoot, "Engine_R", engineInner, new Vector3(0.00454f, 0.00229f, -0.00875f));

            DMJetpackThrusterVfx thrusterVfx = jetpackRoot.GetComponent<DMJetpackThrusterVfx>();
            if (thrusterVfx == null)
                thrusterVfx = jetpackRoot.gameObject.AddComponent<DMJetpackThrusterVfx>();

            SFX_EngineController[] engines = jetpackRoot.GetComponentsInChildren<SFX_EngineController>(true);
            SerializedObject thrusterSo = new SerializedObject(thrusterVfx);
            thrusterSo.FindProperty("jetpack").objectReferenceValue = jetpackController;
            thrusterSo.FindProperty("engineControllers").arraySize = engines.Length;
            for (int i = 0; i < engines.Length; i++)
                thrusterSo.FindProperty("engineControllers").GetArrayElementAtIndex(i).objectReferenceValue = engines[i];
            thrusterSo.ApplyModifiedPropertiesWithoutUndo();

            DMJetpackAnimatorDriver animatorDriver = root.GetComponent<DMJetpackAnimatorDriver>();
            SerializedObject driverSo = new SerializedObject(animatorDriver);
            driverSo.FindProperty("jetpack").objectReferenceValue = jetpackController;
            driverSo.FindProperty("profile").objectReferenceValue = profile;
            driverSo.FindProperty("animator").objectReferenceValue = animator;
            driverSo.ApplyModifiedPropertiesWithoutUndo();

            EditorUtility.SetDirty(root);
            log.AppendLine("Jetpack hierarchy + components wired on '" + root.name + "'.");
            return log.ToString();
        }

        private static Transform EnsureJetpackModel(Transform spine2)
        {
            Transform existing = spine2.Find("DM_Jetpack");
            if (existing != null)
                return existing;

            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(JetpackModelPath);
            if (source == null)
                throw new System.InvalidOperationException("Missing jetpack FBX at " + JetpackModelPath);

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source, spine2);
            instance.name = "DM_Jetpack";
            Transform transform = instance.transform;
            transform.localPosition = new Vector3(-0.0059992317f, -0.03186857f, -0.18263593f);
            transform.localRotation = new Quaternion(0.68828803f, 0.000005259877f, 0.000005127862f, -0.7254376f);
            transform.localScale = new Vector3(19.196104f, 19.196096f, 19.196096f);

            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>(true);
            Material bodyMat = AssetDatabase.LoadAssetAtPath<Material>(
                "Assets/_Project/Models/DM_Jetpack/Materials/DM_Jetpack_texture.mat");
            if (bodyMat != null)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer.gameObject.name.Contains("Engine"))
                        continue;

                    renderer.sharedMaterial = bodyMat;
                }
            }

            return transform;
        }

        private static void EnsureEngineChild(
            Transform jetpackRoot,
            string engineName,
            Material engineInnerMaterial,
            Vector3 localPosition)
        {
            Transform existing = jetpackRoot.Find(engineName);
            if (existing != null)
            {
                ConfigureEngine(existing.gameObject, engineInnerMaterial, localPosition);
                return;
            }

            GameObject enginePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnginePrefabPath);
            if (enginePrefab == null)
                throw new System.InvalidOperationException("Missing QFX Engine prefab at " + EnginePrefabPath);

            GameObject engine = (GameObject)PrefabUtility.InstantiatePrefab(enginePrefab, jetpackRoot);
            engine.name = engineName;
            ConfigureEngine(engine, engineInnerMaterial, localPosition);
        }

        private static void ConfigureEngine(GameObject engine, Material engineInnerMaterial, Vector3 localPosition)
        {
            Transform transform = engine.transform;
            transform.localPosition = localPosition;
            transform.localScale = new Vector3(0.005613854f, 0.0048227515f, 0.0026419356f);
            transform.localRotation = Quaternion.Euler(-97.006f, -160.856f, -18.60199f);

            SetActiveByName(engine.transform, "PS_Sparks Fast", false);
            SetActiveByName(engine.transform, "PS_Sparks Slow", false);
            SetActiveByName(engine.transform, "PS_Small Flare Distortion", false);

            Transform inner = FindChildRecursive(engine.transform, "SM_Jets_Inner");
            if (inner != null && engineInnerMaterial != null)
            {
                Renderer renderer = inner.GetComponent<Renderer>();
                if (renderer != null)
                    renderer.sharedMaterial = engineInnerMaterial;
            }

            SFX_EngineController controller = engine.GetComponent<SFX_EngineController>();
            if (controller != null && inner != null)
            {
                SerializedObject so = new SerializedObject(controller);
                so.FindProperty("EngineInner").objectReferenceValue = inner.gameObject;
                so.ApplyModifiedPropertiesWithoutUndo();
                controller.InitializeJetpackDrive();
            }
        }

        private static Material EnsureJetpackEngineInnerMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(JetpackEngineInnerPath);
            if (existing != null)
                return existing;

            Material template = AssetDatabase.LoadAssetAtPath<Material>(EngineInnerTemplatePath);
            if (template == null)
                return null;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Models/DM_Jetpack/Materials"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Models/DM_Jetpack"))
                    AssetDatabase.CreateFolder("Assets/_Project/Models", "DM_Jetpack");
                AssetDatabase.CreateFolder("Assets/_Project/Models/DM_Jetpack", "Materials");
            }

            Material jetpackInner = new Material(template)
            {
                name = "DM_Jetpack_EngineInner",
            };

            if (jetpackInner.HasProperty("_TintColor"))
            {
                jetpackInner.SetColor(
                    "_TintColor",
                    new Color(48f, 10f, 32f, 0.76f));
            }

            if (jetpackInner.HasProperty("_EmissionColor"))
            {
                jetpackInner.SetColor(
                    "_EmissionColor",
                    new Color(0.85f, 0.18f, 0.48f, 1f));
            }

            AssetDatabase.CreateAsset(jetpackInner, JetpackEngineInnerPath);
            AssetDatabase.SaveAssets();
            return jetpackInner;
        }

        private static T AddIfMissing<T>(GameObject go) where T : Component
        {
            T component = go.GetComponent<T>();
            return component != null ? component : go.AddComponent<T>();
        }

        private static DMJetpackProfile EnsureDefaultProfile()
        {
            DMJetpackProfile profile = AssetDatabase.LoadAssetAtPath<DMJetpackProfile>(DefaultProfilePath);
            if (profile != null)
                return profile;

            EnsureJetpackDataFolder();
            profile = ScriptableObject.CreateInstance<DMJetpackProfile>();
            AssetDatabase.CreateAsset(profile, DefaultProfilePath);
            AssetDatabase.SaveAssets();
            return profile;
        }

        private static void EnsureJetpackDataFolder()
        {
            if (!AssetDatabase.IsValidFolder(ProjectAssetPaths.Features))
                AssetDatabase.CreateFolder(ProjectAssetPaths.Root, "Features");

            if (!AssetDatabase.IsValidFolder(ProjectAssetPaths.Features + "/Jetpack"))
                AssetDatabase.CreateFolder(ProjectAssetPaths.Features, "Jetpack");

            if (!AssetDatabase.IsValidFolder(ProjectAssetPaths.Features + "/Jetpack/Data"))
                AssetDatabase.CreateFolder(ProjectAssetPaths.Features + "/Jetpack", "Data");
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            if (parent.name == childName)
                return parent;

            for (int i = 0; i < parent.childCount; i++)
            {
                Transform found = FindChildRecursive(parent.GetChild(i), childName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static void SetActiveByName(Transform root, string objectName, bool active)
        {
            Transform target = FindChildRecursive(root, objectName);
            if (target != null)
                target.gameObject.SetActive(active);
        }
    }
}
