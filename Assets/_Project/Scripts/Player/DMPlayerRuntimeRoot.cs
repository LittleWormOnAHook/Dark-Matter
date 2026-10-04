using Project.Core;
using UnityEngine;

namespace Project.Player
{
    /// <summary>Resolves the active Player_v7 root (Combat Variant, then Variant, then Player_v7).</summary>
    internal static class DMPlayerRuntimeRoot
    {
        internal static GameObject Find()
        {
            return PlayerLocator.FindPlayerObject();
        }
    }
}
