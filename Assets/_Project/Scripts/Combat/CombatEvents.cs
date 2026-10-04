using System;
using UnityEngine;

namespace Project.Combat
{
    public static class CombatEvents
    {
        public static event Action<DamageInfo, GameObject> HitApplied;
        public static event Action<DamageInfo> CriticalHit;
        public static event Action<DamageInfo, GameObject> Stagger;
        public static event Action<DamageInfo, GameObject> Kill;
        public static event Action<DamageInfo, StatusEffectType, GameObject> StatusApplied;
        public static event Action<GameObject, GameObject> PerfectDodge;

        public static bool LogHitsToConsole { get; set; }

        public static void RaiseHitApplied(in DamageInfo info, GameObject target)
        {
            if (info.IsCritical)
                CriticalHit?.Invoke(info);

            HitApplied?.Invoke(info, target);

            if (LogHitsToConsole && target != null)
            {
                Debug.Log(
                    $"[CombatEvents] Hit {target.name} amount={info.Amount:0.#} element={info.Element} crit={info.IsCritical} poise={info.PoiseDamage:0.#}",
                    target);
            }
        }

        public static void RaiseStagger(in DamageInfo info, GameObject target)
        {
            Stagger?.Invoke(info, target);

            if (LogHitsToConsole && target != null)
            {
                Debug.Log(
                    $"[CombatEvents] Stagger (poise break) on {target.name} from hit amount={info.Amount:0.#}",
                    target);
            }
        }

        public static void RaiseKill(in DamageInfo info, GameObject target) =>
            Kill?.Invoke(info, target);

        public static void RaiseStatusApplied(in DamageInfo info, StatusEffectType type, GameObject target) =>
            StatusApplied?.Invoke(info, type, target);

        public static void RaisePerfectDodge(GameObject defender, GameObject attacker) =>
            PerfectDodge?.Invoke(defender, attacker);
    }
}
