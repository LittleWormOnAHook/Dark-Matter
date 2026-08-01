using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// True when the current brain target (or resolved threat) is inside the player camera frustum.
    /// Used to boost spit chance when the fight is on-screen.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMIInPlayerViewDecision",
        menuName = "Dark Matter Genesis/Creatures/Brain/In Player View Decision")]
    public class DMIInPlayerViewDecision : MAIDecision
    {
        public override string DisplayName => "DMI/In Player View";

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            Transform target = brain != null ? brain.Target : null;
            if (target == null)
            {
                DMICreatureBridge bridge = ResolveBridge(brain);
                target = bridge != null ? bridge.CurrentThreat : null;
            }

            return DMICreatureViewUtility.IsInPlayerCameraView(target);
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
