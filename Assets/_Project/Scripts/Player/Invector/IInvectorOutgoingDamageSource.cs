using Invector;
using UnityEngine;

namespace Project.Player.Invector
{
    /// <summary>
    /// Any Invector character (player or companion) that rolls Pioneer outgoing damage from ItemData.
    /// </summary>
    public interface IInvectorOutgoingDamageSource
    {
        float ResolveOutgoingDamage(vDamage damage, GameObject source, out bool isCritical);
    }
}
