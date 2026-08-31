using Project.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    /// <summary>
    /// Builds the 8-way Protofactor climb blend tree on the live player animator
    /// and spawns a Climbable test wall.
    /// </summary>
    public static class DMClimbBlendTreeBuilder
    {
        public const string ControllerPath =
            "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";

        public const string ClipFolder =
            "Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/Climbing Animset/FBX Motions";

        private const string ClimbLayerName = "Climb";
        private const string ClimbBlendName = "ClimbBlend";
        private const string ClimbEmptyName = "ClimbEmpty";
        private const string ClimbMantleName = "ClimbMantle";
        private const string ClimbStandupName = "ClimbStandup";
        private const string ClimbEnterName = "ClimbEnter";
        private const string ClimbExitName = "ClimbExit";

        [MenuItem(DarkMatterGenesisEditorMenus.Climb + "Build Climb Blend Tree", false, 1)]
        public static void BuildClimbBlendTreeMenu()
        {
            bool ok = BuildOrUpdate(out string message);
            EditorUtility.DisplayDialog(ok ? "Climb Blend Tree" : "Climb Blend Tree Failed", message, "OK");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller != null)
            {
                Selection.activeObject = controller;
                EditorGUIUtility.PingObject(controller);
            }
        }

        [MenuItem(DarkMatterGenesisEditorMenus.Climb + "Add Climb Manager to Player_v7", false, 3)]
        public static void AddClimbManagerToPlayer()
        {
            GameObject player = GameObject.Find("Player_v7");
            if (player == null)
            {
                EditorUtility.DisplayDialog("Climb Manager", "Player_v7 is not in the open scene.", "OK");
                return;
            }

            var manager = player.GetComponent<Project.Features.Climb.DMClimbController>();
            if (manager == null)
                manager = Undo.AddComponent<Project.Features.Climb.DMClimbController>(player);

            var so = new SerializedObject(manager);
            var profileProp = so.FindProperty("profile");
            if (profileProp != null && profileProp.objectReferenceValue == null)
            {
                var profile = UnityEngine.Resources.Load<Project.Features.Climb.DMClimbProfile>("Climb/DMClimbProfile");
                if (profile != null)
                    profileProp.objectReferenceValue = profile;
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(player);
            Selection.activeGameObject = player;
            EditorGUIUtility.PingObject(player);
            EditorUtility.DisplayDialog(
                "Climb Manager",
                "Climb manager is on Player_v7. Edit the Climb Profile on that component (or Assets/_Project/Resources/Climb/DMClimbProfile). Space or E only grabs a Climbable wall.",
                "OK");
        }
        [MenuItem(DarkMatterGenesisEditorMenus.Climb + "Spawn Climb Test Wall", false, 2)]
        public static void SpawnClimbTestWallMenu()
        {
            EditorTagUtility.EnsureTag("Climbable");
            int layer = EditorTagUtility.EnsureLayer("Climbable");

            Vector3 pos = ResolveSpawnPosition();
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = "ClimbTestWall";
            wall.transform.position = pos + Vector3.up * 6f;
            wall.transform.rotation = Quaternion.identity;
            wall.transform.localScale = new Vector3(8f, 12f, 1f);

            if (layer >= 0)
                wall.layer = layer;

            try
            {
                wall.tag = "Climbable";
            }
            catch (UnityException)
            {
                Debug.LogWarning("Climbable tag missing; Tag Manager may need a domain reload.");
            }

            Collider col = wall.GetComponent<Collider>();
            if (col != null)
                col.enabled = true;

            Rigidbody rb = wall.GetComponent<Rigidbody>();
            if (rb == null)
                rb = wall.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Undo.RegisterCreatedObjectUndo(wall, "Spawn Climb Test Wall");
            Selection.activeGameObject = wall;

            string layerNote = layer >= 0 ? layer.ToString() : "unresolved";
            EditorUtility.DisplayDialog(
                "Climb Test Wall",
                "Spawned ClimbTestWall (8x12x1) tagged/layered Climbable at layer " + layerNote +
                ".\n\nFace it, press Space (or E) to attach. WASD to climb. Space again to drop.\n" +
                "If the layer is new, click Build Climb Blend Tree once so the profile mask can pick it up.",
                "OK");
        }

        public static bool BuildOrUpdate(out string message)
        {
            EditorTagUtility.EnsureTag("Climbable");
            EditorTagUtility.EnsureLayer("Climbable");

            AnimationClip idle = LoadClip("Humanoid@IdleWallClimb.fbx");
            AnimationClip up = LoadClip("Humanoid@WallClimbUp.fbx");
            AnimationClip down = LoadClip("Humanoid@WallClimbDown.fbx");
            AnimationClip left = LoadClip("Humanoid@WallClimbLeft.fbx");
            AnimationClip right = LoadClip("Humanoid@WallClimbRight.fbx");
            AnimationClip upLeft = LoadClip("Humanoid@WallClimbUpLeft.fbx");
            AnimationClip upRight = LoadClip("Humanoid@WallClimbUpRight.fbx");
            AnimationClip downLeft = LoadClip("Humanoid@WallClimbDownLeft.fbx");
            AnimationClip downRight = LoadClip("Humanoid@WallClimbDownRight.fbx");

            if (idle == null || up == null || down == null || left == null || right == null ||
                upLeft == null || upRight == null || downLeft == null || downRight == null)
            {
                message = "Missing one or more in-place Protofactor wall-climb clips in:\n" + ClipFolder;
                return false;
            }

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                message = "Could not load live animator:\n" + ControllerPath;
                return false;
            }

            EnsureParameter(controller, "ClimbX", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "ClimbY", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "ClimbSpeed", AnimatorControllerParameterType.Float);
            EnsureParameter(controller, "IsClimbing", AnimatorControllerParameterType.Bool);
            EnsureParameter(controller, "Mantle", AnimatorControllerParameterType.Trigger);

            AnimatorControllerLayer climbLayer = GetOrCreateClimbLayer(controller);
            AnimatorStateMachine sm = climbLayer.stateMachine;

            AnimatorState empty = GetOrCreateState(sm, ClimbEmptyName, new Vector3(40f, 80f, 0f));
            empty.motion = null;
            empty.writeDefaultValues = false;

            AnimatorState blendState = GetOrCreateState(sm, ClimbBlendName, new Vector3(320f, 80f, 0f));
            blendState.motion = BuildOrReuseBlendTree(controller, blendState, idle, up, down, left, right, upLeft, upRight, downLeft, downRight);
            blendState.speed = 1f;
            blendState.writeDefaultValues = true;

            sm.defaultState = empty;

            RemoveAnyStateTo(sm, blendState);
            RemoveOutgoing(blendState);
            RemoveOutgoing(empty);

            AnimatorStateTransition enter = sm.AddAnyStateTransition(blendState);
            enter.hasExitTime = false;
            enter.duration = 0.12f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "IsClimbing");

            AnimatorStateTransition leave = blendState.AddTransition(empty);
            leave.hasExitTime = false;
            leave.duration = 0.12f;
            leave.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");

            TryAddOneShot(
                controller,
                sm,
                empty,
                ClimbEnterName,
                new Vector3(320f, 200f, 0f),
                LoadClip("Humanoid@EnterWallBottom.fbx"));
            TryAddOneShot(
                controller,
                sm,
                empty,
                ClimbExitName,
                new Vector3(40f, 200f, 0f),
                LoadClip("Humanoid@ExitDropFromWall.fbx"));
            TryAddMantle(
                controller,
                sm,
                empty,
                LoadClipByName("H_Free_Climb_Edge NoRoot") ??
                LoadClipByName("H_Free_Climb_Edge"),
                null);

            GateBaseLayerAirborne(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            message = "Built 2D Freeform Directional climb blend on layer '" + ClimbLayerName + "' at:\n" + ControllerPath;
            return true;
        }

        private static BlendTree BuildOrReuseBlendTree(
            AnimatorController controller,
            AnimatorState blendState,
            AnimationClip idle,
            AnimationClip up,
            AnimationClip down,
            AnimationClip left,
            AnimationClip right,
            AnimationClip upLeft,
            AnimationClip upRight,
            AnimationClip downLeft,
            AnimationClip downRight)
        {
            BlendTree tree = blendState.motion as BlendTree;
            if (tree == null || tree.name != ClimbBlendName)
            {
                tree = new BlendTree { name = ClimbBlendName };
                AssetDatabase.AddObjectToAsset(tree, controller);
            }

            while (tree.children.Length > 0)
                tree.RemoveChild(0);

            tree.blendType = BlendTreeType.FreeformDirectional2D;
            tree.blendParameter = "ClimbX";
            tree.blendParameterY = "ClimbY";
            tree.useAutomaticThresholds = false;

            tree.AddChild(idle, new Vector2(0f, 0f));
            tree.AddChild(up, new Vector2(0f, 1f));
            tree.AddChild(down, new Vector2(0f, -1f));
            tree.AddChild(left, new Vector2(-1f, 0f));
            tree.AddChild(right, new Vector2(1f, 0f));
            tree.AddChild(upLeft, new Vector2(-1f, 1f));
            tree.AddChild(upRight, new Vector2(1f, 1f));
            tree.AddChild(downLeft, new Vector2(-1f, -1f));
            tree.AddChild(downRight, new Vector2(1f, -1f));

            EditorUtility.SetDirty(tree);
            return tree;
        }

        private static void TryAddOneShot(
            AnimatorController controller,
            AnimatorStateMachine sm,
            AnimatorState empty,
            string stateName,
            Vector3 pos,
            AnimationClip clip)
        {
            if (clip == null)
                return;

            AnimatorState state = GetOrCreateState(sm, stateName, pos);
            state.motion = clip;
            state.writeDefaultValues = true;
            RemoveOutgoing(state);

            AnimatorStateTransition done = state.AddTransition(empty);
            done.hasExitTime = true;
            done.exitTime = 0.9f;
            done.duration = 0.1f;
        }

        private static void TryAddMantle(
            AnimatorController controller,
            AnimatorStateMachine sm,
            AnimatorState empty,
            AnimationClip upOver,
            AnimationClip standup)
        {
            if (upOver == null)
                return;

            AnimatorState over = GetOrCreateState(sm, ClimbMantleName, new Vector3(320f, -40f, 0f));
            over.motion = upOver;
            over.speed = 0.9f;
            over.writeDefaultValues = true;
            RemoveOutgoing(over);
            RemoveAnyStateTo(sm, over);

            AnimatorStateTransition enter = sm.AddAnyStateTransition(over);
            enter.hasExitTime = false;
            enter.duration = 0.08f;
            enter.canTransitionToSelf = false;
            enter.AddCondition(AnimatorConditionMode.If, 0f, "Mantle");

            AnimatorState after = empty;
            if (standup != null)
            {
                after = GetOrCreateState(sm, ClimbStandupName, new Vector3(520f, -40f, 0f));
                after.motion = standup;
                after.writeDefaultValues = true;
                RemoveOutgoing(after);
                RemoveAnyStateTo(sm, after);

                AnimatorStateTransition blend = over.AddTransition(after);
                blend.hasExitTime = true;
                blend.exitTime = 0.72f;
                blend.duration = 0.22f;
                blend.hasFixedDuration = true;
            }

            AnimatorStateTransition done = after.AddTransition(empty);
            done.hasExitTime = true;
            done.exitTime = 0.88f;
            done.duration = 0.12f;
        }

        private static void GateBaseLayerAirborne(AnimatorController controller)
        {
            if (controller.layers == null || controller.layers.Length == 0)
                return;

            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorStateTransition[] transitions = root.anyStateTransitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition == null)
                    continue;

                bool requiresUngrounded = false;
                bool alreadyGated = false;
                AnimatorCondition[] conditions = transition.conditions;
                for (int c = 0; c < conditions.Length; c++)
                {
                    if (conditions[c].parameter == "IsGrounded" &&
                        conditions[c].mode == AnimatorConditionMode.IfNot)
                        requiresUngrounded = true;

                    if (conditions[c].parameter == "IsClimbing")
                        alreadyGated = true;
                }

                if (requiresUngrounded && !alreadyGated)
                    transition.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsClimbing");
            }
        }

        private static AnimatorControllerLayer GetOrCreateClimbLayer(AnimatorController controller)
        {
            AnimatorControllerLayer[] layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                if (layers[i].name == ClimbLayerName)
                {
                    layers[i].defaultWeight = 0f;
                    controller.layers = layers;
                    return controller.layers[i];
                }
            }

            controller.AddLayer(ClimbLayerName);
            layers = controller.layers;
            AnimatorControllerLayer layer = layers[layers.Length - 1];
            layer.defaultWeight = 0f;
            layer.blendingMode = AnimatorLayerBlendingMode.Override;
            layer.iKPass = false;
            layers[layers.Length - 1] = layer;
            controller.layers = layers;
            return controller.layers[controller.layers.Length - 1];
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name, Vector3 pos)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == name)
                    return child.state;
            }

            return sm.AddState(name, pos);
        }

        private static void RemoveAnyStateTo(AnimatorStateMachine sm, AnimatorState destination)
        {
            for (int i = sm.anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition t = sm.anyStateTransitions[i];
                if (t != null && t.destinationState == destination)
                    sm.RemoveAnyStateTransition(t);
            }
        }

        private static void RemoveOutgoing(AnimatorState state)
        {
            if (state == null)
                return;

            for (int i = state.transitions.Length - 1; i >= 0; i--)
                state.RemoveTransition(state.transitions[i]);
        }

        private static void EnsureParameter(
            AnimatorController controller,
            string name,
            AnimatorControllerParameterType type)
        {
            foreach (AnimatorControllerParameter parameter in controller.parameters)
            {
                if (parameter.name == name)
                    return;
            }

            controller.AddParameter(name, type);
        }

        private static AnimationClip LoadClipByName(string clipName)
        {
            string[] guids = AssetDatabase.FindAssets("t:AnimationClip");
            if (guids == null)
                return null;
            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (string.IsNullOrEmpty(path) || path.IndexOf(clipName, System.StringComparison.OrdinalIgnoreCase) < 0
                    && path.IndexOf("H_Free_Climb_Edge", System.StringComparison.OrdinalIgnoreCase) < 0
                    && path.IndexOf("Sr388", System.StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
                if (assets == null)
                    continue;
                for (int a = 0; a < assets.Length; a++)
                {
                    AnimationClip clip = assets[a] as AnimationClip;
                    if (clip != null && clip.name == clipName)
                        return clip;
                }
            }
            return null;
        }

        private static AnimationClip LoadClip(string fileName)
        {
            string path = ClipFolder + "/" + fileName;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            if (assets == null || assets.Length == 0)
            {
                string alt = fileName.EndsWith(".FBX")
                    ? fileName.Substring(0, fileName.Length - 4) + ".fbx"
                    : fileName.EndsWith(".fbx")
                        ? fileName.Substring(0, fileName.Length - 4) + ".FBX"
                        : fileName;
                assets = AssetDatabase.LoadAllAssetsAtPath(ClipFolder + "/" + alt);
            }

            if (assets == null)
                return null;

            string want = System.IO.Path.GetFileNameWithoutExtension(fileName);
            AnimationClip first = null;
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is not AnimationClip clip)
                    continue;
                if (clip.name.StartsWith("__preview__"))
                    continue;
                if (clip.name == want || clip.name.Contains(want))
                    return clip;
                if (first == null)
                    first = clip;
            }

            return first;
        }

        private static Vector3 ResolveSpawnPosition()
        {
            GameObject player = GameObject.Find("Player_v7");
            if (player != null)
                return player.transform.position + player.transform.forward * 4f;

            SceneView view = SceneView.lastActiveSceneView;
            if (view != null && view.camera != null)
            {
                Ray ray = view.camera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
                if (Physics.Raycast(ray, out RaycastHit hit, 80f))
                    return hit.point + hit.normal * 0.6f;
                return view.pivot;
            }

            return Vector3.zero;
        }
    }
}