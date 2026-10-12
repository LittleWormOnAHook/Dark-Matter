using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Project.AI.Invector;
using Project.Combat;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>
    /// Weapon placement data (Bake to template). One entry per weapon item id + kind (Drawn / Holstered) + holder bone, optionally for one body type only.
    /// Rebuild From Template / Fix (restore holders) / Sync to Template read this asset FIRST, so hand-tweaked placements survive.
    /// </summary>
    public sealed class DMWeaponPlacementProfile : ScriptableObject
    {
        public const string AssetPath = "Assets/_Project/Data/Combat/DM_WeaponPlacementProfile.asset";

        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Weapon item id: the slot name without its Drawn_ / Holstered_ prefix (for example Blade_Of_Chaos).")] public string itemId;
            [Tooltip("True = Drawn_<item> (in hand), false = Holstered_<item> (on the body).")] public bool drawn;
            [Tooltip("Name of the holder bone / holder object the slot sits under (for example R_Hand, Spine2, RifleHolder). Empty = any holder.")] public string holderBone;
            [Tooltip("Any = every body type. Otherwise this entry only applies to that body type and wins over the Any entry.")] public bool anyBodyType = true;
            public DMEnemyBodyType bodyType = DMEnemyBodyType.Humanoid;
            public Vector3 localPosition;
            public Vector3 localEulerAngles;
            public Vector3 localScale = Vector3.one;

            public string Label => (drawn ? "Drawn_" : "Holstered_") + itemId + " @ " + (string.IsNullOrEmpty(holderBone) ? "*" : holderBone) + (anyBodyType ? " [any body]" : " [" + bodyType + "]");
        }

        public List<Entry> entries = new List<Entry>();

        public static DMWeaponPlacementProfile LoadOrCreate(bool create)
        {
            DMWeaponPlacementProfile p = AssetDatabase.LoadAssetAtPath<DMWeaponPlacementProfile>(AssetPath);
            if (p != null || !create)
                return p;

            Directory.CreateDirectory(Path.GetDirectoryName(AssetPath));
            p = CreateInstance<DMWeaponPlacementProfile>();
            AssetDatabase.CreateAsset(p, AssetPath);
            AssetDatabase.SaveAssets();
            return p;
        }

        /// <summary>Best entry: exact holder + body type > exact holder, any body > any holder + body type > any holder, any body.</summary>
        public Entry Find(string itemId, bool drawn, string holderBone, DMEnemyBodyType body)
        {
            Entry best = null;
            int bestScore = -1;
            for (int i = 0; i < entries.Count; i++)
            {
                Entry e = entries[i];
                if (e == null || e.drawn != drawn || !string.Equals(e.itemId, itemId, StringComparison.Ordinal))
                    continue;
                if (!string.IsNullOrEmpty(e.holderBone) && !string.Equals(e.holderBone, holderBone, StringComparison.Ordinal))
                    continue;
                if (!e.anyBodyType && e.bodyType != body)
                    continue;

                int score = (string.IsNullOrEmpty(e.holderBone) ? 0 : 2) + (e.anyBodyType ? 0 : 1);
                if (score > bestScore)
                {
                    bestScore = score;
                    best = e;
                }
            }

            return best;
        }
    }

    /// <summary>Capture / diff / apply of weapon slot placements, plus the hook that makes every rebuild path read the profile first.</summary>
    [InitializeOnLoad]
    public static class DMWeaponPlacementBake
    {
        const float Eps = 0.0005f;

        static DMWeaponPlacementBake()
        {
            EnemyInvectorWeaponHolderRebind.AfterRebind = root => ApplyFromProfile(root, null);
        }

        // Existing placements captured right before a Studio rebuild / fix / sync (see DMHumanoidVisualRebuildUtility.BackupExistingOutput).
        sealed class Snapshot
        {
            public double Time;
            public readonly Dictionary<string, (Vector3 p, Vector3 r, Vector3 s)> ByKey = new Dictionary<string, (Vector3, Vector3, Vector3)>();
            public readonly Dictionary<string, (Vector3 p, Vector3 r, Vector3 s)> ByName = new Dictionary<string, (Vector3, Vector3, Vector3)>();
        }

        static readonly Dictionary<string, Snapshot> Snapshots = new Dictionary<string, Snapshot>();
        const double SnapshotSeconds = 180.0;

        public static void SnapshotExisting(string prefabPath)
        {
            if (string.IsNullOrEmpty(prefabPath) || !prefabPath.EndsWith(".prefab", StringComparison.Ordinal) || !File.Exists(prefabPath))
                return;
            try
            {
                GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
                try
                {
                    var snap = new Snapshot { Time = EditorApplication.timeSinceStartup };
                    foreach (Transform slot in Slots(root))
                    {
                        var v = (slot.localPosition, slot.localEulerAngles, slot.localScale);
                        snap.ByKey[slot.name + "@" + slot.parent.name] = v;
                        snap.ByName[slot.name] = v;
                    }

                    Snapshots[Path.GetFileNameWithoutExtension(prefabPath)] = snap;
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            catch (Exception ex) { Debug.LogWarning("[WeaponPlacement] snapshot failed: " + ex.Message); }
        }

        static bool TrySnapshot(GameObject root, Transform slot, out (Vector3 p, Vector3 r, Vector3 s) v)
        {
            v = default;
            string key = root.name;
            if (key.EndsWith("(Clone)", StringComparison.Ordinal)) key = key.Substring(0, key.Length - 7);
            if (!Snapshots.TryGetValue(key, out Snapshot snap) || EditorApplication.timeSinceStartup - snap.Time > SnapshotSeconds)
                return false;
            return snap.ByKey.TryGetValue(slot.name + "@" + slot.parent.name, out v) || snap.ByName.TryGetValue(slot.name, out v);
        }

        public static bool IsSlotName(string n) => n.StartsWith("Drawn_", StringComparison.Ordinal) || n.StartsWith("Holstered_", StringComparison.Ordinal);

        public static DMEnemyBodyType BodyTypeOf(GameObject root)
        {
            DMEnemyHitMarks marks = root != null ? root.GetComponentInChildren<DMEnemyHitMarks>(true) : null;
            return marks != null ? marks.BodyType : DMEnemyBodyType.Humanoid;
        }

        /// <summary>Top-level weapon slots (a slot nested inside another slot is part of that weapon, not a placement).</summary>
        public static List<Transform> Slots(GameObject root)
        {
            var list = new List<Transform>();
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t == root.transform || !IsSlotName(t.name) || t.parent == null || IsSlotName(t.parent.name))
                    continue;
                list.Add(t);
            }

            return list;
        }

        static (string item, bool drawn) Split(string slotName)
        {
            bool drawn = slotName.StartsWith("Drawn_", StringComparison.Ordinal);
            return (slotName.Substring(drawn ? 6 : 10), drawn);
        }

        static bool Same(Vector3 a, Vector3 b) => (a - b).sqrMagnitude < Eps * Eps;

        static bool SameAngles(Vector3 a, Vector3 b) => Quaternion.Angle(Quaternion.Euler(a), Quaternion.Euler(b)) < 0.05f;

        static string V(Vector3 v) => "(" + v.x.ToString("0.###") + ", " + v.y.ToString("0.###") + ", " + v.z.ToString("0.###") + ")";

        public sealed class Change
        {
            public DMWeaponPlacementProfile.Entry Entry; // target entry (existing or new)
            public bool IsNew;
            public string Text;
        }

        /// <summary>What baking this prefab would write into the profile (nothing is modified).</summary>
        public static List<Change> Diff(GameObject root, DMWeaponPlacementProfile profile)
        {
            var changes = new List<Change>();
            DMEnemyBodyType body = BodyTypeOf(root);
            foreach (Transform slot in Slots(root))
            {
                var (item, drawn) = Split(slot.name);
                string bone = slot.parent.name;
                Vector3 pos = slot.localPosition, rot = slot.localEulerAngles, scl = slot.localScale;

                // 1) exact (bone + this body type) entry: update it. 2) any-body entry: equal = nothing, different = body-type override. 3) none: new any-body entry.
                DMWeaponPlacementProfile.Entry exactBody = profile.entries.FirstOrDefault(e => e != null && e.drawn == drawn && e.itemId == item && e.holderBone == bone && !e.anyBodyType && e.bodyType == body);
                DMWeaponPlacementProfile.Entry anyBody = profile.entries.FirstOrDefault(e => e != null && e.drawn == drawn && e.itemId == item && e.holderBone == bone && e.anyBodyType);
                DMWeaponPlacementProfile.Entry target = exactBody;
                bool isNew = false;
                if (target == null)
                {
                    if (anyBody == null)
                    {
                        target = new DMWeaponPlacementProfile.Entry { itemId = item, drawn = drawn, holderBone = bone, anyBodyType = true };
                        isNew = true;
                    }
                    else if (Same(anyBody.localPosition, pos) && SameAngles(anyBody.localEulerAngles, rot) && Same(anyBody.localScale, scl))
                    {
                        continue; // already covered
                    }
                    else
                    {
                        target = new DMWeaponPlacementProfile.Entry { itemId = item, drawn = drawn, holderBone = bone, anyBodyType = false, bodyType = body };
                        isNew = true;
                    }
                }

                if (!isNew && Same(target.localPosition, pos) && SameAngles(target.localEulerAngles, rot) && Same(target.localScale, scl))
                    continue;

                var c = new Change { Entry = target, IsNew = isNew };
                c.Text = (isNew ? "NEW " : "UPDATE ") + target.Label + ": pos " + (isNew ? "-" : V(target.localPosition)) + " -> " + V(pos) + ", rot " + (isNew ? "-" : V(target.localEulerAngles)) + " -> " + V(rot) + ", scale " + (isNew ? "-" : V(target.localScale)) + " -> " + V(scl);
                // stash new values
                c.Entry = new DMWeaponPlacementProfile.Entry { itemId = target.itemId, drawn = target.drawn, holderBone = target.holderBone, anyBodyType = target.anyBodyType, bodyType = target.bodyType, localPosition = pos, localEulerAngles = rot, localScale = scl };
                c.IsNew = isNew;
                changes.Add(c);
            }

            return changes;
        }

        static void ApplyChangesInMemory(DMWeaponPlacementProfile profile, List<Change> changes)
        {
            foreach (Change c in changes)
            {
                DMWeaponPlacementProfile.Entry e = profile.entries.FirstOrDefault(x => x != null && x.drawn == c.Entry.drawn && x.itemId == c.Entry.itemId && x.holderBone == c.Entry.holderBone && x.anyBodyType == c.Entry.anyBodyType && (x.anyBodyType || x.bodyType == c.Entry.bodyType));
                if (e == null) { profile.entries.Add(c.Entry); continue; }
                e.localPosition = c.Entry.localPosition; e.localEulerAngles = c.Entry.localEulerAngles; e.localScale = c.Entry.localScale;
            }
        }

        public static void ApplyChanges(DMWeaponPlacementProfile profile, List<Change> changes)
        {
            Undo.RecordObject(profile, "Bake weapon placements");
            foreach (Change c in changes)
            {
                DMWeaponPlacementProfile.Entry e = profile.entries.FirstOrDefault(x => x != null && x.drawn == c.Entry.drawn && x.itemId == c.Entry.itemId && x.holderBone == c.Entry.holderBone && x.anyBodyType == c.Entry.anyBodyType && (x.anyBodyType || x.bodyType == c.Entry.bodyType));
                if (e == null)
                {
                    profile.entries.Add(c.Entry);
                    continue;
                }

                e.localPosition = c.Entry.localPosition;
                e.localEulerAngles = c.Entry.localEulerAngles;
                e.localScale = c.Entry.localScale;
            }

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
        }

        /// <summary>Apply the profile to the slots of a root (prefab contents or instance). Returns the number of slots moved. Log lists old -> new.</summary>
        public static int ApplyFromProfile(GameObject root, StringBuilder log)
        {
            DMWeaponPlacementProfile profile = DMWeaponPlacementProfile.LoadOrCreate(false);
            if (root == null)
                return 0;

            DMEnemyBodyType body = BodyTypeOf(root);
            int moved = 0;
            foreach (Transform slot in Slots(root))
            {
                // 1) the placement the prefab had before this rebuild / fix (never reset what already exists)
                if (TrySnapshot(root, slot, out var old))
                {
                    if (!Same(slot.localPosition, old.p) || !SameAngles(slot.localEulerAngles, old.r) || !Same(slot.localScale, old.s))
                    {
                        string l0 = "[WeaponPlacement] " + root.name + " " + slot.name + ": kept existing placement: pos " + V(slot.localPosition) + " -> " + V(old.p) + ", rot " + V(slot.localEulerAngles) + " -> " + V(old.r) + ", scale " + V(slot.localScale) + " -> " + V(old.s);
                        slot.localPosition = old.p;
                        slot.localRotation = Quaternion.Euler(old.r);
                        slot.localScale = old.s;
                        moved++;
                        log?.AppendLine(l0);
                        Debug.Log(l0);
                    }

                    continue;
                }

                // 2) a newly created slot: the baked profile, else whatever the template gave it
                if (profile == null || profile.entries.Count == 0)
                    continue;
                var (item, drawn) = Split(slot.name);
                DMWeaponPlacementProfile.Entry e = profile.Find(item, drawn, slot.parent.name, body);
                if (e == null)
                    continue;
                if (Same(slot.localPosition, e.localPosition) && SameAngles(slot.localEulerAngles, e.localEulerAngles) && Same(slot.localScale, e.localScale))
                    continue;

                string line = "[WeaponPlacement] " + root.name + " " + slot.name + ": pos " + V(slot.localPosition) + " -> " + V(e.localPosition) + ", rot " + V(slot.localEulerAngles) + " -> " + V(e.localEulerAngles) + ", scale " + V(slot.localScale) + " -> " + V(e.localScale) + " (" + e.Label + ")";
                slot.localPosition = e.localPosition;
                slot.localRotation = Quaternion.Euler(e.localEulerAngles);
                slot.localScale = e.localScale;
                moved++;
                log?.AppendLine(line);
                Debug.Log(line);
            }

            return moved;
        }

        // ---- prefab level -------------------------------------------------------------------------------

        public static List<string> EnemyPrefabPaths()
        {
            var list = new List<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Project/Prefabs/Combat/Enemies" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                string n = Path.GetFileNameWithoutExtension(path);
                if (n.IndexOf("Lisa", StringComparison.OrdinalIgnoreCase) >= 0 || n == "TrainingDummy" || n == "EnemyLootBox" || n == "Gongo")
                    continue;
                list.Add(path);
            }

            list.Sort(StringComparer.Ordinal);
            return list;
        }

        public static string DiffText(IEnumerable<string> paths, out List<(DMWeaponPlacementProfile, List<Change>)> work)
        {
            work = new List<(DMWeaponPlacementProfile, List<Change>)>();
            DMWeaponPlacementProfile profile = DMWeaponPlacementProfile.LoadOrCreate(true);
            var sb = new StringBuilder();
            var all = new List<Change>();
            // Work on a copy so each prefab is compared with what the earlier prefabs of this pass already wrote (a differing rig becomes a body-type override, never silently dropped).
            DMWeaponPlacementProfile real = profile;
            profile = UnityEngine.Object.Instantiate(profile);
            var ordered = paths.OrderBy(p => Path.GetFileNameWithoutExtension(p) == "HumanoidEnemy_Invector" ? 0 : 1).ThenBy(p => p, StringComparer.Ordinal).ToList();
            foreach (string path in ordered)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // Changes from earlier prefabs in the same pass count as already applied (so Bake all does not flip-flop between prefabs sharing a key).
                    var changes = Diff(root, profile);
                    ApplyChangesInMemory(profile, changes);
                    sb.AppendLine(Path.GetFileNameWithoutExtension(path) + " (" + BodyTypeOf(root) + "): " + (changes.Count == 0 ? "no change" : changes.Count + " change(s)"));
                    foreach (Change c in changes)
                        sb.AppendLine("   " + c.Text);
                    all.AddRange(changes);
                }
                finally
                {
                    PrefabUtility.UnloadPrefabContents(root);
                }
            }

            UnityEngine.Object.DestroyImmediate(profile);
            work.Add((real, all));
            return sb.ToString();
        }

        /// <summary>Per-prefab / all button: show the old-vs-new diff, write the profile only after confirming. Prefabs are never modified by baking.</summary>
        public static string BakeInteractive(IEnumerable<string> paths, bool confirm = true)
        {
            string diff = DiffText(paths, out var work);
            var (profile, changes) = work[0];
            if (changes.Count == 0)
            {
                if (confirm) EditorUtility.DisplayDialog("Bake weapon placements", "Nothing to bake: the profile already matches.\n\n" + diff, "OK");
                return diff;
            }

            string shown = diff.Length > 1800 ? diff.Substring(0, 1800) + "\n..." : diff;
            if (confirm && !EditorUtility.DisplayDialog("Bake weapon placements", "Write these placements into " + DMWeaponPlacementProfile.AssetPath + "?\n(Prefabs are not modified; Rebuild / Fix / Sync read the profile first.)\n\n" + shown, "Bake", "Cancel"))
                return diff;

            ApplyChanges(profile, changes);
            Debug.Log("[WeaponPlacement] baked " + changes.Count + " change(s):\n" + diff);
            return diff;
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Weapon Placement/Bake all enemy prefabs")]
        static void MenuBakeAll() => BakeInteractive(EnemyPrefabPaths());

        [MenuItem("Tools/Dark Matter Genesis/Combat/Weapon Placement/Apply profile to selected prefab")]
        static void MenuApplySelected()
        {
            foreach (UnityEngine.Object o in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(o);
                if (!path.EndsWith(".prefab", StringComparison.Ordinal)) continue;
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    int n = ApplyFromProfile(root, null);
                    if (n > 0) PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
        }

        [MenuItem("Tools/Dark Matter Genesis/Combat/Weapon Placement/Select placement profile")]
        static void MenuSelectProfile() => Selection.activeObject = DMWeaponPlacementProfile.LoadOrCreate(true);

        [MenuItem("Tools/Dark Matter Genesis/Combat/Weapon Placement/Bake window")]
        static void MenuWindow() => EditorWindow.GetWindow<DMWeaponPlacementBakeWindow>("Weapon Placement");
    }

    public sealed class DMWeaponPlacementBakeWindow : EditorWindow
    {
        Vector2 scroll;
        string last = "";

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Bake to template: captures the local position / rotation / scale of Drawn_<weapon> and Holstered_<weapon> from a hand-tweaked prefab into DM_WeaponPlacementProfile (keyed by weapon id + holder bone, body-type override when two rigs differ). Rebuild From Template, Fix and Sync to Template read the profile first. Baking never modifies the prefab; you confirm the old -> new diff first.", MessageType.Info);
            if (GUILayout.Button("Bake all enemy prefabs", GUILayout.Height(26f)))
                last = DMWeaponPlacementBake.BakeInteractive(DMWeaponPlacementBake.EnemyPrefabPaths());
            if (GUILayout.Button("Select placement profile"))
                Selection.activeObject = DMWeaponPlacementProfile.LoadOrCreate(true);
            EditorGUILayout.Space();
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (string path in DMWeaponPlacementBake.EnemyPrefabPaths())
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(Path.GetFileNameWithoutExtension(path));
                if (GUILayout.Button("Bake placements", GUILayout.Width(130f)))
                    last = DMWeaponPlacementBake.BakeInteractive(new[] { path });
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();
            if (!string.IsNullOrEmpty(last))
                EditorGUILayout.TextArea(last, GUILayout.MinHeight(80f));
        }
    }
}
