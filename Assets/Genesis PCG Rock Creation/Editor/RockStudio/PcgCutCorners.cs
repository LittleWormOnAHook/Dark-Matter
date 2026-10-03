#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UIElements;

namespace GenesisPCG.RockCreation.Editor
{
    /// <summary>
    /// Optional chamfered ("cut corner") background for studio elements, driven only by USS custom properties so a host
    /// theme can switch it on without code: --pcg-cut (px, 0 = off), --pcg-cut-fill, --pcg-cut-line, --pcg-cut-line-width.
    /// Top-left and bottom-right corners are cut. With the default stylesheet it is off (plain backgrounds).
    /// </summary>
    public static class PcgCutCorners
    {
        private static readonly CustomStyleProperty<float> s_cut = new CustomStyleProperty<float>("--pcg-cut");
        private static readonly CustomStyleProperty<Color> s_fill = new CustomStyleProperty<Color>("--pcg-cut-fill");
        private static readonly CustomStyleProperty<Color> s_line = new CustomStyleProperty<Color>("--pcg-cut-line");
        private static readonly CustomStyleProperty<float> s_lineWidth = new CustomStyleProperty<float>("--pcg-cut-line-width");

        private sealed class State
        {
            public float cut, lineWidth;
            public Color fill = Color.clear, line = Color.clear;
        }

        /// <summary>Sets the text of a button that went through <see cref="Apply{T}"/> (its text lives in a child label).</summary>
        public static void SetText(TextElement te, string text)
        {
            if (te == null) return;
            var l = te.Q<Label>(className: "pcg-cut__label");
            if (l != null) l.text = text; else te.text = text;
        }

        public static T Apply<T>(T ve) where T : VisualElement
        {
            if (ve == null || ve.ClassListContains("pcg-cut")) return ve;
            ve.AddToClassList("pcg-cut");
            var st = new State();
            ve.RegisterCallback<CustomStyleResolvedEvent>(e =>
            {
                ICustomStyle cs = e.customStyle;
                st.cut = cs.TryGetValue(s_cut, out float c) ? c : 0f;
                st.fill = cs.TryGetValue(s_fill, out Color f) ? f : Color.clear;
                st.line = cs.TryGetValue(s_line, out Color l) ? l : Color.clear;
                st.lineWidth = cs.TryGetValue(s_lineWidth, out float w) ? w : 0f;
                ve.MarkDirtyRepaint();
            });
            ve.generateVisualContent += ctx => Draw(ctx, ve, st);
            // A TextElement (Button) draws its own text in its content, BEFORE this fill: move the text into a child label
            // so it renders on top of the cut-corner background (children draw after their parent).
            if (ve is TextElement te && !(ve is Label) && !string.IsNullOrEmpty(te.text))
            {
                var label = new Label(te.text) { pickingMode = PickingMode.Ignore };
                label.AddToClassList("pcg-cut__label");
                label.style.flexGrow = 1f;
                label.style.marginLeft = 0; label.style.marginRight = 0; label.style.marginTop = 0; label.style.marginBottom = 0;
                label.style.paddingLeft = 0; label.style.paddingRight = 0; label.style.paddingTop = 0; label.style.paddingBottom = 0;
                te.text = string.Empty;
                ve.Add(label);
            }
            return ve;
        }

        private static void Draw(MeshGenerationContext ctx, VisualElement ve, State st)
        {
            if (st.cut <= 0f) return;
            float w = ve.layout.width, h = ve.layout.height;
            if (!(w > 1f) || !(h > 1f)) return;
            float c = Mathf.Min(st.cut, Mathf.Min(w, h) * 0.45f);
            Painter2D p = ctx.painter2D;
            if (st.fill.a > 0f)
            {
                p.fillColor = st.fill;
                Path(p, 0f, w, h, c);
                p.Fill();
            }
            if (st.lineWidth > 0f && st.line.a > 0f)
            {
                float i = st.lineWidth * 0.5f;
                p.strokeColor = st.line;
                p.lineWidth = st.lineWidth;
                p.lineJoin = LineJoin.Miter;
                Path(p, i, w, h, c);
                p.Stroke();
            }
        }

        private static void Path(Painter2D p, float i, float w, float h, float c)
        {
            p.BeginPath();
            p.MoveTo(new Vector2(c + i * 0.4f, i));
            p.LineTo(new Vector2(w - i, i));
            p.LineTo(new Vector2(w - i, h - c - i * 0.4f));
            p.LineTo(new Vector2(w - c - i * 0.4f, h - i));
            p.LineTo(new Vector2(i, h - i));
            p.LineTo(new Vector2(i, c + i * 0.4f));
            p.ClosePath();
        }
    }
}
#endif
