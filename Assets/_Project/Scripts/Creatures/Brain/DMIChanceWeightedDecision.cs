using MalbersAnimations.Controller.AI;
using UnityEngine;

namespace Project.Creatures.Brain
{
    /// <summary>
    /// Weighted random decision. When the target is in the player camera view, uses
    /// <see cref="viewBoostedChance"/>; otherwise <see cref="baseChance"/>.
    /// </summary>
    [CreateAssetMenu(
        fileName = "DMIChanceWeightedDecision",
        menuName = "Dark Matter Genesis/Creatures/Brain/Chance Weighted Decision")]
    public class DMIChanceWeightedDecision : MAIDecision
    {
        public override string DisplayName => "DMI/Chance Weighted";

        [Range(0f, 1f)] public float baseChance = 0.12f;
        [Range(0f, 1f)] public float viewBoostedChance = 0.45f;

        [Tooltip("When true, pull chances from the creature definition spit fields if present.")]
        public bool useDefinitionSpitChances = true;

        public override bool Decide(MAnimalBrain brain, int Index)
        {
            float baseRoll = baseChance;
            float viewRoll = viewBoostedChance;

            DMICreatureBridge bridge = ResolveBridge(brain);
            if (useDefinitionSpitChances && bridge != null && bridge.Definition != null)
            {
                baseRoll = bridge.Definition.spitBaseChance;
                viewRoll = bridge.Definition.spitViewBoostedChance;
            }
            else if (bridge != null && bridge.SpitAttack != null)
            {
                baseRoll = bridge.SpitAttack.BaseChance;
                viewRoll = bridge.SpitAttack.ViewBoostedChance;
            }

            Transform target = brain != null ? brain.Target : null;
            if (target == null && bridge != null)
                target = bridge.CurrentThreat;

            bool inView = DMICreatureViewUtility.IsInPlayerCameraView(target);
            float chance = inView ? viewRoll : baseRoll;
            return Random.value <= chance;
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
