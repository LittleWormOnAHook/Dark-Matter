using UnityEngine;
using UnityEngine.UIElements;

namespace Project.EditorTools.Theme
{
    /// <summary>Panel with cut (chamfered) top-left and bottom-right corners, a 1px edge and a short accent strip.</summary>
    public class GChamferPanel : VisualElement
    {
        static readonly CustomStyleProperty<Color> s_Fill = new CustomStyleProperty<Color>("--g-fill");
        static readonly CustomStyleProperty<Color> s_Stroke = new CustomStyleProperty<Color>("--g-stroke");
        static readonly CustomStyleProperty<Color> s_Strip = new CustomStyleProperty<Color>("--g-strip");
        static readonly CustomStyleProperty<int> s_Cut = new CustomStyleProperty<int>("--g-cut");

        Color m_Fill = GenesisTheme.Bg2;
        Color m_Stroke = GenesisTheme.Line;
        Color m_Strip = GenesisTheme.Accent;
        float m_Cut = 8f;

        public bool ShowStrip { get; set; } = true;

        public GChamferPanel() : this(null) { }

        public GChamferPanel(string title)
        {
            AddToClassList("g-chamfer");
            generateVisualContent += Draw;
            RegisterCallback<CustomStyleResolvedEvent>(OnStyles);
            if (!string.IsNullOrEmpty(title))
            {
                var t = GenesisTheme.HeaderLabel(title);
                t.AddToClassList("g-panel__title");
                Add(t);
            }
        }

        void OnStyles(CustomStyleResolvedEvent e)
        {
            if (customStyle.TryGetValue(s_Fill, out var f)) m_Fill = f;
            if (customStyle.TryGetValue(s_Stroke, out var s)) m_Stroke = s;
            if (customStyle.TryGetValue(s_Strip, out var a)) m_Strip = a;
            if (customStyle.TryGetValue(s_Cut, out var c)) m_Cut = Mathf.Max(0, c);
            MarkDirtyRepaint();
        }

        void Draw(MeshGenerationContext ctx)
        {
            float w = layout.width, h = layout.height;
            if (w < 2f || h < 2f || float.IsNaN(w) || float.IsNaN(h)) return;
            float c = Mathf.Min(m_Cut, Mathf.Min(w, h) * 0.5f);
            var p = ctx.painter2D;

            p.BeginPath();
            p.MoveTo(new Vector2(c, 0.5f));
            p.LineTo(new Vector2(w - 0.5f, 0.5f));
            p.LineTo(new Vector2(w - 0.5f, h - c));
            p.LineTo(new Vector2(w - c, h - 0.5f));
            p.LineTo(new Vector2(0.5f, h - 0.5f));
            p.LineTo(new Vector2(0.5f, c));
            p.ClosePath();
            p.fillColor = m_Fill;
            p.Fill();
            p.strokeColor = m_Stroke;
            p.lineWidth = 1f;
            p.Stroke();

            if (ShowStrip)
            {
                p.BeginPath();
                p.MoveTo(new Vector2(c, 1f));
                p.LineTo(new Vector2(c + 36f, 1f));
                p.strokeColor = m_Strip;
                p.lineWidth = 2f;
                p.Stroke();
            }
        }
    }
}
