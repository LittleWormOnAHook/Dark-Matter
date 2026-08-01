using System.Collections.Generic;

namespace Project.Creatures
{
    /// <summary>
    /// Semantic bone names: Meshy Sulfur Hound → Malbers Wolf Lite AC.
    /// Used by authoring bind and runtime sockpuppet retarget.
    /// </summary>
    public static class DMICreatureBoneMap
    {
        /// <summary>
        /// Conservative sockpuppet set: body/head/tail only.
        /// Limb bones are intentionally omitted — Sulfur vs Wolf joint axes differ enough
        /// that world-rotation copy spaghetti-deforms the mesh.
        /// </summary>
        public static readonly Dictionary<string, string> SulfurToAc = new Dictionary<string, string>
        {
            { "Hips", "Pelvis" },
            { "chest", "Spine1" },
            { "head", "Head" },
            { "tail", "Tail" },
            { "tail1", "Tail1" },
            { "tail2", "Tail2" },
            { "tail3", "Tail3" },
        };

        /// <summary>Visual bones that should also track driver world position (root only).</summary>
        public static bool ShouldCopyPosition(string visualBoneName)
        {
            return visualBoneName == "Hips";
        }
    }
}
