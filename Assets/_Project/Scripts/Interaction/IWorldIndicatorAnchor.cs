using UnityEngine;

namespace Project.Interaction
{
    /// <summary>
    /// World interactables that expose a stable indicator attach point (bounds center or marker).
    /// UITK world chrome reads this instead of guessing <see cref="Transform.position"/>.
    /// Pickup stems grow world-up from this anchor; the proximity dot stays on the tip.
    /// </summary>
    public interface IWorldIndicatorAnchor
    {
        /// <summary>True when this instance should show a proximity/interaction indicator.</summary>
        bool IsIndicatorAvailable { get; }

        /// <summary>World-space stem base - visual/bounds center, or an explicit marker.</summary>
        Vector3 GetIndicatorWorldAnchor();

        /// <summary>World-up stem length (meters) at lock-on / far range.</summary>
        float IndicatorStemMinHeight { get; }

        /// <summary>World-up stem length (meters) when planar distance is within near reach.</summary>
        float IndicatorStemMaxHeight { get; }
    }
}
