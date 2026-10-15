using System.Collections.Generic;
using Project.AI;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Project.Diagnostics
{
    /// <summary>
    /// Play-mode movement debug for one enemy.
    /// <b>History</b> (solid gold): past positions via <see cref="DMMovementPathHistoryBuffer"/> — future path learning hook.
    /// <b>Virtual</b> (dashed yellow): in-memory intent polyline from <see cref="EnemyAiController.FillVirtualSteerPolyline"/> — not spawned geometry.
    /// Draw when: brain <see cref="DM_EnemyBrainProfile.enableMovementPathDebug"/>, local <see cref="drawPath"/>, or enemy selected in Hierarchy (Scene click).
    /// Requires Play mode + Gizmos on in Scene/Game view. Does not move the enemy or change AI state.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Diagnostics/Enemy Movement Path Debug")]
    public sealed class DMEnemyMovementPathDebug : MonoBehaviour
    {
        private static readonly Color HistoryColor = new Color(0.831f, 0.627f, 0.090f, 0.95f);
        private static readonly Color VirtualSteerColor = new Color(1f, 0.92f, 0.22f, 0.9f);

        [SerializeField] private bool useBrainProfileToggle = true;
        [SerializeField] private bool drawWhenSelected = true;
        [SerializeField] private bool drawPath;
        [SerializeField] private float minRecordDistance = 0.12f;
        [SerializeField] private float sampleInterval = 0.2f;
        [SerializeField] private float drawYLift = 0.08f;
        [SerializeField] private float debugLineDuration = 0.35f;
        [SerializeField] private int historyCapacity = DMMovementPathHistoryBuffer.DefaultCapacity;

        private DMMovementPathHistoryBuffer history;
        private readonly List<Vector3> virtualPolylineScratch = new List<Vector3>(4);
        private EnemyAiController ai;
        private bool wasDrawing;

        private void Awake()
        {
            history = new DMMovementPathHistoryBuffer(historyCapacity);
            ai = GetComponent<EnemyAiController>();
        }

        private void LateUpdate()
        {
            bool draw = ShouldDraw();
            if (!draw)
            {
                if (wasDrawing)
                    history.Clear();
                wasDrawing = false;
                return;
            }

            wasDrawing = true;
            history.Record(transform.position, minRecordDistance, sampleInterval);
            history.DrawDebugLines(HistoryColor, drawYLift, debugLineDuration);

            if (ShouldDrawVirtualSteer() && ai != null && ai.FillVirtualSteerPolyline(virtualPolylineScratch) >= 2)
            {
                DMMovementPathHistoryBuffer.DrawPolylineDashed(
                    virtualPolylineScratch,
                    VirtualSteerColor,
                    drawYLift + 0.04f,
                    debugLineDuration);
            }
        }

        private bool ShouldDraw()
        {
            if (!Application.isPlaying)
                return false;

            // The profile is the master switch.  Keeping this gate ahead of selection
            // prevents a selected enemy from rendering debug paths when the feature is off.
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            if (useBrainProfileToggle && (profile == null || !profile.enableMovementPathDebug))
                return false;

            if (drawPath)
                return true;

            if (drawWhenSelected && IsSelectedInHierarchy(transform))
                return true;

            if (useBrainProfileToggle)
                return profile != null && profile.enableMovementPathDebug;

            return false;
        }

        private bool ShouldDrawVirtualSteer()
        {
            DM_EnemyBrainProfile profile = DM_EnemyBrainProfile.Live;
            return profile != null && profile.drawVirtualSteerPath;
        }

        private static bool IsSelectedInHierarchy(Transform t)
        {
#if UNITY_EDITOR
            GameObject active = Selection.activeGameObject;
            if (active == null || t == null)
                return false;

            return active.transform == t || active.transform.IsChildOf(t);
#else
            return false;
#endif
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !ShouldDraw() || history == null)
                return;

            history.DrawGizmos(HistoryColor, drawYLift);

            if (ShouldDrawVirtualSteer() && ai != null && ai.FillVirtualSteerPolyline(virtualPolylineScratch) >= 2)
            {
                DMMovementPathHistoryBuffer.DrawPolylineDashedGizmos(
                    virtualPolylineScratch,
                    VirtualSteerColor,
                    drawYLift + 0.04f);
            }
        }

        private void OnDrawGizmosSelected()
        {
            OnDrawGizmos();
        }
#endif
    }
}
