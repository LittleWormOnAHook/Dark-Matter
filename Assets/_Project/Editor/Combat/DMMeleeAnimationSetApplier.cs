using System.Collections.Generic;
using Invector.vMelee;
using Project.Combat;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>
    /// Builds DM_MeleeAnimationSet clip refs and applies one-hand sword motions to Invector attack states.
    /// </summary>
    /// <remarks>
    /// Jetpack FullBody mapping (do not regress):
    /// <list type="bullet">
    /// <item>WeakAttacks/SwordAttack A,B,C ← oneHandSword lightA/lightB/lightC (WeakAttack_SwordA→B→C chain).</item>
    /// <item>WeakAttacks/SwordRandomAttack A,B,C ← Mixamo parallel swings only (never combo light slots).</item>
    /// <item>StrongAttacks/SwordAttack A,B,C ← strongA/strongB/strongC.</item>
    /// <item>StrongAttacks/SwordCharge ← chargeHold (1HandSwordChargeUp). Runtime freezes the draw-back pose; never assign a swing clip.</item>
    /// <item>WeakAttacks entry AttackID==1 → SwordAttack sub-SM (not SwordRandomAttack).</item>
    /// <item>FullBody Weak/Strong/Random A/B/C + Parry return to Attacks.Null (exitTime ~0.9, no conditions). SwordCharge has no exit-time.</item>
    /// Never call AssignSlotStates on SwordRandomAttack — that copies combo clips into the random pool.
    /// </list>
    /// </remarks>
    public static class DMMeleeAnimationSetApplier
    {
        private const string SetAssetPath = "Assets/_Project/Resources/Combat/DM_MeleeAnimationSet.asset";
        private const string PlayerControllerPath =
            "Assets/_Project/Animations/Player/Invector@ShooterMelee_Jetpack.controller";
        private const string EnemyControllerPath =
            "Assets/_Project/Animations/Enemies/The_Evil_OneController.controller";

        private const string AttackCPath =
            "Assets/PROTOFACTOR/Ultimate Animation Collection/Animations/1Handed Melee Weapon Animset/FBX Motions/Humanoid@AttackC1hMelee.fbx";

        /// <summary>When PROTOFACTOR AttackC is not on disk — loopable sword wind-up, not strongB release.</summary>
        private const string ChargeHoldFallbackPath =
            "Assets/Animations/Props Animations/Animations/Medievil.fbx";

        private const string ChargeHoldFallbackClipName = "1HandSwordChargeUp";

        private const string InvectorSwordWeakFbxPath =
            "Assets/Invector-3rdPersonController/Melee Combat/3DModels/Animations/Melee_CombatSet.fbx";

        /// <summary>Parallel random weak swings — not the WeakAttack_Sword A→B→C combo chain.</summary>
        private const string RandomLightAPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Attack.fbx";
        private const string RandomLightBPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Slash.fbx";
        private const string RandomLightCPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Attack (1).fbx";

        private const string Parry01FbxPath =
            "Assets/Animations/Melee Warrior Animations/Animations/OneHanded/RightHand/RightHand@Parry01.fbx";

        private const string InteractHoldComboPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/One Hand Sword Combo.fbx";

        private const string StrongBPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/Axe/Attacks/Standing Melee Combo Attack Ver. 1.fbx";
        private const string StrongCPath =
            "Assets/Animations/Mixamo Animations/Melee Weapons/Axe/Attacks/Standing Melee Attack 360 Low.fbx";

        [MenuItem("Tools/Dark Matter Genesis/Combat/Build And Apply Melee Animation Set", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Build_And_Apply_Melee_Animation_Set)]
        public static void BuildAndApply()
        {
            DM_MeleeAnimationSet set = LoadOrCreateSet();
            EnsureHumanoidImport(InteractHoldComboPath);
            PopulateDefaultClips(set);
            EditorUtility.SetDirty(set);
            AssetDatabase.SaveAssets();

            ApplyOneHandSwordToController(PlayerControllerPath, set, applyOverlays: true);
            ApplyOneHandSwordToController(EnemyControllerPath, set, applyOverlays: false);

            AssetDatabase.SaveAssets();
            Debug.Log("DMMeleeAnimationSetApplier: applied sword clips, Hold E upper-body combo, parry states, and FullBody Null returns.");
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
            AnimationClip chargeHold = LoadClipByName(ChargeHoldFallbackPath, ChargeHoldFallbackClipName);
            if (chargeHold == null)
                Debug.LogWarning("[DMMeleeAnimationSetApplier] Missing 1HandSwordChargeUp — SwordCharge must not fall back to a swing clip.");

            set.oneHandSword.lightA = LoadClipByName(InvectorSwordWeakFbxPath, "WeakAttack_SwordA");
            set.oneHandSword.lightB = LoadClipByName(InvectorSwordWeakFbxPath, "WeakAttack_SwordB");
            set.oneHandSword.lightC = LoadClipByName(InvectorSwordWeakFbxPath, "WeakAttack_SwordC");
            if (set.oneHandSword.strongA == null)
            {
                set.oneHandSword.strongA = LoadFirstClip(
                    "Assets/Animations/Mixamo Animations/Melee Weapons/One Hand/Sword And Shield Slash (1).fbx");
            }

            AnimationClip strongB = LoadFirstClip(StrongBPath);
            AnimationClip strongC = LoadFirstClip(StrongCPath);
            if (strongB != null)
                set.oneHandSword.strongB = strongB;
            if (strongC != null)
                set.oneHandSword.strongC = strongC;
            set.oneHandSword.chargeHold = chargeHold;
            set.oneHandSword.parry01 = LoadClipByName(Parry01FbxPath, "1H-RH@Parry01");
            set.oneHandSword.parry01Hit = LoadClipByName(Parry01FbxPath, "1H-RH@Parry01_Hit");
            AnimationClip interactCombo = LoadFirstClip(InteractHoldComboPath);
            CopyMeleeEventsFromTemplate(interactCombo, set.oneHandSword.lightA);
            set.oneHandSword.interactHoldCombo = interactCombo;

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

        private static void ApplyOneHandSwordToController(
            string controllerPath,
            DM_MeleeAnimationSet set,
            bool applyOverlays = true)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogWarning($"DMMeleeAnimationSetApplier: missing controller at {controllerPath}");
                return;
            }

            DMMeleeClipSlots slots = set.oneHandSword;
            ApplyOneHandSwordToLayer(controller, "FullBody", slots);

            if (applyOverlays)
            {
                ApplyOverlayAttackStates(controller, slots);
                EnsureUpperBodyInteractHold(controller, slots);
                ClearFullBodyNullWriteDefaults(controller);
                EnsureFullBodyAttackReturnsToNull(controller);
            }

            ApplyStrongMeleeAnimSpeedFromProfile(controller);
            EditorUtility.SetDirty(controller);
        }

        private static void ApplyOneHandSwordToLayer(
            AnimatorController controller,
            string layerName,
            DMMeleeClipSlots slots)
        {
            AnimatorControllerLayer layer = FindLayer(controller, layerName);
            if (layer == null)
                return;

            AnimatorStateMachine attacks = FindSubStateMachine(layer.stateMachine, "Attacks");
            if (attacks == null)
                return;

            AnimatorStateMachine weakAttacks = FindSubStateMachine(attacks, "WeakAttacks");
            AnimatorStateMachine strongAttacks = FindSubStateMachine(attacks, "StrongAttacks");
            if (weakAttacks != null)
            {
                AnimatorStateMachine swordCombo = FindSubStateMachine(weakAttacks, "SwordAttack");
                ApplyWeakSwordComboClips(swordCombo, slots);
                if (layerName == "FullBody")
                    RouteWeakAttackIdToSwordAttack(weakAttacks, swordCombo);

                AnimatorStateMachine swordRandom = FindSubStateMachine(weakAttacks, "SwordRandomAttack");
                ApplySwordRandomParallelClips(swordRandom);
            }

            if (strongAttacks != null)
            {
                AnimatorStateMachine swordStrong = FindSubStateMachine(strongAttacks, "SwordAttack");
                AssignStrongSwordMotions(swordStrong, slots);

                AnimatorState charge = GetOrCreateState(strongAttacks, "SwordCharge");
                AnimationClip chargeClip = slots.chargeHold;
                if (chargeClip == null)
                    chargeClip = LoadClipByName(ChargeHoldFallbackPath, ChargeHoldFallbackClipName);
                if (chargeClip != null)
                    charge.motion = chargeClip;
                charge.writeDefaultValues = true;
                charge.speed = 1f;
            }
        }

        /// <summary>
        /// Writes strong B and C every apply. Leaves strong A alone when it already has a clip.
        /// </summary>
        private static void AssignStrongSwordMotions(AnimatorStateMachine swordStrong, DMMeleeClipSlots slots)
        {
            if (swordStrong == null || slots == null)
                return;

            AnimatorState strongA = GetOrCreateState(swordStrong, "A");
            if (strongA.motion == null && slots.strongA != null)
                strongA.motion = slots.strongA;

            if (slots.strongB != null)
                SetStateMotion(swordStrong, "B", slots.strongB);
            if (slots.strongC != null)
                SetStateMotion(swordStrong, "C", slots.strongC);
        }

        private static void ApplyOverlayAttackStates(AnimatorController controller, DMMeleeClipSlots slots)
        {
            if (controller == null || slots == null)
                return;

            AnimatorControllerLayer layer = FindLayer(controller, "FullBody");
            if (layer == null)
                return;

            AnimatorStateMachine attacks = FindSubStateMachine(layer.stateMachine, "Attacks");
            if (attacks == null)
                return;

            if (slots.parry01 != null)
            {
                AnimatorState parry = FindParryWindupState(layer.stateMachine);
                if (parry == null)
                    parry = GetOrCreateState(attacks, "Parry01");
                parry.motion = slots.parry01;
                parry.writeDefaultValues = true;
            }

            if (slots.parry01Hit != null)
            {
                AnimatorState parryHit = FindStateRecursive(layer.stateMachine, "Parry01_Hit");
                if (parryHit == null)
                    parryHit = GetOrCreateState(attacks, "Parry01_Hit");
                parryHit.motion = slots.parry01Hit;
                parryHit.writeDefaultValues = true;
            }

            if (slots.interactHoldCombo != null)
            {
                AnimatorState interact = GetOrCreateState(attacks, "InteractHoldCombo");
                AnimationClip hipSafe = HipSafeClip(slots.interactHoldCombo, "Interact");
                interact.motion = hipSafe != null ? hipSafe : slots.interactHoldCombo;
                interact.writeDefaultValues = false;
            }

            AnimatorState attacksNull = FindAttacksNullState(controller);
            if (attacksNull != null)
            {
                AnimatorState parryState = FindParryWindupState(layer.stateMachine);
                if (parryState != null)
                    EnsureReturnTransition(parryState, attacksNull, 0.9f);
                AnimatorState parryHitState = FindStateRecursive(layer.stateMachine, "Parry01_Hit");
                if (parryHitState != null)
                    EnsureReturnTransition(parryHitState, attacksNull, 0.9f);
                AnimatorState interactState = FindStateRecursive(attacks, "InteractHoldCombo");
                if (interactState != null)
                    EnsureReturnTransition(interactState, attacksNull, 0.9f);
            }
        }

        private const string MeleeUpperFolder = "Assets/_Project/Animations/Player/MeleeUpper";
        private static AnimationClip HipSafeClip(AnimationClip source, string slotName)
        {
            if (source == null)
                return null;

            if (!AssetDatabase.IsValidFolder(MeleeUpperFolder))
            {
                if (!AssetDatabase.IsValidFolder("Assets/_Project/Animations/Player"))
                    AssetDatabase.CreateFolder("Assets/_Project/Animations", "Player");
                AssetDatabase.CreateFolder("Assets/_Project/Animations/Player", "MeleeUpper");
            }

            string path = MeleeUpperFolder + "/DM_Upper_" + slotName + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
            {
                clip = new AnimationClip { name = "DM_Upper_" + slotName };
                AssetDatabase.CreateAsset(clip, path);
            }

            clip.ClearCurves();
            EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(source);
            int kept = 0;
            for (int i = 0; i < bindings.Length; i++)
            {
                if (IsHipOrRootBinding(bindings[i]))
                    continue;

                AnimationUtility.SetEditorCurve(
                    clip,
                    bindings[i],
                    AnimationUtility.GetEditorCurve(source, bindings[i]));
                kept++;
            }

            if (kept == 0)
            {
                Debug.LogWarning(
                    "DMMeleeAnimationSetApplier: no upper-body curves copied for " + slotName
                    + ". The masked layer will not use the raw clip.");
            }

            AnimationUtility.SetAnimationEvents(clip, System.Array.Empty<AnimationEvent>());
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(source);
            settings.loopTime = false;
            settings.keepOriginalPositionY = false;
            settings.keepOriginalPositionXZ = false;
            settings.keepOriginalOrientation = false;
            settings.loopBlendPositionY = false;
            settings.loopBlendPositionXZ = false;
            settings.loopBlendOrientation = false;
            settings.heightFromFeet = false;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            clip.frameRate = source.frameRate;
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static bool IsHipOrRootBinding(EditorCurveBinding binding)
        {
            string property = binding.propertyName ?? string.Empty;
            if (property.StartsWith("RootT") || property.StartsWith("RootQ"))
                return true;

            string path = binding.path ?? string.Empty;
            string bone = path;
            int slash = path.LastIndexOf('/');
            if (slash >= 0 && slash < path.Length - 1)
                bone = path.Substring(slash + 1);

            // Muscle curves use an empty path. Transform curves name the bone in the last segment.
            // Do not match ancestor "Hips" or every Mixamo child curve is thrown away.
            string token = string.IsNullOrEmpty(path) ? property : bone;
            if (token.IndexOf("Hips", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (token.IndexOf("Leg", System.StringComparison.OrdinalIgnoreCase) >= 0
                && token.IndexOf("Arm", System.StringComparison.OrdinalIgnoreCase) < 0)
                return true;
            if (token.IndexOf("Foot", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (token.IndexOf("Toe", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            return false;
        }

        private static void ClearFullBodyNullWriteDefaults(AnimatorController controller)
        {
            AnimatorControllerLayer fullBody = FindLayer(controller, "FullBody");
            if (fullBody != null && fullBody.stateMachine != null)
            {
                if (fullBody.stateMachine.defaultState != null)
                    fullBody.stateMachine.defaultState.writeDefaultValues = false;
                ClearNullWriteDefaults(fullBody.stateMachine);
            }

            AnimatorControllerLayer upper = FindLayer(controller, "UpperBody");
            if (upper != null && upper.stateMachine != null && upper.stateMachine.defaultState != null)
                upper.stateMachine.defaultState.writeDefaultValues = false;
        }

        private static void ClearNullWriteDefaults(AnimatorStateMachine machine)
        {
            if (machine == null)
                return;

            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state == null)
                    continue;
                if (child.state.name == "Null" || child.state.name == "null")
                    child.state.writeDefaultValues = false;
            }

            foreach (ChildAnimatorStateMachine child in machine.stateMachines)
                ClearNullWriteDefaults(child.stateMachine);
        }

        /// <summary>
        /// Hold E only. UpperBody masked swing using the hip-safe combo. Does not add a melee overlay layer.
        /// </summary>
        private static void EnsureUpperBodyInteractHold(AnimatorController controller, DMMeleeClipSlots slots)
        {
            if (controller == null || slots == null || slots.interactHoldCombo == null)
                return;

            AnimatorControllerLayer upper = FindLayer(controller, "UpperBody");
            if (upper == null || upper.stateMachine == null)
                return;

            AnimationClip clip = HipSafeClip(slots.interactHoldCombo, "Interact");
            if (clip == null)
                clip = slots.interactHoldCombo;

            AnimatorState state = GetOrCreateState(upper.stateMachine, "InteractHoldCombo");
            state.motion = clip;
            state.writeDefaultValues = false;
            state.iKOnFeet = false;
            if (upper.stateMachine.defaultState != null)
                EnsureReturnTransition(state, upper.stateMachine.defaultState, 0.9f);
            EnsureAttackBehaviour(state, 0.18f, 0.82f);
        }

        private static void EnsureMoveState(
            AnimatorStateMachine root,
            string stateName,
            AnimationClip clip,
            float damageStart,
            float damageEnd,
            bool damage,
            AnimatorState returnTo = null)
        {
            if (clip == null)
                return;

            AnimatorState state = GetOrCreateState(root, stateName);
            state.motion = clip;
            state.writeDefaultValues = false;
            state.iKOnFeet = false;
            if (!damage)
            {
                // Charge is a hold pose. An exit-time transition drops it while the button is still down.
                ClearStateTransitions(state);
            }
            else if (returnTo != null && returnTo != state)
                EnsureReturnTransition(state, returnTo, 0.9f);
            else
                EnsureExitTransition(state, 0.9f);
            if (damage)
                EnsureAttackBehaviour(state, damageStart, damageEnd);
        }

        private static void ClearStateTransitions(AnimatorState state)
        {
            if (state == null)
                return;

            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null)
                    state.RemoveTransition(transitions[i]);
            }
        }

        private static void EnsureReturnTransition(AnimatorState state, AnimatorState destination, float exitTime)
        {
            if (state == null || destination == null)
                return;

            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition existing = transitions[i];
                if (existing == null || existing.destinationState != destination)
                    continue;

                existing.hasExitTime = true;
                existing.exitTime = exitTime;
                existing.duration = 0.1f;
                existing.hasFixedDuration = true;
                ClearTransitionConditions(existing);
                return;
            }

            AnimatorStateTransition transition = state.AddTransition(destination);
            transition.hasExitTime = true;
            transition.exitTime = exitTime;
            transition.duration = 0.1f;
            transition.hasFixedDuration = true;
            transition.interruptionSource = TransitionInterruptionSource.None;
            ClearTransitionConditions(transition);
        }

        private static void ClearTransitionConditions(AnimatorTransitionBase transition)
        {
            if (transition == null)
                return;

            AnimatorCondition[] conditions = transition.conditions;
            for (int i = conditions.Length - 1; i >= 0; i--)
                transition.RemoveCondition(conditions[i]);
        }

        /// <summary>
        /// Nested Attacks.Null is empty but Pioneer CrossFade into SwordAttack never reaches it.
        /// Idle_Empty lives on the FullBody root (and Attacks) with null motion. Unconditional
        /// StrongAttacks/WeakAttacks → Null SM transitions yank SwordCharge and are removed.
        /// </summary>
        private static void EnsureFullBodyAttackReturnsToNull(AnimatorController controller)
        {
            AnimatorControllerLayer fullBody = FindLayer(controller, "FullBody");
            if (fullBody == null)
                return;

            AnimatorStateMachine attacks = FindSubStateMachine(fullBody.stateMachine, "Attacks");
            if (attacks == null)
                return;

            AnimatorStateMachine weak = FindSubStateMachine(attacks, "WeakAttacks");
            AnimatorStateMachine strong = FindSubStateMachine(attacks, "StrongAttacks");
            EnsureIdleEmptyStates(fullBody, attacks);
            RemoveUnconditionalAttackSmReturns(attacks, weak);
            RemoveUnconditionalAttackSmReturns(attacks, strong);

            AnimatorState empty = FindStateInMachine(attacks, "Idle_Empty");
            if (empty == null)
                empty = FindStateInMachine(fullBody.stateMachine, "Idle_Empty");
            if (empty == null)
                empty = FindAttacksNullState(controller);
            if (empty == null)
                return;

            EnsureSwordStatesReturnToNull(FindSubStateMachine(weak, "SwordAttack"), empty);
            EnsureSwordStatesReturnToNull(FindSubStateMachine(weak, "SwordRandomAttack"), empty);
            EnsureSwordStatesReturnToNull(FindSubStateMachine(strong, "SwordAttack"), empty);

            AnimatorState parry = FindParryWindupState(fullBody.stateMachine);
            if (parry != null)
                EnsureReturnTransition(parry, empty, 0.9f);
            AnimatorState parryHit = FindStateRecursive(fullBody.stateMachine, "Parry01_Hit");
            if (parryHit != null)
                EnsureReturnTransition(parryHit, empty, 0.9f);
            AnimatorState interactFull = FindStateRecursive(attacks, "InteractHoldCombo");
            if (interactFull != null)
                EnsureReturnTransition(interactFull, empty, 0.9f);

            AnimatorControllerLayer upper = FindLayer(controller, "UpperBody");
            if (upper != null && upper.stateMachine != null && upper.stateMachine.defaultState != null)
            {
                AnimatorState interact = FindStateRecursive(upper.stateMachine, "InteractHoldCombo");
                if (interact != null)
                    EnsureReturnTransition(interact, upper.stateMachine.defaultState, 0.9f);
            }
        }

        private static void EnsureSwordStatesReturnToNull(AnimatorStateMachine sword, AnimatorState nullState)
        {
            if (sword == null || nullState == null)
                return;

            EnsureReturnTransition(FindStateInMachine(sword, "A"), nullState, 0.98f);
            EnsureReturnTransition(FindStateInMachine(sword, "B"), nullState, 0.98f);
            EnsureReturnTransition(FindStateInMachine(sword, "C"), nullState, 0.98f);
        }

        private static void EnsureIdleEmptyStates(
            AnimatorControllerLayer fullBody,
            AnimatorStateMachine attacks)
        {
            if (fullBody != null && fullBody.stateMachine != null)
            {
                AnimatorState rootEmpty = GetOrCreateState(fullBody.stateMachine, "Idle_Empty");
                rootEmpty.motion = null;
                rootEmpty.writeDefaultValues = false;
            }

            if (attacks != null)
            {
                AnimatorState attacksEmpty = GetOrCreateState(attacks, "Idle_Empty");
                attacksEmpty.motion = null;
                attacksEmpty.writeDefaultValues = false;
            }
        }

        private static void RemoveUnconditionalAttackSmReturns(
            AnimatorStateMachine parent,
            AnimatorStateMachine child)
        {
            if (parent == null || child == null)
                return;

            AnimatorTransition[] existing = parent.GetStateMachineTransitions(child);
            for (int i = 0; i < existing.Length; i++)
            {
                AnimatorTransition transition = existing[i];
                if (transition == null || transition.conditions.Length > 0)
                    continue;
                parent.RemoveStateMachineTransition(child, transition);
            }
        }

        private static AnimatorState FindStateInMachine(AnimatorStateMachine sm, string name)
        {
            if (sm == null)
                return null;

            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state != null && child.state.name == name)
                    return child.state;
            }

            return null;
        }

        private static void EnsureChildStateMachineReturnToNull(
            AnimatorStateMachine parent,
            AnimatorStateMachine child,
            AnimatorState nullState)
        {
            if (parent == null || child == null || nullState == null)
                return;

            AnimatorTransition[] existing = parent.GetStateMachineTransitions(child);
            for (int i = 0; i < existing.Length; i++)
            {
                AnimatorTransition transition = existing[i];
                if (transition == null)
                    continue;
                if (transition.destinationState == nullState)
                {
                    ClearTransitionConditions(transition);
                    return;
                }
            }

            AnimatorTransition added = parent.AddStateMachineTransition(child, nullState);
            if (added != null)
                ClearTransitionConditions(added);
        }

        private static AnimatorState FindAttacksNullState(AnimatorController controller)
        {
            AnimatorControllerLayer fullBody = FindLayer(controller, "FullBody");
            if (fullBody == null)
                return null;

            AnimatorStateMachine attacks = FindSubStateMachine(fullBody.stateMachine, "Attacks");
            if (attacks == null)
                return null;

            AnimatorStateMachine nullSm = FindSubStateMachine(attacks, "Null");
            if (nullSm != null)
            {
                foreach (ChildAnimatorState child in nullSm.states)
                {
                    if (child.state != null
                        && (child.state.name == "Null" || child.state.name == "null"))
                        return child.state;
                }

                if (nullSm.defaultState != null)
                    return nullSm.defaultState;
            }

            return FindStateRecursive(attacks, "Null");
        }

        private static void EnsureExitTransition(AnimatorState state, float exitTime)
        {
            if (state == null)
                return;

            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null && transitions[i].isExit)
                    return;
            }

            AnimatorStateTransition exit = state.AddExitTransition();
            exit.hasExitTime = true;
            exit.exitTime = exitTime;
            exit.duration = 0.1f;
            exit.hasFixedDuration = true;
            exit.hasExitTime = true;
        }

        private static void EnsureAttackBehaviour(AnimatorState state, float start, float end)
        {
            if (state == null)
                return;

            StateMachineBehaviour[] behaviours = state.behaviours;
            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is vMeleeAttackControl)
                    return;
            }

            vMeleeAttackControl control = state.AddStateMachineBehaviour<vMeleeAttackControl>();
            control.startDamage = start;
            control.endDamage = end;
            control.meleeAttackType = vAttackType.MeleeWeapon;
            control.bodyParts = new List<string> { "RightLowerArm" };
        }

        private static void CopyMeleeEventsFromTemplate(AnimationClip target, AnimationClip template)
        {
            if (target == null || template == null)
                return;

            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(template);
            if (events == null || events.Length == 0)
                return;

            AnimationUtility.SetAnimationEvents(target, events);
            EditorUtility.SetDirty(target);
        }

        /// <summary>
        /// StrongAttacks/SwordCharge + Strong SwordAttack B only — never weak combo states.
        /// </summary>
        private static void ApplyStrongMeleeAnimSpeedFromProfile(AnimatorController controller)
        {
            if (controller == null)
                return;

            DM_CombatCoreProfile profile = AssetDatabase.LoadAssetAtPath<DM_CombatCoreProfile>(
                "Assets/_Project/Resources/Combat/DM_CombatCoreProfile.asset");
            AnimatorControllerLayer fullBody = FindLayer(controller, "FullBody");
            if (fullBody == null)
                return;

            AnimatorStateMachine attacks = FindSubStateMachine(fullBody.stateMachine, "Attacks");
            AnimatorStateMachine strongAttacks = attacks != null ? FindSubStateMachine(attacks, "StrongAttacks") : null;
            if (strongAttacks == null)
                return;

            // Play mode reads DM_CombatCoreProfile.Live via PioneerMeleeDamageWindowTracker (light/strong per-slot Animator.speed).
            AnimatorState charge = GetOrCreateState(strongAttacks, "SwordCharge");
            charge.speed = 1f;

            AnimatorStateMachine swordStrong = FindSubStateMachine(strongAttacks, "SwordAttack");
            if (swordStrong != null)
            {
                AnimatorState strongB = GetOrCreateState(swordStrong, "B");
                strongB.speed = 1f;
            }
        }

        /// <summary>Invector weak combo — WeakAttacks/SwordAttack A→B→C only.</summary>
        private static void ApplyWeakSwordComboClips(AnimatorStateMachine swordCombo, DMMeleeClipSlots slots)
        {
            AssignSlotStates(swordCombo, slots, useStrongSlots: false);
        }

        /// <summary>
        /// SwordRandomAttack must not reuse WeakAttack_SwordA/B/C (those live on chained SwordAttack).
        /// </summary>
        private static void ApplySwordRandomParallelClips(AnimatorStateMachine swordRandom)
        {
            if (swordRandom == null)
                return;

            SetStateMotion(swordRandom, "A", LoadFirstClip(RandomLightAPath));
            SetStateMotion(swordRandom, "B", LoadFirstClip(RandomLightBPath));
            SetStateMotion(swordRandom, "C", LoadFirstClip(RandomLightCPath));
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

        /// <summary>
        /// AttackID 1 (one-hand) must enter Weak SwordAttack so Invector can chain A→B→C.
        /// SwordRandomAttack is not the WeakAttack entry dest (combo uses SwordAttack).
        /// </summary>
        private static void RouteWeakAttackIdToSwordAttack(
            AnimatorStateMachine weakAttacks,
            AnimatorStateMachine swordCombo)
        {
            if (weakAttacks == null || swordCombo == null)
                return;

            AnimatorTransition[] entries = weakAttacks.entryTransitions;
            for (int i = 0; i < entries.Length; i++)
            {
                AnimatorTransition transition = entries[i];
                if (transition == null || !TransitionHasIntEquals(transition, "AttackID", 1))
                    continue;

                transition.destinationStateMachine = swordCombo;
                transition.destinationState = null;
                transition.isExit = false;
            }
        }

        private static bool TransitionHasIntEquals(AnimatorTransition transition, string parameter, int value)
        {
            AnimatorCondition[] conditions = transition.conditions;
            for (int i = 0; i < conditions.Length; i++)
            {
                AnimatorCondition condition = conditions[i];
                if (condition.mode == AnimatorConditionMode.Equals
                    && condition.parameter == parameter
                    && Mathf.Approximately(condition.threshold, value))
                    return true;
            }

            return false;
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

        private static AnimatorState FindParryWindupState(AnimatorStateMachine root)
        {
            if (root == null)
                return null;

            string[] preferred =
            {
                "Parry01",
                "Parry",
                "1H-RH@Parry01",
                "1H@Parry01"
            };

            for (int i = 0; i < preferred.Length; i++)
            {
                AnimatorState named = FindStateRecursive(root, preferred[i]);
                if (named != null)
                    return named;
            }

            return FindParryWindupRecursive(root);
        }

        private static AnimatorState FindParryWindupRecursive(AnimatorStateMachine sm)
        {
            if (sm == null)
                return null;

            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state == null)
                    continue;

                string name = child.state.name;
                if (name.IndexOf("Parry", System.StringComparison.OrdinalIgnoreCase) >= 0
                    && name.IndexOf("Hit", System.StringComparison.OrdinalIgnoreCase) < 0)
                    return child.state;
            }

            foreach (ChildAnimatorStateMachine child in sm.stateMachines)
            {
                AnimatorState found = FindParryWindupRecursive(child.stateMachine);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static AnimatorState FindStateRecursive(AnimatorStateMachine sm, string name)
        {
            if (sm == null || string.IsNullOrEmpty(name))
                return null;

            foreach (ChildAnimatorState child in sm.states)
            {
                if (child.state != null && child.state.name == name)
                    return child.state;
            }

            foreach (ChildAnimatorStateMachine child in sm.stateMachines)
            {
                AnimatorState found = FindStateRecursive(child.stateMachine, name);
                if (found != null)
                    return found;
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

        private static void AssignInteractMotion(AnimatorController controller, AnimationClip clip)
        {
            if (controller == null || clip == null)
                return;

            AnimatorControllerLayer layer = FindLayer(controller, "FullBody");
            if (layer == null)
                return;

            AnimatorStateMachine attacks = FindSubStateMachine(layer.stateMachine, "Attacks");
            if (attacks == null)
                return;

            foreach (ChildAnimatorState child in attacks.states)
            {
                if (child.state != null && child.state.name == "InteractHoldCombo")
                    child.state.motion = clip;
            }
        }

        /// <summary>
        /// The Hold E combo ships as a Generic Mixamo take. On the humanoid player that bakes hip translation into the mesh.
        /// Humanoid import lets the upper-body mask and curve strip keep root and legs on the walk.
        /// </summary>
        private static void EnsureHumanoidImport(string assetPath)
        {
            ModelImporter importer = AssetImporter.GetAtPath(assetPath) as ModelImporter;
            if (importer == null || importer.animationType == ModelImporterAnimationType.Human)
                return;

            importer.animationType = ModelImporterAnimationType.Human;
            importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
            importer.SaveAndReimport();
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

        private static AnimationClip LoadClipByName(string assetPath, string clipName)
        {
            if (string.IsNullOrEmpty(clipName))
                return null;

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip
                    && clip.name == clipName
                    && !clip.name.StartsWith("__"))
                    return clip;
            }

            return null;
        }

    }
}
