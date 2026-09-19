#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools
{
    /// <summary>Edit-mode only low-poly anchor / wire-hook preview (not shown in Play mode builds).</summary>
    internal static class DMSplineCreatorEditPreviewDraw
    {
        private const int WireSegments = 6;

        public static void DrawPivotSphere(Vector3 center, float radius, Color color)
        {
            DrawLowPolySphere(center, radius, color, 0.22f);
        }

        public static void DrawWireHookSphere(Vector3 center, float radius, Color color)
        {
            DrawLowPolySphere(center, radius, color, 0.35f);
        }

        private static void DrawLowPolySphere(Vector3 center, float radius, Color color, float fillAlpha)
        {
            if (radius <= 0.0001f)
                return;

            Color wire = new Color(color.r, color.g, color.b, 0.95f);
            Color fill = new Color(color.r, color.g, color.b, fillAlpha);

            Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
            Handles.color = fill;
            Handles.SphereHandleCap(0, center, Quaternion.identity, radius * 2f, EventType.Repaint);

            Handles.color = wire;
            Handles.DrawWireDisc(center, Vector3.up, radius, WireSegments);
            Handles.DrawWireDisc(center, Vector3.right, radius, WireSegments);
            Handles.DrawWireDisc(center, Vector3.forward, radius, WireSegments);

            Vector3 top = center + Vector3.up * radius;
            Vector3 bottom = center - Vector3.up * radius;
            Vector3 left = center - Vector3.right * radius;
            Vector3 right = center + Vector3.right * radius;
            Vector3 fwd = center + Vector3.forward * radius;
            Vector3 back = center - Vector3.forward * radius;
            Handles.DrawLine(top, left);
            Handles.DrawLine(top, right);
            Handles.DrawLine(top, fwd);
            Handles.DrawLine(top, back);
            Handles.DrawLine(bottom, left);
            Handles.DrawLine(bottom, right);
            Handles.DrawLine(bottom, fwd);
            Handles.DrawLine(bottom, back);
        }

        public static float RadiusHandle(Vector3 center, float radius, Color color)
        {
            Handles.color = color;
            EditorGUI.BeginChangeCheck();
            float next = Handles.RadiusHandle(Quaternion.identity, center, radius);
            return EditorGUI.EndChangeCheck() ? Mathf.Max(0.05f, next) : radius;
        }
    }
}
#endif
