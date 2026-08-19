using Invector.vCharacterController;
using Project.AI.Invector;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Player.Invector
{
    /// <summary>
    /// Remounts orphan VBOT ragdoll rigidbodies onto the active Meshy/humanoid avatar bones
    /// after Player Prefab Creator visual swaps.
    /// </summary>
    public static class PlayerInvectorRagdollSetup
    {
        private const string TemplatePrefabPath = "Assets/_Project/Prefabs/Players/Player_Invector.prefab";

        public static int RepairSeparatedRagdoll(GameObject root)
        {
            if (root == null)
                return 0;

            RestoreRagdollSettings(root);

            int remounted = EnemyInvectorRagdollRigRepair.TryRemountOrphanRagdollOntoAvatar(root);
            if (EnemyInvectorRagdollRigRepair.HasUsableRagdollUnderAvatar(root))
                return remounted;

#if UNITY_EDITOR
            remounted = TryRestoreRagdollFromTemplate(root);
            if (remounted > 0)
                return remounted;

            remounted = EnemyInvectorRagdollRigRepair.TryRemountOrphanRagdollOntoAvatar(root);
#endif

            return remounted;
        }

        private static void RestoreRagdollSettings(GameObject root)
        {
            vThirdPersonController controller = root.GetComponent<vThirdPersonController>();
            if (controller != null)
                controller.deathBy = vCharacter.DeathBy.Ragdoll;

            vRagdoll ragdoll = root.GetComponent<vRagdoll>();
            if (ragdoll == null)
                return;

            ragdoll.startRagdolled = false;
            ragdoll.enabled = true;
        }

#if UNITY_EDITOR
        private static int TryRestoreRagdollFromTemplate(GameObject root)
        {
            GameObject templatePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TemplatePrefabPath);
            if (templatePrefab == null)
                return 0;

            GameObject templateInstance = PrefabUtility.InstantiatePrefab(templatePrefab) as GameObject;
            if (templateInstance == null)
                return 0;

            try
            {
                return EnemyInvectorRagdollRigRepair.TryCopyRagdollFromTemplateAvatar(root, templateInstance);
            }
            finally
            {
                Object.DestroyImmediate(templateInstance);
            }
        }
#endif
    }
}
