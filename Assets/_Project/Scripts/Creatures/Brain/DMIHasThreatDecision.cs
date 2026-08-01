using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// True when a valid threat is within engage range (or current threat still within leash).
    /// Uses <see cref="DMICreatureBridge.HasActiveThreat"/> so bridge leash/lose-target logic is shared.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMIHasThreatDecision",
        menuName = "Dark Matter Genesis/Creatures/Brain/Has Threat Decision")]
    public class DMIHasThreatDecision : MAIDecision
    {
        public override string DisplayName => "DMI/Has Threat";

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            DMICreatureBridge bridge = ResolveBridge(brain);
            return bridge != null && bridge.HasActiveThreat;
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
