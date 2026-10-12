using System;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Utility brain tuning (Combat Plan Â§5â€“Â§8, Â§12, Phase 3). Score per action =
    /// archetype base Ã— personality multipliers Ã— condition multiplier + small randomness. Highest wins.
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
        [Tooltip("Â± random added to every score (seeded per enemy).")]
        [Range(0f, 1f)] public float scoreRandomness = 0.25f;
        [Tooltip("Defend is only scored if the target hit this enemy within this many seconds, or is inside its attack range.")]
        [Range(0f, 5f)] public float defendThreatMemory = 1.2f;
        [Tooltip("Added to Defend when the target hit this enemy recently.")]
        public float defendWhenHitBonus = 0.6f;
        [Range(0f, 20f)] public float defendCooldown = 3f;
        [Range(0f, 1f)] public float defendBlockShare = 0.6f;

        [Header("Enemy block (extends Defend: the brain picks block with defendBlockShare = the block chance)")]
        [Tooltip("Master switch. Off = the guard pose is cosmetic again (hits are not absorbed, the player is not recoiled).")]
        public bool enableEnemyBlock = true;
        [Tooltip("Seconds before THIS enemy may start another block. Blocks only start between swings while the enemy holds the attack token, so the one-attacker cap is untouched.")]
        [Range(0f, 30f)] public float blockCooldown = 4f;
        [Tooltip("How long a block lasts (s). 0 = use the prefab defensiveBlockDuration (1.2).")]
        [Range(0f, 4f)] public float blockDurationSeconds = 1f;
        [Tooltip("Fraction of the damage taken by a blocked hit (0 = full absorb). Poise is fed with the reduced damage, so a block also reduces poise damage.")]
        [Range(0f, 1f)] public float blockDamageFraction = 0.1f;
        [Tooltip("Frontal arc (degrees, total) in which a blocking enemy can block. Hits from behind land normally.")]
        [Range(30f, 360f)] public float blockArcDegrees = 150f;
        [Tooltip("Light (non-charged) player melee hits can be blocked.")]
        public bool blockLightHits = true;
        [Tooltip("Charged / heavy player melee hits can be blocked (otherwise they land fully and the block stays up).")]
        public bool blockChargedHits = true;
        [Tooltip("A blockable charged / heavy hit breaks the guard: the block drops and the hit lands normally (full damage, normal reaction). A poise break also drops the guard.")]
        public bool chargedBreaksBlock = true;
        [Tooltip("Chance a blocked hit recoils the player (Invector BreakAttack with playerBlockedRecoilId). Rolled per blocked hit.")]
        [Range(0f, 1f)] public float playerBlockedRecoilChance = 0.6f;
        [Tooltip("Player animator recoil id used for a blocked-hit recoil. 1 = recoil_unarmed (the clip the player own guard impact uses), 2 = recoil_low (default, visibly different), 3 = recoil_hard (CustomAction, long; clears the player block). The enemy block reaction is a procedural flinch, so these never look the same.")]
        [Range(1, 3)] public int playerBlockedRecoilId = 2;
        [Tooltip("After a recoil, the player cannot be recoiled again by a blocked hit for this long (no stun-lock).")]
        [Range(0f, 6f)] public float playerBlockedRecoilImmunitySeconds = 1.5f;
        [Tooltip("Blocked-hit sparks at the blade contact point: scale (player parry sparks use DM_CombatCoreProfile parryClashVfxScale 1.15).")]
        [Range(0.3f, 2.5f)] public float blockSparkScale = 0.8f;
        [Tooltip("Optional spark prefab for enemy blocks. Empty = the same SparksLong prefab the parry uses.")]
        public GameObject blockSparkPrefab;
        [Tooltip("Camera shake on a blocked hit (player block uses 0.18 / 0.08).")]
        [Range(0f, 2f)] public float blockedHitShakeAmplitude = 0.3f;
        [Range(0f, 0.4f)] public float blockedHitShakeDurationSeconds = 0.1f;

        [Header("Dodge roll (single owner: brain Defend -> EnemyAiController.BeginBrainDefend -> EnemyInvectorCombatBridge.TryBeginDodgeRoll)")]
        [Tooltip("When a Defend decision does not block, chance the enemy actually rolls (otherwise it falls back to a block, then to attacking). 1 = old behaviour.")]
        [Range(0f, 1f)] public float dodgeRollChance = 1f;
        [Tooltip("Seconds before THIS enemy may roll again (per enemy). Defend cooldown still applies on top.")]
        [Range(0f, 20f)] public float dodgeRollCooldown = 2f;
        [Tooltip("Relative weight of rolling to the LEFT and, separately, to the RIGHT of the threat (each side gets this weight). Old behaviour: sides 1 each, back 1.")]
        [Min(0f)] public float dodgeRollSideWeight = 1f;
        [Tooltip("Relative weight of rolling directly AWAY from the threat (backwards).")]
        [Min(0f)] public float dodgeRollBackWeight = 1f;
        [Tooltip("Relative weight of rolling TOWARD the threat. 0 = never roll forward (the forward roll does not look right). If every weight is 0 the roll goes backwards.")]
        [Min(0f)] public float dodgeRollForwardWeight = 0f;
        [Tooltip("Meters the dodge roll travels along the chosen direction (the roll clip is played in place on transform-driven enemies, so the AI moves the body). 0 = roll in place.")]
        [Range(0f, 6f)] public float dodgeRollDistance = 3.2f;
        [Tooltip("Movement of the side roll runs with the roll clip: it reaches full distance when the Roll animator state reaches this normalized time (the state exits at 0.7 and blends out for 0.1 s). Higher = movement lasts longer into the exit blend.")]
        [Range(0.3f, 1f)] public float dodgeRollMoveEndNormalized = 0.75f;
        [Tooltip("Same for the backward dodge clip (DM_DodgeBack): the displacement finishes at this normalized time of the clip.")]
        [Range(0.3f, 1f)] public float dodgeBackMoveEndNormalized = 0.85f;
        [Tooltip("Fallback / timeout length (s) of a side roll when the animator state cannot be read. Movement normally follows the clip, not this timer (was a fixed 0.65 s).")]
        [Range(0.3f, 2f)] public float dodgeRollSeconds = 1.1f;
        [Tooltip("BACKWARD dodges play the Humanoid@DodgeBackwardsCombat clip (animator state DM_DodgeBack in Invector@ShooterMelee.controller) instead of the forward-roll clip turned around. The body faces the threat and the AI moves it away. Off, or no such state on the controller = old turned-around roll.")]
        public bool dodgeBackUseClip = true;
        [Tooltip("Length (s) of the backward dodge. The clip (1.4 s) is sped up to fit (animator parameter DMDodgeBackSpeed).")]
        [Range(0.3f, 1.6f)] public float dodgeBackSeconds = 0.75f;
        [Tooltip("Meters the backward dodge travels away from the threat (the clip itself is in place; the AI moves the body).")]
        [Range(0f, 5f)] public float dodgeBackDistance = 2.2f;
        [Tooltip("Cross-fade IN time (s) from whatever the animator was playing into the backward dodge clip.")]
        [Range(0f, 0.4f)] public float dodgeBackBlendIn = 0.08f;
        [Tooltip("Cross-fade OUT time (s) from the backward dodge clip back to locomotion (starts this long before the dodge ends).")]
        [Range(0f, 0.5f)] public float dodgeBackBlendOut = 0.15f;
        [Tooltip("Turn the enemy to the roll direction instantly when the roll starts, so the roll animation (a forward roll) does not first travel toward the threat.")]
        public bool dodgeRollSnapFacing = true;

        [Header("Side dodge clip (clipfix): side rolls use a real side-dodge clip, the body faces the threat and never plays the forward roll")]
        [Tooltip("On = a side roll plays DM_DodgeLeft / DM_DodgeRight (Standing Dodge Left/Right) with the body facing the threat. Off = old forward Roll clip turned sideways.")]
        public bool dodgeSideUseClip = true;
        [Tooltip("Total length of a side dodge (clip is time-scaled to this).")]
        [Range(0.3f, 1.6f)] public float dodgeSideSeconds = 0.95f;
        [Tooltip("Distance travelled by a side dodge (m).")]
        [Range(0f, 5f)] public float dodgeSideDistance = 2.6f;
        [Tooltip("Normalized clip time at which the side dodge movement is complete.")]
        [Range(0.3f, 1f)] public float dodgeSideMoveEndNormalized = 0.85f;
        [Range(0f, 0.4f)] public float dodgeSideBlendIn = 0.08f;
        [Range(0f, 0.5f)] public float dodgeSideBlendOut = 0.15f;
        [Tooltip("On = hit reactions (flinch, stagger, hit slide) are ignored while a dodge roll plays, so the roll is never cut mid-animation. Damage still applies.")]
        public bool dodgeRollBlocksHitReactions = true;
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

        [Header("Archetypes (Â§6)")]
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

        [Header("Personalities (Â§7) â€” adjust, never override")]
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
    
        [Header("Dodge strike (occasional dodge that flows into a quick melee swing; HumanM@Dodge01 clip)")]
        [Tooltip("Master switch. Melee engagers (the enemy holding the attack token) only; the dodge is a short side weave at the current distance, then the normal attack runs.")]
        public bool enableDodgeStrike = true;
        [Tooltip("Chance per roll that a ready engager opens its swing with a dodge strike (scaled per enemy type by EnemyDefinition.dodgeStrikeChanceMultiplier).")]
        [Range(0f, 1f)] public float dodgeStrikeChance = 0.15f;
        [Tooltip("Seconds between rolls while the attack is ready (so the chance is per opportunity, not per frame).")]
        [Range(0.2f, 5f)] public float dodgeStrikeRollInterval = 1.5f;
        [Tooltip("Minimum seconds between two dodge strikes of the same enemy.")]
        [Range(0f, 30f)] public float dodgeStrikeCooldown = 6f;
        [Tooltip("Only starts when the target is inside this multiple of the enemy's attack range (keeps the D1 melee band; the weave moves around the target at constant distance).")]
        [Range(0.3f, 2f)] public float dodgeStrikeMaxRangeFactor = 1.1f;
        [Tooltip("Sideways weave length in metres, taken around the target at constant distance (min, max).")]
        public Vector2 dodgeStrikeSideDistance = new Vector2(0.6f, 1.0f);
        [Tooltip("Length in seconds the HumanM@Dodge01 clip (1.07 s natural) is played at before the swing starts; smaller = snappier.")]
        [Range(0.4f, 1.5f)] public float dodgeStrikeSeconds = 0.8f;
        [Tooltip("The dodge hands over to the normal attack when the clip reaches this normalized time (the clip is blended out to locomotion).")]
        [Range(0.3f, 1f)] public float dodgeStrikeAttackAtNormalized = 0.6f;
        [Tooltip("The side weave reaches its full length at this normalized time of the clip (ease-out ends with the weave, before the hand-over).")]
        [Range(0.2f, 1f)] public float dodgeStrikeMoveEndNormalized = 0.5f;
        [Range(0f, 0.4f)] public float dodgeStrikeBlendIn = 0.08f;
        [Range(0f, 0.4f)] public float dodgeStrikeBlendOut = 0.12f;
        [Tooltip("Turn rate (deg/s) used to keep facing the target during the weave.")]
        [Range(90f, 1080f)] public float dodgeStrikeTurnRate = 540f;
}
}
