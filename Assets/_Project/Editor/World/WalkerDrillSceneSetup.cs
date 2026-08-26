using System;
using System.IO;
using Project.Data;
using Project.EditorTools;
using Project.Interaction;
using Project.World;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.World
{
    /// <summary>
    /// Builds Walker Drill animator controller from FBX clips and wires scene/prefab components.
    /// </summary>
    public static class WalkerDrillAnimatorFactory
    {
        public const string ControllerFolder = "Assets/_Project/Models/Drill Mech";
        public const string ControllerPath = ControllerFolder + "/WalkerDrill.controller";
        public const string MoveFbxPath = ControllerFolder + "/Walker DrillDrill Move.fbx";
        public const string SpinFbxPath = ControllerFolder + "/Walker DrillDrill Spin.fbx";

        public static RuntimeAnimatorController BuildOrUpdateController(out string message)
        {
            AnimationClip moveClip = FindPrimaryClip(MoveFbxPath);
            AnimationClip spinClip = FindPrimaryClip(SpinFbxPath);

            if (moveClip == null && spinClip == null)
            {
                message = $"No animation clips found. Import FBX at:\n  {MoveFbxPath}\n  {SpinFbxPath}";
                return null;
            }

            if (spinClip != null)
                SetClipLoop(spinClip, loop: true);

            EnsureFolder(ControllerFolder);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);

            if (controller.layers == null || controller.layers.Length == 0)
            {
                message = "AnimatorController has no layers.";
                return null;
            }

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            ClearStates(stateMachine);

            AnimatorState idleState = stateMachine.AddState(DMWalkerDrillController.IdleState);
            idleState.writeDefaultValues = true;

            if (moveClip != null)
            {
                AnimatorState moveState = stateMachine.AddState(DMWalkerDrillController.MoveState);
                moveState.motion = moveClip;
            }

            if (spinClip != null)
            {
                AnimatorState spinState = stateMachine.AddState(DMWalkerDrillController.SpinState);
                spinState.motion = spinClip;
            }

            stateMachine.defaultState = idleState;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            message = $"Walker Drill controller updated at {ControllerPath}";
            return controller;
        }

        private static AnimationClip FindPrimaryClip(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath) || !File.Exists(assetPath))
                return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            AnimationClip fallback = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not AnimationClip clip)
                    continue;

                if (clip.name.StartsWith("__", StringComparison.Ordinal))
                    continue;

                if (!clip.legacy)
                    return clip;

                fallback ??= clip;
            }

            return fallback;
        }

        private static void SetClipLoop(AnimationClip clip, bool loop)
        {
            if (clip == null)
                return;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static void ClearStates(AnimatorStateMachine stateMachine)
        {
            ChildAnimatorState[] children = stateMachine.states;
            for (int i = children.Length - 1; i >= 0; i--)
                stateMachine.RemoveState(children[i].state);

            for (int i = stateMachine.stateMachines.Length - 1; i >= 0; i--)
                stateMachine.RemoveStateMachine(stateMachine.stateMachines[i].stateMachine);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder(parent, leaf);
        }
    }

    public static class WalkerDrillSceneSetup
    {
        private const string WalkerDrillObjectName = "Walker Drill";
        private const string PrefabPath = "Assets/_Project/Prefabs/World/WalkerDrill.prefab";
        private const string ResourcesPrefabPath = "Assets/_Project/Resources/World/WalkerDrill.prefab";
        private const string MoveAudioPath = "Assets/_Project/Audio/World/WalkerDrill_Move.wav";
        private const string SpinAudioPath = "Assets/_Project/Audio/World/WalkerDrill_Spin.wav";
        private const string ItemPath = "Assets/_Project/Data/Items/World/Walker Drill.asset";
        private const string ItemRegistryPath = "Assets/_Project/Resources/ItemRegistry.asset";

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Setup Walker Drill In Scene")]
        [MenuItem("Dark Matter Genesis/World/Setup Walker Drill In Scene")]
        public static void SetupWalkerDrillInScene()
        {
            GameObject walkerDrill = FindOrCreateWalkerDrillRoot();
            if (walkerDrill == null)
            {
                Debug.LogError("[WalkerDrill] Could not create Walker Drill root.");
                return;
            }

            UnpackCompletelyIfPrefabInstance(walkerDrill);
            WireWalkerDrillComponents(walkerDrill);
            PersistCompletePrefabs(walkerDrill);

            Selection.activeGameObject = walkerDrill;
            Debug.Log($"[WalkerDrill] Setup complete on '{walkerDrill.name}'. Complete prefab written to {PrefabPath} and {ResourcesPrefabPath}. Play Mode: press E near the drill, or Deploy from inventory.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Build Walker Drill Animator Controller")]
        [MenuItem("Dark Matter Genesis/World/Build Walker Drill Animator Controller")]
        public static void BuildAnimatorControllerOnly()
        {
            WalkerDrillAnimatorFactory.BuildOrUpdateController(out string message);
            if (!string.IsNullOrEmpty(message))
                Debug.Log(message);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Save Walker Drill Prefab")]
        [MenuItem("Dark Matter Genesis/World/Save Walker Drill Prefab")]
        public static void SaveWalkerDrillPrefab()
        {
            GameObject walkerDrill = GameObject.Find(WalkerDrillObjectName);
            if (walkerDrill == null)
            {
                DMWalkerDrillController[] controllers = UnityEngine.Object.FindObjectsByType<DMWalkerDrillController>(FindObjectsInactive.Include);
                if (controllers != null && controllers.Length > 0 && controllers[0] != null)
                    walkerDrill = controllers[0].gameObject;
            }

            if (walkerDrill == null)
            {
                Debug.LogWarning($"[WalkerDrill] No scene object named '{WalkerDrillObjectName}'. Run setup first.");
                return;
            }

            UnpackCompletelyIfPrefabInstance(walkerDrill);
            WireWalkerDrillComponents(walkerDrill);
            PersistCompletePrefabs(walkerDrill);
        }

        private static void UnpackCompletelyIfPrefabInstance(GameObject walkerDrill)
        {
            if (walkerDrill == null || !PrefabUtility.IsPartOfPrefabInstance(walkerDrill))
                return;

            PrefabUtility.UnpackPrefabInstance(walkerDrill, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            Debug.Log($"[WalkerDrill] Unpacked prefab instance '{walkerDrill.name}' so Animator / controller / usable / audio can be saved on a complete prefab.");
        }

        private static void WireWalkerDrillComponents(GameObject walkerDrill)
        {
            RuntimeAnimatorController controller = WalkerDrillAnimatorFactory.BuildOrUpdateController(out string controllerMessage);
            if (!string.IsNullOrEmpty(controllerMessage))
                Debug.Log(controllerMessage);

            Animator animator = walkerDrill.GetComponent<Animator>();
            if (animator == null)
                animator = walkerDrill.AddComponent<Animator>();

            if (controller != null)
                animator.runtimeAnimatorController = controller;

            DMWalkerDrillController drillController = walkerDrill.GetComponent<DMWalkerDrillController>();
            if (drillController == null)
                drillController = walkerDrill.AddComponent<DMWalkerDrillController>();

            DMWalkerDrillUsable usable = walkerDrill.GetComponent<DMWalkerDrillUsable>();
            if (usable == null)
                usable = walkerDrill.AddComponent<DMWalkerDrillUsable>();

            usable.EnsureInteractionCollider();
            TryParentModelUnderRoot(walkerDrill);

            drillController.Configure(animator, 2f);
            drillController.EnsureAudioSource();

            AudioClip moveClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MoveAudioPath);
            AudioClip spinClip = AssetDatabase.LoadAssetAtPath<AudioClip>(SpinAudioPath);
            if (moveClip != null || spinClip != null)
                drillController.SetAudioClips(moveClip, spinClip);
            else
                Debug.LogWarning($"[WalkerDrill] Audio clips missing. Expected:\n  {MoveAudioPath}\n  {SpinAudioPath}");

            ItemData walkerDrillItem = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPath);
            if (walkerDrillItem != null)
            {
                usable.SetWalkerDrillItem(walkerDrillItem);
                EnsureItemInRegistry(walkerDrillItem);
            }
            else
            {
                Debug.LogWarning($"[WalkerDrill] ItemData missing at {ItemPath}");
            }

            EditorUtility.SetDirty(walkerDrill);
            EditorUtility.SetDirty(drillController);
            EditorUtility.SetDirty(usable);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        }

        private static void PersistCompletePrefabs(GameObject walkerDrill)
        {
            if (walkerDrill == null)
                return;

            EnsureAssetFolder("Assets/_Project/Prefabs/World");
            EnsureAssetFolder("Assets/_Project/Resources/World");

            Vector3 scenePosition = walkerDrill.transform.position;
            Quaternion sceneRotation = walkerDrill.transform.rotation;
            walkerDrill.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(walkerDrill, PrefabPath);
            GameObject resourcesPrefab = PrefabUtility.SaveAsPrefabAsset(walkerDrill, ResourcesPrefabPath);

            walkerDrill.transform.SetPositionAndRotation(scenePosition, sceneRotation);

            ItemData walkerDrillItem = AssetDatabase.LoadAssetAtPath<ItemData>(ItemPath);
            if (walkerDrillItem != null)
                AssignItemPrefabs(walkerDrillItem);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
                Debug.Log($"[WalkerDrill] Complete gameplay prefab saved to {PrefabPath}. Walker Drill.asset deployedPrefab/worldPrefab now point at this prefab's actual root GameObject.");
            else
                Debug.LogWarning($"[WalkerDrill] SaveAsPrefabAsset failed for {PrefabPath}");

            if (resourcesPrefab != null)
                Debug.Log($"[WalkerDrill] Resources fallback prefab saved to {ResourcesPrefabPath} (Resources.Load(\"World/WalkerDrill\")).");
        }

        private static void EnsureAssetFolder(string folderPath)
        {
            folderPath = folderPath.Replace('\\', '/');
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string parent = Path.GetDirectoryName(folderPath)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folderPath);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(leaf))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureAssetFolder(parent);

            if (!AssetDatabase.IsValidFolder(folderPath))
                AssetDatabase.CreateFolder(parent, leaf);
        }

        private static GameObject FindOrCreateWalkerDrillRoot()
        {
            GameObject existing = GameObject.Find(WalkerDrillObjectName);
            if (existing != null)
                return existing;

            DMWalkerDrillController[] controllers = UnityEngine.Object.FindObjectsByType<DMWalkerDrillController>(FindObjectsInactive.Include);
            if (controllers != null && controllers.Length > 0 && controllers[0] != null)
                return controllers[0].gameObject;

            GameObject selected = Selection.activeGameObject;
            if (selected != null)
            {
                string selectedName = selected.name;
                if (selectedName.IndexOf("Walker", StringComparison.OrdinalIgnoreCase) >= 0
                    && selectedName.IndexOf("Drill", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return selected;
                }
            }

            Debug.LogError("[WalkerDrill] No scene object named 'Walker Drill'. Select the existing drill or place Assets/_Project/Prefabs/World/WalkerDrill.prefab in the scene, then run this menu again. (Will not spawn a duplicate at a guessed position.)");
            return null;
        }

        private static void AssignItemPrefabs(ItemData item)
        {
            if (item == null)
                return;

            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
                return;

            SerializedObject serialized = new SerializedObject(item);
            bool changed = false;
            SerializedProperty deployed = serialized.FindProperty("deployedPrefab");
            if (deployed != null)
            {
                deployed.objectReferenceValue = prefab;
                changed = true;
            }

            SerializedProperty world = serialized.FindProperty("worldPrefab");
            if (world != null)
            {
                world.objectReferenceValue = prefab;
                changed = true;
            }

            if (!changed)
                return;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(item);
        }

        private static void EnsureItemInRegistry(ItemData item)
        {
            if (item == null)
                return;

            ItemRegistry registry = AssetDatabase.LoadAssetAtPath<ItemRegistry>(ItemRegistryPath);
            if (registry == null)
            {
                Debug.LogWarning($"[WalkerDrill] ItemRegistry not found at {ItemRegistryPath}");
                return;
            }

            SerializedObject serialized = new SerializedObject(registry);
            SerializedProperty items = serialized.FindProperty("items");
            if (items == null || !items.isArray)
                return;

            for (int i = 0; i < items.arraySize; i++)
            {
                if (items.GetArrayElementAtIndex(i).objectReferenceValue == item)
                    return;
            }

            int index = items.arraySize;
            items.arraySize = index + 1;
            items.GetArrayElementAtIndex(index).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(registry);
            Debug.Log("[WalkerDrill] Added Walker Drill to ItemRegistry.");
        }

        private static void TryParentModelUnderRoot(GameObject root)
        {
            if (root.transform.childCount > 0)
                return;

            GameObject moveModel = AssetDatabase.LoadAssetAtPath<GameObject>(WalkerDrillAnimatorFactory.MoveFbxPath);
            if (moveModel == null)
                return;

            GameObject visual = (GameObject)PrefabUtility.InstantiatePrefab(moveModel);
            if (visual == null)
                visual = UnityEngine.Object.Instantiate(moveModel);

            visual.name = "Visual";
            visual.transform.SetParent(root.transform, false);
        }
    }
}
