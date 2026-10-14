using Project.Combat;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Applies the per-type identity on <see cref="EnemyDefinition"/> (body type, brain archetype / personalities,
    /// brain / engagement / hit-mark profile overrides) to a spawned enemy. Override when set, else the global
    /// Resources/Combat profile defaults. Called from EnemyInvectorGameplaySetup.ApplyDefinition and when the
    /// utility brain is auto-attached.
    /// </summary>
    public static class EnemyDefinitionOverrides
    {
        public static void Apply(GameObject root, EnemyDefinition definition)
        {
            if (root == null || definition == null)
                return;

            DMEnemyHitMarks marks = DMEnemyHitMarks.Ensure(root);
            if (marks != null)
            {
                marks.BodyType = definition.bodyType;
                marks.ProfileOverride = definition.hitMarkProfileOverride;
            }

            EnemyAiController ai = root.GetComponent<EnemyAiController>();
            if (ai != null)
                ai.ApplyDefinitionOverrides(definition);

            DMEnemyBrain brain = root.GetComponent<DMEnemyBrain>();
            if (brain != null)
                ApplyBrain(brain, definition, isNewBrain: false);

            ApplyInstance(root, definition);
        }

        private static Invector.DMEnemyInstanceOverrides InstanceOf(Component c)
        {
            var lb = c != null ? c.GetComponent<Invector.EnemyInvectorLoadoutBridge>() : null;
            return lb != null ? lb.ProfileOverrides : null;
        }

        /// <summary>2026-10-13: per-instance overrides (EnemyInvectorLoadoutBridge "Profile OVERRIDES") on top of the definition.</summary>
        public static void ApplyInstance(GameObject root, EnemyDefinition definition)
        {
            if (root == null) return;
            var lb = root.GetComponent<Invector.EnemyInvectorLoadoutBridge>();
            var o = lb != null ? lb.ProfileOverrides : null;
            if (o == null || !o.Any) return;

            DMEnemyHitMarks marks = DMEnemyHitMarks.Ensure(root);
            if (marks != null)
            {
                if (o.overrideBodyType) marks.BodyType = o.bodyType;
                if (o.hitMarkProfileOverride != null) marks.ProfileOverride = o.hitMarkProfileOverride;
            }

            EnemyAiController ai = root.GetComponent<EnemyAiController>();
            if (ai != null && o.engagementProfileOverride != null)
                ai.SetEngagementProfileOverride(o.engagementProfileOverride);

            DMEnemyBrain brain = root.GetComponent<DMEnemyBrain>();
            if (brain != null)
            {
                if (o.brainProfileOverride != null) brain.ProfileOverride = o.brainProfileOverride;
                if (o.overrideBrain) brain.Configure(o.brainArchetype, o.primaryPersonality, o.secondaryPersonality);
            }
        }

        /// <summary>Brain profile: instance override, then definition, then null (= global).</summary>
        public static DM_EnemyBrainProfile EffectiveBrainProfile(Component c, EnemyDefinition definition)
        {
            var o = InstanceOf(c);
            if (o != null && o.brainProfileOverride != null) return o.brainProfileOverride;
            return definition != null ? definition.brainProfileOverride : null;
        }

        /// <param name="isNewBrain">Auto-attached brains take the profile defaults when the definition does not override;
        /// a brain authored on the prefab keeps its own archetype unless the definition overrides.</param>
        public static void ApplyBrain(DMEnemyBrain brain, EnemyDefinition definition, bool isNewBrain)
        {
            if (brain == null)
                return;

            brain.ProfileOverride = EffectiveBrainProfile(brain, definition);
            var inst = InstanceOf(brain);
            if (inst != null && inst.overrideBrain)
            {
                brain.Configure(inst.brainArchetype, inst.primaryPersonality, inst.secondaryPersonality);
                return;
            }
            if (definition != null && definition.overrideBrain)
            {
                brain.Configure(definition.brainArchetype, definition.primaryPersonality, definition.secondaryPersonality);
                return;
            }

            if (!isNewBrain)
                return;

            DM_EnemyBrainProfile profile = brain.Profile;
            if (profile != null)
                brain.ApplyProfileDefaults(profile.defaultArchetype, profile.defaultPersonality);
        }
    }
}
