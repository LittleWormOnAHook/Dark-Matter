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
        }

        /// <param name="isNewBrain">Auto-attached brains take the profile defaults when the definition does not override;
        /// a brain authored on the prefab keeps its own archetype unless the definition overrides.</param>
        public static void ApplyBrain(DMEnemyBrain brain, EnemyDefinition definition, bool isNewBrain)
        {
            if (brain == null)
                return;

            brain.ProfileOverride = definition != null ? definition.brainProfileOverride : null;
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
