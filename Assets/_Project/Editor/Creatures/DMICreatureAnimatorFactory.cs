using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Project.Creatures;
using Project.EditorTools;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools.Creatures
{
    /// <summary>
    /// Builds AnimatorControllers from a variable list of
    /// <see cref="DMICreatureAnimEntry"/> slots (RiggedNative path).
    /// </summary>
    public static class DMICreatureAnimatorFactory
    {
        public const string IdleState = "Idle";
        public const string WalkState = "Walk";
        public const string RunState = "Run";
        public const string AttackState = "Attack";
        public const string DeathState = "Death";
        public const string HitState = "Hit";

        public static string GetControllerFolder(DMICreatureDefinition definition)
        {
            string id = Sanitize(definition != null ? definition.creatureId : null, "Creature");
            return $"{ProjectAssetPaths.Animations}/Creatures/{id}";
        }

        public static string GetControllerPath(DMICreatureDefinition definition)
        {
            string id = Sanitize(definition != null ? definition.creatureId : null, "Creature");
            string file = Sanitize(
                definition != null ? definition.prefabFileName : null,
                id);
            return $"{GetControllerFolder(definition)}/{file}.controller";
        }

        /// <summary>
        /// Creates or overwrites an AnimatorController from definition animation entries.
        /// Ensures Idle/Walk/Run/Attack/Death exist (with fallbacks) for
        /// <see cref="DMICreatureAnimationDriver"/>.
        /// </summary>
        public static RuntimeAnimatorController BuildOrUpdateController(
            DMICreatureDefinition definition,
            out string message)
        {
            if (definition == null)
            {
                message = "Definition is null.";
                return null;
            }

            definition.EnsureAnimationEntriesMigrated();

            Dictionary<string, AnimationClip> map = CollectClips(definition);
            AnimationClip any = FirstClip(map);
            if (any == null)
            {
                any = FindFirstClipOnModel(definition.visualMeshSource);
                if (any != null)
                    map[IdleState] = any;
            }

            if (any == null)
            {
                message = "No animation clips assigned and none found on the visual mesh.";
                return null;
            }

            EnsureRequiredWithFallbacks(map, any);
            EnsureLocomotionClipsLoop(map);

            string folder = GetControllerFolder(definition);
            CraftingEditorUtility.EnsureFolder(folder);
            string path = GetControllerPath(definition);

            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (controller == null)
                controller = AnimatorController.CreateAnimatorControllerAtPath(path);

            if (controller.layers == null || controller.layers.Length == 0)
            {
                message = "AnimatorController has no layers.";
                return null;
            }

            AnimatorStateMachine sm = controller.layers[0].stateMachine;
            ClearStates(sm);

            AnimatorState defaultState = null;
            var builtNames = new List<string>();
            AnimationClip walkClip = GetOr(map, WalkState, null);
            AnimationClip runClip = GetOr(map, RunState, null);
            bool runIsWalkFallback = runClip != null && walkClip != null && runClip == walkClip;

            foreach (KeyValuePair<string, AnimationClip> kvp in map)
            {
                if (kvp.Value == null || string.IsNullOrWhiteSpace(kvp.Key))
                    continue;

                AnimatorState state = sm.AddState(kvp.Key);
                state.motion = kvp.Value;
                // No dedicated Run clip — play Walk faster for chase/run.
                if (runIsWalkFallback
                    && string.Equals(kvp.Key, RunState, StringComparison.OrdinalIgnoreCase))
                    state.speed = 1.65f;

                builtNames.Add(
                    $"{kvp.Key}={kvp.Value.name}"
                    + (runIsWalkFallback
                       && string.Equals(kvp.Key, RunState, StringComparison.OrdinalIgnoreCase)
                        ? "@1.65x"
                        : string.Empty));
                if (defaultState == null
                    || string.Equals(kvp.Key, IdleState, StringComparison.OrdinalIgnoreCase))
                    defaultState = state;
            }

            if (defaultState != null)
                sm.defaultState = defaultState;

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            definition.v2AnimatorController = controller;
            EditorUtility.SetDirty(definition);

            var sb = new StringBuilder();
            sb.Append("Controller ready at ").Append(path).Append(" (").Append(string.Join(", ", builtNames))
                .Append(").");
            message = sb.ToString();
            return controller;
        }

        /// <summary>
        /// Fills / appends animation entries from clips embedded in a model FBX.
        /// Heuristic state names for common locomotion; unmatched clips get their clip name as state.
        /// </summary>
        public static int PullClipsFromModel(DMICreatureDefinition definition, GameObject model)
        {
            if (definition == null || model == null)
                return 0;

            definition.EnsureAnimationEntriesMigrated();

            string assetPath = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrEmpty(assetPath))
                return 0;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            var clips = new List<AnimationClip>();
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    clips.Add(clip);
            }

            if (clips.Count == 0)
                return 0;

            var list = new List<DMICreatureAnimEntry>();
            if (definition.animationEntries != null)
                list.AddRange(definition.animationEntries);

            int assigned = 0;
            foreach (AnimationClip clip in clips)
            {
                string suggested = SuggestStateName(clip.name);
                if (TrySetOrAddEntry(list, suggested, clip))
                    assigned++;
                else if (TrySetOrAddEntry(list, clip.name, clip))
                    assigned++;
            }

            if (assigned == 0 && clips.Count > 0)
            {
                if (TrySetOrAddEntry(list, IdleState, clips[0]))
                    assigned++;
                if (TrySetOrAddEntry(list, WalkState, clips[0]))
                    assigned++;
            }

            definition.animationEntries = list.ToArray();
            if (assigned > 0)
                EditorUtility.SetDirty(definition);

            return assigned;
        }

        public static AnimationClip FindFirstClipOnModel(GameObject model)
        {
            if (model == null)
                return null;

            string assetPath = AssetDatabase.GetAssetPath(model);
            if (string.IsNullOrEmpty(assetPath))
                return null;

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
            for (int i = 0; i < assets.Length; i++)
            {
                if (assets[i] is AnimationClip clip && !clip.name.StartsWith("__preview__"))
                    return clip;
            }

            return null;
        }

        private static Dictionary<string, AnimationClip> CollectClips(DMICreatureDefinition definition)
        {
            var map = new Dictionary<string, AnimationClip>(StringComparer.OrdinalIgnoreCase);
            if (definition.animationEntries == null)
                return map;

            for (int i = 0; i < definition.animationEntries.Length; i++)
            {
                DMICreatureAnimEntry entry = definition.animationEntries[i];
                if (entry == null || entry.clip == null || string.IsNullOrWhiteSpace(entry.stateName))
                    continue;

                string key = entry.stateName.Trim();
                if (!map.ContainsKey(key))
                    map[key] = entry.clip;
            }

            return map;
        }

        private static AnimationClip FirstClip(Dictionary<string, AnimationClip> map)
        {
            foreach (AnimationClip clip in map.Values)
            {
                if (clip != null)
                    return clip;
            }

            return null;
        }

        private static void EnsureRequiredWithFallbacks(
            Dictionary<string, AnimationClip> map,
            AnimationClip any)
        {
            AnimationClip idle = GetOr(map, IdleState, null) ?? GetOr(map, WalkState, null) ?? any;
            AnimationClip walk = GetOr(map, WalkState, null) ?? idle;
            AnimationClip run = GetOr(map, RunState, null) ?? walk;
            AnimationClip attack = GetOr(map, AttackState, null) ?? idle;
            AnimationClip death = GetOr(map, DeathState, null) ?? idle;

            map[IdleState] = idle;
            map[WalkState] = walk;
            map[RunState] = run;
            map[AttackState] = attack;
            map[DeathState] = death;
        }

        /// <summary>
        /// Idle/Walk/Run must loop; Attack/Death stay one-shot.
        /// Safe for FBX sub-assets and editable project clips.
        /// </summary>
        private static void EnsureLocomotionClipsLoop(Dictionary<string, AnimationClip> map)
        {
            SetClipLoop(GetOr(map, IdleState, null), true);
            SetClipLoop(GetOr(map, WalkState, null), true);
            SetClipLoop(GetOr(map, RunState, null), true);
            // Keep combat one-shots non-looping even if shared with Idle fallback.
            AnimationClip attack = GetOr(map, AttackState, null);
            AnimationClip death = GetOr(map, DeathState, null);
            AnimationClip idle = GetOr(map, IdleState, null);
            AnimationClip walk = GetOr(map, WalkState, null);
            if (attack != null && attack != idle && attack != walk)
                SetClipLoop(attack, false);
            if (death != null && death != idle && death != walk && death != attack)
                SetClipLoop(death, false);
        }

        private static void SetClipLoop(AnimationClip clip, bool loop)
        {
            if (clip == null)
                return;

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            if (settings.loopTime == loop)
                return;

            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip);
        }

        private static AnimationClip GetOr(
            Dictionary<string, AnimationClip> map,
            string key,
            AnimationClip fallback)
        {
            if (map.TryGetValue(key, out AnimationClip clip) && clip != null)
                return clip;
            return fallback;
        }

        private static string SuggestStateName(string clipName)
        {
            string n = clipName.ToLowerInvariant();
            if (n.Contains("idle") || n.Contains("stand"))
                return IdleState;
            if (n.Contains("walk") || n.Contains("locomotion"))
                return WalkState;
            if (n.Contains("run") || n.Contains("sprint") || n.Contains("trot"))
                return RunState;
            if (n.Contains("attack") || n.Contains("bite") || n.Contains("claw") || n.Contains("melee"))
                return AttackState;
            if (n.Contains("death") || n.Contains("die"))
                return DeathState;
            if (n.Contains("hit") || n.Contains("damage") || n.Contains("flinch"))
                return HitState;
            return null;
        }

        private static bool TrySetOrAddEntry(
            List<DMICreatureAnimEntry> list,
            string stateName,
            AnimationClip clip)
        {
            if (string.IsNullOrWhiteSpace(stateName) || clip == null)
                return false;

            for (int i = 0; i < list.Count; i++)
            {
                DMICreatureAnimEntry entry = list[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.stateName))
                    continue;
                if (!string.Equals(entry.stateName.Trim(), stateName.Trim(), StringComparison.OrdinalIgnoreCase))
                    continue;
                if (entry.clip != null)
                    return false;
                entry.clip = clip;
                return true;
            }

            list.Add(new DMICreatureAnimEntry { stateName = stateName.Trim(), clip = clip });
            return true;
        }

        private static void ClearStates(AnimatorStateMachine sm)
        {
            if (sm == null)
                return;

            ChildAnimatorState[] states = sm.states;
            for (int i = states.Length - 1; i >= 0; i--)
                sm.RemoveState(states[i].state);

            ChildAnimatorStateMachine[] machines = sm.stateMachines;
            for (int i = machines.Length - 1; i >= 0; i--)
                sm.RemoveStateMachine(machines[i].stateMachine);
        }

        private static string Sanitize(string value, string fallback)
        {
            if (string.IsNullOrWhiteSpace(value))
                return fallback;

            foreach (char c in Path.GetInvalidFileNameChars())
                value = value.Replace(c, '_');
            return value.Replace(' ', '_');
        }
    }
}
