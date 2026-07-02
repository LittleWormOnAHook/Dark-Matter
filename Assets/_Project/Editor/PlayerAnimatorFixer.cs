using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System.IO;

namespace Project.AnimationFixer
{
    public static class PlayerAnimatorFixer
    {
        private const string ControllerPath = "Assets/_Project/Animations/ProjectGKCCharacterController.controller";

        // Avatar Masks
        private const string MaskArmRight = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Arms/Human Arm Right Mask.mask";
        private const string MaskArmLeft = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Arms/Human Arm Left Mask.mask";
        private const string MaskHandRight = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Hands/Human Hand Right Mask.mask";
        private const string MaskHandLeft = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Hands/Human Hand Left Mask.mask";
        private const string MaskBodyUpper = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Body Upper Mask.mask";
        private const string MaskHead = "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Head Mask.mask";

        // Locomotion Assets
        private const string UnarmedIdlePath = "Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/HumanoidIdle.fbx";
        private const string UnarmedRunPath = "Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/HumanoidRun.fbx";

        // Walk Strafe FBXs (Unarmed)
        private const string WalkForward = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx";
        private const string WalkBackward = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Backward.fbx";
        private const string WalkLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_Left.fbx";
        private const string WalkRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_Right.fbx";
        private const string WalkForwardLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_ForwardLeft.fbx";
        private const string WalkForwardRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_ForwardRight.fbx";
        private const string WalkBackwardLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_BackwardLeft.fbx";
        private const string WalkBackwardRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeWalk/HumanM@StrafeWalk01_BackwardRight.fbx";

        // Run Strafe FBXs (Unarmed)
        private const string RunForward = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx";
        private const string RunBackward = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Backward.fbx";
        private const string RunLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Left.fbx";
        private const string RunRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_Right.fbx";
        private const string RunForwardLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx";
        private const string RunForwardRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx";
        private const string RunBackwardLeft = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx";
        private const string RunBackwardRight = "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx";

        // 1H Melee Strafe (Weapon ID 1)
        private const string Melee1H_Idle = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Idle.fbx";
        private const string Melee1H_WalkForward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Walk.fbx";
        private const string Melee1H_WalkBackward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Walk (1).fbx";
        private const string Melee1H_WalkLeft = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Strafe.fbx";
        private const string Melee1H_WalkRight = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Strafe (1).fbx";
        private const string Melee1H_RunForward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Run.fbx";
        private const string Melee1H_RunBackward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee One Hand/Sword And Shield Run (1).fbx";

        // 2H Melee Strafe (Weapon ID 2)
        private const string Melee2H_Idle = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Idle.fbx";
        private const string Melee2H_WalkForward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Walk.fbx";
        private const string Melee2H_WalkBackward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Walk (1).fbx";
        private const string Melee2H_WalkLeft = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Strafe.fbx";
        private const string Melee2H_WalkRight = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Strafe (1).fbx";
        private const string Melee2H_RunForward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Run (1).fbx";
        private const string Melee2H_RunBackward = "Assets/Animations/Mixamo Animations/Strafe/Strafe Armed/Strafe Melee Two Hands/Great Sword Run (4).fbx";

        [MenuItem("Tools/Fix Player Animator")]
        public static void FixPlayerAnimator()
        {
            Debug.Log("=== Starting Automated Player Animator Fix ===");

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
            if (controller == null)
            {
                Debug.LogError($"Failed to load AnimatorController at: {ControllerPath}");
                return;
            }

            Undo.RecordObject(controller, "Fix Player Animator Controller");

            FixLayerMasks(controller);

            var baseStateMachine = controller.layers[0].stateMachine;
            FixLocomotionAndStrafeTrees(baseStateMachine);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log("=== Automated Player Animator Fix Completed Successfully! ===");
        }

        private static void FixLayerMasks(AnimatorController controller)
        {
            var layers = controller.layers;
            for (int i = 0; i < layers.Length; i++)
            {
                var layer = layers[i];
                string layerName = layer.name;
                string maskPath = null;

                if (layerName == "Right Arm") maskPath = MaskArmRight;
                else if (layerName == "Left Arm") maskPath = MaskArmLeft;
                else if (layerName == "Right Hand") maskPath = MaskHandRight;
                else if (layerName == "Left Hand") maskPath = MaskHandLeft;
                else if (layerName == "Upper Body") maskPath = MaskBodyUpper;
                else if (layerName == "Head") maskPath = MaskHead;
                else if (layerName == "Upper Body With Movement") maskPath = MaskBodyUpper;

                if (maskPath != null)
                {
                    AvatarMask mask = AssetDatabase.LoadAssetAtPath<AvatarMask>(maskPath);
                    if (mask != null)
                    {
                        layer.avatarMask = mask;
                    }
                }
            }
            controller.layers = layers;
        }

        private static void FixLocomotionAndStrafeTrees(AnimatorStateMachine stateMachine)
        {
            foreach (var state in stateMachine.states)
            {
                if (state.state.motion is BlendTree tree)
                {
                    FixTreeRecursive(tree);
                }
            }

            foreach (var subMachine in stateMachine.stateMachines)
            {
                FixLocomotionAndStrafeTrees(subMachine.stateMachine);
            }
        }

        private static void FixTreeRecursive(BlendTree tree)
        {
            string treeName = tree.name;

            if (treeName == "Idle Type")
            {
                var children = tree.children;
                if (children.Length > 0 && children[0].motion == null)
                    children[0].motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnarmedIdlePath);
                if (children.Length > 2 && children[2].motion == null)
                    children[2].motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnarmedIdlePath);
                tree.children = children;
            }

            if (treeName == "Normal")
            {
                var children = tree.children;
                if (children.Length > 15 && children[15].motion == null)
                    children[15].motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(UnarmedRunPath);
                tree.children = children;
            }

            // Melee Weapon ID 1 (OneHandSword)
            if (treeName == "Weapon ID 1")
            {
                PopulateMeleeTree(tree, 1);
            }

            // Melee Weapon ID 2 (TwoHand)
            if (treeName == "Weapon ID 2")
            {
                PopulateMeleeTree(tree, 2);
            }

            if (treeName == "Walk Strafe" || treeName == "Run Strafe")
            {
                PopulateStrafeTree(tree, treeName.Contains("Walk"));
            }

            if (treeName == "Airborne" || treeName == "Airbone")
            {
                PopulateAirborneTree(tree);
            }

            foreach (var child in tree.children)
            {
                if (child.motion is BlendTree subTree)
                {
                    FixTreeRecursive(subTree);
                }
            }
        }

        private static void PopulateMeleeTree(BlendTree tree, int weaponId)
        {
            var children = tree.children;
            bool modified = false;

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                Vector2 pos = child.position;
                string clipPath = ResolveMeleeClipPath(pos, weaponId);
                
                if (clipPath != null)
                {
                    child.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                    if (child.motion != null)
                    {
                        children[i] = child;
                        modified = true;
                    }
                }
            }

            if (modified)
                tree.children = children;
        }

        private static string ResolveMeleeClipPath(Vector2 pos, int weaponId)
        {
            float x = pos.x;
            float y = pos.y;

            if (weaponId == 1) // 1H Melee
            {
                if (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f) return Melee1H_Idle;
                if (Mathf.Abs(x) < 0.1f && y > 0.1f) return Melee1H_WalkForward;
                if (Mathf.Abs(x) < 0.1f && y < -0.1f) return Melee1H_WalkBackward;
                if (x < -0.1f && Mathf.Abs(y) < 0.1f) return Melee1H_WalkLeft;
                if (x > 0.1f && Mathf.Abs(y) < 0.1f) return Melee1H_WalkRight;
            }
            else if (weaponId == 2) // 2H Melee
            {
                if (Mathf.Abs(x) < 0.1f && Mathf.Abs(y) < 0.1f) return Melee2H_Idle;
                if (Mathf.Abs(x) < 0.1f && y > 0.1f) return Melee2H_WalkForward;
                if (Mathf.Abs(x) < 0.1f && y < -0.1f) return Melee2H_WalkBackward;
                if (x < -0.1f && Mathf.Abs(y) < 0.1f) return Melee2H_WalkLeft;
                if (x > 0.1f && Mathf.Abs(y) < 0.1f) return Melee2H_WalkRight;
            }

            return null;
        }

        private static void PopulateStrafeTree(BlendTree tree, bool isWalk)
        {
            var children = tree.children;
            bool modified = false;

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child.motion == null)
                {
                    Vector2 pos = child.position;
                    string clipPath = ResolveStrafeClipPath(pos, isWalk);
                    if (clipPath != null)
                    {
                        child.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
                        if (child.motion != null)
                        {
                            children[i] = child;
                            modified = true;
                        }
                    }
                }
            }

            if (modified)
                tree.children = children;
        }

        private static string ResolveStrafeClipPath(Vector2 pos, bool isWalk)
        {
            float x = pos.x;
            float y = pos.y;

            if (isWalk)
            {
                if (Mathf.Abs(x) < 0.05f && y > 0.1f) return WalkForward;
                if (Mathf.Abs(x) < 0.05f && y < -0.1f) return WalkBackward;
                if (x < -0.1f && Mathf.Abs(y) < 0.05f) return WalkLeft;
                if (x > 0.1f && Mathf.Abs(y) < 0.05f) return WalkRight;
                if (x < -0.1f && y > 0.1f) return WalkForwardLeft;
                if (x > 0.1f && y > 0.1f) return WalkForwardRight;
                if (x < -0.1f && y < -0.1f) return WalkBackwardLeft;
                if (x > 0.1f && y < -0.1f) return WalkBackwardRight;
            }
            else
            {
                if (Mathf.Abs(x) < 0.05f && y > 0.1f) return RunForward;
                if (Mathf.Abs(x) < 0.05f && y < -0.1f) return RunBackward;
                if (x < -0.1f && Mathf.Abs(y) < 0.05f) return RunLeft;
                if (x > 0.1f && Mathf.Abs(y) < 0.05f) return RunRight;
                if (x < -0.1f && y > 0.1f) return RunForwardLeft;
                if (x > 0.1f && y > 0.1f) return RunForwardRight;
                if (x < -0.1f && y < -0.1f) return RunBackwardLeft;
                if (x > 0.1f && y < -0.1f) return RunBackwardRight;
            }

            return null;
        }

        private static void PopulateAirborneTree(BlendTree tree)
        {
            var children = tree.children;
            bool modified = false;

            string fallPath = "Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/HumanoidFall.fbx";
            string midairPath = "Assets/ECM2/Shared Assets/Models/UnityCharacter/Animations/HumanoidMidAir.fbx";

            var fallClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(fallPath);
            var midairClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(midairPath);

            for (int i = 0; i < children.Length; i++)
            {
                var child = children[i];
                if (child.motion == null)
                {
                    if (child.position.y < -1f || i == 0 || i == 3)
                        child.motion = fallClip;
                    else
                        child.motion = midairClip;

                    if (child.motion != null)
                    {
                        children[i] = child;
                        modified = true;
                    }
                }
            }

            if (modified)
                tree.children = children;
        }
    }
}


