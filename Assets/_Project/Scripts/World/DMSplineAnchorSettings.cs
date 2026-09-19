using System;
using UnityEngine;

namespace Project.World
{
    [Serializable]
    public class DMSplineAnchorSettings
    {
        public Color gizmoColor = new Color(0.92f, 0.55f, 0.18f, 1f);
        public bool snapToGround = true;
        public bool overrideUpAxis;
        public DMSplinePrefabUpAxis upAxis = DMSplinePrefabUpAxis.PositiveY;
        [Tooltip("Extra offset along this anchor's up axis (only used until wire attach is pinned).")]
        public float lineAttachOffset = 0f;
        public bool useCustomAttachLocalOffset;
        [Tooltip("Wire attach point in anchor local space (pinned to this object).")]
        public Vector3 attachLocalOffset;
        [Tooltip("When set, wire height is never recalculated from mesh bounds or global sliders.")]
        public bool wireAttachPinned;
    }
}
