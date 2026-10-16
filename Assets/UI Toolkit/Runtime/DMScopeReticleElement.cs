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
        public enum ReticleStyle
        {
            Binocular,
            /// <summary>Full-width horizontal wire, vertical post from bottom to center, red dot, optional hex grid.</summary>
            RifleClassic,
            /// <summary>Small ring + center reticle only (outer mil-dot is a separate sprite).</summary>
            ScopedInnerRing
        }

        public float RadiusFraction = 0.38f;
        public float PresentationScale = 1f;
        public bool DrawCrosshair = true;
        public ReticleStyle Style = ReticleStyle.Binocular;
        public bool DrawHexGrid = true;
        public Vector2 Offset;
        public float RingOuterDiameterPx = 48f;
        public bool UseWeaponScopedColors;
        public bool WeaponIsRifle;

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
            float scale = Mathf.Clamp(PresentationScale, 0.1f, 1.5f);
            float radius = Mathf.Max(8f, RadiusFraction * r.height * scale);

            Color ringCol = UseWeaponScopedColors ? p.ResolveScopedRingColor(WeaponIsRifle) : p.ringColor;
            Color retCol = UseWeaponScopedColors ? p.ResolveScopedReticleColor(WeaponIsRifle) : p.reticleColor;

            float lw = p.reticleLineWidth * scale;
            float arm = radius * 0.96f;

            if (Style == ReticleStyle.ScopedInnerRing)
            {
                float outerR = Mathf.Max(6f, RingOuterDiameterPx * 0.5f * scale);
                float ringW = Mathf.Max(2f, lw * 1.25f);
                g.strokeColor = ringCol;
                g.lineWidth = ringW;
                g.BeginPath();
                g.Arc(c, outerR - ringW * 0.5f, 0f, 360f);
                g.Stroke();

                if (!DrawCrosshair)
                    return;

                float wire = Mathf.Max(1.2f, lw);
                float innerArm = outerR * 0.42f;
                float innerGap = Mathf.Max(1.5f, p.reticleCenterGap * scale * 0.2f);
                g.strokeColor = retCol;
                Line(g, c + new Vector2(-innerArm, 0f), c + new Vector2(-innerGap, 0f), wire);
                Line(g, c + new Vector2(innerGap, 0f), c + new Vector2(innerArm, 0f), wire);
                Line(g, c + new Vector2(0f, -innerArm), c + new Vector2(0f, -innerGap), wire);
                Line(g, c + new Vector2(0f, innerGap), c + new Vector2(0f, innerArm), wire);
                g.fillColor = retCol;
                Dot(g, c, Mathf.Max(1.5f, wire * 0.85f));
                return;
            }

            if (Style == ReticleStyle.RifleClassic)
            {
                if (DrawHexGrid)
                    DrawHexGridInside(g, c, radius * 0.92f);

                g.strokeColor = ringCol;
                g.lineWidth = Mathf.Max(3f, radius * 0.055f);
                g.BeginPath();
                g.Arc(c, radius - g.lineWidth * 0.5f, 0f, 360f);
                g.Stroke();

                if (!DrawCrosshair)
                    return;

                float wire = Mathf.Max(1.4f, lw * 1.35f);
                float innerR = Mathf.Max(4f, radius - g.lineWidth * 0.65f);
                float centerGap = Mathf.Max(2f, wire * 1.2f);

                g.strokeColor = retCol;
                Line(g, c + new Vector2(-innerR, 0f), c + new Vector2(innerR, 0f), wire);
                Line(g, c + new Vector2(0f, innerR), c + new Vector2(0f, centerGap), wire);

                g.fillColor = retCol;
                Dot(g, c, Mathf.Max(2f, wire * 1.1f));
                return;
            }

            // Lens ring (dark bezel just inside the mask edge) + thin inner rim.
            g.strokeColor = ringCol;
            g.lineWidth = Mathf.Max(2f, radius * 0.045f);
            g.BeginPath();
            g.Arc(c, radius - g.lineWidth * 0.5f, 0f, 360f);
            g.Stroke();
            g.strokeColor = new Color(retCol.r, retCol.g, retCol.b, retCol.a * 0.5f);
            g.lineWidth = 1f;
            g.BeginPath();
            g.Arc(c, radius * 0.93f, 0f, 360f);
            g.Stroke();

            if (!DrawCrosshair)
                return;

            float gap = p.reticleCenterGap * scale;
            g.strokeColor = retCol;
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
                g.fillColor = retCol;
                for (int i = 1; i <= dots; i++)
                {
                    float d = gap + step * i;
                    Dot(g, c + new Vector2(d, 0), lw * 1.4f);
                    Dot(g, c + new Vector2(-d, 0), lw * 1.4f);
                    Dot(g, c + new Vector2(0, d), lw * 1.4f);
                    Dot(g, c + new Vector2(0, -d), lw * 1.4f);
                }
            }
            g.fillColor = retCol;
            Dot(g, c, Mathf.Max(1.5f, lw));
        }

        private static void DrawHexGridInside(Painter2D g, Vector2 center, float radius)
        {
            float hexR = radius * 0.11f;
            if (hexR < 6f)
                return;

            float w = hexR * 2f;
            float h = hexR * 1.732f;
            g.strokeColor = new Color(0.35f, 0.72f, 0.95f, 0.22f);
            g.lineWidth = 1f;

            int cols = Mathf.CeilToInt(radius * 2f / (w * 0.75f)) + 2;
            int rows = Mathf.CeilToInt(radius * 2f / h) + 2;
            for (int row = -rows; row <= rows; row++)
            {
                for (int col = -cols; col <= cols; col++)
                {
                    float ox = col * w * 0.75f;
                    float oy = row * h + (col & 1) * (h * 0.5f);
                    Vector2 hc = center + new Vector2(ox, oy);
                    if ((hc - center).sqrMagnitude > radius * radius)
                        continue;
                    StrokeHex(g, hc, hexR);
                }
            }
        }

        private static void StrokeHex(Painter2D g, Vector2 c, float r)
        {
            g.BeginPath();
            for (int i = 0; i < 6; i++)
            {
                float a = (60f * i - 30f) * Mathf.Deg2Rad;
                Vector2 p = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (i == 0)
                    g.MoveTo(p);
                else
                    g.LineTo(p);
            }
            g.ClosePath();
            g.Stroke();
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
