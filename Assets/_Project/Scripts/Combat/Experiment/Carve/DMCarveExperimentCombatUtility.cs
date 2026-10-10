using Project.Data;
using UnityEngine;

namespace Project.Combat.Experiment.Carve
{
    /// <summary>Resolves experiment ammo flags without spreading type checks across combat code.</summary>
    public static class DMCarveExperimentCombatUtility
    {
        public const int PlayerLayerIndex = 8;

        /// <summary>All layers except the player capsule (layer 8).</summary>
        public static int AllLayersExceptPlayer => Physics.AllLayers & ~(1 << PlayerLayerIndex);

        public static bool TryGetProfile(ItemData ammoItem, out DMCarveExperimentAmmoProfile profile)
        {
            profile = null;
            if (ammoItem == null)
                return false;

            if (ammoItem is DMCarveExperimentAmmoProfile direct)
            {
                profile = direct;
                return true;
            }

            if (ammoItem.fxProfile is DMCarveExperimentAmmoProfile viaFx)
            {
                profile = viaFx;
                return true;
            }

            return false;
        }

        public static bool UsesAllPhysicsLayers(ItemData ammoItem) =>
            TryGetProfile(ammoItem, out DMCarveExperimentAmmoProfile p) && p.useAllPhysicsLayers;

        public static bool ForcesMeshDeformation(ItemData ammoItem) =>
            TryGetProfile(ammoItem, out DMCarveExperimentAmmoProfile p) && p.forceMeshDeformation;

        public static bool AllowsCarveOnEnemyReceivers(ItemData ammoItem) =>
            TryGetProfile(ammoItem, out DMCarveExperimentAmmoProfile p) && p.allowCarveOnEnemyReceivers;

        public static bool BypassesWorldImpactCharacterBlocks(ItemData ammoItem) =>
            TryGetProfile(ammoItem, out DMCarveExperimentAmmoProfile p) && p.bypassWorldImpactCharacterBlocks;
    }
}
