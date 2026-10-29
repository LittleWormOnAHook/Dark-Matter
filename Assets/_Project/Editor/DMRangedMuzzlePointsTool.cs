// 2026-10-28e: edit-mode per-form muzzle points for Anthony's placed DM Ranged (drawn) instances.
// Only ADDS missing child objects named DMR_Muzzle_<formId> under the matching barrel/slide part.
// Never moves, re-parents or edits the DM Ranged instance, and never touches a muzzle point that already exists.
using System;
using System.Collections.Generic;
using Project.Combat.DMRanged;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools
{
    public static class DMRangedMuzzlePointsTool
    {
        // form id -> (part name, local axis of the part pointing out of the barrel)
        static readonly Dictionary<string, (string part, Vector3 axis)> Map = new Dictionary<string, (string, Vector3)>(StringComparer.OrdinalIgnoreCase)
        {
            { "sniper",   ("Barrel 002",        Vector3.forward) },
            { "smp",      ("Barrel 1",          Vector3.forward) },
            { "pistol_3", ("Weapon.033_Low002", Vector3.down) },
            { "pistol_2", ("Slide 2",           Vector3.down) },
            { "pistol_1", ("Slide 2",           Vector3.down) },
        };

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "DM Ranged Muzzle Points (Create or Find)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Menu_Band)]
        public static void Run() => Debug.Log("[DMRanged] " + CreateOrFind());

        public static string CreateOrFind()
        {
            if (Application.isPlaying) return "Exit Play mode first (muzzle points must be created in Edit mode).";
            var s = DM_RangedMorphSettings.Instance;
            if (s == null || s.FormCount == 0) return "DM_RangedMorphSettings not found.";
            int created = 0, found = 0, instances = 0;
            var log = new System.Text.StringBuilder();
            foreach (var anim in UnityEngine.Object.FindObjectsByType<Animation>(FindObjectsInactive.Include))
            {
                Transform root = anim != null ? anim.transform : null;
                if (!DMRangedMorphRuntime.IsDMRangedVisual(root) || EditorUtility.IsPersistent(root)) continue;
                if (!HasAncestor(root, "Drawn_") || HasAncestor(root, "Holstered_")) continue;
                instances++;
                for (int i = 0; i < s.FormCount; i++)
                {
                    string id = s.forms[i].id;
                    string name = DMRangedMorphDriver.MuzzlePrefix + id;
                    if (FindDeep(root, name) != null) { found++; continue; } // never overwrite one he moved
                    if (!Map.TryGetValue(id, out var spec)) { log.Append(" no default part for '" + id + "';"); continue; }
                    Transform part = FindDeep(root, spec.part);
                    if (part == null) { log.Append(" part '" + spec.part + "' missing;"); continue; }
                    var go = new GameObject(name);
                    Undo.RegisterCreatedObjectUndo(go, "Create DM Ranged muzzle point");
                    go.transform.SetParent(part, false);
                    go.transform.localPosition = EndOf(part, spec.axis);
                    Vector3 up = Mathf.Abs(Vector3.Dot(spec.axis, Vector3.up)) > 0.9f ? Vector3.forward : Vector3.up;
                    go.transform.localRotation = Quaternion.LookRotation(spec.axis, up); // +Z = shot direction
                    go.transform.localScale = Vector3.one;
                    EditorSceneManager.MarkSceneDirty(root.gameObject.scene);
                    created++;
                }
            }
            return $"DM Ranged instances: {instances}, muzzle points created: {created}, already present (left as-is): {found}.{log}" +
                   (created > 0 ? " Save the scene yourself to keep them." : "");
        }

        static Vector3 EndOf(Transform part, Vector3 axis)
        {
            Mesh mesh = null;
            var mf = part.GetComponent<MeshFilter>();
            if (mf != null) mesh = mf.sharedMesh;
            var smr = part.GetComponent<SkinnedMeshRenderer>();
            if (mesh == null && smr != null) mesh = smr.sharedMesh;
            if (mesh == null) return Vector3.zero;
            Bounds b = mesh.bounds;
            Vector3 e = Vector3.Scale(b.extents, new Vector3(Mathf.Abs(axis.x), Mathf.Abs(axis.y), Mathf.Abs(axis.z)));
            return b.center + Vector3.Scale(axis, e);
        }

        static bool HasAncestor(Transform t, string prefix)
        {
            for (Transform p = t.parent; p != null; p = p.parent)
                if (p.name.StartsWith(prefix, StringComparison.Ordinal)) return true;
            return false;
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t != root && string.Equals(t.name, name, StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }
    }

    [CustomEditor(typeof(DM_RangedMorphSettings))]
    public sealed class DM_RangedMorphSettingsEditor : Editor
    {
        string status;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
                "Muzzle points (2026-10-28e): one child per form named DMR_Muzzle_<form id> under the barrel/slide part " +
                "(sniper = Rifle form: Barrel 002 Z+, smp: Barrel 1 Z+, pistol_3: Weapon.033_Low002 Y-, pistol_2/1: Slide 2 Y-). " +
                "They follow the morph and are used as-is (blue +Z = shot direction). Move them freely in Edit mode, then save the scene. " +
                "Left-hand IK (Drawn_ slot leftHandIK) and the DM Ranged mesh position are never written at runtime.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Create / Find Muzzle Points")) status = DMRangedMuzzlePointsTool.CreateOrFind();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}