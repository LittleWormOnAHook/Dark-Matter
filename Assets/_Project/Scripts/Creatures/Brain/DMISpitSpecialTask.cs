using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// Fires Sulfur Hound spit special when ready, in range, and chance roll succeeds.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMISpitSpecialTask",
        menuName = "Dark Matter Genesis/Creatures/Brain/Spit Special Task")]
    public class DMISpitSpecialTask : MTask
    {
        public override string DisplayName => "DMI/Spit Special";

        [Tooltip("Face the target before launching spit.")]
        public bool alignToTarget = true;

        public override void StartTask(MAnimalBrain brain, int index)
        {
            TrySpit(brain);
            brain.TaskDone(index);
        }

        public override void UpdateTask(MAnimalBrain brain, int index)
        {
            // One-shot on start; Update reserved for future sustain spit modes.
        }

        private void TrySpit(MAnimalBrain brain)
        {
            DMICreatureBridge bridge = ResolveBridge(brain);
            if (bridge == null)
                return;

            DMISulfurSpitAttack spit = bridge.SpitAttack;
            if (spit == null)
                spit = bridge.GetComponent<DMISulfurSpitAttack>();

            if (spit == null || !spit.IsReady)
                return;

            Transform target = brain.Target != null ? brain.Target : bridge.CurrentThreat;
            if (target == null || !DMICreatureTargetResolver.IsValidSpitOrMeleeTarget(bridge, target))
                return;

            float distance = Vector3.Distance(bridge.transform.position, target.position);
            if (distance > spit.Range)
                return;

            bool inView = DMICreatureViewUtility.IsInPlayerCameraView(target);
            if (!spit.RollSpitChance(inView))
                return;

            if (alignToTarget && bridge.Animal != null)
            {
                Vector3 flat = target.position - bridge.transform.position;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.001f)
                    bridge.transform.rotation = Quaternion.LookRotation(flat.normalized, Vector3.up);
            }

            spit.TryFire(target);
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
