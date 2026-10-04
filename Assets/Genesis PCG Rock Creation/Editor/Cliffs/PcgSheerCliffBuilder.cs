using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace GenesisPCG.RockCreation.Editor
{
    [InitializeOnLoad]
    internal static class PcgSheerCliffRegistration
    {
        static PcgSheerCliffRegistration() => PcgStudioExtensions.RegisterCliffBuilder(new PcgSheerCliffBuilder());
    }

    /// <summary>"Sheer Cliff" card in the Rock Studio Cliffs tab: create / pick a cliff path, edit its settings, generate, bake, save.</summary>
    public sealed class PcgSheerCliffBuilder : IPcgCliffBuilder
    {
        public string DisplayName => "Sheer Cliff";

        private IPcgStudioContext m_ctx;
        private VisualElement m_root, m_settingsHost;
        private ObjectField m_pathField;
        private Toggle m_live;
        private PcgCliffPath m_path;
        private SerializedObject m_so;
        private double m_regenAt = -1;

        public VisualElement CreateGUI(IPcgStudioContext context)
        {
            m_ctx = context;
            m_root = new VisualElement();

            var hint = new Label("Draw the cliff's top edge in the Scene view: Shift+click adds points, Ctrl+click removes one, drag the handles to move them. The arrows show the way the face points.");
            hint.AddToClassList("pcg-label-dim");
            hint.AddToClassList("pcg-wrap");
            m_root.Add(hint);

            m_pathField = new ObjectField("Cliff Path") { objectType = typeof(PcgCliffPath), allowSceneObjects = true };
            m_pathField.AddToClassList("pcg-field");
            m_pathField.RegisterValueChangedCallback(e => Bind(e.newValue as PcgCliffPath));
            m_root.Add(m_pathField);

            var row = Row();
            row.Add(Btn("New Sheer Cliff", CreateNew, true));
            row.Add(Btn("Use Selection", () => Bind(Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<PcgCliffPath>() : null), false));
            m_root.Add(row);

            m_live = new Toggle("Live Regenerate") { value = true };
            m_live.AddToClassList("pcg-toggle");
            m_root.Add(m_live);

            m_settingsHost = new VisualElement();
            m_root.Add(m_settingsHost);

            var r1 = Row();
            r1.Add(Btn("Generate", () => Run(p => p.Regenerate(true).ToString()), true));
            r1.Add(Btn("Reseed", () => Run(p =>
            {
                Undo.RecordObject(p, "Reseed Cliff");
                p.settings.seed = Random.Range(1, 99999);
                return p.Regenerate(true).ToString();
            }), false));
            r1.Add(Btn("Clear", () => Run(p => { p.ClearGenerated(); return "Cleared."; }), false));
            m_root.Add(r1);
            var r2 = Row();
            r2.Add(Btn("Bake Chunks", () => Run(PcgCliffBake.Bake), false));
            r2.Add(Btn("Save to Library", () => Run(p => PcgCliffBake.SaveToLibrary(p, m_ctx)), true));
            m_root.Add(r2);

            Selection.selectionChanged += OnSelection;
            EditorApplication.update += Tick;
            OnSelection();
            return m_root;
        }

        public void Dispose()
        {
            Selection.selectionChanged -= OnSelection;
            EditorApplication.update -= Tick;
            m_so = null;
            m_path = null;
        }

        private static VisualElement Row()
        {
            var r = new VisualElement();
            r.AddToClassList("pcg-row");
            return r;
        }

        private static Button Btn(string text, System.Action click, bool primary)
        {
            var b = new Button(click) { text = text };
            b.AddToClassList("pcg-btn");
            if (primary) b.AddToClassList("pcg-btn-primary");
            PcgCutCorners.Apply(b);
            return b;
        }

        private void Status(string s) { m_ctx?.SetStatus(s); }

        private void Run(System.Func<PcgCliffPath, string> act)
        {
            if (m_path == null) { Status("Create or select a Sheer Cliff first."); return; }
            Status(act(m_path));
        }

        private void OnSelection()
        {
            GameObject go = Selection.activeGameObject;
            PcgCliffPath p = go != null ? go.GetComponentInParent<PcgCliffPath>() : null;
            if (p != null && p != m_path) Bind(p);
        }

        private void Bind(PcgCliffPath p)
        {
            if (m_root == null) return;
            m_path = p;
            m_pathField.SetValueWithoutNotify(p);
            m_settingsHost.Clear();
            m_so = null;
            if (p == null) return;
            m_so = new SerializedObject(p);
            m_so.FindProperty("settings").isExpanded = true;
            var closed = new PropertyField(m_so.FindProperty("closed"), "Closed Loop");
            var settings = new PropertyField(m_so.FindProperty("settings"), "Settings");
            var holder = new VisualElement(); // fresh element per binding so old trackers go away with it
            holder.Add(closed);
            holder.Add(settings);
            m_settingsHost.Add(holder);
            holder.Bind(m_so);
            holder.TrackSerializedObjectValue(m_so, _ => { if (m_live != null && m_live.value) m_regenAt = EditorApplication.timeSinceStartup + 0.35; });
            Status(p.LastStats.parts > 0 ? p.LastStats.ToString() : $"Editing {p.name}.");
        }

        private void Tick()
        {
            if (m_regenAt < 0 || EditorApplication.timeSinceStartup < m_regenAt) return;
            m_regenAt = -1;
            if (m_path == null || EditorApplication.isPlayingOrWillChangePlaymode) return;
            Status(m_path.Regenerate(true).ToString());
            SceneView.RepaintAll();
        }

        private void CreateNew()
        {
            var go = new GameObject("Sheer Cliff");
            Vector3 pos = Vector3.zero;
            SceneView sv = SceneView.lastActiveSceneView;
            if (sv != null)
            {
                pos = sv.pivot;
                if (PcgSurfaceSnap.SampleGroundY(pos, 500f, 1000f, ~0, PcgSurfaceSnapSettings.Default, new System.Collections.Generic.List<Transform>(), out float gy)) pos.y = gy;
            }
            go.transform.position = pos;
            var p = go.AddComponent<PcgCliffPath>();
            p.settings.kit = PcgCliffBake.DefaultKit();
            p.settings.seed = Random.Range(1, 99999);
            Undo.RegisterCreatedObjectUndo(go, "New Sheer Cliff");
            Selection.activeGameObject = go;
            Bind(p);
            Status(p.settings.kit == null ? "No rock kit found. Pick one in Settings > Kit." : p.Regenerate(true).ToString());
        }
    }
}
