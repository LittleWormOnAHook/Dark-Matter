using Project.Interaction;
using UnityEngine;

namespace Project.Combat
{
    /// <summary>Applies <see cref="DamageInfo"/> to targets, poise, i-frames, and raises <see cref="CombatEvents"/>.</summary>
    public static class CombatDamageApplicator
    {
        public static bool ApplyToCollider(Collider collider, in DamageInfo info)
        {
            if (collider == null || info.Amount <= 0f)
                return false;

            IDamageable damageable = DamageableUtility.GetDamageable(collider);
            if (damageable == null)
                return false;

            GameObject targetRoot = collider.transform.root.gameObject;
            return ApplyToDamageable(damageable, targetRoot, in info);
        }

        public static bool ApplyToDamageable(IDamageable damageable, GameObject targetRoot, in DamageInfo info)
        {
            if (damageable == null || info.Amount <= 0f)
                return false;

            if (TryBlockByInvulnerability(targetRoot, info.Source))
                return false;

            if (damageable is IDamageReceiver receiver)
                receiver.ReceiveDamage(in info);
            else
                damageable.TakeDamage(info.Amount, info.Source, info.IsCritical);

            ApplyPoise(targetRoot, in info);
            CombatEvents.RaiseHitApplied(in info, targetRoot);
            return true;
        }

        private static bool TryBlockByInvulnerability(GameObject targetRoot, GameObject source)
        {
            DMCombatIFrameController frames = targetRoot.GetComponentInChildren<DMCombatIFrameController>();
            if (frames == null || !frames.IsInvulnerable)
                return false;

            CombatEvents.RaisePerfectDodge(targetRoot, source);
            return true;
        }

        private static void ApplyPoise(GameObject targetRoot, in DamageInfo info)
        {
            if (info.PoiseDamage <= 0f)
                return;

            // EnemyHealth.TakeDamage already applies poise so melee / companion /
            // grenade paths that skip this applicator still ding. Skip here to
            // avoid double-applying on the CombatHitResolver ranged path.
            if (targetRoot != null && targetRoot.GetComponentInChildren<Project.AI.EnemyHealth>() != null)
                return;

            CombatPoise poise = targetRoot.GetComponentInChildren<CombatPoise>();
            if (poise == null)
                return;

            poise.ApplyPoiseDamage(info.PoiseDamage, in info);
        }
    }
}
