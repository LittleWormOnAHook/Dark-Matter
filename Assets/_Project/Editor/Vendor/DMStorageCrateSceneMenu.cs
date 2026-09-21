#if UNITY_EDITOR
using Project.EditorTools;
using Project.Events;
using Project.Interaction;
using Project.Map;
using Project.Storage;
using Project.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools.Vendor
{
    public static class DMStorageCrateSceneMenu
    {
        [MenuItem(DarkMatterGenesisEditorMenus.World + "Wire Existing Storage Crate")]
        public static void WireExistingStorageCrate()
        {
            GameObject root = GameObject.Find("Storage Crate");
            if (root == null)
            {
                Debug.LogWarning("[DM] No Hierarchy object named Storage Crate.");
                return;
            }

            Undo.RegisterCompleteObjectUndo(root, "Wire Storage Crate");

            GameObject camp = GameObject.Find("Camp Storage Crate");
            if (camp != null)
                Undo.DestroyObjectImmediate(camp);

            DMStorageCrate crate = root.GetComponent<DMStorageCrate>();
            if (crate == null)
                crate = Undo.AddComponent<DMStorageCrate>(root);

            crate.ResolvePresentation();

            SerializedObject so = new SerializedObject(crate);
            so.FindProperty("crateId").stringValue = "camp_storage_01";
            so.FindProperty("slotCount").intValue = 20;
            so.FindProperty("interactRange").floatValue = 3.5f;
            so.FindProperty("promptText").stringValue = "Press E — Storage";

            Animation anim = root.GetComponent<Animation>();
            if (anim != null)
                so.FindProperty("chestAnimation").objectReferenceValue = anim;

            Transform particle = root.transform.Find("Particle System");
            if (particle != null)
                so.FindProperty("openParticle").objectReferenceValue = particle.gameObject;

            Transform collection = root.transform.Find("collection");
            if (collection != null)
            {
                Collider trigger = collection.GetComponent<Collider>();
                if (trigger != null)
                    so.FindProperty("interactCollider").objectReferenceValue = trigger;

                DMItemCollection loot = collection.GetComponent<DMItemCollection>();
                if (loot != null)
                    loot.enabled = false;

                Transform prompt = collection.Find("vActionText (1)");
                if (prompt != null)
                    prompt.gameObject.SetActive(false);
            }

            so.ApplyModifiedPropertiesWithoutUndo();

            ScannableTarget scan = root.GetComponent<ScannableTarget>();
            if (scan != null)
                scan.Configure("Storage", DarkMatterGenesisUiPalette.MapPoiBlack, ScannerTargetCategory.Interactable, true, false, true);

            MapMarker marker = root.GetComponent<MapMarker>();
            if (marker != null)
                marker.ConfigureScannedPoi("Storage", DarkMatterGenesisUiPalette.MapPoiBlack);

            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(crate);
            PrefabUtility.RecordPrefabInstancePropertyModifications(root);
            PrefabUtility.RecordPrefabInstancePropertyModifications(crate);
            EditorSceneManager.MarkSceneDirty(root.scene);
            Selection.activeGameObject = root;
            Debug.Log("[DM] Storage Crate wired for persistent stash. Camp Storage Crate removed.");
        }
    }
}
#endif
