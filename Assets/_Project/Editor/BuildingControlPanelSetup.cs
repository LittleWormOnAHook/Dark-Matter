using Project.Building;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    public static class BuildingControlPanelSetup
    {
        [MenuItem(SurvivalPioneerEditorMenus.Content + "Add Building Control Panel to Selected")]
        private static void AddBuildingControlPanelToSelected()
        {
            GameObject[] selection = Selection.gameObjects;
            if (selection == null || selection.Length == 0)
            {
                EditorUtility.DisplayDialog(
                    "Building Control Panel",
                    "Select one or more GameObjects in the Hierarchy first.",
                    "OK");
                return;
            }

            int added = 0;
            for (int i = 0; i < selection.Length; i++)
            {
                GameObject target = selection[i];
                if (target == null)
                    continue;

                Undo.RecordObject(target, "Add Building Control Panel");

                BuildingControlPanel panel = target.GetComponent<BuildingControlPanel>();
                if (panel == null)
                {
                    panel = Undo.AddComponent<BuildingControlPanel>(target);
                    if (panel == null)
                    {
                        Debug.LogWarning($"[BuildingControlPanelSetup] Could not add BuildingControlPanel to '{target.name}'.");
                        continue;
                    }

                    added++;
                }

                EnsureInteractionColliderForEditor(target);
                EditorUtility.SetDirty(target);
            }

            Debug.Log($"[BuildingControlPanelSetup] Ensured BuildingControlPanel on {selection.Length} object(s) ({added} added).");
        }

        private static void EnsureInteractionColliderForEditor(GameObject target)
        {
            BoxCollider box = target.GetComponent<BoxCollider>();
            if (box == null)
            {
                Collider existing = target.GetComponent<Collider>();
                if (existing != null && existing is not BoxCollider)
                {
                    Transform triggerHost = target.transform.Find("PanelInteract");
                    if (triggerHost == null)
                    {
                        GameObject hostObject = new GameObject("PanelInteract");
                        Undo.RegisterCreatedObjectUndo(hostObject, "Add Panel Interact");
                        triggerHost = hostObject.transform;
                        triggerHost.SetParent(target.transform, false);
                    }

                    box = triggerHost.GetComponent<BoxCollider>();
                    if (box == null)
                        box = Undo.AddComponent<BoxCollider>(triggerHost.gameObject);
                }
                else if (existing == null)
                {
                    box = Undo.AddComponent<BoxCollider>(target);
                }
                else
                {
                    box = (BoxCollider)existing;
                }
            }

            if (box == null)
            {
                Debug.LogWarning($"[BuildingControlPanelSetup] Failed to create interaction BoxCollider on '{target.name}'.");
                return;
            }

            box.isTrigger = true;
            box.center = new Vector3(0f, 1.1f, 0f);
            box.size = new Vector3(1.6f, 2.2f, 1.2f);
            EditorUtility.SetDirty(box);
        }
    }
}
