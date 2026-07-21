using Project.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Builds an enemy AnimatorController by copying Invector@ShooterMelee.controller as the full
    /// base (all layers and blend trees preserved), then ADDING enemy-specific states to the Base
    /// Layer. Nothing in the non-Base layers is touched.
    ///
    /// This means every enemy automatically inherits the full UpperBody, Shot, and OnlyArms layer
    /// stack and all future layers added to ShooterMelee.
    /// </summary>
    public static class EnemyShooterControllerBuilder
    {
        public const string ShooterMeleePath =
            "Assets/Invector-3rdPersonController/Shooter/Animator/Invector@ShooterMelee.controller";

        /// <summary>
        /// Creates a new controller at <paramref name="destPath"/> based on ShooterMelee, with
        /// the enemy's own animation clips added to the Base Layer.
        /// Deletes any existing file at destPath before copying.
        /// </summary>
        public static AnimatorController Build(EnemyDefinition definition, string destPath)
        {
            if (definition == null || string.IsNullOrEmpty(destPath))
                return null;

            // Remove stale controller so CopyAsset doesn't fail.
            if (AssetDatabase.LoadAssetAtPath<Object>(destPath) != null)
                AssetDatabase.DeleteAsset(destPath);

            if (!AssetDatabase.CopyAsset(ShooterMeleePath, destPath))
            {
                Debug.LogError($"[EnemyShooterControllerBuilder] Failed to copy {ShooterMeleePath} → {destPath}");
                return null;
            }

            AssetDatabase.ImportAsset(destPath);
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(destPath);
            if (controller == null)
            {
                Debug.LogError($"[EnemyShooterControllerBuilder] Could not load copied controller at {destPath}");
                return null;
            }

            AddEnemyStatesToBaseLayer(controller, definition);
            EnsureEnemyParams(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            return controller;
        }

        /// <summary>
        /// Overwrites an existing controller file at <paramref name="destPath"/> with a fresh
        /// ShooterMelee copy, restoring the Base Layer states that were in the old controller.
        /// Use this to upgrade an existing enemy controller (e.g. The Evil One) that was built
        /// before the ShooterMelee base approach was adopted.
        /// </summary>
        public static AnimatorController RebuildFromShooterMeleeBase(string destPath)
        {
            if (string.IsNullOrEmpty(destPath))
                return null;

            // ── 1. Extract existing Base Layer state names + motions ──
            AnimatorController existing =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(destPath);

            SavedState[] savedStates = existing != null
                ? ExtractBaseLayerStates(existing)
                : System.Array.Empty<SavedState>();

            // ── 2. Replace with ShooterMelee copy ──
            AssetDatabase.DeleteAsset(destPath);

            if (!AssetDatabase.CopyAsset(ShooterMeleePath, destPath))
            {
                Debug.LogError($"[EnemyShooterControllerBuilder] Failed to copy {ShooterMeleePath} → {destPath}");
                return null;
            }

            AssetDatabase.ImportAsset(destPath);
            AnimatorController controller =
                AssetDatabase.LoadAssetAtPath<AnimatorController>(destPath);
            if (controller == null)
                return null;

            // ── 3. Re-add saved enemy states to Base Layer ──
            AnimatorStateMachine baseRoot = controller.layers[0].stateMachine;
            foreach (SavedState ss in savedStates)
            {
                if (HasState(baseRoot, ss.name)) continue;
                AnimatorState s = baseRoot.AddState(ss.name);
                s.motion = ss.motion;
            }

            EnsureEnemyParams(controller);

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();

            Debug.Log($"[EnemyShooterControllerBuilder] Rebuilt '{destPath}' from ShooterMelee base " +
                      $"+ {savedStates.Length} restored state(s).");
            return controller;
        }

        // ── Internal helpers ─────────────────────────────────────────────────────

        private static void AddEnemyStatesToBaseLayer(
            AnimatorController controller, EnemyDefinition definition)
        {
            AnimatorStateMachine root = controller.layers[0].stateMachine;
            AnimatorState firstIdle = null;

            if (HasClips(definition.idleClips))
            {
                for (int i = 0; i < definition.idleClips.Length; i++)
                {
                    if (definition.idleClips[i] == null) continue;
                    string name = $"Idle{i + 1:00}";
                    if (HasState(root, name)) continue;
                    AnimatorState s = root.AddState(name);
                    s.motion = definition.idleClips[i];
                    if (firstIdle == null) firstIdle = s;
                }
            }

            AddSingle(root, "Walk",   definition.walkClips);
            AddSingle(root, "Run",    definition.runClips);

            if (HasClips(definition.attackClips))
            {
                for (int i = 0; i < definition.attackClips.Length; i++)
                {
                    if (definition.attackClips[i] == null) continue;
                    string name = $"Attack{i + 1:00}";
                    if (HasState(root, name)) continue;
                    AnimatorState s = root.AddState(name);
                    s.motion = definition.attackClips[i];
                }
            }

            if (HasClips(definition.hitClips))
            {
                for (int i = 0; i < definition.hitClips.Length; i++)
                {
                    if (definition.hitClips[i] == null) continue;
                    string name = $"Hit{i + 1:00}";
                    if (HasState(root, name)) continue;
                    AnimatorState s = root.AddState(name);
                    s.motion = definition.hitClips[i];
                }
            }

            if (HasClips(definition.deathClips) && !HasState(root, "Death"))
            {
                AnimatorState s = root.AddState("Death");
                s.motion = definition.deathClips[0];
            }
        }

        private static void AddSingle(
            AnimatorStateMachine root, string name, AnimationClip[] clips)
        {
            if (!HasClips(clips) || HasState(root, name)) return;
            AnimatorState s = root.AddState(name);
            s.motion = clips[0];
        }

        private static void EnsureEnemyParams(AnimatorController ctrl)
        {
            // These are not in ShooterMelee but EnemyAnimationController uses them.
            EnsureFloat(ctrl, "Forward");
            EnsureFloat(ctrl, "Turn");
        }

        private static void EnsureFloat(AnimatorController ctrl, string name)
        {
            foreach (AnimatorControllerParameter p in ctrl.parameters)
                if (p.name == name) return;
            ctrl.AddParameter(name, AnimatorControllerParameterType.Float);
        }

        private static bool HasState(AnimatorStateMachine sm, string name)
        {
            foreach (ChildAnimatorState cs in sm.states)
                if (cs.state.name == name) return true;
            return false;
        }

        // ── State extraction ─────────────────────────────────────────────────────

        private struct SavedState
        {
            public string name;
            public Motion motion;
        }

        private static SavedState[] ExtractBaseLayerStates(AnimatorController ctrl)
        {
            if (ctrl == null || ctrl.layers.Length == 0)
                return System.Array.Empty<SavedState>();

            ChildAnimatorState[] states = ctrl.layers[0].stateMachine.states;
            SavedState[] saved = new SavedState[states.Length];
            for (int i = 0; i < states.Length; i++)
                saved[i] = new SavedState
                {
                    name   = states[i].state.name,
                    motion = states[i].state.motion,
                };
            return saved;
        }

        public static bool HasClips(AnimationClip[] clips)
        {
            if (clips == null) return false;
            foreach (AnimationClip c in clips)
                if (c != null) return true;
            return false;
        }
    }
}
