using Invector;
using Project.AI;
using Project.AI.Invector;
using UnityEngine;

namespace Project.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatPoise : MonoBehaviour
    {
        [SerializeField] private float maxPoise = 100f;
        [SerializeField] private bool useProfileDefaults = true;

        private float currentPoise;
        private float lastPoiseHitTime = float.NegativeInfinity;
        private EnemyInvectorRagdollBridge ragdollBridge;

        public float CurrentPoise => currentPoise;

        // Attack interrupt bookkeeping (read / written by AttackInterruptEvaluator).
        /// <summary>Frame number of the last poise break (== Time.frameCount when the current hit broke poise).</summary>
        public int LastBreakFrame { get; private set; } = -1;
        public float LastInterruptTime { get; set; } = float.NegativeInfinity;
        public float LastPlayerHitTime { get; set; } = float.NegativeInfinity;
        public int ChainHitCount { get; set; }

        // Slice 2 hit reactions (DMEnemyHitReaction).
        public int ReactionChainCount { get; set; }
        public float LastReactionHitTime { get; set; } = float.NegativeInfinity;
        public float BigReactionImmuneUntil { get; set; } = float.NegativeInfinity;
        public float MaxPoise => maxPoise;

        private void Awake()
        {
            ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>();
            ApplyProfileMax();
            currentPoise = maxPoise;
        }

        private void OnEnable()
        {
            ApplyProfileMax();
            if (currentPoise <= 0f)
                currentPoise = maxPoise;
            else
                currentPoise = Mathf.Min(currentPoise, maxPoise);
        }

        public void ResetPoise()
        {
            ApplyProfileMax();
            currentPoise = maxPoise;
        }

        private void Update()
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null || profile.poiseRegenPerSecond <= 0f)
                return;

            if (Time.time < lastPoiseHitTime + profile.poiseRegenDelayAfterHit)
                return;

            if (currentPoise >= maxPoise)
                return;

            currentPoise = Mathf.Min(maxPoise, currentPoise + profile.poiseRegenPerSecond * Time.deltaTime);
        }

        public void ApplyPoiseDamage(float amount, in DamageInfo info)
        {
            ApplyProfileMax();
            if (amount <= 0f || maxPoise <= 0f)
                return;

            amount = ClampPoiseDamagePerHit(amount);
            if (amount <= 0f)
                return;

            lastPoiseHitTime = Time.time;
            currentPoise = Mathf.Max(0f, currentPoise - amount);

            if (currentPoise > 0f)
                return;

            currentPoise = maxPoise;
            LastBreakFrame = Time.frameCount;
            TriggerPoiseBreak(in info);
        }

        private float ClampPoiseDamagePerHit(float amount)
        {
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            float fraction = profile != null ? profile.maxPoiseDamageFractionPerHit : 0.4f;
            if (fraction <= 0f)
                fraction = 0.4f;
            if (fraction >= 1f)
                return amount;

            return Mathf.Min(amount, maxPoise * fraction);
        }

        private void TriggerPoiseBreak(in DamageInfo info)
        {
            // Audio listens to CombatEvents.Stagger. Raise it before any visual
            // ragdoll / settle guard so the triple ding still plays when
            // TryHitStagger bails during spawn settle.
            CombatEvents.RaiseStagger(in info, gameObject);

            TrainingDummy dummy = GetComponent<TrainingDummy>();
            if (dummy != null)
            {
                dummy.ApplyPoiseStaggerReaction(info.Source);
                return;
            }

            if (ragdollBridge == null)
                ragdollBridge = GetComponent<EnemyInvectorRagdollBridge>()
                    ?? GetComponentInChildren<EnemyInvectorRagdollBridge>();

            if (ragdollBridge == null)
            {
                DM_CombatCoreProfile profileLog = DM_CombatCoreProfile.Live;
                if (profileLog != null && profileLog.logCombatEventsInPlay)
                {
                    Debug.Log(
                        $"[CombatStagger] Poise skipped visual: no EnemyInvectorRagdollBridge on {gameObject.name}",
                        gameObject);
                }

                return;
            }

            var pioneerDamage = new vDamage(Mathf.Max(1, Mathf.RoundToInt(info.Amount)))
            {
                sender = info.Source != null ? info.Source.transform : null
            };
            float staggerSeconds = 0.32f;
            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile != null && profile.humanoidPoiseStaggerSeconds > 0f)
                staggerSeconds = profile.humanoidPoiseStaggerSeconds;

            ragdollBridge.TryHitStagger(
                pioneerDamage,
                info.Amount,
                isCritical: false,
                weaponRequestsStagger: true,
                weaponStaggerSeconds: staggerSeconds);
        }

        private void ApplyProfileMax()
        {
            if (!useProfileDefaults)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            if (GetComponent<TrainingDummy>() != null)
                maxPoise = profile.trainingDummyPoiseMax;
            else
            {
                // EnemyDefinition.poiseMultiplier scales the humanoid max (definitions are the authority).
                EnemyAiController ai = GetComponent<EnemyAiController>();
                EnemyDefinition def = ai != null ? ai.AppliedDefinition : null;
                float mult = def != null ? Mathf.Max(0.25f, def.poiseMultiplier) : 1f;
                maxPoise = profile.humanoidPoiseMax * mult;
            }
        }
    }
}
