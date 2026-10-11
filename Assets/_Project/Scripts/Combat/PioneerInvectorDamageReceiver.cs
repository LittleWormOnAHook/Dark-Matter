using Invector;
using Project.AI;
using Project.AI.Invector;
using Project.Data;
using Project.Player.Invector;
using Project.UI;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Forwards Invector vDamage hits into Pioneer EnemyHealth / IDamageable with ItemData-based rolls.
    /// </summary>
    [DisallowMultipleComponent]
    public class PioneerInvectorDamageReceiver : MonoBehaviour, vIDamageReceiver
    {
        [SerializeField] private bool applyToChildren = true;

        private readonly OnReceiveDamage _onStartReceiveDamage = new OnReceiveDamage();
        private readonly OnReceiveDamage _onReceiveDamage = new OnReceiveDamage();

        public OnReceiveDamage onStartReceiveDamage => _onStartReceiveDamage;
        public OnReceiveDamage onReceiveDamage => _onReceiveDamage;

        private Collider pendingMeleeHitCollider;

        private void Awake()
        {
            // Invector melee only damages colliders whose tag is in vMeleeManager.hitProperties
            // .hitDamageTags ("Enemy"). Ranged hits use physics layers (vShooterManager.damageLayer),
            // not tags — this tag assignment is for melee only.
            if (gameObject.CompareTag("Untagged"))
                gameObject.tag = "Enemy";
        }

        /// <summary>Forwards melee hits from ragdoll bone colliders with the struck shape for zone FX / multipliers.</summary>
        public void TakeDamageFromBone(vDamage damage, Collider boneCollider)
        {
            pendingMeleeHitCollider = boneCollider;
            try
            {
                TakeDamage(damage);
            }
            finally
            {
                pendingMeleeHitCollider = null;
            }
        }

        public void TakeDamage(vDamage damage)
        {
            if (damage == null)
                return;

            GameObject senderForDedupe = damage.sender != null ? damage.sender.gameObject : null;
            if (!PioneerMeleeSwingHitDedupe.TryAcceptHit(senderForDedupe, gameObject))
                return;

            // Humanoid enemies are transform-driven — strip knockback so shots do not launch the body.
            EnemyHealth enemyHealth = GetComponentInParent<EnemyHealth>();
            bool wantsHitStagger = damage.activeRagdoll;
            float staggerSeconds = damage.senselessTime;
            vDamage staggerSnapshot = new vDamage(damage);

            if (enemyHealth != null)
            {
                damage.force = Vector3.zero;
                damage.activeRagdoll = false;
                damage.hitReaction = false;
            }

            onStartReceiveDamage.Invoke(damage);

            GameObject sender = damage.sender != null ? damage.sender.gameObject : null;
            float pioneerDamage = PioneerInvectorDamageResolver.ResolveOutgoingDamage(damage, sender, out bool critical);

            if (enemyHealth != null)
            {
                Collider zoneCollider = pendingMeleeHitCollider != null ? pendingMeleeHitCollider : GetComponent<Collider>();
                Animator animator = enemyHealth.GetComponentInChildren<Animator>();
                CombatBodyPart zone = DMCombatBodyPartUtility.ResolveFromCollider(zoneCollider, animator);
                pioneerDamage = DMCombatBodyPartUtility.ApplyZoneDamageMultiplier(
                    pioneerDamage,
                    zone,
                    DM_EnemyHitMarkProfile.LiveOrDefault);
            }

            GameObject source = sender != null
                ? sender
                : (damage.receiver != null ? damage.receiver.gameObject : null);
            source = CombatPlayerSourceUtility.NormalizeForDamageEvents(source) ?? source;

            // Invector stamps the weapon hitbox's world position at the moment of impact.
            Vector3? hitPoint = damage.hitPosition != Vector3.zero ? damage.hitPosition : (Vector3?)null;
            ApplyToPioneerHealth(pioneerDamage, source, critical, hitPoint, enemyHealth, staggerSnapshot, wantsHitStagger, staggerSeconds);

            onReceiveDamage.Invoke(damage);
        }

        private void ApplyToPioneerHealth(
            float damage,
            GameObject source,
            bool isCritical,
            Vector3? hitPoint,
            EnemyHealth enemyHealth,
            vDamage staggerSnapshot,
            bool wantsHitStagger,
            float staggerSeconds)
        {
            if (applyToChildren)
            {
                if (enemyHealth == null)
                    enemyHealth = GetComponentInParent<EnemyHealth>();

                if (enemyHealth != null)
                {
                    if (enemyHealth.IsDead || damage <= 0f)
                        return;

                    EnemyInvectorRagdollBridge ragdollBridge = enemyHealth.GetComponent<EnemyInvectorRagdollBridge>();
                    if (ragdollBridge != null)
                    {
                        // Killing blows must feed corpse impulse before Died runs death presentation.
                        ragdollBridge.RememberHitForDeath(staggerSnapshot);
                        bool heavyHit = AttackInterruptEvaluator.IsPlayerChargedHit(source, out _);
                        CombatPoise hitPoise = enemyHealth.GetComponent<CombatPoise>();
                        CombatUiSpawner.NextEnemyDamageHeavy = heavyHit;
                        enemyHealth.TakeDamage(damage, source, isCritical);
                        CombatUiSpawner.NextEnemyDamageHeavy = false;
                        DMEnemyHitMarks.MeleeHeavyContext = heavyHit;
                        DMEnemyHitMarks.MeleeChainContext = hitPoise != null ? hitPoise.ChainHitCount : 0;
                        SpawnHitVfx(damage, source, hitPoint);
                        DMEnemyHitMarks.MeleeHeavyContext = false;
                        DMEnemyHitMarks.MeleeChainContext = 0;
                        PlayMeleeHitAudio(hitPoint, isCritical);

                        if (enemyHealth.IsDead)
                        {
                            DMCombatCameraShake.TryPlayMeleeHit(source, true, true);
                            ragdollBridge.ActivateCorpseRagdoll(staggerSnapshot);
                        }
                        else
                        {
                            ragdollBridge.TryHitStagger(
                                staggerSnapshot,
                                damage,
                                isCritical,
                                wantsHitStagger,
                                staggerSeconds);
                            NotifyAttackInterrupt(enemyHealth, damage, source, isCritical, hitPoint, staggerSnapshot);
                            bool poiseBreak = hitPoise != null && hitPoise.LastBreakFrame == Time.frameCount;
                            DMEnemyHitReaction.OnPlayerMeleeHit(enemyHealth, ragdollBridge, staggerSnapshot, damage, source, isCritical, poiseBreak);
                            DMCombatCameraShake.TryPlayMeleeHit(source, isCritical, poiseBreak);
                            DMMeleeHitStop.OnPlayerMeleeHit(source, enemyHealth, isCritical);
                        }

                        return;
                    }

                    CombatUiSpawner.NextEnemyDamageHeavy = AttackInterruptEvaluator.IsPlayerChargedHit(source, out _);
                    enemyHealth.TakeDamage(damage, source, isCritical);
                    CombatUiSpawner.NextEnemyDamageHeavy = false;
                    SpawnHitVfx(damage, source, hitPoint);
                    PlayMeleeHitAudio(hitPoint, isCritical);
                    DMCombatCameraShake.TryPlayMeleeHit(source, isCritical, false);
                    return;
                }
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                PioneerInvectorDamageBridge.ApplyPioneerDamageToCollider(collider, damage, source, isCritical, hitPoint);
                PlayMeleeHitAudio(hitPoint, isCritical);
            }
        }

        /// <summary>Hands the landed player hit to the attack interrupt evaluator (beside CombatPoise).</summary>
        private static void NotifyAttackInterrupt(
            EnemyHealth enemyHealth,
            float damage,
            GameObject source,
            bool isCritical,
            Vector3? hitPoint,
            vDamage damageSnapshot)
        {
            if (enemyHealth == null || enemyHealth.IsDead || source == null)
                return;

            // Player attacks only (companion hits do not interrupt).
            if (source.GetComponentInParent<PioneerShooterMeleeInput>() == null)
                return;

            var info = new DamageInfo
            {
                Amount = damage,
                IsCritical = isCritical,
                Source = source,
                HitPoint = hitPoint ?? enemyHealth.transform.position,
                IsChargedHit = AttackInterruptEvaluator.IsPlayerChargedHit(source, out int swingStep),
                SwingStep = swingStep
            };
            AttackInterruptEvaluator.TryInterruptFromPlayerHit(enemyHealth, in info, damageSnapshot);
        }

        private void PlayMeleeHitAudio(Vector3? hitPoint, bool isCritical)
        {
            Vector3 point = hitPoint ?? transform.position;
            CombatHitAudio.PlayWeaponHit(point, isCritical, GetComponent<Collider>());
        }

        private void SpawnHitVfx(float damage, GameObject source, Vector3? hitPoint)
        {
            Vector3 point = hitPoint ?? transform.position;

            Collider receiverCollider = pendingMeleeHitCollider != null
                ? pendingMeleeHitCollider
                : GetComponent<Collider>();
            if (receiverCollider != null && receiverCollider.enabled)
                point = receiverCollider.ClosestPoint(point);

            Vector3 direction = source != null
                ? (point - source.transform.position).normalized
                : transform.forward;

            EnemyHealth enemyHealth = GetComponentInParent<EnemyHealth>();
            if (enemyHealth != null)
            {
                ItemData weapon = PioneerInvectorDamageBridge.ResolveEquippedWeapon(source);
                DMEnemyHitMarks.HandleMeleeHit(
                    enemyHealth,
                    receiverCollider,
                    point,
                    direction,
                    damage,
                    weapon,
                    source);
                return;
            }

            CombatHitVfx.SpawnBloodSplatter(point, direction, -direction, damage);
        }
    }
}
