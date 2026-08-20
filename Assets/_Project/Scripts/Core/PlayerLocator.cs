using Project.Player;
using UnityEngine;

namespace Project.Core
{
    public static class PlayerLocator
    {
        public static GameObject FindPlayerObject()
        {
            Transform cached = PlayerReference.Transform;
            if (cached != null)
                return cached.gameObject;

            Transform resolved = PlayerReference.ResolveTransform();
            return resolved != null ? resolved.gameObject : null;
        }

        public static PlayerController FindPlayerController()
        {
            Transform cached = PlayerReference.Transform;
            if (cached != null)
            {
                PlayerController onCached = cached.GetComponent<PlayerController>();
                if (onCached != null)
                    return onCached;
            }

            GameObject player = FindPlayerObject();
            return player != null ? player.GetComponent<PlayerController>() : null;
        }
    }
}
