using UnityEngine;
using UnityEngine.UI;

namespace Project.UI
{
    /// <summary>
    /// Top-half circular health arc (180° over the portrait).
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public class HalfCircleHealthBarGraphic : MaskableGraphic
    {
        [SerializeField] private float thickness = 4f;
        [SerializeField] private int segments = 28;
        [SerializeField] private float fillAmount = 1f;

        public float Thickness
        {
            get => thickness;
            set
            {
                if (Mathf.Approximately(thickness, value))
                    return;

                thickness = value;
                SetVerticesDirty();
            }
        }

        public float FillAmount
        {
            get => fillAmount;
            set
            {
                value = Mathf.Clamp01(value);
                if (Mathf.Approximately(fillAmount, value))
                    return;

                fillAmount = value;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();

            Rect rect = rectTransform.rect;
            float outerRadius = Mathf.Min(rect.width, rect.height) * 0.5f;
            if (outerRadius <= 0.01f || thickness <= 0.01f || fillAmount <= 0.0001f)
                return;

            float innerRadius = Mathf.Max(0.01f, outerRadius - thickness);
            float span = Mathf.PI * fillAmount;
            float startAngle = Mathf.PI;
            int stepCount = Mathf.Max(2, Mathf.CeilToInt(segments * fillAmount));

            UIVertex vertex = UIVertex.simpleVert;
            vertex.color = color;

            for (int i = 0; i < stepCount; i++)
            {
                float t0 = i / (float)stepCount;
                float t1 = (i + 1) / (float)stepCount;
                float angle0 = startAngle - span * t0;
                float angle1 = startAngle - span * t1;

                Vector2 outer0 = AngleToPoint(angle0, outerRadius);
                Vector2 outer1 = AngleToPoint(angle1, outerRadius);
                Vector2 inner0 = AngleToPoint(angle0, innerRadius);
                Vector2 inner1 = AngleToPoint(angle1, innerRadius);

                int baseIndex = vh.currentVertCount;
                vertex.position = outer0;
                vh.AddVert(vertex);
                vertex.position = outer1;
                vh.AddVert(vertex);
                vertex.position = inner1;
                vh.AddVert(vertex);
                vertex.position = inner0;
                vh.AddVert(vertex);

                vh.AddTriangle(baseIndex, baseIndex + 1, baseIndex + 2);
                vh.AddTriangle(baseIndex, baseIndex + 2, baseIndex + 3);
            }
        }

        private static Vector2 AngleToPoint(float angleRadians, float radius)
        {
            return new Vector2(Mathf.Cos(angleRadians) * radius, Mathf.Sin(angleRadians) * radius);
        }
    }
}
