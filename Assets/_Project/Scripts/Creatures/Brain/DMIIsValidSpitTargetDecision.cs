using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// True when spit is off cooldown, a valid non-ally target exists, and it is within spit range.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMIIsValidSpitTargetDecision",
        menuName = "Dark Matter Genesis/Creatures/Brain/Is Valid Spit Target Decision")]
    public class DMIIsValidSpitTargetDecision : MAIDecision
    {
        public override string DisplayName => "DMI/Is Valid Spit Target";

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            DMICreatureBridge bridge = ResolveBridge(brain);
            if (bridge == null)
                return false;

            DMISulfurSpitAttack spit = bridge.SpitAttack;
            if (spit == null || !spit.IsReady)
                return false;

            Transform target = brain.Target != null ? brain.Target : bridge.CurrentThreat;
            if (target == null)
            {
                float sense = bridge.Definition != null ? bridge.Definition.threatSenseRange : 9f;
                if (!DMICreatureTargetResolver.TryResolveThreat(bridge, sense, out target, out _))
                    return false;
            }

            if (!DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(bridge, target))
                return false;

            float distance = Vector3.Distance(bridge.transform.position, target.position);
            return distance <= spit.Range;
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
