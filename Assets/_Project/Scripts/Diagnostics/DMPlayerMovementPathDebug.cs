using Project.Core;
using Project.Player;
using UnityEngine;

namespace Project.Diagnostics
{
    /// <summary>
    /// Play-mode gold/yellow movement path for the player. Default off via <see cref="drawPath"/>.
    /// Add to Player_v7 Variant (not a Player_v7 capsule retune). History buffer is for future
    /// path analysis — not shipped gameplay yet.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Dark Matter/Diagnostics/Player Movement Path Debug")]
    public sealed class DMPlayerMovementPathDebug : MonoBehaviour
    {
        private static readonly Color PathColor = new Color(0.831f, 0.627f, 0.090f, 0.95f);

        [SerializeField] private bool drawPath;
        [SerializeField] private float minRecordDistance = 0.12f;
        [SerializeField] private float sampleInterval = 0.2f;
        [SerializeField] private float drawYLift = 0.08f;
        [SerializeField] private float debugLineDuration = 0.35f;
        [SerializeField] private int historyCapacity = DMMovementPathHistoryBuffer.DefaultCapacity;

        private DMMovementPathHistoryBuffer history;

        public bool DrawPath
        {
            get => drawPath;
            set => drawPath = value;
        }

        private void Awake()
        {
            history = new DMMovementPathHistoryBuffer(historyCapacity);
        }

        public static DMPlayerMovementPathDebug EnsureOnPlayer(bool enableDraw = false)
        {
            GameObject player = PlayerLocator.FindPlayerObject();
            if (player == null)
                return null;

            DMPlayerMovementPathDebug existing = player.GetComponent<DMPlayerMovementPathDebug>();
            if (existing != null)
            {
                if (enableDraw)
                    existing.drawPath = true;
                return existing;
            }

            DMPlayerMovementPathDebug added = player.AddComponent<DMPlayerMovementPathDebug>();
            if (enableDraw)
                added.drawPath = true;
            return added;
        }

        private void LateUpdate()
        {
            if (!Application.isPlaying || !drawPath)
            {
                if (history.Count > 0)
                    history.Clear();
                return;
            }

            history.Record(transform.position, minRecordDistance, sampleInterval);
            history.DrawDebugLines(PathColor, drawYLift, debugLineDuration);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!Application.isPlaying || !drawPath || history == null)
                return;

            history.DrawGizmos(PathColor, drawYLift);
        }
#endif
    }
}
