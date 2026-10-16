using UnityEngine;
using UnityEngine.UIElements;

namespace Project.UI
{
    /// <summary>
    /// Full-screen scoped ADS vignette — dims the viewport outside the inner ring outer edge (profile-driven aperture).
    /// </summary>
    public sealed class DMScopeCenterMaskElement : VisualElement
    {
        public float ApertureDiameterPx = 48f;
        public Color MaskColor = new Color(0f, 0f, 0f, 1f);
        public float MaskAlpha = 0.32f;
        public float EdgeSoftnessPx = 12f;
        public Vector2 Offset;

        public DMScopeCenterMaskElement()
        {
            name = "optics-scope-center-mask";
            pickingMode = PickingMode.Ignore;
            style.position = Position.Absolute;
            style.left = 0;
            style.right = 0;
            style.top = 0;
            style.bottom = 0;
            generateVisualContent += Draw;
        }

        public void ApplySettings(float apertureOuterDiameterPx, Color color, float alpha, float edgeSoftnessPx, Vector2 offset)
        {
            ApertureDiameterPx = apertureOuterDiameterPx;
            MaskColor = color;
            MaskAlpha = alpha;
            EdgeSoftnessPx = edgeSoftnessPx;
            Offset = offset;
            RefreshLayout();
        }

        public void RefreshLayout() => MarkDirtyRepaint();

        private void Draw(MeshGenerationContext ctx)
        {
            if (MaskAlpha <= 0.001f || ApertureDiameterPx < 4f)
                return;

            Rect r = contentRect;
            if (r.width < 4f || r.height < 4f)
                return;

            Vector2 c = r.center + Offset;
            float innerR = Mathf.Max(4f, ApertureDiameterPx * 0.5f);
            float soft = Mathf.Clamp(EdgeSoftnessPx, 0.5f, innerR * 0.95f);
            float coverR = Mathf.Sqrt(r.width * r.width + r.height * r.height) * 0.5f + 2f;

            Color fill = MaskColor;
            Painter2D g = ctx.painter2D;
            const int rings = 32;
            for (int i = 0; i < rings; i++)
            {
                float t0 = i / (float)rings;
                float t1 = (i + 1) / (float)rings;
                float rOuter = innerR + (coverR - innerR) * t1;
                float rInner = innerR + (coverR - innerR) * t0;
                if (rOuter <= innerR + 0.25f)
                    continue;
                if (rInner < innerR + 0.25f)
                    rInner = innerR + 0.25f;

                float mid = (rInner + rOuter) * 0.5f;
                float distFromAperture = mid - innerR;
                float a = MaskAlpha * Mathf.SmoothStep(0f, 1f, distFromAperture / Mathf.Max(0.001f, soft));
                if (a <= 0.001f)
                    continue;

                Color ringColor = fill;
                ringColor.a = a;
                g.fillColor = ringColor;
                g.BeginPath();
                g.Arc(c, rOuter, 0f, 360f);
                g.Arc(c, rInner, 360f, 0f);
                g.ClosePath();
                g.Fill();
            }
        }
    }
}
