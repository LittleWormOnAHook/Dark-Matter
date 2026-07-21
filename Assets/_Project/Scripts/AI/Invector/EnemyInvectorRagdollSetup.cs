using System.Collections.Generic;
using Invector.vCharacterController;
using UnityEngine;

namespace Project.AI.Invector
{
    /// <summary>
    /// Ensures humanoid enemy prefabs have a configured <see cref="vRagdoll"/> component.
    /// Activation is owned by <see cref="EnemyInvectorRagdollBridge"/>.
    /// </summary>
    public static class EnemyInvectorRagdollSetup
    {
        public static vRagdoll EnsurePresent(GameObject root)
        {
            if (root == null)
                return null;

            vRagdoll ragdoll = root.GetComponent<vRagdoll>();
            if (ragdoll == null)
                ragdoll = root.AddComponent<vRagdoll>();

            ConfigureForCorpse(ragdoll);
            return ragdoll;
        }

        public static void ConfigureForCorpse(vRagdoll ragdoll)
        {
            if (ragdoll == null)
                return;

            ragdoll.startRagdolled = false;
            ragdoll.keepRagdolled = false;
            ragdoll.ignoreGetUpAnimation = true;
            ragdoll.invertGetUpAnim = false;
            ragdoll.removePhysicsAfterDie = false;
            ragdoll.disableColliders = true;
            ragdoll.groundLayer = ResolveGroundLayerMask();
            ragdoll.horizontalMultiplier = 1f;
            ragdoll.verticalMultiplier = 0f;

            if (ragdoll.ignoreTags == null)
                ragdoll.ignoreTags = new List<string>();

            if (!ragdoll.ignoreTags.Contains("Weapon"))
                ragdoll.ignoreTags.Add("Weapon");
            if (!ragdoll.ignoreTags.Contains("Ignore Ragdoll"))
                ragdoll.ignoreTags.Add("Ignore Ragdoll");
        }

        private static LayerMask ResolveGroundLayerMask()
        {
            int mask = 1 << 0;
            TryAddLayer(ref mask, "Terrain");
            TryAddLayer(ref mask, "Walkable");
            return mask;
        }

        private static void TryAddLayer(ref int mask, string layerName)
        {
            int layer = LayerMask.NameToLayer(layerName);
            if (layer >= 0)
                mask |= 1 << layer;
        }
    }
}
