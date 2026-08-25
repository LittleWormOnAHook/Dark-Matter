using System;
using System.IO;
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

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Setup Walker Drill In Scene")]
        public static void SetupWalkerDrillInScene()
        {
            GameObject walkerDrill = FindOrCreateWalkerDrillRoot();
            if (walkerDrill == null)
            {
                Debug.LogError("[WalkerDrill] Could not create Walker Drill root.");
                return;
            }

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

            EditorUtility.SetDirty(walkerDrill);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Selection.activeGameObject = walkerDrill;
            Debug.Log($"[WalkerDrill] Setup complete on '{walkerDrill.name}'. Run Play Mode and press E near the drill.");
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Build Walker Drill Animator Controller")]
        public static void BuildAnimatorControllerOnly()
        {
            WalkerDrillAnimatorFactory.BuildOrUpdateController(out string message);
            if (!string.IsNullOrEmpty(message))
                Debug.Log(message);
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Root + "World/Save Walker Drill Prefab")]
        public static void SaveWalkerDrillPrefab()
        {
            GameObject walkerDrill = GameObject.Find(WalkerDrillObjectName);
            if (walkerDrill == null)
            {
                Debug.LogWarning($"[WalkerDrill] No scene object named '{WalkerDrillObjectName}'. Run setup first.");
                return;
            }

            string folder = Path.GetDirectoryName(PrefabPath)?.Replace('\\', '/');
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/_Project/Prefabs", "World");

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(walkerDrill, PrefabPath);
            if (prefab != null)
                Debug.Log($"[WalkerDrill] Prefab saved to {PrefabPath}");
        }

        private static GameObject FindOrCreateWalkerDrillRoot()
        {
            GameObject existing = GameObject.Find(WalkerDrillObjectName);
            if (existing != null)
                return existing;

            existing = new GameObject(WalkerDrillObjectName);
            existing.transform.position = Vector3.zero;
            existing.transform.rotation = Quaternion.identity;
            return existing;
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
