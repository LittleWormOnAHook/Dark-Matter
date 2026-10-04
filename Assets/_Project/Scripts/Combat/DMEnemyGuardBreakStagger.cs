using Invector;
using Invector.vEventSystems;
using Project.AI;
using Project.AI.Invector;
using Project.Audio;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>
    /// Applies block / parry guard-break stagger on humanoid enemies when the player successfully blocks melee.
    /// </summary>
    public static class DMEnemyGuardBreakStagger
    {
        public static void TryApplyFromBlock(
            vIMeleeFighter attacker,
            Transform defender,
            bool isParry,
            Transform damageSender = null)
        {
            if (defender == null)
                return;

            DM_CombatCoreProfile profile = DM_CombatCoreProfile.Live;
            if (profile == null)
                return;

            float staggerSeconds = isParry ? profile.parryStaggerSeconds : profile.blockStaggerSeconds;
            if (staggerSeconds <= 0f)
                return;

            // vMeleeManager.fighter is null on enemies: the stripper removes vMeleeCombatInput,
            // then Init assigns fighter from that missing component. The hit still sets
            // damage.sender to the melee-manager root. A null fighter used to return here
            // before the stagger and before the block ding / parry triple ring.
            Transform source = ResolveAttackerRoot(attacker, damageSender);
            if (source == null)
            {
                LogSkip(profile, isParry, "no attacker root");
                return;
            }

            EnemyHealth health = source.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead)
            {
                LogSkip(profile, isParry, health == null ? "no EnemyHealth" : "attacker dead");
                return;
            }

            EnemyInvectorRagdollBridge bridge = health.GetComponent<EnemyInvectorRagdollBridge>()
                ?? health.GetComponentInChildren<EnemyInvectorRagdollBridge>()
                ?? health.GetComponentInParent<EnemyInvectorRagdollBridge>();
            if (bridge == null)
            {
                LogSkip(profile, isParry, "no EnemyInvectorRagdollBridge");
                return;
            }

            Vector3 flat = health.transform.position - defender.position;
            flat.y = 0f;
            if (flat.sqrMagnitude < 0.01f)
                flat = -defender.forward;
            flat.Normalize();

            float impulseScale = profile.blockParryStaggerImpulseScale;
            if (isParry)
                impulseScale *= profile.parryStaggerImpulseBonus;

            var sourceDamage = new vDamage(0)
            {
                sender = defender,
                hitReaction = true,
                force = flat * (0.18f * Mathf.Max(0.5f, impulseScale))
            };

            bridge.TryGuardBreakStagger(sourceDamage, staggerSeconds, isParry);
            PlayGuardBreakAudio(isParry);

            if (profile.logCombatEventsInPlay)
            {
                string kind = isParry ? "Parry" : "Block";
                Debug.Log(
                    $"[CombatStagger] {kind} guard-break on {health.name} for {staggerSeconds:0.##}s",
                    health);
            }
        }

        private static Transform ResolveAttackerRoot(vIMeleeFighter attacker, Transform damageSender)
        {
            if (damageSender != null)
                return damageSender;

            if (!IsLiveFighter(attacker))
                return null;

            return attacker.transform;
        }

        private static bool IsLiveFighter(vIMeleeFighter attacker)
        {
            if (attacker == null)
                return false;

            // A destroyed MonoBehaviour stored as an interface is not a C# null.
            if (attacker is Object unityObject && unityObject == null)
                return false;

            return true;
        }

        private static void LogSkip(DM_CombatCoreProfile profile, bool isParry, string reason)
        {
            if (profile == null || !profile.logCombatEventsInPlay)
                return;

            string kind = isParry ? "Parry" : "Block";
            Debug.Log($"[CombatStagger] {kind} guard-break skipped: {reason}");
        }

        private static void PlayGuardBreakAudio(bool isParry)
        {
            GameAudioManager.EnsureExists();
            if (GameAudioManager.Instance == null)
                return;

            if (isParry)
                GameAudioManager.Instance.PlayPoiseStaggerTripleDing();
            else
                GameAudioManager.Instance.PlayGuardBlockSoftDing();
        }
    }
}
