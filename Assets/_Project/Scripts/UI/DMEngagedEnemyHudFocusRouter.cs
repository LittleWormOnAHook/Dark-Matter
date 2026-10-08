using Project.AI;
using Project.Combat;
using UnityEngine;

namespace Project.UI
{
    /// <summary>
    /// Player outbound damage always retargets the top engaged enemy HUD (overrides "enemy hit player first").
    /// </summary>
    public static class DMEngagedEnemyHudFocusRouter
    {
        private static bool subscribed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            subscribed = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bind()
        {
            if (subscribed)
                return;

            subscribed = true;
            CombatEvents.HitApplied += HandleHitApplied;
        }

        private static void HandleHitApplied(DamageInfo info, GameObject target)
        {
            if (target == null || info.Amount <= 0f)
                return;

            if (!CombatPlayerSourceUtility.IsPlayerOrPioneerSource(info.Source))
                return;

            EnemyHealth health = target.GetComponent<EnemyHealth>();
            if (health == null)
                health = target.GetComponentInChildren<EnemyHealth>();
            if (health == null)
                return;

            EnemyHealthBarPresenter.RequestEngagedHudFocus(health);
        }
    }
}
