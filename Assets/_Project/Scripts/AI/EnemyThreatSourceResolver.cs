using Project.Companions;
using Project.Companions.Invector;
using Project.Player.Invector;
using Project.Survival;
using UnityEngine;

namespace Project.AI
{
    /// <summary>
    /// Resolves a damage source GameObject to the combat root (player or pioneer).
    /// </summary>
    public static class EnemyThreatSourceResolver
    {
        public static Transform ResolveThreatRoot(GameObject source)
        {
            if (source == null)
                return null;

            CompanionHealth companionHealth = source.GetComponentInParent<CompanionHealth>();
            if (companionHealth != null)
                return companionHealth.transform;

            PioneerCompanionAgent companion = source.GetComponentInParent<PioneerCompanionAgent>();
            if (companion != null)
                return companion.transform;

            SurvivalStats player = source.GetComponentInParent<SurvivalStats>();
            if (player != null)
                return player.transform;

            PioneerInvectorDamageBridge playerBridge = source.GetComponentInParent<PioneerInvectorDamageBridge>();
            if (playerBridge != null)
                return playerBridge.transform;

            CompanionInvectorDamageBridge companionBridge = source.GetComponentInParent<CompanionInvectorDamageBridge>();
            if (companionBridge != null)
                return companionBridge.transform;

            return null;
        }
    }
}
