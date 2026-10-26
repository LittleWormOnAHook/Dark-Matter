// 2026-10-24as: Io anti-ghosting. Hidden, never-saved global volume (priority 95) that disables HDRP Volumetric
// Clouds in Edit + Play without touching BOTD/Gaia profiles or the scene. Toggle: DM_IoSkyProfile.disableVolumetricClouds.
using Project.World.Sky;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.World
{
    [InitializeOnLoad]
    internal static class DMIoAntiGhostVolume
    {
        private const string ObjName = "DM Io AntiGhost (hidden)";
        private static GameObject go;
        private static Volume vol;
        private static VolumeProfile prof;
        private static double nextCheck;

        static DMIoAntiGhostVolume()
        {
            EditorApplication.update += Tick;
        }

        private static void Tick()
        {
            if (EditorApplication.timeSinceStartup < nextCheck) return;
            nextCheck = EditorApplication.timeSinceStartup + 1.0;

            DM_IoSkyProfile p = DM_IoSkyProfile.Live;
            bool want = p != null && p.disableVolumetricClouds;
            // 24ay: remove orphans left by domain reloads (static 'go' resets, HideAndDontSave objects survive).
            foreach (Volume v in Resources.FindObjectsOfTypeAll<Volume>())
                if (v != null && v.gameObject.name == ObjName && v.gameObject != go) Object.DestroyImmediate(v.gameObject);
            if (!want) { if (go != null) Object.DestroyImmediate(go); go = null; vol = null; return; }

            if (prof == null)
            {
                prof = ScriptableObject.CreateInstance<VolumeProfile>();
                prof.name = "DM Io AntiGhost (runtime)";
                prof.hideFlags = HideFlags.HideAndDontSave;
                VolumetricClouds vc = prof.Add<VolumetricClouds>();
                vc.enable.Override(false);
            }
            if (go == null)
            {
                go = new GameObject(ObjName);
                go.hideFlags = HideFlags.HideAndDontSave;
                vol = go.AddComponent<Volume>();
                vol.isGlobal = true;
                vol.priority = 95f;
                vol.weight = 1f;
                vol.sharedProfile = prof;
            }
        }
    }
}