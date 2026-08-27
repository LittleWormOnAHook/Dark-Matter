using System.Collections.Generic;
using MalbersAnimations;
using MalbersAnimations.Controller;
using MalbersAnimations.Controller.AI;
using Project.AI;
using Project.Combat;
using Project.Companions;
using Project.Interaction;
using Project.Survival;
using UnityEngine;

namespace Project.Creatures
{
    /// <summary>
    /// Runtime glue between Malbers Animal Controller and Dark Matter combat/encounter systems.
    /// Malbers owns locomotion/modes/brain; DMI owns target pick, IDamageable damage, death â†’ loot.
    /// </summary>
    [DisallowMultipleComponent]
    public class DMICreatureBridge : MonoBehaviour
    {
        [SerializeField] private DMICreatureDefinition definition;
        [SerializeField] private MAnimal animal;
        [SerializeField] private MAnimalBrain brain;
        [SerializeField] private MAnimalAIControl aiControl;
        [SerializeField] private DMICreatureHealth creatureHealth;
        [SerializeField] private EnemyHealth legacyHealth;
        [SerializeField] private DMISulfurSpitAttack spitAttack;
        [SerializeField] private MDamageable damageable;

        [Header("Threat")]
        [SerializeField] private float threatSenseRange = 9f;
        [SerializeField] private float threatLeashMultiplier = 1.4f;
        [SerializeField] private float loseTargetDelay = 2.5f;
        [SerializeField] private float meleeEngageRange = 2.75f;
        [SerializeField] private float targetRefreshInterval = 0.5f;
        [SerializeField] private bool autoAcquireThreats = true;

        [Header("Melee Outgoing")]
        [SerializeField] private float meleeDamage = 12f;
        [SerializeField] private float meleeHitCooldown = 0.35f;
        [SerializeField] private float meleeHitIntervalVariation = 0f;

        private MAttackTrigger[] attackTriggers;
        private float nextTargetRefreshTime;
        private float nextMeleeHitTime;
        private float lostTargetTimer;
        private bool deathHandled;
        private bool syncingFromMalbersDamage;
        private Transform currentThreat;

        internal static readonly List<DMICreatureBridge> Live = new List<DMICreatureBridge>(64);

        public DMICreatureDefinition Definition => definition;
        public MAnimal Animal => animal;
        public MAnimalBrain Brain => brain;
        public MAnimalAIControl AiControl => aiControl;
        public DMICreatureHealth Health => creatureHealth;
        public DMISulfurSpitAttack SpitAttack => spitAttack;
        public Transform CurrentThreat => currentThreat;

        /// <summary>True while a chase target is held (within engage/leash rules).</summary>
        public bool HasActiveThreat => currentThreat != null;

        private void Awake()
        {
            CacheReferences();
            attackTriggers = GetComponentsInChildren<MAttackTrigger>(true);
            // Definition is CM authority â€” re-apply at runtime so prefab bake drift cannot disagree.
            if (definition != null)
                ConfigureFromDefinition(definition);
        }

        private void OnEnable()
        {
            if (!Live.Contains(this))
                Live.Add(this);

            CacheReferences();
            BindHealthEvents(true);
            BindMalbersDamageEvents(true);
            BindAttackTriggers(true);
            deathHandled = false;
        }

        private void OnDisable()
        {
            Live.Remove(this);

            BindHealthEvents(false);
            BindMalbersDamageEvents(false);
            BindAttackTriggers(false);
        }

        private void OnValidate()
        {
            CacheReferences();
        }

        private void Update()
        {
            if (!autoAcquireThreats || deathHandled)
                return;

            if (creatureHealth != null && creatureHealth.IsDead)
                return;

            if (Time.time < nextTargetRefreshTime)
                return;

            nextTargetRefreshTime = Time.time + Mathf.Max(0.1f, targetRefreshInterval);
            RefreshThreatTarget(moveToTarget: true);
        }

        public void ConfigureFromDefinition(DMICreatureDefinition creatureDefinition)
        {
            definition = creatureDefinition;
            CacheReferences();

            if (definition != null)
            {
                threatSenseRange = definition.threatSenseRange;
                threatLeashMultiplier = definition.threatLeashMultiplier;
                loseTargetDelay = definition.loseTargetDelay;
                meleeEngageRange = definition.meleeEngageRange;
                meleeDamage = definition.meleeDamage;
                meleeHitCooldown = Mathf.Max(0.05f, definition.meleeAttackCooldown);
                meleeHitIntervalVariation = Mathf.Clamp(definition.meleeIntervalVariation, 0f, 10f);
            }

            if (spitAttack != null && definition != null)
                spitAttack.ConfigureFromDefinition(definition);

            if (brain != null && definition != null && definition.startBrainState != null)
                brain.currentState = definition.startBrainState;
        }

        /// <summary>Sets Malbers AI target (when present) and tracks current threat for V2-A AI.</summary>
        public bool RefreshThreatTarget(bool moveToTarget)
        {
            float engageRange = definition != null ? definition.threatSenseRange : threatSenseRange;
            float leashMul = definition != null ? definition.threatLeashMultiplier : threatLeashMultiplier;
            float leashRange = engageRange * Mathf.Max(1f, leashMul);
            float loseDelay = definition != null ? definition.loseTargetDelay : loseTargetDelay;
            float step = Mathf.Max(0.1f, targetRefreshInterval);

            // Keep existing target while inside leash; drop after loseTargetDelay outside leash.
            if (currentThreat != null)
            {
                if (IsUsableThreat(currentThreat))
                {
                    float dist = Vector3.Distance(transform.position, currentThreat.position);
                    if (dist <= leashRange)
                    {
                        lostTargetTimer = 0f;
                        if (moveToTarget)
                            SetThreatTarget(currentThreat, true);
                        else if (brain != null && brain.Target != currentThreat)
                            brain.Target = currentThreat;
                        return true;
                    }

                    lostTargetTimer += step;
                    if (lostTargetTimer < Mathf.Max(0.05f, loseDelay))
                    {
                        if (moveToTarget)
                            SetThreatTarget(currentThreat, true);
                        return true;
                    }
                }

                ClearThreatTarget();
            }

            if (!DMICreatureTargetResolver.TryResolveThreat(this, engageRange, out Transform threat, out _))
            {
                ClearThreatTarget();
                return false;
            }

            lostTargetTimer = 0f;
            SetThreatTarget(threat, moveToTarget);
            return true;
        }

        /// <summary>
        /// V2-A / non-Malbers melee: apply definition melee damage to the current threat.
        /// </summary>
        public bool TryDealMeleeToThreat()
        {
            if (currentThreat == null || deathHandled)
                return false;

            float before = nextMeleeHitTime;
            HandleAttackHit(currentThreat);
            return nextMeleeHitTime > before;
        }

        public void ClearThreatTarget()
        {
            currentThreat = null;
            lostTargetTimer = 0f;

            if (aiControl != null)
                aiControl.ClearTarget();

            if (brain != null)
                brain.Target = null;
        }

        public void SetThreatTarget(Transform threat, bool moveToTarget)
        {
            if (threat == null)
            {
                ClearThreatTarget();
                return;
            }

            if (DMICreatureTargetResolver.IsAllyCreature(this, threat))
                return;

            currentThreat = threat;

            // Malbers V1 only â€” V2-A has no MAnimalAIControl.
            if (aiControl != null)
                aiControl.SetTarget(threat, moveToTarget);

            if (brain != null)
                brain.Target = threat;
        }

        private bool IsUsableThreat(Transform threat)
        {
            if (threat == null)
                return false;

            if (!threat.gameObject.activeInHierarchy)
                return false;

            return DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(this, threat);
        }

        /// <summary>Called when something damages this creature â€” retarget the attacker if valid.</summary>
        public void NotifyDamagedBy(GameObject source)
        {
            if (source == null || deathHandled)
                return;

            if (definition != null && !definition.aggroOnDamaged)
                return;

            // Prefer player/companion combat root (weapon/projectile children resolve here).
            Transform threat = EnemyThreatSourceResolver.ResolveThreatRoot(source);
            if (threat == null)
                threat = source.transform;

            if (!DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(this, threat))
                return;

            SetThreatTarget(threat, moveToTarget: true);
        }

        private void CacheReferences()
        {
            if (animal == null)
                animal = GetComponent<MAnimal>() ?? GetComponentInChildren<MAnimal>(true);

            if (brain == null)
                brain = GetComponent<MAnimalBrain>() ?? GetComponentInChildren<MAnimalBrain>(true);

            if (aiControl == null)
                aiControl = GetComponent<MAnimalAIControl>() ?? GetComponentInChildren<MAnimalAIControl>(true);

            if (creatureHealth == null)
                creatureHealth = GetComponent<DMICreatureHealth>();

            if (legacyHealth == null)
                legacyHealth = GetComponent<EnemyHealth>();

            if (spitAttack == null)
                spitAttack = GetComponent<DMISulfurSpitAttack>();

            if (damageable == null)
                damageable = GetComponent<MDamageable>() ?? GetComponentInChildren<MDamageable>(true);
        }

        private void BindHealthEvents(bool subscribe)
        {
            if (legacyHealth == null)
                legacyHealth = GetComponent<EnemyHealth>();

            if (legacyHealth == null)
                return;

            if (subscribe)
            {
                legacyHealth.Died += HandleDeath;
                legacyHealth.DamagedBy += NotifyDamagedBy;
            }
            else
            {
                legacyHealth.Died -= HandleDeath;
                legacyHealth.DamagedBy -= NotifyDamagedBy;
            }
        }

        private void BindMalbersDamageEvents(bool subscribe)
        {
            if (damageable == null || damageable.events == null)
                return;

            if (subscribe)
            {
                damageable.events.OnReceivingDamage.AddListener(HandleMalbersReceivingDamage);
                damageable.events.OnDamager.AddListener(HandleMalbersDamager);
            }
            else
            {
                damageable.events.OnReceivingDamage.RemoveListener(HandleMalbersReceivingDamage);
                damageable.events.OnDamager.RemoveListener(HandleMalbersDamager);
            }
        }

        private void BindAttackTriggers(bool subscribe)
        {
            if (attackTriggers == null || attackTriggers.Length == 0)
                attackTriggers = GetComponentsInChildren<MAttackTrigger>(true);

            if (attackTriggers == null)
                return;

            for (int i = 0; i < attackTriggers.Length; i++)
            {
                MAttackTrigger trigger = attackTriggers[i];
                if (trigger == null)
                    continue;

                if (subscribe)
                    trigger.OnHit.AddListener(HandleAttackHit);
                else
                    trigger.OnHit.RemoveListener(HandleAttackHit);
            }
        }

        private void HandleMalbersReceivingDamage(float amount)
        {
            if (syncingFromMalbersDamage || amount <= 0f || legacyHealth == null || legacyHealth.IsDead)
                return;

            syncingFromMalbersDamage = true;
            try
            {
                GameObject source = damageable != null ? damageable.Damager : null;
                legacyHealth.TakeDamage(amount, source, false);
            }
            finally
            {
                syncingFromMalbersDamage = false;
            }
        }

        private void HandleMalbersDamager(GameObject damager)
        {
            NotifyDamagedBy(damager);
        }

        private void HandleAttackHit(Transform hitTransform)
        {
            if (hitTransform == null || deathHandled)
                return;

            if (Time.time < nextMeleeHitTime)
                return;

            if (!DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(this, hitTransform))
                return;

            float damage = definition != null ? definition.meleeDamage : meleeDamage;
            if (damage <= 0f)
                return;

            CompanionHealth companion = hitTransform.GetComponentInParent<CompanionHealth>();
            if (companion != null && !companion.IsDead)
            {
                nextMeleeHitTime = Time.time + SampleMeleeInterval();
                ((IDamageable)companion).TakeDamage(damage, gameObject, false);
                return;
            }

            SurvivalStats survival = hitTransform.GetComponentInParent<SurvivalStats>();
            if (survival != null)
            {
                nextMeleeHitTime = Time.time + SampleMeleeInterval();
                ((IDamageable)survival).TakeDamage(damage, gameObject, false);
                return;
            }

            Collider hitCollider = hitTransform.GetComponent<Collider>();
            if (hitCollider == null)
                hitCollider = hitTransform.GetComponentInChildren<Collider>();
            if (hitCollider == null)
                hitCollider = hitTransform.GetComponentInParent<Collider>();

            // Skip when Malbers MDamageable already applied damage on this target (avoid double-hit).
            if (hitTransform.GetComponentInParent<MDamageable>() != null)
                return;

            IDamageable damageableTarget = null;
            if (hitCollider != null)
                damageableTarget = DamageableUtility.GetDamageable(hitCollider);

            if (damageableTarget == null)
            {
                MonoBehaviour[] behaviours = hitTransform.GetComponentsInParent<MonoBehaviour>(true);
                for (int i = 0; i < behaviours.Length; i++)
                {
                    if (behaviours[i] is IDamageable found)
                    {
                        damageableTarget = found;
                        break;
                    }
                }
            }

            if (damageableTarget == null)
                return;

            nextMeleeHitTime = Time.time + SampleMeleeInterval();

            if (hitCollider != null)
            {
                CombatHitResolver.ApplyDirectHit(
                    hitCollider,
                    hitCollider.bounds.center,
                    (hitTransform.position - transform.position).normalized,
                    damage,
                    false,
                    gameObject);
            }
            else
            {
                damageableTarget.TakeDamage(damage, gameObject, false);
            }
        }

        private float SampleMeleeInterval()
        {
            if (definition != null)
                return definition.SampleMeleeInterval();

            float interval = Mathf.Max(0.05f, meleeHitCooldown);
            float variation = Mathf.Clamp(meleeHitIntervalVariation, 0f, 10f);
            return variation > 0f ? interval + Random.Range(0f, variation) : interval;
        }

        private void HandleDeath()
        {
            if (deathHandled)
                return;

            deathHandled = true;
            autoAcquireThreats = false;
            ClearThreatTarget();

            if (brain != null)
                brain.enabled = false;

            if (aiControl != null)
                aiControl.enabled = false;

            // Malbers MAnimal.OnDisable sets RB.linearVelocity â€” Unity 6 rejects that on kinematic bodies.
            PrepareRigidbodyForMalbersDisable();

            if (animal != null)
                animal.State_Activate(StateEnum.Death);

            // Loot + disintegrate remain on EnemyLootable / EnemyDeathSequence via EnemyHealth.Died.
        }

        private void PrepareRigidbodyForMalbersDisable()
        {
            Rigidbody rb = animal != null && animal.RB != null
                ? animal.RB
                : GetComponent<Rigidbody>();
            if (rb == null)
                return;

            if (rb.isKinematic)
                rb.isKinematic = false;

            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
