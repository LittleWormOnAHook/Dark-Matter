namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Common surface for seeded edit-mode generators (e.g. DmRockCombiner, or a game's own generators) so the editor
    /// paste watcher and the Shift+drag duplicate tool can treat them the same way.
    /// </summary>
    public interface IPcgSeeded
    {
        int Seed { get; }
        bool LockSeed { get; }
        long PlacementStamp { get; }
        void NewPlacementStamp();
        void SetSeed(int value);

        /// <summary>Surface snap rules used by paste/duplicate/Shift+drag.</summary>
        PcgSurfaceSnapSettings SnapSettings { get; }
    }
}

namespace GenesisPCG.RockCreation
{
    /// <summary>Optional: extra depth a generator wants below the snapped ground (user sink offset); honoured by every snap.</summary>
    public interface IPcgSinkOffset
    {
        float UserSinkOffset { get; }
    }
}
