using UnityEngine;

namespace Project.Features.Locomotion
{
    /// <summary>2026-10-20y: restores the fresh animator pose before Invector aim IK / vHeadTrack (order 0) when the
    /// Animator skipped evaluation this frame, so aim IK and body weight never stack on last frame's output. Added at runtime.</summary>
    [DisallowMultipleComponent]
    [DefaultExecutionOrder(-500)]
    public sealed class DMPlayerBodyWeightPreIK : MonoBehaviour
    {
        [System.NonSerialized] public DMPlayerBodyWeight Owner;
        private void LateUpdate() { if (Owner != null) Owner.PreIK(); }
        private void FixedUpdate() { if (Owner != null) Owner.MarkStep(); } // 2026-10-24h
    }
}