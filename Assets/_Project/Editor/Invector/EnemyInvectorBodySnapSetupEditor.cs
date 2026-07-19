#if UNITY_EDITOR
using System.IO;
using Invector;
using UnityEditor;
using UnityEngine;

namespace Project.AI.Invector
{
    public static class EnemyInvectorBodySnapSetupEditor
    {
        private const string PlayerPrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";

        public static void ConfigureEditor(GameObject root)
        {
            if (root == null)
                return;

            EnsurePresentEditor(root);

            vBodySnappingControl bodySnap = root.GetComponentInChildren<vBodySnappingControl>(true);
            if (bodySnap == null)
                return;

            bodySnap.LoadBones();
            EnemyInvectorBodySnapSetup.WireSnapComponents(root, bodySnap);
        }

        public static void EnsurePresentEditor(GameObject root)
        {
            if (root == null || root.GetComponentInChildren<vBodySnappingControl>(true) != null)
                return;

            CopyBodySnapsFromPlayer(root);
        }

        private static void CopyBodySnapsFromPlayer(GameObject root)
        {
            if (!File.Exists(PlayerPrefabPath))
            {
                Debug.LogError($"Missing {PlayerPrefabPath}. Cannot restore enemy body snaps.");
                return;
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject playerInstance = PrefabUtility.InstantiatePrefab(playerPrefab) as GameObject;
            if (playerInstance == null)
                return;

            try
            {
                Transform source = playerInstance.transform.Find("InvectorComponents/BodySnaps");
                if (source == null)
                    source = playerInstance.transform.Find("BodySnaps");

                if (source == null)
                {
                    Debug.LogWarning("Player_Invector has no BodySnaps hierarchy to copy.");
                    return;
                }

                GameObject copy = Object.Instantiate(source.gameObject);
                copy.name = "BodySnaps";
                copy.transform.SetParent(root.transform, false);
                copy.transform.localPosition = Vector3.zero;
                copy.transform.localRotation = Quaternion.identity;
                copy.transform.localScale = Vector3.one;
            }
            finally
            {
                Object.DestroyImmediate(playerInstance);
            }
        }
    }
}
#endif
