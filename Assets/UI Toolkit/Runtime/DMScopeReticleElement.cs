using Project.Combat;
using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// 2026-10-13 (h): procedural scope / binocular art (Painter2D): thick lens ring, fine crosshair with centre gap,
    /// mil-dots and a centre dot. Colours / widths from DM_ScopeProfile. Replaces the old sprite frame.
    /// </summary>
    public sealed class DMScopeReticleElement : VisualElement
    {
        public float RadiusFraction = 0.38f;
        public bool DrawCrosshair = true;
        public Vector2 Offset;

        public DMScopeReticleElement()
        {
            name = "optics-scope-reticle";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0; style.right = 0; style.top = 0; style.bottom = 0;
            generateVisualContent += Draw;
        }

        public void Refresh() => MarkDirtyRepaint();

        private void Draw(MeshGenerationContext ctx)
        {
            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f)
                return;
            DM_ScopeProfile p = DM_ScopeProfile.Live;
            Painter2D g = ctx.painter2D;
            Vector2 c = r.center + Offset;
            float radius = Mathf.Max(8f, RadiusFraction * r.height);

            // Lens ring (dark bezel just inside the mask edge) + thin inner rim.
            g.strokeColor = p.ringColor;
            g.lineWidth = Mathf.Max(2f, radius * 0.045f);
            g.BeginPath();
            g.Arc(c, radius - g.lineWidth * 0.5f, 0f, 360f);
            g.Stroke();
            g.strokeColor = new Color(p.reticleColor.r, p.reticleColor.g, p.reticleColor.b, p.reticleColor.a * 0.5f);
            g.lineWidth = 1f;
            g.BeginPath();
            g.Arc(c, radius * 0.93f, 0f, 360f);
            g.Stroke();

            if (!DrawCrosshair)
                return;

            float lw = p.reticleLineWidth;
            float gap = p.reticleCenterGap;
            float arm = radius * 0.93f;
            g.strokeColor = p.reticleColor;
            // Thick outer posts + fine inner wires.
            Line(g, c + new Vector2(-arm, 0), c + new Vector2(-arm * 0.55f, 0), lw * 3f);
            Line(g, c + new Vector2(arm * 0.55f, 0), c + new Vector2(arm, 0), lw * 3f);
            Line(g, c + new Vector2(0, arm * 0.55f), c + new Vector2(0, arm), lw * 3f);
            Line(g, c + new Vector2(-arm * 0.55f, 0), c + new Vector2(-gap, 0), lw);
            Line(g, c + new Vector2(gap, 0), c + new Vector2(arm * 0.55f, 0), lw);
            Line(g, c + new Vector2(0, -arm), c + new Vector2(0, -gap), lw);
            Line(g, c + new Vector2(0, gap), c + new Vector2(0, arm * 0.55f), lw);

            int dots = Mathf.Max(0, p.milDotsPerArm);
            if (dots > 0)
            {
                float step = (arm * 0.55f - gap) / (dots + 1);
                g.fillColor = p.reticleColor;
                for (int i = 1; i <= dots; i++)
                {
                    float d = gap + step * i;
                    Dot(g, c + new Vector2(d, 0), lw * 1.4f);
                    Dot(g, c + new Vector2(-d, 0), lw * 1.4f);
                    Dot(g, c + new Vector2(0, d), lw * 1.4f);
                    Dot(g, c + new Vector2(0, -d), lw * 1.4f);
                }
            }
            g.fillColor = new Color(0.85f, 0.1f, 0.08f, 0.95f);
            Dot(g, c, Mathf.Max(1.5f, lw));
        }

        private static void Line(Painter2D g, Vector2 a, Vector2 b, float w)
        {
            g.lineWidth = w;
            g.BeginPath();
            g.MoveTo(a);
            g.LineTo(b);
            g.Stroke();
        }

        private static void Dot(Painter2D g, Vector2 c, float r)
        {
            g.BeginPath();
            g.Arc(c, r, 0f, 360f);
            g.Fill();
        }
    }
}
