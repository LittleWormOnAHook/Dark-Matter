using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Serialization;
using Project.AI.Invector;
using Project.Companions;
using Project.Core;
using Project.Survival;

namespace Project.AI
{
    // Aggro/threat-ledger tracking, player-target legality (the "who is this enemy allowed to hit"
    // gate that keeps bystander players out of pioneer brawls), and player-died/revived cleanup.
    // Split out of EnemyAiController.cs.
    public partial class EnemyAiController
    {
        private void TrySubscribePlayerEvents()
        {
            if (playerSurvivalStats != null)
                return;

            Transform playerTransform = PlayerReference.ResolveTransform();
            if (playerTransform == null)
                return;

            playerSurvivalStats = playerTransform.GetComponent<SurvivalStats>();
            if (playerSurvivalStats == null)
                return;

            playerSurvivalStats.PlayerDied += HandlePlayerDied;
            playerSurvivalStats.PlayerRevived += HandlePlayerRevived;
        }

        private void UnsubscribePlayerEvents()
        {
            if (playerSurvivalStats == null)
                return;

            playerSurvivalStats.PlayerDied -= HandlePlayerDied;
            playerSurvivalStats.PlayerRevived -= HandlePlayerRevived;
            playerSurvivalStats = null;
        }

        private void HandlePlayerDied()
        {
            ClearPlayerThreat();
        }

        private void HandlePlayerRevived()
        {
            ClearPlayerThreat();
        }

        /// <summary>
        /// Drop the player as a combat target on death/respawn so enemies do not keep pounding
        /// a corpse or instantly re-aggro a freshly respawned player.
        /// </summary>
        private void ClearPlayerThreat()
        {
            if (IsCombatTargetPlayer(combat.CurrentTarget))
                combat.SetTarget(null);

            if (aggroTarget != null && !IsPioneer(aggroTarget))
            {
                aggroTarget = null;
                aggroUntil = 0f;
            }

            playerTarget = null;

            if ((state == AiState.Chase || state == AiState.Attack || state == AiState.Defensive) &&
                !HasActiveAggroTarget() && !IsTargetingLivingPioneer())
                GiveUpChaseAndReturnHome();
        }

        public void NotifyAggroFromThreat(Transform attacker)
        {
            if (attacker == null || health == null || health.IsDead)
                return;

            if (IsCombatTargetPlayer(attacker) && !ShouldEngagePlayer(attacker) && !HasThreatDamage(attacker))
                return;

            ApplyAggroTarget(attacker);
            if (state != AiState.Attack && state != AiState.Chase && state != AiState.Defensive)
            {
                float dist = HorizontalDistance(transform.position, attacker.position);
                EnterState(ResolveAttackEntryState(attacker, dist));
            }
        }

        /// <summary>
        /// Ranged impact heard within sense range — chance to aggro the resolved shooter (player/pioneer).
        /// </summary>
        public void NotifyHeardCombatImpact(GameObject source)
        {
            if (!aggroOnHeardHit || health == null || health.IsDead || !chasePlayer)
                return;

            if (Time.time < nextHearingAggroTime)
                return;

            nextHearingAggroTime = Time.time + Mathf.Max(0.05f, hearingCooldown);

            if (hearingAggroChance <= 0f || UnityEngine.Random.value > hearingAggroChance)
                return;

            Transform attacker = EnemyThreatSourceResolver.ResolveThreatRoot(source);
            if (attacker == null)
                return;

            // Treat heard-hit as provocation so AllowsCombatTarget / ShouldEngagePlayer allow the player.
            RecordThreatDamage(attacker, 0.01f);
            NotifyAggroFromThreat(attacker);

            if (debugAggro)
            {
                Debug.Log(
                    $"[EnemyAggro] {name} heard-hit aggro -> {attacker.name}",
                    this);
            }
        }

        public void NotifyAggroFromDamage(Transform attacker, float damage)
        {
            if (!aggroOnDamaged || attacker == null || health == null || health.IsDead || damage <= 0f)
                return;

            RecordThreatDamage(attacker, damage);
            ApplyAggroTarget(ResolvePrimaryThreat() ?? attacker);

            if (debugAggro)
            {
                Debug.Log(
                    $"[EnemyAggro] {name} damage aggro -> {aggroTarget.name} dmg={damage:F1}",
                    this);
            }

            if (state != AiState.Attack && state != AiState.Chase && state != AiState.Defensive)
            {
                float dist = HorizontalDistance(transform.position, attacker.position);
                EnterState(ResolveAttackEntryState(attacker, dist));
            }
        }

        private void HandleDamagedWithSource(float damage, GameObject source, bool isCritical)
        {
            if (!aggroOnDamaged || source == null || health == null || health.IsDead || damage <= 0f)
                return;

            Transform attacker = EnemyThreatSourceResolver.ResolveThreatRoot(source);
            if (attacker == null)
            {
                if (debugAggro)
                    Debug.LogWarning($"[EnemyAggro] {name} could not resolve threat from {source.name}", this);
                return;
            }

            NotifyAggroFromDamage(attacker, damage);
        }

        private void ApplyAggroTarget(Transform attacker)
        {
            aggroTarget = attacker;
            aggroUntil = Time.time + AggroDuration;
            lastKnownPlayerPosition = attacker.position;
            combat.SetTarget(attacker);
        }

        private void RecordThreatDamage(Transform attacker, float damage)
        {
            if (attacker == null || damage <= 0f)
                return;

            if (firstBloodTarget == null)
                firstBloodTarget = attacker;

            if (threatLedger.TryGetValue(attacker, out ThreatEntry entry))
            {
                entry.TotalDamage += damage;
                entry.LastHitTime = Time.time;
                threatLedger[attacker] = entry;
            }
            else
            {
                threatLedger[attacker] = new ThreatEntry
                {
                    Root = attacker,
                    TotalDamage = damage,
                    LastHitTime = Time.time
                };
            }
        }

        private void ClearThreatLedger()
        {
            threatLedger.Clear();
            firstBloodTarget = null;
        }

        private bool HasThreatDamage(Transform candidate)
        {
            if (candidate == null)
                return false;

            return threatLedger.TryGetValue(candidate, out ThreatEntry entry) &&
                   entry.TotalDamage > 0f;
        }

        private float GetThreatDamage(Transform candidate)
        {
            if (candidate == null)
                return 0f;

            return threatLedger.TryGetValue(candidate, out ThreatEntry entry)
                ? entry.TotalDamage
                : 0f;
        }

        private Transform ResolvePrimaryThreat()
        {
            Transform best = null;
            float bestDamage = 0f;
            float bestRecent = float.MinValue;

            foreach (KeyValuePair<Transform, ThreatEntry> pair in threatLedger)
            {
                ThreatEntry entry = pair.Value;
                if (entry.Root == null || !IsLivingThreat(entry.Root))
                    continue;

                if (entry.TotalDamage > bestDamage ||
                    (Mathf.Approximately(entry.TotalDamage, bestDamage) && entry.LastHitTime > bestRecent))
                {
                    best = entry.Root;
                    bestDamage = entry.TotalDamage;
                    bestRecent = entry.LastHitTime;
                }
            }

            if (best == null)
                return firstBloodTarget;

            Transform current = aggroTarget;
            if (current == null || !IsLivingThreat(current))
                return best;

            float currentDamage = GetThreatDamage(current);
            if (best == current)
                return current;

            float leadRequired = currentDamage * (1f + threatSwitchLeadFraction);
            return bestDamage >= leadRequired ? best : current;
        }

        private static bool IsLivingThreat(Transform candidate)
        {
            if (candidate == null)
                return false;

            CompanionHealth companionHealth = candidate.GetComponentInParent<CompanionHealth>();
            if (companionHealth != null)
                return !companionHealth.IsDead;

            SurvivalStats stats = candidate.GetComponentInParent<SurvivalStats>();
            return stats == null || !stats.IsDead;
        }

        /// <summary>
        /// Central gate for whether this enemy is allowed to acquire or keep the player as
        /// a melee target. Blocks bystanders while pioneers are actively fighting nearby.
        /// </summary>
        public bool AllowsCombatTarget(Transform candidate)
        {
            if (candidate == null)
                return false;

            if (!IsCombatTargetPlayer(candidate))
                return true;

            SurvivalStats stats = candidate.GetComponent<SurvivalStats>();
            if (stats != null && (stats.IsDead || stats.HasEnemyCombatImmunity))
                return false;

            // Pioneer holds aggro — player is never a legal target until that window expires.
            if (HasActiveAggroTarget() && IsPioneer(aggroTarget))
                return false;

            // Player who personally provoked us may be struck even with pioneers nearby.
            if (HasActivePlayerAggro() && aggroTarget == candidate)
                return true;

            if (HasThreatDamage(candidate))
                return true;

            // Pioneers are the front line. A bystander player in the melee scrum is not
            // fair game — that was the "death by unknown" pattern (enemy aggro on pioneer,
            // but still pounding the player standing next to them).
            if (HasNearbyLivingPioneer(pioneerRetargetRadius * 1.75f))
                return false;

            // Hostile on sight: a visible lone player within threat/vision range is fair game.
            if (HorizontalDistance(transform.position, candidate.position) <= playerThreatRange)
                return true;

            return senses.CanSeeThreat(candidate);
        }

        private bool HasPioneerDamageAggro()
        {
            return HasActiveAggroTarget() && IsPioneer(aggroTarget);
        }

        private bool HasActivePlayerAggro()
        {
            return HasActiveAggroTarget() && !IsPioneer(aggroTarget);
        }

        /// <summary>
        /// Picks the closest visible threat this enemy is allowed to engage — pioneers by
        /// sight (they used to be invisible to senses), the player only when legal.
        /// </summary>
        private bool TryPickVisibleThreat(Transform visiblePlayer, out Transform threat)
        {
            Transform visiblePioneer = senses.GetVisiblePioneerTarget();
            bool playerAllowed = visiblePlayer != null && AllowsCombatTarget(visiblePlayer);

            if (visiblePioneer != null && playerAllowed)
            {
                // Pioneer holds damage aggro; otherwise the player is the higher-priority threat.
                if (HasActiveAggroTarget() && IsPioneer(aggroTarget))
                    threat = visiblePioneer;
                else if (playerAllowed)
                    threat = visiblePlayer;
                else
                    threat = visiblePioneer;
                return true;
            }

            threat = visiblePioneer != null ? visiblePioneer : (playerAllowed ? visiblePlayer : null);
            return threat != null;
        }

        private bool HasNearbyLivingPioneer(float maxRange)
        {
            return PickClosestNearbyPioneerWithin(maxRange) != null;
        }

        private void TryCorrectIllegalPlayerTarget()
        {
            Transform current = combat.CurrentTarget;
            if (!IsCombatTargetPlayer(current) || AllowsCombatTarget(current))
                return;

            Transform pioneer = PickClosestNearbyPioneerWithin(pioneerRetargetRadius * 1.75f);
            combat.SetTarget(pioneer);
        }

        /// <summary>
        /// Visible players are not auto-combat targets. Only engage if they damaged us,
        /// are in melee threat range, or are not immune after respawn.
        /// </summary>
        private bool ShouldEngagePlayer(Transform player)
        {
            if (player == null)
                return false;

            SurvivalStats stats = player.GetComponent<SurvivalStats>();
            if (stats != null && (stats.IsDead || stats.HasEnemyCombatImmunity))
                return false;

            if (HasActiveAggroTarget() && !IsPioneer(aggroTarget))
                return true;

            if (HasThreatDamage(player))
                return true;

            if (HasActiveAggroTarget() && IsPioneer(aggroTarget))
                return false;

            return HorizontalDistance(transform.position, player.position) <= playerThreatRange;
        }

        private static bool IsCombatTargetPlayer(Transform candidate)
        {
            return candidate != null && candidate.GetComponent<SurvivalStats>() != null;
        }

        private bool HasActiveAggroTarget()
        {
            if (aggroTarget == null || Time.time >= aggroUntil)
                return false;

            CompanionHealth companionHealth = aggroTarget.GetComponentInParent<CompanionHealth>();
            if (companionHealth != null)
                return !companionHealth.IsDead;

            SurvivalStats stats = aggroTarget.GetComponentInParent<SurvivalStats>();
            return stats == null || !stats.IsDead;
        }

        private static bool IsPioneer(Transform candidate)
        {
            return candidate != null && candidate.GetComponentInParent<PioneerCompanionAgent>() != null;
        }

        private bool IsTargetingLivingPioneer()
        {
            Transform current = combat.CurrentTarget;
            if (current == null)
                return false;

            if (current.GetComponentInParent<CompanionHealth>() is { IsDead: false })
                return true;

            return current.GetComponentInParent<PioneerCompanionAgent>() != null && combat.HasLivingTarget();
        }
    }
}
