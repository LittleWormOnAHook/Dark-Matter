using System.Collections.Generic;
using System.Text;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Player
{
    /// <summary>
    /// Removes orphan animator sub-assets that crash the Animator graph editor (Edge.WakeUp NRE).
    /// </summary>
    [InitializeOnLoad]
    public static class AnimatorControllerGraphRepairUtility
    {
        private const string AutoRepairSessionKey =
            "AnimatorControllerGraphRepairUtility.AutoRepairComplete.v6";

        private const string UpperBodyMaskPath =
            "Assets/Animations/Human Animations Melee/Human Animations/Models/Avatar Masks/Human Body Upper Mask.mask";

        static AnimatorControllerGraphRepairUtility()
        {
            EditorApplication.delayCall += TryAutoRepairAnimatorGraphOnce;
        }

        [UnityEditor.Callbacks.DidReloadScripts]
        private static void OnScriptsReloaded()
        {
            EditorApplication.delayCall += TryAutoRepairAnimatorGraphOnce;
        }

        private static void TryAutoRepairAnimatorGraphOnce()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return;

            if (SessionState.GetBool(AutoRepairSessionKey, false))
                return;

            if (!IsAutoRepairSupported(PlayerControllerPath))
            {
                SessionState.SetBool(AutoRepairSessionKey, true);
                return;
            }

            int fixedCount = RepairPlayerAnimatorControllerGraphSilent();
            if (fixedCount <= 0)
            {
                SessionState.SetBool(AutoRepairSessionKey, true);
                return;
            }

            if (!HasAnimatorGraphIssues(PlayerControllerPath))
            {
                SessionState.SetBool(AutoRepairSessionKey, true);
                Debug.Log(
                    $"Auto-repaired animator controller graph ({fixedCount} item(s)) " +
                    $"in {PlayerControllerPath}.");
                return;
            }

            Debug.LogWarning(
                $"AnimatorControllerGraphRepairUtility: graph issues remain in {PlayerControllerPath} " +
                $"after auto-repair ({fixedCount} item(s) fixed). " +
                "Run Tools > Survival Pioneer > Maintenance > Repair Animator Controller Graph.");
        }

        public static bool HasAnimatorGraphIssues(string controllerPath)
        {
            if (!IsDestructiveRepairSupported(controllerPath))
                return false;

            if (HasMissingUpperBodyCombatStateMachine(controllerPath))
                return true;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return false;

            List<AnimatorStateTransition> broken = new List<AnimatorStateTransition>();
            for (int i = 0; i < controller.layers.Length; i++)
                CollectBrokenTransitions(controller.layers[i].stateMachine, broken);

            if (broken.Count > 0)
                return true;

            return CountOrphanSubAssets(controllerPath) > 0;
        }

        private static int CountOrphanSubAssets(string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return 0;

            HashSet<Object> referenced = new HashSet<Object>();
            referenced.Add(controller);

            for (int i = 0; i < controller.layers.Length; i++)
                CollectStateMachine(controller.layers[i].stateMachine, referenced);

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
            int orphanCount = 0;
            for (int i = 0; i < subAssets.Length; i++)
            {
                Object asset = subAssets[i];
                if (asset == null || asset == controller || referenced.Contains(asset))
                    continue;

                if (asset is AnimatorStateTransition
                    || asset is AnimatorState
                    || asset is AnimatorStateMachine
                    || asset is BlendTree)
                {
                    orphanCount++;
                }
            }

            return orphanCount;
        }

        public static bool HasMissingUpperBodyCombatStateMachine(string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return false;

            int layerIndex = FindLayerIndex(controller, UpperBodyCombatLayerName);
            return layerIndex >= 0 && controller.layers[layerIndex].stateMachine == null;
        }

        public static int RepairPlayerAnimatorControllerGraphSilent()
        {
            if (!IsDestructiveRepairSupported(PlayerControllerPath))
                return 0;

            int orphans = RemoveOrphanSubAssets(PlayerControllerPath);
            int duplicateStates = RemoveDuplicateBaseLayerCombatStates(PlayerControllerPath);
            int rebuiltStates = HasMissingUpperBodyCombatStateMachine(PlayerControllerPath)
                ? RebuildUpperBodyCombatLayer(PlayerControllerPath)
                : 0;
            int brokenTransitions = RemoveBrokenTransitions(PlayerControllerPath);
            orphans += RemoveOrphanSubAssets(PlayerControllerPath);
            return duplicateStates + rebuiltStates + brokenTransitions + orphans;
        }
        private const string PlayerControllerPath = PlayerAnimatorControllerPaths.LegacyControllerPath;

        private static bool IsAutoRepairSupported(string controllerPath)
        {
            return controllerPath == PlayerAnimatorControllerPaths.LegacyControllerPath;
        }

        private static bool IsDestructiveRepairSupported(string controllerPath)
        {
            // GKC is a large third-party controller; orphan cleanup misidentifies valid sub-assets.
            return IsAutoRepairSupported(controllerPath);
        }

        private const string UpperBodyCombatLayerName = GkcAnimatorConstants.UpperBodyCombatLayer;
        private const string EmptyStateName = "Empty";

        private const float AttackExitTime = 0.88f;
        private const float AttackTransitionDuration = 0.22f;

        private const string OneHandCombatFolder =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/1H";

        private const string TwoHandCombatFolder =
            "Assets/Animations/Human Animations Melee/Human Animations/Animations/Male/Combat/2H";

        private static readonly (string StateName, string AssetPath, string ClipName)[] DefaultCombatStateClips =
        {
            ("AttackCombo1", $"{OneHandCombatFolder}/HumanM@Attack1H01_R.fbx", "HumanM@Attack1H01_R"),
            ("AttackCombo2", $"{OneHandCombatFolder}/HumanM@Attack1H02_R.fbx", "HumanM@Attack1H02_R"),
            ("AttackCombo3", $"{OneHandCombatFolder}/HumanM@Attack1H03_R.fbx", "HumanM@Attack1H03_R"),
            ("AttackCombo4", $"{OneHandCombatFolder}/HumanM@Attack1H04_R.fbx", "HumanM@Attack1H04_R"),
            ("AttackCombo5", $"{OneHandCombatFolder}/HumanM@AttackDW02.fbx", "HumanM@AttackDW02"),
            ("TwoHandAttack1", $"{TwoHandCombatFolder}/HumanM@Attack2H01.fbx", "HumanM@Attack2H01"),
            ("TwoHandAttack2", $"{TwoHandCombatFolder}/HumanM@Attack2H02.fbx", "HumanM@Attack2H02"),
            ("TwoHandAttack3", $"{TwoHandCombatFolder}/HumanM@Attack2H03.fbx", "HumanM@Attack2H03"),
            ("TwoHandAttack4", $"{TwoHandCombatFolder}/HumanM@Attack2H04.fbx", "HumanM@Attack2H04"),
            ("TwoHandPowerHit", $"{TwoHandCombatFolder}/HumanM@Attack2H04.fbx", "HumanM@Attack2H04")
        };

        private struct CombatStateSnapshot
        {
            public string Name;
            public Vector3 Position;
            public Motion Motion;
            public float Speed;
            public string Tag;
            public bool WriteDefaultValues;
            public bool SpeedParameterActive;
            public string SpeedParameter;
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Repair Animator Controller Graph (Silent)", false, 18)]
        public static void RepairPlayerAnimatorControllerGraphSilentMenu()
        {
            EditorLayoutGuard.ClearSelectionOnly();
            int total = RepairPlayerAnimatorControllerGraphSilent();
            Debug.Log(
                total > 0
                    ? $"Silent animator graph repair updated {total} item(s) in {PlayerControllerPath}."
                    : $"Silent animator graph repair found no changes in {PlayerControllerPath}.");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Repair Animator Controller Graph", false, 17)]
        public static void RepairPlayerAnimatorControllerGraphMenu()
        {
            EditorLayoutGuard.ClearSelectionOnly();

            int duplicateStates = RemoveDuplicateBaseLayerCombatStates(PlayerControllerPath);
            int orphansBefore = RemoveOrphanSubAssets(PlayerControllerPath);
            int rebuiltStates = RebuildUpperBodyCombatLayer(PlayerControllerPath);
            int brokenTransitions = RemoveBrokenTransitions(PlayerControllerPath);
            int orphansAfter = RemoveOrphanSubAssets(PlayerControllerPath);
            int orphans = orphansBefore + orphansAfter;
            int total = duplicateStates + rebuiltStates + brokenTransitions + orphans;

            if (total > 0)
            {
                Debug.Log(
                    $"Repaired animator controller graph in {PlayerControllerPath}: " +
                    $"{duplicateStates} duplicate base-layer combat state(s), " +
                    $"{rebuiltStates} rebuilt upper-body combat state(s), " +
                    $"{brokenTransitions} broken transition(s), " +
                    $"{orphans} orphan sub-asset(s).");
                EditorUtility.DisplayDialog(
                    "Animator Graph Repair",
                    $"Removed {duplicateStates} duplicate base-layer combat state(s).\n" +
                    $"Rebuilt {rebuiltStates} upper-body combat state(s).\n" +
                    $"Removed {brokenTransitions} broken transition(s).\n" +
                    $"Removed {orphans} orphan sub-asset(s).\n\n" +
                    "The Animator window should open without errors now.",
                    "OK");
                return;
            }

            if (HasAnimatorGraphIssues(PlayerControllerPath))
            {
                EditorUtility.DisplayDialog(
                    "Animator Graph Repair",
                    "Upper Body Combat layer is still missing its state machine, or orphan graph assets remain. " +
                    "Check the Console for clip load warnings.",
                    "OK");
                return;
            }

            EditorUtility.DisplayDialog(
                "Animator Graph Repair",
                "No animator graph issues found.",
                "OK");
        }

        public static int RemoveDuplicateBaseLayerCombatStates(string controllerPath)
        {
            if (!IsDestructiveRepairSupported(controllerPath))
                return 0;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null || controller.layers.Length == 0)
                return 0;

            AnimatorStateMachine combatMachine = FindLayerStateMachine(controller, UpperBodyCombatLayerName);
            if (combatMachine == null)
                return 0;

            HashSet<string> combatStateNames = new HashSet<string>();
            foreach (ChildAnimatorState child in combatMachine.states)
            {
                if (child.state != null)
                    combatStateNames.Add(child.state.name);
            }

            AnimatorStateMachine baseMachine = controller.layers[0].stateMachine;
            List<AnimatorState> toRemove = new List<AnimatorState>();
            foreach (ChildAnimatorState child in baseMachine.states)
            {
                if (child.state != null && combatStateNames.Contains(child.state.name))
                    toRemove.Add(child.state);
            }

            if (toRemove.Count == 0)
                return 0;

            StringBuilder log = new StringBuilder();
            log.AppendLine(
                $"Removing {toRemove.Count} duplicate combat state(s) from base layer in {controllerPath}:");
            for (int i = 0; i < toRemove.Count; i++)
            {
                log.AppendLine($"  - {toRemove[i].name}");
                baseMachine.RemoveState(toRemove[i]);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
            return toRemove.Count;
        }

        /// <summary>
        /// Replaces the Upper Body Combat layer state machine so negative fileID sub-assets no longer crash Edge.WakeUp.
        /// </summary>
        public static int RebuildUpperBodyCombatLayer(string controllerPath)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"AnimatorControllerGraphRepairUtility: Missing controller at {controllerPath}");
                return 0;
            }

            int layerIndex = FindLayerIndex(controller, UpperBodyCombatLayerName);
            if (layerIndex < 0)
                return 0;

            AnimatorControllerLayer existingLayer = controller.layers[layerIndex];
            AnimatorStateMachine oldMachine = existingLayer.stateMachine;

            List<CombatStateSnapshot> snapshots = SnapshotNonEmptyCombatStates(oldMachine);
            if (snapshots.Count == 0)
                snapshots = SnapshotOrphanCombatStates(controllerPath);
            if (snapshots.Count == 0)
                snapshots = BuildDefaultCombatSnapshots();

            AvatarMask avatarMask = existingLayer.avatarMask;
            if (avatarMask == null)
            {
                avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(UpperBodyMaskPath);
            }

            float defaultWeight = existingLayer.defaultWeight;
            AnimatorLayerBlendingMode blendingMode = existingLayer.blendingMode;
            int syncedLayerIndex = existingLayer.syncedLayerIndex;
            bool ikPass = existingLayer.iKPass;

            if (oldMachine != null)
                Object.DestroyImmediate(oldMachine, true);

            controller.RemoveLayer(layerIndex);

            AnimatorStateMachine newMachine = new AnimatorStateMachine
            {
                name = UpperBodyCombatLayerName,
                hideFlags = HideFlags.HideInHierarchy
            };
            AssetDatabase.AddObjectToAsset(newMachine, controllerPath);

            AnimatorControllerLayer newLayer = new AnimatorControllerLayer
            {
                name = UpperBodyCombatLayerName,
                stateMachine = newMachine,
                avatarMask = avatarMask,
                defaultWeight = defaultWeight,
                blendingMode = blendingMode,
                syncedLayerIndex = syncedLayerIndex,
                iKPass = ikPass
            };
            controller.AddLayer(newLayer);

            AnimatorState emptyState = newMachine.AddState(EmptyStateName, new Vector3(280f, 120f, 0f));
            emptyState.writeDefaultValues = false;
            newMachine.defaultState = emptyState;

            for (int i = 0; i < snapshots.Count; i++)
            {
                CombatStateSnapshot snapshot = snapshots[i];
                AnimatorState state = newMachine.AddState(snapshot.Name, snapshot.Position);
                state.motion = snapshot.Motion;
                state.speed = snapshot.Speed;
                state.tag = snapshot.Tag;
                state.writeDefaultValues = snapshot.WriteDefaultValues;
                state.speedParameterActive = snapshot.SpeedParameterActive;
                state.speedParameter = snapshot.SpeedParameter;
                EnsureEmptyTransition(state, emptyState);
            }

            EditorUtility.SetDirty(newMachine);
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log(
                $"Rebuilt Upper Body Combat layer in {controllerPath}: " +
                $"{snapshots.Count} attack state(s) with fresh sub-asset IDs.");
            return snapshots.Count;
        }

        private static List<CombatStateSnapshot> SnapshotOrphanCombatStates(string controllerPath)
        {
            List<CombatStateSnapshot> snapshots = new List<CombatStateSnapshot>();
            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
            for (int i = 0; i < subAssets.Length; i++)
            {
                if (subAssets[i] is not AnimatorState state || state.motion == null)
                    continue;

                if (state.name == EmptyStateName || !IsCombatStateName(state.name))
                    continue;

                snapshots.Add(new CombatStateSnapshot
                {
                    Name = state.name,
                    Position = new Vector3(540f, 40f - snapshots.Count * 56f, 0f),
                    Motion = state.motion,
                    Speed = state.speed,
                    Tag = string.IsNullOrEmpty(state.tag) ? "Attack" : state.tag,
                    WriteDefaultValues = state.writeDefaultValues,
                    SpeedParameterActive = state.speedParameterActive,
                    SpeedParameter = state.speedParameter
                });
            }

            return snapshots;
        }

        private static List<CombatStateSnapshot> BuildDefaultCombatSnapshots()
        {
            List<CombatStateSnapshot> snapshots = new List<CombatStateSnapshot>();
            for (int i = 0; i < DefaultCombatStateClips.Length; i++)
            {
                (string stateName, string assetPath, string clipName) = DefaultCombatStateClips[i];
                AnimationClip clip = LoadAnimationClip(assetPath, clipName);
                if (clip == null)
                {
                    Debug.LogWarning(
                        $"AnimatorControllerGraphRepairUtility: missing clip '{clipName}' at {assetPath}");
                    continue;
                }

                snapshots.Add(new CombatStateSnapshot
                {
                    Name = stateName,
                    Position = new Vector3(540f, 40f - i * 56f, 0f),
                    Motion = clip,
                    Speed = 1f,
                    Tag = "Attack",
                    WriteDefaultValues = true,
                    SpeedParameterActive = true,
                    SpeedParameter = "AttackAnimSpeed"
                });
            }

            return snapshots;
        }

        private static bool IsCombatStateName(string stateName)
        {
            for (int i = 0; i < DefaultCombatStateClips.Length; i++)
            {
                if (DefaultCombatStateClips[i].StateName == stateName)
                    return true;
            }

            return false;
        }

        private static AnimationClip LoadAnimationClip(string assetPath, string clipName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip &&
                    clip.name == clipName &&
                    !clip.name.StartsWith("__preview__", System.StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return null;
        }

        public static int RemoveBrokenTransitions(string controllerPath)
        {
            if (!IsDestructiveRepairSupported(controllerPath))
                return 0;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
                return 0;

            List<AnimatorStateTransition> broken = new List<AnimatorStateTransition>();
            for (int i = 0; i < controller.layers.Length; i++)
                CollectBrokenTransitions(controller.layers[i].stateMachine, broken);

            if (broken.Count == 0)
                return 0;

            StringBuilder log = new StringBuilder();
            log.AppendLine($"Removing {broken.Count} broken transition(s) from {controllerPath}:");
            for (int i = 0; i < broken.Count; i++)
            {
                AnimatorStateTransition transition = broken[i];
                if (transition == null)
                    continue;

                log.AppendLine($"  - {transition.name}");
                Object.DestroyImmediate(transition, true);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
            return broken.Count;
        }

        public static int RemoveOrphanSubAssets(string controllerPath)
        {
            if (!IsDestructiveRepairSupported(controllerPath))
                return 0;

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                Debug.LogError($"AnimatorControllerGraphRepairUtility: Missing controller at {controllerPath}");
                return 0;
            }

            HashSet<Object> referenced = new HashSet<Object>();
            referenced.Add(controller);

            for (int i = 0; i < controller.layers.Length; i++)
                CollectStateMachine(controller.layers[i].stateMachine, referenced);

            Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(controllerPath);
            List<Object> orphans = new List<Object>();
            for (int i = 0; i < subAssets.Length; i++)
            {
                Object asset = subAssets[i];
                if (asset == null || asset == controller || referenced.Contains(asset))
                    continue;

                if (asset is AnimatorStateTransition
                    || asset is AnimatorState
                    || asset is AnimatorStateMachine
                    || asset is BlendTree)
                {
                    orphans.Add(asset);
                }
            }

            if (orphans.Count == 0)
                return 0;

            StringBuilder log = new StringBuilder();
            log.AppendLine($"Removing {orphans.Count} orphan sub-asset(s) from {controllerPath}:");
            for (int i = 0; i < orphans.Count; i++)
                log.AppendLine($"  - {orphans[i].GetType().Name}: {orphans[i].name}");

            for (int i = 0; i < orphans.Count; i++)
                Object.DestroyImmediate(orphans[i], true);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            Debug.Log(log.ToString());
            return orphans.Count;
        }

        private static List<CombatStateSnapshot> SnapshotNonEmptyCombatStates(AnimatorStateMachine machine)
        {
            List<CombatStateSnapshot> snapshots = new List<CombatStateSnapshot>();
            if (machine == null)
                return snapshots;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                AnimatorState state = states[i].state;
                if (state == null || state.name == EmptyStateName)
                    continue;

                snapshots.Add(new CombatStateSnapshot
                {
                    Name = state.name,
                    Position = states[i].position,
                    Motion = state.motion,
                    Speed = state.speed,
                    Tag = state.tag,
                    WriteDefaultValues = state.writeDefaultValues,
                    SpeedParameterActive = state.speedParameterActive,
                    SpeedParameter = state.speedParameter
                });
            }

            return snapshots;
        }

        private static void EnsureEmptyTransition(AnimatorState attackState, AnimatorState emptyState)
        {
            AnimatorStateTransition[] transitions = attackState.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                AnimatorStateTransition transition = transitions[i];
                if (transition.destinationState == emptyState &&
                    transition.hasExitTime &&
                    Mathf.Approximately(transition.exitTime, AttackExitTime))
                    return;
            }

            AnimatorStateTransition newTransition = attackState.AddTransition(emptyState);
            newTransition.hasExitTime = true;
            newTransition.exitTime = AttackExitTime;
            newTransition.duration = AttackTransitionDuration;
            newTransition.hasFixedDuration = true;
            newTransition.canTransitionToSelf = false;
        }

        private static int FindLayerIndex(AnimatorController controller, string layerName)
        {
            for (int i = 0; i < controller.layers.Length; i++)
            {
                if (controller.layers[i].name == layerName)
                    return i;
            }

            return -1;
        }

        private static AnimatorStateMachine FindLayerStateMachine(AnimatorController controller, string layerName)
        {
            int layerIndex = FindLayerIndex(controller, layerName);
            return layerIndex >= 0 ? controller.layers[layerIndex].stateMachine : null;
        }

        private static void CollectBrokenTransitions(
            AnimatorStateMachine machine,
            List<AnimatorStateTransition> broken)
        {
            if (machine == null)
                return;

            foreach (ChildAnimatorState child in machine.states)
            {
                if (child.state == null)
                    continue;

                AnimatorStateTransition[] transitions = child.state.transitions;
                for (int i = transitions.Length - 1; i >= 0; i--)
                {
                    AnimatorStateTransition transition = transitions[i];
                    if (transition == null || transition.destinationState != null)
                        continue;

                    broken.Add(transition);
                    child.state.RemoveTransition(transition);
                }
            }

            AnimatorStateTransition[] anyStateTransitions = machine.anyStateTransitions;
            for (int i = anyStateTransitions.Length - 1; i >= 0; i--)
            {
                AnimatorStateTransition transition = anyStateTransitions[i];
                if (transition == null || transition.destinationState != null)
                    continue;

                broken.Add(transition);
                machine.RemoveAnyStateTransition(transition);
            }

            ChildAnimatorStateMachine[] childMachines = machine.stateMachines;
            for (int i = 0; i < childMachines.Length; i++)
            {
                if (childMachines[i].stateMachine != null)
                    CollectBrokenTransitions(childMachines[i].stateMachine, broken);
            }
        }

        private static void CollectStateMachine(AnimatorStateMachine machine, HashSet<Object> referenced)
        {
            if (machine == null || !referenced.Add(machine))
                return;

            ChildAnimatorState[] states = machine.states;
            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null)
                    CollectState(states[i].state, referenced);
            }

            ChildAnimatorStateMachine[] childMachines = machine.stateMachines;
            for (int i = 0; i < childMachines.Length; i++)
            {
                if (childMachines[i].stateMachine != null)
                    CollectStateMachine(childMachines[i].stateMachine, referenced);
            }

            AnimatorStateTransition[] anyStateTransitions = machine.anyStateTransitions;
            for (int i = 0; i < anyStateTransitions.Length; i++)
            {
                if (anyStateTransitions[i] != null)
                    referenced.Add(anyStateTransitions[i]);
            }

            AnimatorTransition[] entryTransitions = machine.entryTransitions;
            for (int i = 0; i < entryTransitions.Length; i++)
            {
                if (entryTransitions[i] != null)
                    referenced.Add(entryTransitions[i]);
            }
        }

        private static void CollectState(AnimatorState state, HashSet<Object> referenced)
        {
            if (state == null || !referenced.Add(state))
                return;

            AnimatorStateTransition[] transitions = state.transitions;
            for (int i = 0; i < transitions.Length; i++)
            {
                if (transitions[i] != null)
                    referenced.Add(transitions[i]);
            }

            CollectMotion(state.motion, referenced);
        }

        private static void CollectMotion(Motion motion, HashSet<Object> referenced)
        {
            if (motion == null || !referenced.Add(motion))
                return;

            if (motion is not BlendTree blendTree)
                return;

            ChildMotion[] children = blendTree.children;
            for (int i = 0; i < children.Length; i++)
                CollectMotion(children[i].motion, referenced);
        }
    }
}
