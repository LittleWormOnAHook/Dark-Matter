using System;

namespace Project.UI
{
    /// <summary>
    /// Health source for the top-center engaged target HUD (enemies, training dummy).
    /// </summary>
    public interface IEngagedHealthHudTarget
    {
        event Action<float, float> HealthChanged;
        event Action Died;

        float CurrentHealth { get; }
        float MaxHealth { get; }
        bool IsDead { get; }
    }
}
