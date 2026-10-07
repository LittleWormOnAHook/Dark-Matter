using System;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Utility brain tuning (Combat Plan §5–§8, §12, Phase 3). Score per action =
    /// archetype base × personality multipliers × condition multiplier + small randomness. Highest wins.
    /// Resources/Combat/DM_EnemyBrainProfile, read live so Genesis Studio edits apply in Play.
    /// </summary>
    [CreateAssetMenu(menuName = "Dark Matter/Combat/Enemy Brain Profile", fileName = "DM_EnemyBrainProfile")]
    public sealed class DM_EnemyBrainProfile : ScriptableObject
    {
        public const string ResourcesPath = "Combat/DM_EnemyBrainProfile";

        private static DM_EnemyBrainProfile live;
        private static DM_EnemyBrainProfile fallback;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetLiveCache()
        {
            live = null;
        }

        public static DM_EnemyBrainProfile Live
        {
            get
            {
                if (live == null)
                    live = Resources.Load<DM_EnemyBrainProfile>(ResourcesPath);
                if (live != null)
                    return live;
                if (fallback == null)
                {
                    fallback = CreateInstance<DM_EnemyBrainProfile>();
                    fallback.hideFlags = HideFlags.HideAndDontSave;
                }

                return fallback;
            }
        }

        [Serializable]
        public struct ArchetypeRow
        {
            public DMEnemyArchetype archetype;
            [Tooltip("Base utility of pressing the attack while holding the token.")]
            public float press;
            [Tooltip("Base utility of a block / dodge when threatened.")]
            public float defend;
            [Tooltip("Base utility of a desperation retreat (scaled by condition).")]
            public float retreat;
            [Tooltip("Added to the director focus score (wants the token more / less).")]
            public float tokenBias;
            [Tooltip("Multiplies holder taunt weight.")]
            public float holderTaunt;
            [Tooltip("Multiplies holder feint weight.")]
            public float holderFeint;
        }

        [Serializable]
        public struct PersonalityRow
        {
            public DMEnemyPersonality personality;
            public float pressMul;
            public float defendMul;
            public float retreatMul;
            public float tokenBiasAdd;
            public float holderSidestepMul;
            public float holderTauntMul;
            public float holderFeintMul;
        }

        [Header("Master")]
        [Tooltip("Off = enemies skip utility decisions (no brain-driven defend / retreat, no token bias). Spacing still runs from the engagement profile.")]
        public bool enableUtilityBrain = true;
        [Tooltip("Add a DMEnemyBrain to every EnemyAiController that doesn't have one (uses the defaults below).")]
        public bool autoAttachBrain = true;
        public DMEnemyArchetype defaultArchetype = DMEnemyArchetype.Duelist;
        public DMEnemyPersonality defaultPersonality = DMEnemyPersonality.None;
        public bool debugLogDecisions = false;

        [Header("Decisions")]
        [Tooltip("The brain re-scores this often and holds its pick until the next decision.")]
        [Range(0.1f, 3f)] public float decisionInterval = 0.8f;
        [Tooltip("± random added to every score (seeded per enemy).")]
        [Range(0f, 1f)] public float scoreRandomness = 0.25f;
        [Tooltip("Defend is only scored if the target hit this enemy within this many seconds, or is inside its attack range.")]
        [Range(0f, 5f)] public float defendThreatMemory = 1.2f;
        [Tooltip("Added to Defend when the target hit this enemy recently.")]
        public float defendWhenHitBonus = 0.6f;
        [Range(0f, 20f)] public float defendCooldown = 3f;
        [Range(0f, 1f)] public float defendBlockShare = 0.6f;
        [Tooltip("Desperation retreat: give up the token and back off to the outer ring for this long.")]
        [Range(0f, 20f)] public float retreatSeconds = 5f;
        [Range(0f, 60f)] public float retreatCooldown = 10f;

        [Header("Condition thresholds (health fraction)")]
        [Range(0f, 1f)] public float injuredBelow = 0.75f;
        [Range(0f, 1f)] public float severelyInjuredBelow = 0.5f;
        [Range(0f, 1f)] public float criticalBelow = 0.25f;
        [Tooltip("Press multiplier at Healthy / Injured / Severely injured / Critical.")]
        public Vector4 conditionPress = new Vector4(1f, 1f, 0.9f, 0.8f);
        public Vector4 conditionDefend = new Vector4(1f, 1.15f, 1.3f, 1.5f);
        public Vector4 conditionRetreat = new Vector4(0f, 0f, 0.4f, 1.2f);

        [Header("Archetypes (§6)")]
        public ArchetypeRow[] archetypes =
        {
            new ArchetypeRow { archetype = DMEnemyArchetype.Duelist, press = 1f, defend = 0.35f, retreat = 0.6f, tokenBias = 0f, holderTaunt = 1f, holderFeint = 1f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Rusher, press = 1.3f, defend = 0.15f, retreat = 0.3f, tokenBias = 1f, holderTaunt = 1.2f, holderFeint = 1.5f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Berserker, press = 1.5f, defend = 0.05f, retreat = 0f, tokenBias = 1.5f, holderTaunt = 1.6f, holderFeint = 1.2f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Flanker, press = 1f, defend = 0.3f, retreat = 0.6f, tokenBias = 0f, holderTaunt = 0.6f, holderFeint = 1.4f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Ambusher, press = 1.1f, defend = 0.2f, retreat = 0.8f, tokenBias = 0.5f, holderTaunt = 0.4f, holderFeint = 1f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Tank, press = 0.9f, defend = 0.6f, retreat = 0.1f, tokenBias = 0f, holderTaunt = 1.3f, holderFeint = 0.5f },
            new ArchetypeRow { archetype = DMEnemyArchetype.Guardian, press = 0.8f, defend = 0.7f, retreat = 0.2f, tokenBias = -0.5f, holderTaunt = 1f, holderFeint = 0.5f },
        };

        [Header("Personalities (§7) — adjust, never override")]
        public PersonalityRow[] personalities =
        {
            new PersonalityRow { personality = DMEnemyPersonality.Aggressive, pressMul = 1.3f, defendMul = 0.8f, retreatMul = 0.6f, tokenBiasAdd = 0.75f, holderSidestepMul = 1f, holderTauntMul = 1.4f, holderFeintMul = 1.3f },
            new PersonalityRow { personality = DMEnemyPersonality.Cautious, pressMul = 1.05f, defendMul = 1.4f, retreatMul = 1.2f, tokenBiasAdd = -0.5f, holderSidestepMul = 1.2f, holderTauntMul = 0.6f, holderFeintMul = 0.7f },
            new PersonalityRow { personality = DMEnemyPersonality.Cowardly, pressMul = 0.8f, defendMul = 1.3f, retreatMul = 2f, tokenBiasAdd = -1f, holderSidestepMul = 1.2f, holderTauntMul = 0.3f, holderFeintMul = 0.5f },
            new PersonalityRow { personality = DMEnemyPersonality.Brave, pressMul = 1.15f, defendMul = 1f, retreatMul = 0.4f, tokenBiasAdd = 0.5f, holderSidestepMul = 1f, holderTauntMul = 1.2f, holderFeintMul = 1f },
            new PersonalityRow { personality = DMEnemyPersonality.Reckless, pressMul = 1.4f, defendMul = 0.5f, retreatMul = 0.2f, tokenBiasAdd = 1f, holderSidestepMul = 0.8f, holderTauntMul = 1.5f, holderFeintMul = 1.5f },
            new PersonalityRow { personality = DMEnemyPersonality.Defensive, pressMul = 0.9f, defendMul = 1.6f, retreatMul = 1f, tokenBiasAdd = -0.25f, holderSidestepMul = 1f, holderTauntMul = 0.8f, holderFeintMul = 0.6f },
            new PersonalityRow { personality = DMEnemyPersonality.Opportunistic, pressMul = 1.1f, defendMul = 1f, retreatMul = 1f, tokenBiasAdd = 0.25f, holderSidestepMul = 1f, holderTauntMul = 0.8f, holderFeintMul = 1.6f },
            new PersonalityRow { personality = DMEnemyPersonality.Calculating, pressMul = 1f, defendMul = 1.25f, retreatMul = 1f, tokenBiasAdd = 0f, holderSidestepMul = 1.1f, holderTauntMul = 0.5f, holderFeintMul = 1.3f },
            new PersonalityRow { personality = DMEnemyPersonality.Territorial, pressMul = 1.1f, defendMul = 1.1f, retreatMul = 0.5f, tokenBiasAdd = 0.25f, holderSidestepMul = 0.8f, holderTauntMul = 1.3f, holderFeintMul = 0.8f },
            new PersonalityRow { personality = DMEnemyPersonality.Vengeful, pressMul = 1.2f, defendMul = 0.9f, retreatMul = 0.5f, tokenBiasAdd = 0.5f, holderSidestepMul = 1f, holderTauntMul = 1.2f, holderFeintMul = 1f },
            new PersonalityRow { personality = DMEnemyPersonality.Predatory, pressMul = 1.2f, defendMul = 0.8f, retreatMul = 0.7f, tokenBiasAdd = 0.5f, holderSidestepMul = 1.2f, holderTauntMul = 0.6f, holderFeintMul = 1.4f },
            new PersonalityRow { personality = DMEnemyPersonality.Protective, pressMul = 1f, defendMul = 1.2f, retreatMul = 0.6f, tokenBiasAdd = 0f, holderSidestepMul = 1f, holderTauntMul = 1f, holderFeintMul = 0.8f },
            new PersonalityRow { personality = DMEnemyPersonality.Curious, pressMul = 1f, defendMul = 1f, retreatMul = 1f, tokenBiasAdd = 0f, holderSidestepMul = 1.2f, holderTauntMul = 0.8f, holderFeintMul = 1.2f },
        };

        public ArchetypeRow ResolveArchetype(DMEnemyArchetype archetype)
        {
            ArchetypeRow fallbackRow = new ArchetypeRow
            {
                archetype = DMEnemyArchetype.Duelist, press = 1f, defend = 0.35f, retreat = 0.6f,
                tokenBias = 0f, holderTaunt = 1f, holderFeint = 1f
            };

            if (archetypes == null)
                return fallbackRow;

            bool foundDuelist = false;
            for (int i = 0; i < archetypes.Length; i++)
            {
                if (archetypes[i].archetype == archetype)
                    return archetypes[i];
                if (!foundDuelist && archetypes[i].archetype == DMEnemyArchetype.Duelist)
                {
                    fallbackRow = archetypes[i];
                    foundDuelist = true;
                }
            }

            return fallbackRow;
        }

        public bool TryResolvePersonality(DMEnemyPersonality personality, out PersonalityRow row)
        {
            row = default;
            if (personality == DMEnemyPersonality.None || personalities == null)
                return false;

            for (int i = 0; i < personalities.Length; i++)
            {
                if (personalities[i].personality != personality)
                    continue;
                row = personalities[i];
                return true;
            }

            return false;
        }

        public DMEnemyCondition ResolveCondition(float healthFraction)
        {
            if (healthFraction < criticalBelow)
                return DMEnemyCondition.Critical;
            if (healthFraction < severelyInjuredBelow)
                return DMEnemyCondition.SeverelyInjured;
            if (healthFraction < injuredBelow)
                return DMEnemyCondition.Injured;
            return DMEnemyCondition.Healthy;
        }

        public static float Pick(Vector4 perCondition, DMEnemyCondition condition)
        {
            switch (condition)
            {
                case DMEnemyCondition.Injured: return perCondition.y;
                case DMEnemyCondition.SeverelyInjured: return perCondition.z;
                case DMEnemyCondition.Critical: return perCondition.w;
                default: return perCondition.x;
            }
        }
    }
}
