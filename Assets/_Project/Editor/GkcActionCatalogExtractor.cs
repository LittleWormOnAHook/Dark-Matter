using System.Collections.Generic;
using System.IO;
using Project.EditorTools;
using Project.Player;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Player
{
    public static class GkcActionCatalogExtractor
    {
        private const string OutputPath = "Assets/_Project/Data/Animation/GkcActionCatalog.asset";

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Extract GKC Action Catalog", false, 20)]
        public static void ExtractFromMenu()
        {
            if (!Extract(PlayerAnimatorControllerPaths.GkcControllerSourcePath, OutputPath, out string summary))
            {
                EditorUtility.DisplayDialog("GKC Action Catalog", summary, "OK");
                return;
            }

            EditorUtility.DisplayDialog("GKC Action Catalog", summary, "OK");
        }

        [MenuItem(SurvivalPioneerEditorMenus.Maintenance + "Reseed GKC Action Catalog (Verified IDs)", false, 21)]
        public static void ReseedFromVerifiedIds()
        {
            GkcActionCatalog catalog = AssetDatabase.LoadAssetAtPath<GkcActionCatalog>(OutputPath);
            if (catalog == null)
            {
                EditorUtility.DisplayDialog("GKC Action Catalog", $"Missing catalog at {OutputPath}", "OK");
                return;
            }

            catalog.SetEntries(GkcActionCatalogClassifier.BuildManualSeedEntries());
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            EditorUtility.DisplayDialog(
                "GKC Action Catalog",
                $"Reseeded {catalog.Entries.Count} verified combat entries to {OutputPath}.",
                "OK");
        }

        public static bool Extract(string controllerPath, string outputAssetPath, out string summary)
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
            if (controller == null)
            {
                summary = $"Could not load animator controller at {controllerPath}";
                return false;
            }

            var extracted = new List<GkcActionCatalogEntry>();
            var seen = new HashSet<string>();

            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                AnimatorControllerLayer layer = controller.layers[layerIndex];
                CollectTransitions(layer.stateMachine, layer.name, extracted, seen);
            }

            List<GkcActionCatalogEntry> merged = MergeWithSeed(extracted);
            merged.Sort((a, b) => a.combatAction.CompareTo(b.combatAction));

            string directory = Path.GetDirectoryName(outputAssetPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            GkcActionCatalog catalog = AssetDatabase.LoadAssetAtPath<GkcActionCatalog>(outputAssetPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GkcActionCatalog>();
                AssetDatabase.CreateAsset(catalog, outputAssetPath);
            }

            catalog.SetEntries(merged);
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            summary = $"Extracted {extracted.Count} transition rows, saved {merged.Count} catalog entries to {outputAssetPath}.";
            return true;
        }

        private static void CollectTransitions(
            AnimatorStateMachine stateMachine,
            string layerName,
            List<GkcActionCatalogEntry> output,
            HashSet<string> seen)
        {
            if (stateMachine == null)
                return;

            if (stateMachine.anyStateTransitions != null)
            {
                for (int i = 0; i < stateMachine.anyStateTransitions.Length; i++)
                    TryAddTransition(stateMachine.anyStateTransitions[i], layerName, output, seen);
            }

            foreach (ChildAnimatorState child in stateMachine.states)
                CollectStateTransitions(child.state, layerName, output, seen);

            foreach (ChildAnimatorStateMachine childMachine in stateMachine.stateMachines)
                CollectTransitions(childMachine.stateMachine, layerName, output, seen);
        }

        private static void CollectStateTransitions(
            AnimatorState state,
            string layerName,
            List<GkcActionCatalogEntry> output,
            HashSet<string> seen)
        {
            if (state == null || state.transitions == null)
                return;

            for (int i = 0; i < state.transitions.Length; i++)
                TryAddTransition(state.transitions[i], layerName, output, seen);
        }

        private static void TryAddTransition(
            AnimatorStateTransition transition,
            string layerName,
            List<GkcActionCatalogEntry> output,
            HashSet<string> seen)
        {
            if (transition == null || transition.conditions == null || transition.destinationState == null)
                return;

            bool hasActionId = false;
            bool requiresActionActive = false;
            bool requiresStrafeMode = false;
            int actionId = 0;

            for (int i = 0; i < transition.conditions.Length; i++)
            {
                AnimatorCondition condition = transition.conditions[i];
                if (condition.parameter == "Action ID" && condition.mode == AnimatorConditionMode.Equals)
                {
                    hasActionId = true;
                    actionId = Mathf.RoundToInt(condition.threshold);
                }

                if (condition.parameter == "Action Active" && condition.mode == AnimatorConditionMode.If)
                    requiresActionActive = true;

                if (condition.parameter == "Strafe Mode Active" && condition.mode == AnimatorConditionMode.If)
                    requiresStrafeMode = true;
            }

            if (!hasActionId)
                return;

            string stateName = transition.destinationState.name;
            GkcCombatAction combatAction = GkcActionCatalogClassifier.Classify(stateName, actionId);
            if (combatAction == GkcCombatAction.None)
                return;

            string key = $"{(int)combatAction}:{actionId}:{stateName}:{layerName}";
            if (!seen.Add(key))
                return;

            output.Add(new GkcActionCatalogEntry
            {
                combatAction = combatAction,
                actionId = actionId,
                stateName = stateName,
                layerName = layerName,
                requiresActionActive = requiresActionActive,
                useActionActiveUpperBody = combatAction is GkcCombatAction.Block
                    or GkcCombatAction.Charge1H
                    or GkcCombatAction.Charge2H
                    or GkcCombatAction.ChargeAxe,
                requiresStrafeMode = requiresStrafeMode,
                clearActionIdAfterTrigger = hasActionId && !requiresActionActive,
                weaponFilter = GkcActionCatalogClassifier.ResolveWeaponFilter(combatAction),
                defaultDuration = ResolveDuration(combatAction)
            });
        }

        private static List<GkcActionCatalogEntry> MergeWithSeed(List<GkcActionCatalogEntry> extracted)
        {
            var merged = new Dictionary<GkcCombatAction, GkcActionCatalogEntry>();

            foreach (GkcActionCatalogEntry seed in GkcActionCatalogClassifier.BuildManualSeedEntries())
                merged[seed.combatAction] = seed;

            for (int i = 0; i < extracted.Count; i++)
            {
                GkcActionCatalogEntry row = extracted[i];
                if (row.combatAction == GkcCombatAction.None)
                    continue;

                merged[row.combatAction] = row;
            }

            return new List<GkcActionCatalogEntry>(merged.Values);
        }

        private static float ResolveDuration(GkcCombatAction action) =>
            action switch
            {
                GkcCombatAction.Sword1HPower or GkcCombatAction.Axe1HPower or GkcCombatAction.Sword2HPower
                    or GkcCombatAction.Punch5 => GkcAnimatorConstants.DefaultPowerActionDuration,
                GkcCombatAction.Block => GkcAnimatorConstants.DefaultBlockActionDuration,
                GkcCombatAction.HitReactionArmed or GkcCombatAction.HitReactionUnarmed
                    => GkcAnimatorConstants.DefaultHitReactionDuration,
                GkcCombatAction.Death => 4f,
                _ => GkcAnimatorConstants.DefaultActionDuration
            };
    }
}
