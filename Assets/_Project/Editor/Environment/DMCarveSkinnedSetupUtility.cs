using Project.EditorTools;
using Project.SurfaceCarve;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Environment
{
    public static class DMCarveSkinnedSetupUtility
    {
        [MenuItem(
            DarkMatterGenesisEditorMenus.CombatExperiment + "Setup Carve Target On Selected (Skinned Mesh)",
            false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Experiment_Setup_Carve_Target_On_Selected_Skinned_Mesh)]
        public static void SetupSelected()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Carve Skinned Setup",
                    "Select your creature root (or the object with a SkinnedMeshRenderer) in the Hierarchy.",
                    "OK");
                return;
            }

            int added = 0;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject go = selection[i];
                if (go == null)
                    continue;

                SkinnedMeshRenderer smr = go.GetComponentInChildren<SkinnedMeshRenderer>(true);
                if (smr == null)
                {
                    Debug.LogWarning("[CarveSetup] No SkinnedMeshRenderer under " + go.name, go);
                    continue;
                }

                DMCarveSkinnedCarveTarget target = go.GetComponent<DMCarveSkinnedCarveTarget>();
                if (target == null)
                    target = Undo.AddComponent<DMCarveSkinnedCarveTarget>(go);
                else
                    target = go.GetComponent<DMCarveSkinnedCarveTarget>();

                SerializedObject so = new SerializedObject(target);
                so.FindProperty("skinnedMeshRenderer").objectReferenceValue = smr;
                so.ApplyModifiedPropertiesWithoutUndo();

                WarnIfMeshNotReadable(smr);
                added++;
            }

            Debug.Log("[CarveSetup] Configured " + added + " object(s). Use Carve Experiment ammo; enable Read/Write on FBX if carve fails.");
        }

        [MenuItem(
            DarkMatterGenesisEditorMenus.CombatExperiment + "Enable Read/Write On Selected Model FBX",
            false,
            DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Experiment_Enable_Read_Write_On_Selected_Model_FBX)]
        public static void EnableReadWriteOnSelectedModels()
        {
            Object[] selection = Selection.objects;
            int count = 0;
            for (int i = 0; i < selection.Length; i++)
            {
                string path = AssetDatabase.GetAssetPath(selection[i]);
                if (string.IsNullOrEmpty(path))
                    continue;

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                    continue;

                if (importer.isReadable)
                    continue;

                importer.isReadable = true;
                importer.SaveAndReimport();
                count++;
            }

            Debug.Log("[CarveSetup] Enabled Read/Write on " + count + " model(s).");
        }

        private static void WarnIfMeshNotReadable(SkinnedMeshRenderer smr)
        {
            if (smr == null || smr.sharedMesh == null)
                return;

            if (smr.sharedMesh.isReadable)
                return;

            Debug.LogWarning(
                "[CarveSetup] Mesh '" + smr.sharedMesh.name + "' is not Read/Write. " +
                "Select the FBX in Project → Inspector → Model → Read/Write Enabled, then Apply. " +
                "Or use menu: Enable Read/Write On Selected Model FBX.",
                smr);
        }
    }
}
