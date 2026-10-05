using System.Collections.Generic;
using Project.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Builds DM_MeleeAnimationSet clip refs and applies one-hand sword motions to Invector attack states.
    /// </summary>
    public static class DMMeleeAnimationSetApplier
    {
        private const string SetAssetPath = "Assets/_Project/Resources/Combat/DM_MeleeAnimationSet.asset";
        private const string PlayerControllerPath =
            "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";
        private const string EnemyControllerPath =
            "Assets/_Project/Animations/Enemies/The_Evil_OneController.controller";

        private const string AttackCPath =
            "Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/1Handed Melee Weapon Animset/FBX Motions/Humanoid@AttackC1hMelee.fbx";

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build And Apply Melee Animation Set")]
        public static void BuildAndApply()
        {
            DM_MeleeAnimationSet set = LoadOrCreateSet();
            PopulateDefaultClips(set);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            ApplyOneHandSwordToController(PlayerControllerPath, set);
            ApplyOneHandSwordToController(EnemyControllerPath, set);

            AssetDatabase.SaveAssets();
            Debug.Log("DMMeleeAnimationSetApplier: built clip catalog and applied one-hand sword to player + enemy controllers.");
        }

        public static DM_MeleeAnimationSet LoadOrCreateSet()
        {
            DM_MeleeAnimationSet set = AssetDatabase.LoadAssetAtPath<DM_MeleeAnimationSet>(SetAssetPath);
            if (set != null)
                return set;

            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/Combat"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources"))
                    AssetDatabase.CreateFolder("Assets/_Project", "Resources");
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "Combat");
            }

            set = ScriptableObject.CreateInstance<DM_MeleeAnimationSet>();
            AssetDatabase.CreateAsset(set, SetAssetPath);
            return set;
        }

        private static void PopulateDefaultClips(DM_MeleeAnimationSet set)
        {
            AnimationClip attackC = LoadFirstClip(AttackCPath);

            set.oneHandSword.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Attack.fbx");
            set.oneHandSword.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Slash.fbx");
            set.oneHandSword.lightC = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Attack (1).fbx");
            set.oneHandSword.strongA = attackC;
            set.oneHandSword.strongB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Slash (1).fbx");
            set.oneHandSword.strongC = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Slash (2).fbx");
            set.oneHandSword.chargeHold = attackC;

            set.swordAndShield.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Sword And Shield/sword and shield attack.fbx");
            set.swordAndShield.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Sword And Shield/sword and shield slash.fbx");
            set.swordAndShield.lightC = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Sword And Shield/sword and shield attack (2).fbx");

            set.knife.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Knife/Stable Sword Outward Slash.fbx");
            set.knife.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Knife/Stable Sword Inward Slash.fbx");

            set.axe.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Axe/Attacks/Standing Melee Attack Horizontal.fbx");
            set.axe.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Axe/Attacks/Standing Melee Attack Downward.fbx");
            set.axe.lightC = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Axe/Attacks/Standing Melee Combo Attack Ver. 1.fbx");

            set.twoHand.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Two Hands/Great Sword Slash.fbx");
            set.twoHand.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Two Hands/Great Sword Slash (1).fbx");
            set.twoHand.lightC = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Two Hands/Great Sword High Spin Attack.fbx");

            set.dualWield.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Dual Wield/Dual Weapon Combo.fbx");

            set.torch.lightA = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Torch/Attacks/Standing Torch Melee Attack 02.fbx");
            set.torch.lightB = LoadFirstClip(
                "Assets/Animations/Mixamo Animations/Melee Weapons/Torch/Attacks/Standing Torch Melee Attack 03.fbx");
        }

        private static void ApplyOneHandSwordToController(string controllerPath, DM_MeleeAnimationSet set)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"DMMeleeAnimationSetApplier: missing controller at {controllerPath}");
                return;
            }

            AnimatorControllerLayer fullBody = FindLayer(controller, "FullBody");
            if (fullBody == null)
            {
                Debug.LogWarning($"DMMeleeAnimationSetApplier: FullBody layer not found on {controllerPath}");
                return;
            }

            DMMeleeClipSlots slots = set.oneHandSword;
            AnimatorStateMachine attacks = FindSubStateMachine(fullBody.stateMachine, "Attacks");
            if (attacks == null)
                return;

            AnimatorStateMachine weakAttacks = FindSubStateMachine(attacks, "WeakAttacks");
            AnimatorStateMachine strongAttacks = FindSubStateMachine(attacks, "StrongAttacks");
            if (weakAttacks != null)
            {
                AnimatorStateMachine swordWeak = FindSubStateMachine(weakAttacks, "SwordAttack");
                AssignSlotStates(swordWeak, slots);
            }

            if (strongAttacks != null)
            {
                AnimatorStateMachine swordStrong = FindSubStateMachine(strongAttacks, "SwordAttack");
                AssignSlotStates(swordStrong, slots, useStrongSlots: true);

                AnimationClip chargeClip = slots.chargeHold != null ? slots.chargeHold : slots.strongA;
                AnimatorState charge = GetOrCreateState(strongAttacks, "SwordCharge");
                charge.motion = chargeClip;
                charge.speed = 0f;
                charge.writeDefaultValues = true;
            }

            EditorUtility.SetDirty(controller);
        }

        private static void AssignSlotStates(
            AnimatorStateMachine swordAttack,
            DMMeleeClipSlots slots,
            bool useStrongSlots = false)
        {
            if (swordAttack == null || slots == null)
                return;

            SetStateMotion(swordAttack, "A", useStrongSlots ? slots.strongA : slots.lightA);
            SetStateMotion(swordAttack, "B", useStrongSlots ? slots.strongB : slots.lightB);
            SetStateMotion(swordAttack, "C", useStrongSlots ? slots.strongC : slots.lightC);
        }

        private static void SetStateMotion(AnimatorStateMachine sm, string stateName, AnimationClip clip)
        {
            if (clip == null || sm == null)
                return;

            AnimatorState state = GetOrCreateState(sm, stateName);
            state.motion = clip;
            if (stateName == "A" && state.speed <= 0f)
                state.speed = 1f;
        }

        private static AnimatorControllerLayer FindLayer(AnimatorController controller, string layerName)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layerName)
                    return controller.layers[i];
            }

            return null;
        }

        private static AnimatorStateMachine FindSubStateMachine(AnimatorStateMachine parent, string name)
        {
            if (parent == null)
                return null;

            foreach (ChildAnimatorStateMachine child in parent.stateMachines)
            {
                if (child.stateMachine != null && child.stateMachine.name == name)
                    return child.stateMachine;
            }

            return null;
        }

        private static AnimatorState GetOrCreateState(AnimatorStateMachine sm, string name)
        {
            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state.name == name)
                    return child.state;
            }

            return sm.AddState(name);
        }

        private static AnimationClip LoadFirstClip(string assetPath)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__"))
                    return clip;
            }

            return null;
        }
    }
}
