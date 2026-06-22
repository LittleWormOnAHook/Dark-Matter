using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Project.EditorTools
{
    public static class ReflectionProbeSetup
    {
        private const string ProbeObjectName = "Reflection Probe";
        private const string PrefabPath = ProjectAssetPaths.ReflectionProbePrefab;

        [MenuItem(SurvivalPioneerEditorMenus.Scene + "Reflection Probe", false, 10)]
        public static void SetupActiveReflectionProbe()
        {
            ReflectionProbe probe = FindOrCreateProbe(out GameObject probeObject);
            ConfigureProbe(probe);
            PositionProbe(probeObject.transform);

            probe.enabled = true;
            probeObject.SetActive(true);
            probe.RenderProbe();

            EnsurePrefab(probeObject);
            MarkSceneDirty();

            Selection.activeGameObject = probeObject;
            EditorGUIUtility.PingObject(probeObject);
            Debug.Log($"Active reflection probe configured on '{probeObject.name}' at {probeObject.transform.position}.");
        }

        private static ReflectionProbe FindOrCreateProbe(out GameObject probeObject)
        {
            GameObject existing = GameObject.Find(ProbeObjectName);
            if (existing != null)
            {
                probeObject = existing;
                return existing.GetComponent<ReflectionProbe>() ?? existing.AddComponent<ReflectionProbe>();
            }

            probeObject = new GameObject(ProbeObjectName);
            Undo.RegisterCreatedObjectUndo(probeObject, "Create Reflection Probe");
            return probeObject.AddComponent<ReflectionProbe>();
        }

        private static void ConfigureProbe(ReflectionProbe probe)
        {
            probe.mode = ReflectionProbeMode.Realtime;
            probe.refreshMode = ReflectionProbeRefreshMode.EveryFrame;
            probe.timeSlicingMode = ReflectionProbeTimeSlicingMode.NoTimeSlicing;
            probe.resolution = 256;
            probe.size = new Vector3(120f, 60f, 120f);
            probe.center = Vector3.zero;
            probe.intensity = 1f;
            probe.blendDistance = 2f;
            probe.boxProjection = true;
            probe.hdr = true;
            probe.shadowDistance = 100f;
            probe.clearFlags = ReflectionProbeClearFlags.Skybox;
            probe.cullingMask = ~0;
            probe.importance = 1;
            probe.renderDynamicObjects = true;
        }

        private static void PositionProbe(Transform probeTransform)
        {
            if (TryGetSceneCenter(out Vector3 center))
            {
                probeTransform.position = center + Vector3.up * 12f;
                return;
            }

            probeTransform.position = new Vector3(-18.5f, 15f, 4.5f);
        }

        private static bool TryGetSceneCenter(out Vector3 center)
        {
            center = Vector3.zero;
            Renderer[] renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude);
            if (renderers == null || renderers.Length == 0)
                return false;

            Bounds bounds = renderers[0].bounds;
            for (int i = 1; i < renderers.Length; i++)
            {
                if (renderers[i] == null || renderers[i] is ParticleSystemRenderer)
                    continue;

                bounds.Encapsulate(renderers[i].bounds);
            }

            center = bounds.center;
            return true;
        }

        private static void EnsurePrefab(GameObject probeObject)
        {
            CraftingEditorUtility.EnsureFolder(ProjectAssetPaths.PrefabsWorld);

            PrefabUtility.SaveAsPrefabAsset(probeObject, PrefabPath);
        }

        private static void MarkSceneDirty()
        {
            Scene scene = SceneManager.GetActiveScene();
            if (scene.IsValid())
                EditorSceneManager.MarkSceneDirty(scene);
        }
    }
}
