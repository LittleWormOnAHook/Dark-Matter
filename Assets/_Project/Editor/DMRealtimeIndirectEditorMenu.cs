#if UNITY_EDITOR
using Project.World.Sky;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Project.EditorTools.World
{
    /// <summary>2026-10-24aj/ak: realtime indirect (SSGI) helpers. Never edits the BOTD original; nothing here saves scenes.</summary>
    public static class DMRealtimeIndirectEditorMenu
    {
        [MenuItem("Tools/Dark Matter Genesis/World/Lighting/Ensure Io Post Processing Profile (BOTD copy)", false, 2111)]
        public static void EnsureProfile()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Resources/World"))
                AssetDatabase.CreateFolder("Assets/_Project/Resources", "World");
            string path = DMRealtimeIndirect.ProfileAssetPath;
            VolumeProfile vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (vp == null)
            {
                if (!AssetDatabase.CopyAsset(DMRealtimeIndirect.BotdSourcePath, path)) { Debug.LogWarning("[DM] Could not copy " + DMRealtimeIndirect.BotdSourcePath); return; }
                AssetDatabase.ImportAsset(path);
                vp = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            }
            // Io tuning baked into the copy (runtime re-applies the live Studio values on top).
            var gi = Get<GlobalIllumination>(vp);
            gi.enable.Override(true);
            gi.tracing.Override(RayCastingMode.RayMarching);
            gi.quality.Override(3);
            gi.maxRaySteps = 64;
            var ilc = Get<IndirectLightingController>(vp);
            ilc.indirectDiffuseLightingMultiplier.Override(1.5f);   // BOTD 0.34
            ilc.reflectionLightingMultiplier.Override(1.2f);        // BOTD 1.7
            var env = Get<VisualEnvironment>(vp);
            env.skyAmbientMode.Override(SkyAmbientMode.Dynamic);
            var ao = Get<ScreenSpaceAmbientOcclusion>(vp);
            ao.intensity.Override(0.9f);                             // BOTD 1.17
            ao.directLightingStrength.Override(0.15f);               // BOTD 0.23
            var smh = Get<ShadowsMidtonesHighlights>(vp);
            smh.shadows.Override(new Vector4(1f, 0.86f, 0.62f, 0.06f));

            EditorUtility.SetDirty(vp);
            AssetDatabase.SaveAssetIfDirty(vp);

            DM_IoSkyProfile p = AssetDatabase.LoadAssetAtPath<DM_IoSkyProfile>(DM_IoSkyProfile.AssetPath);
            if (p != null && p.indirectVolumeProfile != vp)
            {
                p.indirectVolumeProfile = vp;
                EditorUtility.SetDirty(p);
                AssetDatabase.SaveAssetIfDirty(p);
            }
            Selection.activeObject = vp;
            Debug.Log("[DM] Io post processing profile ready at " + path);
        }

        private static T Get<T>(VolumeProfile vp) where T : VolumeComponent
        {
            if (vp.TryGet(out T c)) { c.active = true; return c; }
            c = vp.Add<T>();
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, vp);
            return c;
        }

        [MenuItem("Tools/Dark Matter Genesis/World/Lighting/Add Realtime Indirect Volume To Open Scene", false, 2112)]
        public static void AddToScene()
        {
            if (Object.FindAnyObjectByType<DMRealtimeIndirect>(FindObjectsInactive.Include) != null)
            {
                Debug.Log("[DM] Scene already has a DMRealtimeIndirect.");
                return;
            }
            var go = new GameObject("DM Realtime Indirect Volume");
            go.AddComponent<DMRealtimeIndirect>();
            Undo.RegisterCreatedObjectUndo(go, "Add Realtime Indirect Volume");
            EditorSceneManager.MarkSceneDirty(go.scene);
            Selection.activeObject = go;
        }
    }
}
#endif
