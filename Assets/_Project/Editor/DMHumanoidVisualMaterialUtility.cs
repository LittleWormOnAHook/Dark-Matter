#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Keeps custom Visual renderers using materials from the source FBX/prefab after re-apply or reimport.
    /// </summary>
    public static class DMHumanoidVisualMaterialUtility
    {
        public static void RefreshModelImportIfNeeded(GameObject sourceAsset, bool force = false)
        {
            if (sourceAsset == null)
                return;

            string path = AssetDatabase.GetAssetPath(sourceAsset);
            if (string.IsNullOrEmpty(path))
                path = EnemyModelAvatarUtility.FindPrimaryModelAssetPath(sourceAsset);

            if (string.IsNullOrEmpty(path) || !EnemyModelAvatarUtility.IsModelAssetPath(path))
                return;

            if (force)
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        }

        /// <summary>
        /// Copies sharedMaterials from a fresh source instance onto the attached Visual hierarchy.
        /// </summary>
        public static void SyncMaterialsFromSource(GameObject visualRoot, GameObject sourceAsset, bool refreshImport = true)
        {
            if (visualRoot == null || sourceAsset == null)
                return;

            if (refreshImport)
                RefreshModelImportIfNeeded(sourceAsset, force: true);

            GameObject sourceInstance = DMHumanoidVisualAttachUtility.InstantiateVisualSource(sourceAsset);
            if (sourceInstance == null)
                return;

            sourceInstance.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Dictionary<string, Material[]> map = BuildRendererMaterialMap(sourceInstance.transform);
                ApplyRendererMaterialMap(visualRoot.transform, map);
            }
            finally
            {
                Object.DestroyImmediate(sourceInstance);
            }
        }

        static Dictionary<string, Material[]> BuildRendererMaterialMap(Transform sourceRoot)
        {
            var map = new Dictionary<string, Material[]>(32);
            Renderer[] renderers = sourceRoot.GetComponentsInChildren<Renderer>(true);
            var meshNameCounts = new Dictionary<string, int>(16);

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || DMHumanoidVisualAttachUtility.IsWeaponVisualNode(renderer.transform))
                    continue;

                string key = BuildRendererKey(sourceRoot, renderer.transform, meshNameCounts);
                if (string.IsNullOrEmpty(key))
                    continue;

                Material[] mats = renderer.sharedMaterials;
                if (mats == null || mats.Length == 0)
                    continue;

                map[key] = mats;
            }

            return map;
        }

        static void ApplyRendererMaterialMap(Transform visualRoot, Dictionary<string, Material[]> map)
        {
            if (visualRoot == null || map == null || map.Count == 0)
                return;

            var meshNameCounts = new Dictionary<string, int>(16);
            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            int applied = 0;

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null || DMHumanoidVisualAttachUtility.IsWeaponVisualNode(renderer.transform))
                    continue;

                string key = BuildRendererKey(visualRoot, renderer.transform, meshNameCounts);
                if (string.IsNullOrEmpty(key) || !map.TryGetValue(key, out Material[] sourceMats))
                    continue;

                Material[] copy = new Material[sourceMats.Length];
                for (int m = 0; m < sourceMats.Length; m++)
                    copy[m] = sourceMats[m];

                renderer.sharedMaterials = copy;
                renderer.enabled = true;
                if (!renderer.gameObject.activeSelf)
                    renderer.gameObject.SetActive(true);

                applied++;
            }

            if (applied > 0)
            {
                Debug.Log(
                    $"[DMHumanoidVisualMaterial] Applied source materials to {applied} renderer(s) on '{visualRoot.name}'.",
                    visualRoot);
            }
        }

        static string BuildRendererKey(
            Transform hierarchyRoot,
            Transform rendererTransform,
            Dictionary<string, int> meshNameCounts)
        {
            if (hierarchyRoot == null || rendererTransform == null)
                return string.Empty;

            string path = NormalizeHierarchyPath(AnimationUtility.CalculateTransformPath(rendererTransform, hierarchyRoot));
            string meshPart = string.Empty;

            if (rendererTransform.TryGetComponent(out SkinnedMeshRenderer smr) && smr.sharedMesh != null)
                meshPart = smr.sharedMesh.name;
            else if (rendererTransform.TryGetComponent(out MeshRenderer _) &&
                     rendererTransform.TryGetComponent(out MeshFilter filter) &&
                     filter.sharedMesh != null)
                meshPart = filter.sharedMesh.name;

            if (!string.IsNullOrEmpty(meshPart))
            {
                if (!meshNameCounts.TryGetValue(meshPart, out int index))
                    index = 0;
                meshNameCounts[meshPart] = index + 1;
                return $"{path}|{meshPart}|{index}";
            }

            return path;
        }

        static string NormalizeHierarchyPath(string pathFromRoot)
        {
            if (string.IsNullOrEmpty(pathFromRoot))
                return string.Empty;

            int slash = pathFromRoot.IndexOf('/');
            return slash >= 0 ? pathFromRoot.Substring(slash + 1) : string.Empty;
        }
    }
}
#endif
