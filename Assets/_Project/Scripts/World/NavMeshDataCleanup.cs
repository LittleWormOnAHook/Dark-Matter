using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace Project.World
{
    /// <summary>
    /// NavMeshSurface.AddData uses NavMesh.AddNavMeshData. Additive terrain
    /// scenes (and play-mode teardown) can skip RemoveData, which logs
    /// "leaked navmesh data while exiting play-mode".
    /// </summary>
    [DefaultExecutionOrder(32000)]
    public sealed class NavMeshDataCleanup : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Boot()
        {
            if (!Application.isPlaying)
                return;

            if (FindAnyObjectByType<NavMeshDataCleanup>(FindObjectsInactive.Include) != null)
                return;

            GameObject go = new GameObject("NavMeshDataCleanup");
            Object.DontDestroyOnLoad(go);
            go.hideFlags = HideFlags.HideAndDontSave;
            go.AddComponent<NavMeshDataCleanup>();
        }

        private void OnApplicationQuit()
        {
            Release();
        }

        private void OnDestroy()
        {
            Release();
        }

        private static void Release()
        {
            NavMeshSurface[] surfaces = FindObjectsByType<NavMeshSurface>(FindObjectsInactive.Include);
            for (int i = 0; i < surfaces.Length; i++)
            {
                if (surfaces[i] != null)
                    surfaces[i].RemoveData();
            }
        }
    }
}
