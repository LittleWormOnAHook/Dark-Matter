using System;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>Bundle of kit + style + recipe + material override + passage/nook settings (Rock Palette entries).</summary>
    [CreateAssetMenu(fileName = "DM_RockPreset", menuName = "Genesis PCG Rock Creation/Rock Preset")]
    public sealed class DmRockPreset : ScriptableObject
    {
        public static event Action<DmRockPreset> Changed;

        public DmRockKit kit;
        public DmRockStyle style;
        public DmRockCombineRecipe recipe;
        [Tooltip("Null = the pack's original materials (atlased on bake).")]
        public Material materialOverride;
        [Tooltip("Optional 'Genesis PCG/Rock Blend Lit' material (top layer / terrain blend / strata). On bake the rock's atlas " +
                 "(or the material override) becomes its base layer. Null = plain atlas / override material (pack path).")]
        public Material blendMaterial;
        public DmRockFeatureSettings features = new DmRockFeatureSettings();
        [TextArea(1, 3)] public string description = "";

        [NonSerialized] public int Version;

        private void OnValidate()
        {
            Version++;
#if UNITY_EDITOR
            // Raised on the next editor tick: listeners rebuild rocks (AddComponent), which Unity forbids during OnValidate.
            UnityEditor.EditorApplication.delayCall -= DeferredChanged;
            UnityEditor.EditorApplication.delayCall += DeferredChanged;
#else
            Changed?.Invoke(this);
#endif
        }

        private void DeferredChanged()
        {
            if (this != null) Changed?.Invoke(this);
        }
    }
}
