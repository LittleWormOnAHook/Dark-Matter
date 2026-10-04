using UnityEngine;

namespace Project.Combat
{
    /// <summary>Optional tag: scales status duration/potency on bosses and elites.</summary>
    public sealed class CombatBossStatusModifier : MonoBehaviour
    {
        [Range(0.1f, 1f)]
        [SerializeField] private float statusDurationMultiplier = 0.5f;

        public float StatusDurationMultiplier => statusDurationMultiplier;
    }
}
