using System;
using System.Collections.Generic;
using UnityEngine;

namespace GenesisPCG.RockCreation
{
    /// <summary>
    /// Saved PCG creations (rocks now, cliffs later): prefab + the settings it was made from + a 512 icon rendered from a
    /// chosen orbit angle. Game-agnostic data; the Rocks and Cliffs studio edits it.
    /// </summary>
    [CreateAssetMenu(fileName = "PCG_AssetLibrary", menuName = "Genesis PCG Rock Creation/Asset Library")]
    public sealed class PcgAssetLibrary : ScriptableObject
    {
        [Serializable]
        public sealed class Entry
        {
            public string id = Guid.NewGuid().ToString("N");
            public string displayName = "";
            [Tooltip("Rocks, Cliffs, ... (the studio section that made it).")]
            public string category = "Rocks";
            public GameObject prefab;
            public DmRockPreset preset;
            public DmRockStyle style;
            public int seed;
            [Tooltip("512x512 PNG rendered from the orbit angle below.")]
            public Texture2D icon;
            public float iconYaw = 35f;
            public float iconPitch = 22f;
            public float iconZoom = 1f;
            public string createdUtc = "";

            public string Name => !string.IsNullOrEmpty(displayName) ? displayName : (prefab != null ? prefab.name : "(missing prefab)");
        }

        public List<Entry> entries = new List<Entry>();

        /// <summary>Raised after the studio edits the library (add / rename / remove / icon).</summary>
        public static event Action<PcgAssetLibrary> Changed;

        public Entry Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (Entry e in entries)
                if (e != null && e.id == id) return e;
            return null;
        }

        public Entry FindByPrefab(GameObject prefab)
        {
            if (prefab == null) return null;
            foreach (Entry e in entries)
                if (e != null && e.prefab == prefab) return e;
            return null;
        }

        public void NotifyChanged() => Changed?.Invoke(this);

        private void OnValidate() => Changed?.Invoke(this);
    }
}
