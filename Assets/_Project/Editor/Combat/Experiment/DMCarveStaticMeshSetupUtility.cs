#if UNITY_EDITOR
using Project.EditorTools;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat.Experiment
{
    /// <summary>
    /// Adds <see cref="DMCarvable"/> to static MeshFilter/MeshRenderer targets for carve experiment lab.
    /// </summary>
    public static class DMCarveStaticMeshSetupUtility
    {
        private const string MenuPath =
            DarkMatterGenesisEditorMenus.CombatExperiment + "Setup Carve Target On Selected Static Mesh";

        [MenuItem(
            MenuPath,
            false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Experiment_Setup_Carve_Target_On_Selected_Static_Mesh)]
        public static void SetupSelected()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Carve Static Mesh Setup",
                    "Select a prefab root or mesh object in the Hierarchy (or Prefab stage).",
                    "OK");
                return;
            }

            int added = 0;
            int already = 0;
            int skipped = 0;

            Undo.SetCurrentGroupName("Setup Carve Target (Static Mesh)");
            int undoGroup = Undo.GetCurrentGroup();

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject root = selection[i];
                if (root == null)
                    continue;

                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                for (int f = 0; f < filters.Length; f++)
                {
                    MeshFilter mf = filters[f];
                    if (!TryGetStaticCarveTarget(mf, out GameObject targetGo, out string skipReason))
                    {
                        if (!string.IsNullOrEmpty(skipReason))
                        {
                            Debug.Log("[CarveSetup] Skipped " + mf.gameObject.name + ": " + skipReason, mf);
                            skipped++;
                        }

                        continue;
                    }

                    if (targetGo.GetComponent<DMCarvable>() != null)
                    {
                        already++;
                        WarnIfMeshNotReadable(mf);
                        LogMeshColliderNote(targetGo);
                        continue;
                    }

                    Rigidbody rb = targetGo.GetComponent<Rigidbody>();
                    if (rb != null && !rb.isKinematic)
                    {
                        Debug.LogWarning(
                            "[CarveSetup] '" + targetGo.name + "' has a non-kinematic Rigidbody; " +
                            "carve auto-resolve skips moving bodies. Make kinematic or remove RB for lab targets.",
                            targetGo);
                    }

                    Undo.AddComponent<DMCarvable>(targetGo);
                    WarnIfMeshNotReadable(mf);
                    LogMeshColliderNote(targetGo);
                    WarnIfBatchingStatic(targetGo);
                    added++;
                }
            }

            Undo.CollapseUndoOperations(undoGroup);

            if (added > 0)
                EditorUtility.SetDirty(selection[0]);

            Debug.Log(
                "[CarveSetup] Static mesh setup: added DMCarvable on " + added + ", already configured " + already +
                ", skipped " + skipped + ". Use Carve Experiment ammo; enable Read/Write on FBX if carve fails.");
        }

        [MenuItem(MenuPath, true)]
        public static bool ValidateSetupSelected()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
                return false;

            for (int i = 0; i < selection.Length; i++)
            {
                GameObject root = selection[i];
                if (root == null)
                    continue;

                MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
                for (int f = 0; f < filters.Length; f++)
                {
                    if (TryGetStaticCarveTarget(filters[f], out _, out _))
                        return true;
                }
            }

            return false;
        }

        private static bool TryGetStaticCarveTarget(MeshFilter mf, out GameObject targetGo, out string skipReason)
        {
            targetGo = null;
            skipReason = null;

            if (mf == null || mf.sharedMesh == null)
                return false;

            targetGo = mf.gameObject;

            if (mf.GetComponent<SkinnedMeshRenderer>() != null)
            {
                skipReason = "use Skinned Mesh setup instead";
                return false;
            }

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null)
            {
                skipReason = "no MeshRenderer";
                return false;
            }

            if (targetGo.name == "DM_CarveBakeSurface"
                && targetGo.GetComponentInParent<DMCarveSkinnedCarveTarget>() != null)
            {
                skipReason = "skinned bake surface (configure creature root with Skinned Mesh setup)";
                return false;
            }

            return true;
        }

        private static void WarnIfMeshNotReadable(MeshFilter mf)
        {
            if (mf == null || mf.sharedMesh == null || mf.sharedMesh.isReadable)
                return;

            Debug.LogWarning(
                "[CarveSetup] Mesh '" + mf.sharedMesh.name + "' is not Read/Write. " +
                "Select the FBX in Project → Model → Read/Write Enabled, or use " +
                "Combat → Experiment → Enable Read/Write On Selected Model FBX.",
                mf);
        }

        private static void LogMeshColliderNote(GameObject go)
        {
            MeshCollider mc = go.GetComponent<MeshCollider>();
            if (mc == null)
                return;

            Debug.Log(
                "[CarveSetup] '" + go.name + "' has a MeshCollider (optional for ray hits; " +
                "DMCarvable.updateCollider refreshes it after carves when enabled).",
                go);
        }

        private static void WarnIfBatchingStatic(GameObject go)
        {
            if (!GameObjectUtility.AreStaticEditorFlagsSet(go, StaticEditorFlags.BatchingStatic))
                return;

            Debug.LogWarning(
                "[CarveSetup] '" + go.name + "' is Batching Static; Play Mode carve may fail. " +
                "Disable Batching Static on lab carve targets.",
                go);
        }
    }
}
#endif
