using MalbersAnimations;
using MalbersAnimations.Controller.AI;
using Project.Creatures;
using Project.Creatures.Brain;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Creates Sulfur Hound MAIState graph + DMI task/decision assets under Data/Creatures/Brain.
    /// </summary>
    public static class DMICreatureBrainAssetBuilder
    {
        public const string PoisonSpitPrefabPath =
            "Assets/_Project/Prefabs/Particles/Poison Spit.prefab";

        public const string AttackModeIdPath =
            "Assets/Malbers Animations/Common/Scriptables/ID/Mode/ModeID Attack.asset";

        /// <summary>
        /// Patrol ↔ Chase ↔ Melee/Spit with proper disengage (HasThreat).
        /// Does not jump into stock Malbers Attack states that leave our graph.
        /// </summary>
        public static MAIState EnsureSulfurHoundBrainGraph(out string startStatePath)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainData);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainTasks);
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.CreaturesBrainDecisions);

            DMISetThreatTargetTask setThreat = EnsureTaskAsset<DMISetThreatTargetTask>(
                $"{ProjectAssetPaths.CreaturesBrainTasks}/DMI_SetThreatTarget.asset");
            setThreat.moveToTarget = true;
            EditorUtility.SetDirty(setThreat);

            DMISpitSpecialTask spitTask = EnsureTaskAsset<DMISpitSpecialTask>(
                $"{ProjectAssetPaths.CreaturesBrainTasks}/DMI_SpitSpecial.asset");
            spitTask.alignToTarget = true;
            EditorUtility.SetDirty(spitTask);

            PlayModeTask meleeAttack = EnsureTaskAsset<PlayModeTask>(
                $"{ProjectAssetPaths.CreaturesBrainTasks}/DMI_PlayMeleeAttack.asset");
            ConfigurePlayModeAttack(meleeAttack);

            DMIHasThreatDecision hasThreat = EnsureDecisionAsset<DMIHasThreatDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_HasThreat.asset");

            DMIThreatInMeleeRangeDecision inMelee = EnsureDecisionAsset<DMIThreatInMeleeRangeDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_ThreatInMeleeRange.asset");
            inMelee.fallbackMeleeRange = 2.75f;
            EditorUtility.SetDirty(inMelee);

            DMIIsValidSpitTargetDecision validSpit = EnsureDecisionAsset<DMIIsValidSpitTargetDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_IsValidSpitTarget.asset");

            DMIChanceWeightedDecision chance = EnsureDecisionAsset<DMIChanceWeightedDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_ChanceWeightedSpit.asset");
            chance.baseChance = 0.12f;
            chance.viewBoostedChance = 0.45f;
            chance.useDefinitionSpitChances = true;
            EditorUtility.SetDirty(chance);

            EnsureDecisionAsset<DMIInPlayerViewDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_InPlayerView.asset");

            DMIChanceWeightedDecision always = EnsureDecisionAsset<DMIChanceWeightedDecision>(
                $"{ProjectAssetPaths.CreaturesBrainDecisions}/DMI_AlwaysTrue.asset");
            always.baseChance = 1f;
            always.viewBoostedChance = 1f;
            always.useDefinitionSpitChances = false;
            EditorUtility.SetDirty(always);

            MAIState chaseState = EnsureStateAsset(
                $"{ProjectAssetPaths.CreaturesBrainData}/DMI_SulfurHound_Chase.asset",
                "DMI Sulfur Hound Chase",
                new Color(1f, 0.55f, 0.1f));

            MAIState spitState = EnsureStateAsset(
                $"{ProjectAssetPaths.CreaturesBrainData}/DMI_SulfurHound_Spit.asset",
                "DMI Sulfur Hound Spit",
                new Color(0.35f, 0.9f, 0.25f));

            MAIState meleeState = EnsureStateAsset(
                $"{ProjectAssetPaths.CreaturesBrainData}/DMI_SulfurHound_Melee.asset",
                "DMI Sulfur Hound Melee",
                new Color(0.95f, 0.25f, 0.2f));

            MAIState patrolState = EnsureStateAsset(
                $"{ProjectAssetPaths.CreaturesBrainData}/DMI_SulfurHound_Patrol.asset",
                "DMI Sulfur Hound Patrol",
                new Color(0.2f, 0.85f, 0.45f));

            // Patrol: only engage when bridge reports an active threat.
            patrolState.tasks = new MTask[] { setThreat };
            patrolState.transitions = new[]
            {
                new MAITransition
                {
                    decision = hasThreat,
                    trueState = chaseState,
                    falseState = patrolState
                }
            };

            // Chase: drop to patrol when leashed off; spit when ready; melee when close.
            chaseState.tasks = new MTask[] { setThreat };
            chaseState.transitions = new[]
            {
                new MAITransition
                {
                    decision = hasThreat,
                    trueState = chaseState,
                    falseState = patrolState
                },
                new MAITransition
                {
                    decision = validSpit,
                    trueState = spitState,
                    falseState = chaseState
                },
                new MAITransition
                {
                    decision = inMelee,
                    trueState = meleeState,
                    falseState = chaseState
                }
            };

            // Spit: one-shot then back to chase (or patrol if threat lost).
            spitState.tasks = new MTask[] { spitTask };
            spitState.transitions = new[]
            {
                new MAITransition
                {
                    decision = hasThreat,
                    trueState = chaseState,
                    falseState = patrolState
                },
                new MAITransition
                {
                    decision = always,
                    trueState = chaseState,
                    falseState = chaseState
                }
            };

            // Melee: stay in our graph — PlayMode attack + return to chase when out of range.
            meleeState.tasks = new MTask[] { setThreat, meleeAttack };
            meleeState.transitions = new[]
            {
                new MAITransition
                {
                    decision = hasThreat,
                    trueState = meleeState,
                    falseState = patrolState
                },
                new MAITransition
                {
                    decision = inMelee,
                    trueState = meleeState,
                    falseState = chaseState
                },
                new MAITransition
                {
                    decision = validSpit,
                    trueState = spitState,
                    falseState = meleeState
                }
            };

            EditorUtility.SetDirty(chaseState);
            EditorUtility.SetDirty(spitState);
            EditorUtility.SetDirty(meleeState);
            EditorUtility.SetDirty(patrolState);
            AssetDatabase.SaveAssets();

            startStatePath = AssetDatabase.GetAssetPath(patrolState);
            return patrolState;
        }

        public static GameObject LoadPoisonSpitPrefab()
        {
            return DMICreatureParticleCatalog.LoadPoisonSpitPrefab();
        }

        private static void ConfigurePlayModeAttack(PlayModeTask task)
        {
            if (task == null)
                return;

            ModeID attackMode = FindAttackModeId();
            SerializedObject so = new SerializedObject(task);
            SerializedProperty modeProp = so.FindProperty("modeID");
            if (modeProp != null && attackMode != null)
                modeProp.objectReferenceValue = attackMode;

            SerializedProperty near = so.FindProperty("near");
            if (near != null)
                near.boolValue = true;

            // PlayWhen.PlayForever
            SerializedProperty play = so.FindProperty("Play");
            if (play != null)
                play.enumValueIndex = (int)PlayModeTask.PlayWhen.PlayForever;

            SerializedProperty lookAt = so.FindProperty("lookAtAlign");
            if (lookAt != null)
                lookAt.boolValue = true;

            SerializedProperty coolDown = so.FindProperty("CoolDown");
            if (coolDown != null)
            {
                SerializedProperty useConstant = coolDown.FindPropertyRelative("UseConstant");
                SerializedProperty constantValue = coolDown.FindPropertyRelative("ConstantValue");
                if (useConstant != null)
                    useConstant.boolValue = true;
                if (constantValue != null)
                    constantValue.floatValue = 1.5f;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(task);
        }

        private static ModeID FindAttackModeId()
        {
            ModeID direct = AssetDatabase.LoadAssetAtPath<ModeID>(AttackModeIdPath);
            if (direct != null)
                return direct;

            // Fallback: resolve by known guid used by Wolf Lite "Play Main Attack".
            string path = AssetDatabase.GUIDToAssetPath("1286867ad3c4cdd4baacac3373aff92c");
            if (!string.IsNullOrEmpty(path))
                return AssetDatabase.LoadAssetAtPath<ModeID>(path);

            string[] guids = AssetDatabase.FindAssets("t:ModeID Attack");
            for (int i = 0; i < guids.Length; i++)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                ModeID mode = AssetDatabase.LoadAssetAtPath<ModeID>(assetPath);
                if (mode != null && mode.name.IndexOf("Attack", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    return mode;
            }

            return null;
        }

        private static T EnsureTaskAsset<T>(string path) where T : MTask
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static T EnsureDecisionAsset<T>(string path) where T : MAIDecision
        {
            T existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            T created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }

        private static MAIState EnsureStateAsset(string path, string stateName, Color gizmoColor)
        {
            MAIState existing = AssetDatabase.LoadAssetAtPath<MAIState>(path);
            if (existing != null)
            {
                existing.GizmoStateColor = gizmoColor;
                return existing;
            }

            MAIState created = ScriptableObject.CreateInstance<MAIState>();
            created.GizmoStateColor = gizmoColor;
            created.tasks = System.Array.Empty<MTask>();
            created.transitions = System.Array.Empty<MAITransition>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
