using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// Sets Malbers brain / AIControl target via bridge leash-aware refresh.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMISetThreatTargetTask",
        menuName = "Dark Matter Genesis/Creatures/Brain/Set Threat Target Task")]
    public class DMISetThreatTargetTask : MTask
    {
        public override string DisplayName => "DMI/Set Threat Target";

        [Tooltip("When true, AIControl also moves toward the threat.")]
        public bool moveToTarget = true;

        public override void StartTask(MAnimalBrain brain, int index)
        {
            DMICreatureBridge bridge = ResolveBridge(brain);
            if (bridge == null)
            {
                brain.TaskDone(index);
                return;
            }

            bridge.RefreshThreatTarget(moveToTarget);
            brain.TaskDone(index);
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
