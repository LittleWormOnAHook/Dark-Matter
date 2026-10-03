#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using GenesisPCG.RockCreation.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.GenesisStudio
{
    /// <summary>
    /// Thin Genesis Studio adapter for Environment &gt; Strata. The Studio is IMGUI; this reserves the content rect and lays a
    /// UI Toolkit host over it (window.rootVisualElement), with a third tab level (Strata sub-tabs). The Rocks and Cliffs tab
    /// is the package panel <see cref="PcgRockStudioPanel"/> (game-agnostic); theming comes from <see cref="DMStudioFrontierTheme"/>.
    /// Add more Strata sub-tabs by appending to <see cref="Tabs"/>.
    /// </summary>
    public sealed class DMStudioStrataPanel : IDisposable
    {
        public readonly struct StrataTab
        {
            public readonly string Id, Label;
            public readonly Func<VisualElement> Create;
            public StrataTab(string id, string label, Func<VisualElement> create) { Id = id; Label = label; Create = create; }
        }

        /// <summary>Strata sub-tabs (third level). Rocks and Cliffs = the Genesis PCG Rock Creation studio panel.</summary>
        public static readonly List<StrataTab> Tabs = new List<StrataTab>
        {
            new StrataTab("rocks-cliffs", "Rocks and Cliffs", () => new PcgRockStudioPanel()),
        };

        private EditorWindow m_host;
        private VisualElement m_root, m_bar, m_content, m_current;
        private readonly List<Button> m_tabButtons = new List<Button>();
        private int m_tab;
        private bool m_drawn, m_visible;

        public VisualElement Current => m_current;
        public PcgRockStudioPanel RocksPanel => m_current as PcgRockStudioPanel;

        /// <summary>Called from the Studio content area (IMGUI): reserves the rest of the area and positions the UI Toolkit host over it.</summary>
        public void Draw(EditorWindow host)
        {
            Ensure(host);
            Rect r = GUILayoutUtility.GetRect(100f, 100000f, 520f, 100000f, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            if (Event.current.type != EventType.Repaint) return;
            Rect s = GUIUtility.GUIToScreenRect(r);
            Vector2 o = host.position.position;
            m_root.style.left = s.x - o.x;
            m_root.style.top = s.y - o.y;
            m_root.style.width = s.width;
            m_root.style.height = s.height;
            SetVisible(true);
            m_drawn = true;
        }

        /// <summary>Called at the end of the Studio's OnGUI: hides the host when another tab was drawn this repaint.</summary>
        public void EndGUI()
        {
            if (Event.current == null || Event.current.type != EventType.Repaint) return;
            if (!m_drawn) SetVisible(false);
            m_drawn = false;
        }

        private void SetVisible(bool v)
        {
            if (m_root == null || m_visible == v) return;
            m_visible = v;
            m_root.style.display = v ? DisplayStyle.Flex : DisplayStyle.None;
            if (m_current is PcgRockStudioPanel p) p.SetSuspended(!v); // free the preview's GPU memory while hidden
        }

        private void Ensure(EditorWindow host)
        {
            if (m_root != null && m_host == host && m_root.panel != null) return;
            Dispose();
            m_host = host;
            m_root = new VisualElement { name = "dm-strata-host" };
            m_root.AddToClassList("dm-strata");
            m_root.style.position = Position.Absolute;
            DMStudioFrontierTheme.Apply(m_root);

            var crumb = new Label("ENVIRONMENT  ›  STRATA");
            crumb.AddToClassList("dm-strata__crumb");
            crumb.AddToClassList("pcg-header");
            m_root.Add(crumb);
            m_bar = new VisualElement();
            m_bar.AddToClassList("dm-strata__tabs");
            m_root.Add(m_bar);
            m_tabButtons.Clear();
            for (int i = 0; i < Tabs.Count; i++)
            {
                int idx = i;
                var b = new Button(() => Select(idx)) { text = Tabs[i].Label.ToUpperInvariant() };
                b.AddToClassList("dm-strata__tab");
                PcgCutCorners.Apply(b);
                m_bar.Add(b);
                m_tabButtons.Add(b);
            }
            m_content = new VisualElement();
            m_content.AddToClassList("dm-strata__content");
            m_root.Add(m_content);
            host.rootVisualElement.Add(m_root);
            m_visible = true;
            Select(Mathf.Clamp(m_tab, 0, Tabs.Count - 1));
        }

        public void Select(int i)
        {
            if (i < 0 || i >= Tabs.Count || m_content == null) return;
            m_tab = i;
            for (int k = 0; k < m_tabButtons.Count; k++) m_tabButtons[k].EnableInClassList("dm-strata__tab--selected", k == i);
            m_content.Clear(); // detaches (and releases) the previous tab
            m_current = null;
            try { m_current = Tabs[i].Create(); }
            catch (Exception e) { Debug.LogException(e); }
            if (m_current == null) return;
            m_current.style.flexGrow = 1f;
            m_content.Add(m_current);
        }

        public void Dispose()
        {
            if (m_root != null) m_root.RemoveFromHierarchy(); // detach: the panel frees its preview / render texture
            m_root = null;
            m_current = null;
            m_host = null;
            m_visible = false;
        }
    }
}
#endif
