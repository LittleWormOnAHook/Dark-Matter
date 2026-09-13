using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Expedition pioneer on the field — registered for combat targeting without Core referencing CompanionHealth.
    /// </summary>
    public interface IExpeditionCompanionActor
    {
        Transform Transform { get; }
        bool IsDead { get; }
    }
}
