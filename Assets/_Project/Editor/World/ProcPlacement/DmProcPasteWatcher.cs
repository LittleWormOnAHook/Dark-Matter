#if UNITY_EDITOR
namespace Project.EditorTools.World.ProcPlacement
{
    /// <summary>
    /// Copy/paste/Ctrl+D reseeding for DmProcPlacer now runs through the shared, game-agnostic
    /// GenesisPCG.RockCreation.Editor.PcgSeededPasteWatcher (DmProcPlacer implements IPcgSeeded).
    /// This shim keeps the old entry point used by DmProcPlacerEditor.
    /// </summary>
    internal static class DmProcPasteWatcher
    {
        internal static int NewSeed() => GenesisPCG.RockCreation.Editor.PcgSeededPasteWatcher.NewSeed();
    }
}
#endif
