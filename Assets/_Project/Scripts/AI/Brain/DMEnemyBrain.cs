using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Per-enemy utility brain (Combat Plan Phase 3, §5). Sits on top of EnemyAiController (the Body / legacy
    /// state machine stays in charge of perception, movement and Invector bridges) and scores combat actions:
    /// Press, Defend, Retreat, Hold. Score = archetype base × personality × condition + seeded randomness.
    /// It also biases the engagement director's token score and the holder idle-action weights.
    /// Auto-attached by EnemyAiController when DM_EnemyBrainProfile.autoAttachBrain is on.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class DMEnemyBrain : MonoBehaviour
    {
        [Tooltip("Behaviour template (§6). Unknown rows use the Duelist row on DM_EnemyBrainProfile.")]
        [SerializeField] private DMEnemyArchetype archetype = DMEnemyArchetype.Duelist;
        [Tooltip("Personality (§7). Multiplies the archetype weights.")]
        [SerializeField] private DMEnemyPersonality primaryPersonality = DMEnemyPersonality.None;
        [SerializeField] private DMEnemyPersonality secondaryPersonality = DMEnemyPersonality.None;
        [Tooltip("Optional per-type brain profile (set from EnemyDefinition). Empty = global DM_EnemyBrainProfile.")]
        [SerializeField] private DM_EnemyBrainProfile profileOverride;

        private EnemyHealth health;
        private System.Random rng;
        private float nextDecisionTime;
        private float nextDefendAllowed;
        private float retreatUntil;
        private float nextRetreatAllowed;
        private float missRecoveryUntil;
        private bool defendRequested;
        private bool defendPrefersBlock;
        private DMBrainAction currentAction = DMBrainAction.None;
        private DMEnemyCondition condition = DMEnemyCondition.Healthy;
        private float scorePress;
        private float scoreDefend;
        private float scoreRetreat;
        private string lastReason = "idle";

        public DMEnemyArchetype Archetype => archetype;
        public DMEnemyPersonality PrimaryPersonality => primaryPersonality;
        public DMEnemyPersonality SecondaryPersonality => secondaryPersonality;
        public DMBrainAction CurrentAction => currentAction;
        public DMEnemyCondition Condition => condition;
        public bool IsRetreating => Time.time < retreatUntil;
        public bool IsMissRecoveryActive => Time.time < missRecoveryUntil;
        public string LastReason => lastReason;

        public DM_EnemyBrainProfile ProfileOverride
        {
            get => profileOverride;
            set => profileOverride = value;
        }

        /// <summary>Per-type override when set, else the global Resources profile.</summary>
        public DM_EnemyBrainProfile Profile => profileOverride != null ? profileOverride : DM_EnemyBrainProfile.Live;

        /// <summary>Per-type archetype + personalities (EnemyDefinition.overrideBrain).</summary>
        public void Configure(DMEnemyArchetype newArchetype, DMEnemyPersonality primary, DMEnemyPersonality secondary)
        {
            archetype = newArchetype;
            primaryPersonality = primary;
            secondaryPersonality = secondary;
        }

        private bool BrainEnabled
        {
            get
            {
                DM_EnemyBrainProfile p = Profile;
                return p != null && p.enableUtilityBrain;
            }
        }

        public void ApplyProfileDefaults(DMEnemyArchetype defaultArchetype, DMEnemyPersonality defaultPersonality)
        {
            archetype = defaultArchetype;
            primaryPersonality = defaultPersonality;
            secondaryPersonality = DMEnemyPersonality.None;
        }

        private void Awake()
        {
            health = GetComponent<EnemyHealth>();
            rng = new System.Random(gameObject.GetEntityId().GetHashCode() ^ 0x5D3A71);
        }

        private void OnEnable()
        {
            currentAction = DMBrainAction.None;
            defendRequested = false;
            retreatUntil = 0f;
            nextRetreatAllowed = 0f;
            nextDefendAllowed = 0f;
            nextDecisionTime = 0f;
        }

        private float Jitter(float amplitude)
        {
            if (amplitude <= 0f || rng == null)
                return 0f;
            return ((float)rng.NextDouble() * 2f - 1f) * amplitude;
        }

        private void ResolvePersonality(DM_EnemyBrainProfile profile,
            out float press, out float defend, out float retreat, out float tokenAdd,
            out float side, out float taunt, out float feint)
        {
            press = defend = retreat = side = taunt = feint = 1f;
            tokenAdd = 0f;
            Accumulate(profile, primaryPersonality, ref press, ref defend, ref retreat, ref tokenAdd, ref side, ref taunt, ref feint);
            if (secondaryPersonality != primaryPersonality)
                Accumulate(profile, secondaryPersonality, ref press, ref defend, ref retreat, ref tokenAdd, ref side, ref taunt, ref feint);
        }

        private static void Accumulate(DM_EnemyBrainProfile profile, DMEnemyPersonality personality,
            ref float press, ref float defend, ref float retreat, ref float tokenAdd,
            ref float side, ref float taunt, ref float feint)
        {
            if (!profile.TryResolvePersonality(personality, out DM_EnemyBrainProfile.PersonalityRow row))
                return;

            press *= Mathf.Max(0f, row.pressMul);
            defend *= Mathf.Max(0f, row.defendMul);
            retreat *= Mathf.Max(0f, row.retreatMul);
            tokenAdd += row.tokenBiasAdd;
            side *= Mathf.Max(0f, row.holderSidestepMul);
            taunt *= Mathf.Max(0f, row.holderTauntMul);
            feint *= Mathf.Max(0f, row.holderFeintMul);
        }

        /// <summary>Added to the director's focus score for this enemy.</summary>
        public float TokenBias
        {
            get
            {
                DM_EnemyBrainProfile profile = Profile;
                if (profile == null || !profile.enableUtilityBrain)
                    return 0f;

                DM_EnemyBrainProfile.ArchetypeRow row = profile.ResolveArchetype(archetype);
                ResolvePersonality(profile, out _, out _, out _, out float tokenAdd, out _, out _, out _);
                return row.tokenBias + tokenAdd;
            }
        }

        /// <summary>Multipliers for holder sidestep / taunt / feint weights.</summary>
        public void GetHolderActionMultipliers(out float sidestep, out float taunt, out float feint)
        {
            sidestep = taunt = feint = 1f;
            DM_EnemyBrainProfile profile = Profile;
            if (profile == null || !profile.enableUtilityBrain)
                return;

            DM_EnemyBrainProfile.ArchetypeRow row = profile.ResolveArchetype(archetype);
            ResolvePersonality(profile, out _, out _, out _, out _, out sidestep, out taunt, out feint);
            taunt *= Mathf.Max(0f, row.holderTaunt);
            feint *= Mathf.Max(0f, row.holderFeint);
        }

        /// <summary>
        /// Called by EnemyAiController at the agent role tick. Re-scores at the profile decision interval.
        /// </summary>
        public void Tick(bool isEngager, bool threatInReach, bool midSwing, float lastHitByTargetTime)
        {
            DM_EnemyBrainProfile profile = Profile;
            if (profile == null || !profile.enableUtilityBrain)
            {
                currentAction = DMBrainAction.None;
                defendRequested = false;
                lastReason = "brain off";
                return;
            }

            float now = Time.time;
            if (health != null && health.MaxHealth > 0f)
                condition = profile.ResolveCondition(Mathf.Clamp01(health.CurrentHealth / health.MaxHealth));

            if (now < nextDecisionTime)
                return;

            nextDecisionTime = now + Mathf.Max(0.1f, profile.decisionInterval) * (0.85f + 0.3f * (float)rng.NextDouble());

            DM_EnemyBrainProfile.ArchetypeRow row = profile.ResolveArchetype(archetype);
            ResolvePersonality(profile, out float pPress, out float pDefend, out float pRetreat,
                out _, out _, out _, out _);

            float r = profile.scoreRandomness;
            scorePress = row.press * pPress * DM_EnemyBrainProfile.Pick(profile.conditionPress, condition) + Jitter(r);

            bool hitRecently = lastHitByTargetTime > 0f && now - lastHitByTargetTime <= profile.defendThreatMemory;
            bool canDefend = isEngager && !midSwing && now >= nextDefendAllowed && (hitRecently || threatInReach);
            float defendBase = row.defend * pDefend * DM_EnemyBrainProfile.Pick(profile.conditionDefend, condition);
            scoreDefend = canDefend && defendBase > 0.001f
                ? defendBase + (hitRecently ? profile.defendWhenHitBonus : 0f) + Jitter(r)
                : float.NegativeInfinity;

            float retreatBase = row.retreat * pRetreat * DM_EnemyBrainProfile.Pick(profile.conditionRetreat, condition);
            bool canRetreat = !IsRetreating && now >= nextRetreatAllowed && retreatBase > 0.01f && !midSwing;
            scoreRetreat = canRetreat ? retreatBase + Jitter(r) : float.NegativeInfinity;

            DMBrainAction pick;
            if (!isEngager)
            {
                pick = scoreRetreat > scorePress ? DMBrainAction.Retreat : DMBrainAction.Hold;
            }
            else
            {
                pick = DMBrainAction.Press;
                float best = scorePress;
                if (scoreDefend > best)
                {
                    best = scoreDefend;
                    pick = DMBrainAction.Defend;
                }

                if (scoreRetreat > best)
                    pick = DMBrainAction.Retreat;
            }

            if (pick == DMBrainAction.Defend)
            {
                defendRequested = true;
                defendPrefersBlock = rng.NextDouble() < profile.defendBlockShare;
                nextDefendAllowed = now + profile.defendCooldown;
            }
            else if (pick == DMBrainAction.Retreat)
            {
                retreatUntil = now + profile.retreatSeconds;
                nextRetreatAllowed = retreatUntil + profile.retreatCooldown;
            }

            if (pick != currentAction && profile.debugLogDecisions)
                LogDecision(pick);

            currentAction = pick;
            lastReason = condition + (hitRecently ? ", hit recently" : string.Empty) + (threatInReach ? ", in reach" : string.Empty);
        }

        /// <summary>Returns true once per Defend decision.</summary>
        public bool ConsumeDefendRequest(out bool preferBlock)
        {
            preferBlock = defendPrefersBlock;
            if (!defendRequested || !BrainEnabled)
            {
                defendRequested = false;
                return false;
            }

            defendRequested = false;
            return true;
        }

        public void CancelRetreat()
        {
            retreatUntil = 0f;
        }

        /// <summary>Engager stepped in after an intentional melee miss (Combat Plan spacing).</summary>
        public void NotifyIntentionalMeleeMiss(float stepDuration)
        {
            if (stepDuration <= 0f)
                return;

            missRecoveryUntil = Time.time + stepDuration;
            currentAction = DMBrainAction.Press;
            lastReason = "intentional miss — step in";
        }

        public string BuildDebugReport()
        {
            return "Brain " + name + " [" + archetype + "/" + primaryPersonality +
                   (secondaryPersonality != DMEnemyPersonality.None ? "+" + secondaryPersonality : string.Empty) +
                   "] action " + currentAction + " (press " + Format(scorePress) + ", defend " + Format(scoreDefend) +
                   ", retreat " + Format(scoreRetreat) + ") condition " + condition + " — " + lastReason;
        }

        private static string Format(float v)
        {
            return float.IsNegativeInfinity(v) ? "-" : v.ToString("0.00");
        }

        [System.Diagnostics.Conditional("UNITY_EDITOR"), System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        private void LogDecision(DMBrainAction pick)
        {
            Debug.Log("[DMEnemyBrain] " + name + ": " + currentAction + " -> " + pick + " | " + BuildDebugReport(), this);
        }
    }
}
