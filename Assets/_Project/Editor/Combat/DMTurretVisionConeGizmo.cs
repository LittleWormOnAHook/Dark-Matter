#if UNITY_EDITOR
using Project.Combat.Turrets;
using UnityEditor;
using UnityEngine;

namespace Project.EditorTools.Combat
{
    /// <summary>Edit-mode Scene view cones for <see cref="DMTurretTrackingAuthority"/> heading/elevation limits.</summary>
    public static class DMTurretVisionConeGizmo
    {
        // Dark Matter UI palette (heading = magenta/fuchsia, elevation = gold / green)
        static readonly Color HeadingMax = new Color(0.753f, 0.18f, 0.478f, 0.95f);
        static readonly Color HeadingMin = new Color(0.561f, 0.118f, 0.369f, 0.95f);
        static readonly Color ElevationMax = new Color(0.831f, 0.627f, 0.09f, 0.95f);
        static readonly Color ElevationMin = new Color(0.35f, 0.82f, 0.48f, 0.95f);
        static readonly Color CurrentAim = new Color(0.929f, 0.914f, 0.894f, 0.35f);
        static readonly Color ScanRange = new Color(0.2f, 0.6f, 1f, 0.12f);
        static readonly Color TrackRange = new Color(1f, 0.85f, 0.2f, 0.12f);
        static readonly Color FireRange = new Color(1f, 0.2f, 0.2f, 0.12f);

        const int ArcSegments = 24;

        // NonSelected draws all turrets in Scene view (Edit mode only). Never draw during Play — cost scales with turret count.
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected | GizmoType.InSelectionHierarchy)]
        static void DrawVisionCones(DMTurretRuntime runtime, GizmoType gizmoType)
        {
            if (Application.isPlaying)
                return;
            if (runtime == null || !runtime.EditorDrawVisionCones)
                return;

            if (!runtime.TryResolveAimPivot(out Transform swivel, out Transform barrelMount))
                return;

            Vector2 heading = runtime.ResolveEffectiveHeadingLimit();
            Vector2 elevation = runtime.ResolveEffectiveElevationLimit();
            float midHeading = (heading.x + heading.y) * 0.5f;
            float midElevation = (elevation.x + elevation.y) * 0.5f;

            // F3D TrackTarget / DebugDraw: aim lines originate at mount (barrel), not swivel socket.
            Vector3 pivot = barrelMount != null ? barrelMount.position : swivel.position;
            float radius = Mathf.Clamp(runtime.TrackRange > 0.1f ? runtime.TrackRange : 12f, 4f, 48f);

            float curH = SignedEuler(swivel.localEulerAngles.y);
            float curE = barrelMount != null ? SignedEuler(barrelMount.localEulerAngles.x) : 0f;
            Vector3 currentFwd = DMTurretRuntime.BarrelForwardAtLocalLimits(swivel, barrelMount, curH, curE);

            Gizmos.color = CurrentAim;
            Gizmos.DrawLine(pivot, pivot + currentFwd.normalized * radius);

            DrawBoundaryRay(pivot, radius, swivel, barrelMount, heading.x, midElevation, HeadingMin);
            DrawBoundaryRay(pivot, radius, swivel, barrelMount, heading.y, midElevation, HeadingMax);
            DrawBoundaryRay(pivot, radius, swivel, barrelMount, midHeading, elevation.x, ElevationMin);
            DrawBoundaryRay(pivot, radius, swivel, barrelMount, midHeading, elevation.y, ElevationMax);

            Color corner = Dim(HeadingMax, 0.55f);
            DrawBoundaryRay(pivot, radius * 0.85f, swivel, barrelMount, heading.x, elevation.x, corner);
            DrawBoundaryRay(pivot, radius * 0.85f, swivel, barrelMount, heading.y, elevation.x, corner);
            DrawBoundaryRay(pivot, radius * 0.85f, swivel, barrelMount, heading.x, elevation.y, corner);
            DrawBoundaryRay(pivot, radius * 0.85f, swivel, barrelMount, heading.y, elevation.y, corner);

            Vector3 hMinDir = DMTurretRuntime.BarrelForwardAtLocalLimits(swivel, barrelMount, heading.x, midElevation);
            float headingSpan = heading.y - heading.x;
            if (headingSpan > 359.9f)
                headingSpan = 360f;
            Vector3 yawAxis = DMTurretRuntime.SwivelWorldRotationAtLocalHeading(swivel, midHeading) * Vector3.up;
            DrawArc(pivot, hMinDir, yawAxis, headingSpan, HeadingMax, radius);

            Vector3 eMinDir = DMTurretRuntime.BarrelForwardAtLocalLimits(swivel, barrelMount, midHeading, elevation.x);
            Vector3 pitchAxis = DMTurretRuntime.SwivelWorldRotationAtLocalHeading(swivel, midHeading) * Vector3.right;
            float elevSpan = elevation.y - elevation.x;
            DrawArc(pivot, eMinDir, pitchAxis, elevSpan, ElevationMax, radius);

            DrawRangeSpheres(runtime, runtime.transform.position);
        }

        static void DrawBoundaryRay(
            Vector3 pivot,
            float radius,
            Transform swivel,
            Transform barrelMount,
            float headingY,
            float elevationX,
            Color color)
        {
            Vector3 dir = DMTurretRuntime.BarrelForwardAtLocalLimits(swivel, barrelMount, headingY, elevationX);
            if (dir.sqrMagnitude < 0.0001f)
                return;
            Gizmos.color = color;
            Gizmos.DrawLine(pivot, pivot + dir.normalized * radius);
        }

        static void DrawArc(Vector3 pivot, Vector3 fromDir, Vector3 axis, float angleDeg, Color color, float radius)
        {
            if (Mathf.Abs(angleDeg) < 0.05f || fromDir.sqrMagnitude < 0.0001f || axis.sqrMagnitude < 0.0001f)
                return;

            Gizmos.color = color;
            axis.Normalize();
            Vector3 start = fromDir.normalized;
            Vector3 prev = pivot + start * radius;
            for (int i = 1; i <= ArcSegments; i++)
            {
                float t = i / (float)ArcSegments;
                Vector3 dir = Quaternion.AngleAxis(angleDeg * t, axis) * start;
                Vector3 next = pivot + dir * radius;
                Gizmos.DrawLine(prev, next);
                prev = next;
            }
        }

        static void DrawRangeSpheres(DMTurretRuntime runtime, Vector3 center)
        {
            Gizmos.color = ScanRange;
            Gizmos.DrawWireSphere(center, runtime.ScanRange);
            Gizmos.color = TrackRange;
            Gizmos.DrawWireSphere(center, runtime.TrackRange);
            Gizmos.color = FireRange;
            Gizmos.DrawWireSphere(center, runtime.FireRange);
        }

        static float SignedEuler(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }

        static Color Dim(Color c, float alphaScale) =>
            new Color(c.r, c.g, c.b, c.a * alphaScale);
    }
}
#endif
