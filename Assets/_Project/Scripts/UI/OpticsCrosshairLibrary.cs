using UnityEngine;

namespace Project.UI
{
    public class OpticsCrosshairLibrary : ScriptableObject
    {
        [Header("Binoculars")]
        public Texture2D binocularScopeFull;
        public Texture2D binocularScopeInnerGlow;
        public Texture2D binocularScopeOuter;

        [Header("Scanner")]
        public Texture2D scannerHolographic;
        public Texture2D scannerHolographicGlow;
        public Texture2D scannerRectMask;
    }
}
