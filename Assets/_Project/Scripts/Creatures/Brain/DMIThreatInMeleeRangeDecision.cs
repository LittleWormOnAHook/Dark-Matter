using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// True when the current threat is within melee engage distance.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMIThreatInMeleeRangeDecision",
        menuName = "Dark Matter Genesis/Creatures/Brain/Threat In Melee Range Decision")]
    public class DMIThreatInMeleeRangeDecision : MAIDecision
    {
        public override string DisplayName => "DMI/Threat In Melee Range";

        [Tooltip("Fallback melee range when definition has no override.")]
        public float fallbackMeleeRange = 2.75f;

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            DMICreatureBridge bridge = ResolveBridge(brain);
            if (bridge == null)
                return false;

            Transform target = brain.Target != null ? brain.Target : bridge.CurrentThreat;
            if (target == null || !DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(bridge, target))
                return false;

            float melee = bridge.Definition != null && bridge.Definition.meleeEngageRange > 0.1f
                ? bridge.Definition.meleeEngageRange
                : fallbackMeleeRange;

            float distance = Vector3.Distance(bridge.transform.position, target.position);
            return distance <= melee;
        }

        private static DMICreatureBridge ResolveBridge(MAnimalBrain brain)
        {
            if (brain == null)
                return null;

            return brain.GetComponent<DMICreatureBridge>()
                   ?? brain.GetComponentInParent<DMICreatureBridge>()
                   ?? brain.GetComponentInChildren<DMICreatureBridge>(true);
        }
    }
}
