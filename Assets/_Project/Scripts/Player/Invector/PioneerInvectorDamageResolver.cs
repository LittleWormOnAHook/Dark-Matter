using Invector;
using Project.Companions.Invector;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Resolves outgoing Invector damage from the attacker hierarchy instead of a player singleton.
    /// </summary>
    public static class PioneerInvectorDamageResolver
    {
        public static float ResolveOutgoingDamage(vDamage damage, GameObject sender, out bool isCritical)
        {
            isCritical = false;
            if (sender == null)
                return damage != null ? damage.damageValue : 0f;

            IInvectorOutgoingDamageSource source = FindOutgoingDamageSource(sender);
            if (source != null)
                return source.ResolveOutgoingDamage(damage, sender, out isCritical);

            return damage != null ? damage.damageValue : 0f;
        }

        public static IInvectorOutgoingDamageSource FindOutgoingDamageSource(GameObject sender)
        {
            if (sender == null)
                return null;

            PioneerInvectorDamageBridge playerBridge = sender.GetComponentInParent<PioneerInvectorDamageBridge>();
            if (playerBridge != null)
                return playerBridge;

            CompanionInvectorDamageBridge companionBridge =
                sender.GetComponentInParent<CompanionInvectorDamageBridge>();
            return companionBridge;
        }
    }
}
