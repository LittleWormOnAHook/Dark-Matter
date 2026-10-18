using System.Collections.Generic;
using Project.Combat.Turrets;
using Project.Core;
using Project.Survival;
using UnityEngine;

namespace Project.AI
{
    public partial class EnemyAiController
    {
        private DMTurretHealth turretResponse;
        private bool turretDestroyChosen;
        private float turretDecisionUntil;

        /// <summary>
        /// Living player within this enemy's combat / sense / aggro range.
        /// Turrets are never preferred while this is true.
        /// </summary>
        internal bool IsPlayerInCombatRange(out Transform player)
        {
            player = ResolveLivingPlayerForTurretPriority();
            if (player == null)
                return false;

            float distance = HorizontalDistance(transform.position, player.position);
            if (distance <= ResolvePlayerCombatRange())
                return true;

            return HasActivePlayerAggro();
        }

        private Transform ResolveLivingPlayerForTurretPriority()
        {
            Transform candidate = playerTarget;
            if (candidate == null)
                candidate = PlayerReference.ResolveTransform();
            if (candidate == null || DMTurretPlayerControl.IsSeatedPlayer(candidate))
                return null;

            SurvivalStats stats = candidate.GetComponentInParent<SurvivalStats>();
            if (stats != null && stats.IsDead)
                return null;
            if (DMTurretPlayerControl.IsSeatedPlayer(stats != null ? stats.transform : candidate))
                return null;
            return stats != null ? stats.transform : candidate;
        }

        private float ResolvePlayerCombatRange()
        {
            float range = playerThreatRange;
            if (senses != null)
                range = Mathf.Max(range, senses.OuterSenseRange);
            if (combat != null)
                range = Mathf.Max(range, combat.AttackRange);
            if (combatBridge != null && combatBridge.IsArmedRangedPreferred())
                range = Mathf.Max(range, combatBridge.RangedEngageRange);
            return range;
        }

        private void PreferPlayerOverTurretTarget(Transform player)
        {
            if (DMTurretHealth.IsTurretTransform(combat != null ? combat.CurrentTarget : null))
            {
                if (player != null && AllowsCombatTarget(player))
                    combat.SetTarget(player);
                else
                    combat.SetTarget(null);
            }

            turretResponse = null;
            turretDestroyChosen = false;
        }

        /// <summary>
        /// Player turrets that threaten this enemy: flee out of fire range by default,
        /// or roll a profile chance to attack the turret instead. No NavMesh.
        /// Player in combat range always wins over a turret.
        /// </summary>
        private bool TickTurretThreatResponse()
        {
            if (health == null || health.IsDead)
                return false;

            if (IsPlayerInCombatRange(out Transform player))
            {
                PreferPlayerOverTurretTarget(player);
                return false;
            }

            DMTurretProfile profile = DMTurretProfile.Live;
            float scan = profile != null ? profile.ScanRange : 45f;
            float fire = profile != null ? profile.FireRange : 22f;
            float margin = profile != null ? profile.EnemyFleeMargin : 6f;
            float decisionSeconds = profile != null ? profile.EnemyTurretDecisionSeconds : 8f;

            DMTurretHealth nearest = ResolvePlayerTurretThreat(scan);
            if (nearest == null || nearest.IsDead)
            {
                turretResponse = null;
                return false;
            }

            DMTurretRuntime runtime = nearest.GetComponent<DMTurretRuntime>();
            bool firingAtUs = runtime != null
                && runtime.BrainMode == DMTurretBrainMode.Fire
                && runtime.CurrentAutoTarget == transform;
            bool inFire = HorizontalDistance(transform.position, nearest.transform.position) <= fire + 0.5f;
            bool damagedByNearest = HasThreatDamageFromTurret(nearest);
            if (!firingAtUs && !inFire && !damagedByNearest && turretResponse != nearest)
                return false;

            if (turretResponse != nearest || Time.time >= turretDecisionUntil)
            {
                turretResponse = nearest;
                turretDecisionUntil = Time.time + decisionSeconds;
                turretDestroyChosen = Random.value < (profile != null ? profile.RollEnemyDestroyChance() : 0.25f);
            }

            float distance = HorizontalDistance(transform.position, nearest.transform.position);
            if (turretDestroyChosen)
            {
                combat.SetTarget(nearest.transform);
                return false;
            }

            float safe = fire + margin;
            if (distance >= safe)
            {
                turretResponse = null;
                return false;
            }

            Vector3 away = transform.position - nearest.transform.position;
            away.y = 0f;
            StepFlat(away, ResolveChaseSpeed(), safe - distance);
            FaceTowards(nearest.transform.position);
            return true;
        }

        private DMTurretHealth ResolvePlayerTurretThreat(float scanRange)
        {
            DMTurretHealth nearestDamager = null;
            float nearestDamagerDistance = float.MaxValue;
            foreach (KeyValuePair<Transform, ThreatEntry> pair in threatLedger)
            {
                if (pair.Key == null || pair.Value.TotalDamage <= 0f)
                    continue;

                DMTurretHealth turret = pair.Key.GetComponentInParent<DMTurretHealth>();
                if (turret == null || turret.IsDead)
                    continue;

                DMTurretRuntime runtime = turret.GetComponent<DMTurretRuntime>();
                if (runtime == null || runtime.Faction != DMTurretFaction.Player)
                    continue;

                float distance = HorizontalDistance(transform.position, turret.transform.position);
                if (distance > scanRange || distance >= nearestDamagerDistance)
                    continue;

                nearestDamager = turret;
                nearestDamagerDistance = distance;
            }

            if (nearestDamager != null)
                return nearestDamager;

            return DMTurretRegistry.FindNearest(transform.position, scanRange, DMTurretFaction.Player);
        }

        private bool HasThreatDamageFromTurret(DMTurretHealth turret)
        {
            if (turret == null)
                return false;

            foreach (KeyValuePair<Transform, ThreatEntry> pair in threatLedger)
            {
                if (pair.Key == null || pair.Value.TotalDamage <= 0f)
                    continue;
                DMTurretHealth entry = pair.Key.GetComponentInParent<DMTurretHealth>();
                if (entry == turret)
                    return true;
            }

            return false;
        }
    }
}
