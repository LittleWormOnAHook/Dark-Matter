// 2026-10-28l: edit-mode MorphBlade holders on the player (Drawn_MorphBlade / Holstered_MorphBlade), so the
// bridge binds authored slots instead of spawning InvectorWeapon_ at runtime. Find-before-create: an existing
// holder is only selected, never moved. Never moves, renames or re-parents any existing object.
using Project.Interaction;
using Project.Prototypes.Weapons;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Project.EditorTools
{
    public static class DMMorphBladeHoldersTool
    {
        public const string DefaultPlayerPrefab = "Assets/_Project/Prefabs/Players/Player_Kade_v2.prefab";
        public const string HeldPrefabPath = "Assets/_Project/Prefabs/Weapons/Prototypes/MorphBlade_Held.prefab";
        public const string DrawnName = "Drawn_MorphBlade", HolsteredName = "Holstered_MorphBlade";
        const string DrawnRef = "Drawn_Blade_of_Chaos", HolsteredRef = "Holstered_Blade_of_Chaos";

        [MenuItem(DarkMatterGenesisEditorMenus.Combat + "MorphBlade Holders (Create or Find)", false, Project.EditorTools.DarkMatterGenesisMenuPriority.Tools_Dark_Matter_Genesis_Combat_Menu_Band)]
        public static void Run() => Debug.Log("[MorphBlade] " + CreateOrFind());

        /// <summary>Open prefab stage / selected scene player first; otherwise edits and saves Player_Kade_v2.prefab.</summary>
        public static string CreateOrFind()
        {
            if (Application.isPlaying) return "Exit Play mode first.";
            var stage = PrefabStageUtility.GetCurrentPrefabStage();
            if (stage != null)
            {
                string r = Apply(stage.prefabContentsRoot.transform, out bool ch);
                if (ch) EditorSceneManager.MarkSceneDirty(stage.scene);
                return r + (ch ? " (prefab stage: save the prefab)" : "");
            }
            var sel = Selection.activeTransform;
            if (sel != null && FindDeep(sel.root, DrawnRef) != null)
            {
                string r = Apply(sel.root, out bool ch);
                if (ch) EditorSceneManager.MarkSceneDirty(sel.gameObject.scene);
                return r + (ch ? " (scene object: save the scene)" : "");
            }
            return ApplyToPrefabAsset(DefaultPlayerPrefab);
        }

        public static string ApplyToPrefabAsset(string path)
        {
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                string r = Apply(root.transform, out bool ch);
                if (ch) PrefabUtility.SaveAsPrefabAsset(root, path);
                return r + (ch ? " Saved " + path : "");
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        static string Apply(Transform player, out bool changed)
        {
            changed = false;
            var held = AssetDatabase.LoadAssetAtPath<GameObject>(HeldPrefabPath);
            if (held == null) return "Missing " + HeldPrefabPath + " (Combat/Build Morph Weapon Prototypes).";
            string log = "";
            Transform d = FindDeep(player, DrawnName), h = FindDeep(player, HolsteredName);
            if (d == null)
            {
                var reference = FindDeep(player, DrawnRef);
                if (reference == null) return "No " + DrawnRef + " under " + player.name + ".";
                // Copy of the Blade of Chaos drawn holder keeps vMeleeWeapon / hitBox / constrain; its visual is replaced.
                var go = Object.Instantiate(reference.gameObject, reference.parent, false);
                go.name = DrawnName;
                go.transform.SetSiblingIndex(reference.GetSiblingIndex() + 1);
                for (int i = go.transform.childCount - 1; i >= 0; i--)
                {
                    var c = go.transform.GetChild(i);
                    if (c.GetComponent<EquippedVisualMarker>() != null || c.name.StartsWith("PioneerVisual_")) Object.DestroyImmediate(c.gameObject);
                }
                Fill(go.transform, held);
                d = go.transform; changed = true; log += "Created " + DrawnName + ". ";
            }
            else log += "Found " + DrawnName + ". ";
            if (h == null)
            {
                var reference = FindDeep(player, HolsteredRef);
                if (reference == null) return log + "No " + HolsteredRef + ".";
                var go = new GameObject(HolsteredName);
                go.transform.SetParent(reference.parent, false);
                go.transform.localPosition = reference.localPosition;
                go.transform.localRotation = reference.localRotation;
                go.transform.SetSiblingIndex(reference.GetSiblingIndex() + 1);
                Fill(go.transform, held);
                h = go.transform; changed = true; log += "Created " + HolsteredName + ". ";
            }
            else log += "Found " + HolsteredName + ". ";
            if (!Application.isBatchMode) Selection.activeTransform = d;
            return log;
        }

        // Holder > MB_Grip > (MorphBlade_Held instance, MB_Hilt, MB_BladeTip). Move MB_Grip / points freely.
        static void Fill(Transform holder, GameObject held)
        {
            var grip = new GameObject("MB_Grip").transform; grip.SetParent(holder, false);
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(held, grip);
            inst.name = "MorphBlade_Held";
            var hilt = new GameObject("MB_Hilt").transform; hilt.SetParent(grip, false);
            var tip = new GameObject("MB_BladeTip").transform; tip.SetParent(grip, false); tip.localPosition = new Vector3(0f, 1.05f, 0f);
        }

        static Transform FindDeep(Transform root, string name)
        {
            foreach (var t in root.GetComponentsInChildren<Transform>(true))
                if (string.Equals(t.name, name, System.StringComparison.OrdinalIgnoreCase)) return t;
            return null;
        }
    }

    [CustomEditor(typeof(DM_WeaponMorphStudioSettings))]
    public sealed class DM_WeaponMorphStudioSettingsEditor : Editor
    {
        string status;
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox("MorphBlade holders (2026-10-28l): Drawn_MorphBlade / Holstered_MorphBlade next to the Blade of Chaos holders, " +
                "with MB_Grip (move to reposition), MB_Hilt and MB_BladeTip. Uses the open prefab stage, else the selected scene player, else Player_Kade_v2.prefab. Existing holders are only found, never moved.", MessageType.Info);
            using (new EditorGUI.DisabledScope(Application.isPlaying))
                if (GUILayout.Button("Create / Find MorphBlade Holders")) status = DMMorphBladeHoldersTool.CreateOrFind();
            if (!string.IsNullOrEmpty(status)) EditorGUILayout.HelpBox(status, MessageType.None);
            EditorGUILayout.Space(4f);
            DrawDefaultInspector();
        }
    }
}