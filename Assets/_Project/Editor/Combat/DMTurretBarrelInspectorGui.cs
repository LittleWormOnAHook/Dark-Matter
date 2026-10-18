#if UNITY_EDITOR
using System.Collections.Generic;
using FORGE3D;
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>Shared FORGE3D database barrel popup for Inspector and Combat Studio.</summary>
    public static class DMTurretBarrelInspectorGui
    {
        static readonly List<DMTurretBarrelCatalog.Option> Options = new List<DMTurretBarrelCatalog.Option>(24);

        public static bool Draw(
            SerializedObject runtimeSo,
            SerializedProperty authority)
        {
            if (runtimeSo == null)
                return false;

            SerializedProperty database = runtimeSo.FindProperty("barrelDatabase");
            SerializedProperty idProp = runtimeSo.FindProperty("databaseBarrelId");
            SerializedProperty prefabProp = runtimeSo.FindProperty("databaseBarrelPrefab");
            F3DTurretScriptable db = DMTurretBarrelCatalog.LoadDatabase(
                database != null ? database.objectReferenceValue as F3DTurretScriptable : null);

            if (database != null && database.objectReferenceValue == null && db != null)
                database.objectReferenceValue = db;

            DMTurretBarrelCatalog.CollectOptions(db, Options);
            bool changed = DrawPopup("Barrel", idProp, prefabProp, db);

            if (authority != null)
            {
                SerializedProperty overrideDb = authority.FindPropertyRelative("overrideDatabaseBarrel");
                SerializedProperty authorityId = authority.FindPropertyRelative("databaseBarrelId");
                if (overrideDb != null && authorityId != null)
                {
                    EditorGUILayout.BeginHorizontal();
                    overrideDb.boolValue = EditorGUILayout.ToggleLeft(
                        "Authority barrel",
                        overrideDb.boolValue,
                        GUILayout.Width(180f));
                    using (new EditorGUI.DisabledScope(!overrideDb.boolValue))
                    {
                        int current = IndexOf(authorityId.stringValue);
                        string[] labels = BuildLabels();
                        int next = EditorGUILayout.Popup(current, labels);
                        if (next != current)
                        {
                            authorityId.stringValue = IdFromIndex(next);
                            changed = true;
                        }
                    }

                    EditorGUILayout.EndHorizontal();
                }
            }

            return changed;
        }

        static bool DrawPopup(
            string label,
            SerializedProperty idProp,
            SerializedProperty prefabProp,
            F3DTurretScriptable db)
        {
            if (idProp == null)
                return false;

            int current = IndexOf(idProp.stringValue);
            string[] labels = BuildLabels();
            int next = EditorGUILayout.Popup(label, current, labels);
            if (next == current)
                return false;

            string id = IdFromIndex(next);
            idProp.stringValue = id;
            if (prefabProp != null)
                prefabProp.objectReferenceValue = string.IsNullOrEmpty(id)
                    ? null
                    : DMTurretBarrelCatalog.FindPrefab(db, id);
            return true;
        }

        static int IndexOf(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;
            for (int i = 0; i < Options.Count; i++)
            {
                if (DMTurretBarrelCatalog.IdsEqual(Options[i].Id, id))
                    return i + 1;
            }

            return 0;
        }

        static string IdFromIndex(int index)
        {
            if (index <= 0 || index > Options.Count)
                return DMTurretBarrelCatalog.AutoId;
            return Options[index - 1].Id;
        }

        static string[] BuildLabels()
        {
            string[] labels = new string[Options.Count + 1];
            labels[0] = "Auto (infer socket)";
            for (int i = 0; i < Options.Count; i++)
                labels[i + 1] = Options[i].DisplayName;
            return labels;
        }
    }
}
#endif
