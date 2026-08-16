using System.Collections;
using Project.Quests;
using UnityEngine;

namespace Project.PPT
{
    public sealed class PptNpcGestureController : MonoBehaviour
    {
        private const float RotateDurationSeconds = 0.4f;
        private const float PointHoldSeconds = 3f;
        private const float ShrugHoldSeconds = 2f;

        [Header("Animator")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;

        [Header("Upper Body Pointing")]
        [Tooltip("Animator state on the masked upper-body layer for seated NPCs (e.g. Gongo in a chair).")]
        [SerializeField] private string upperBodyPointStateName = "Point";
        [Tooltip("Animator layer name that uses an upper-body AvatarMask.")]
        [SerializeField] private string upperBodyLayerName = "Upper Body";
        [Tooltip("When enabled, overrides PptNpcProfile point gesture mode on this instance.")]
        [SerializeField] private bool usePointGestureModeOverride;
        [SerializeField] private PptPointGestureMode pointGestureModeOverride = PptPointGestureMode.UpperBodyOnly;

        private PptPointGestureMode pointGestureMode = PptPointGestureMode.FullBody;
        private string pointStateName = "Point";
        private string shrugStateName = "Shrug";
        private string idleStateName = "Idle";
        private bool rotateVisualTowardBearing = true;
        private float crossFadeSeconds = 0.15f;
        private int cachedUpperBodyLayerIndex = -1;
        private Coroutine restoreRoutine;
        private AnimatorUpdateMode cachedUpdateMode;
        private bool updateModeOverridden;

        public void Configure(PptNpcProfile profile, QuestGiverNpc questGiver = null)
        {
            if (profile != null)
            {
                pointGestureMode = profile.PointGestureMode;
                pointStateName = profile.PointStateName;
                shrugStateName = profile.ShrugStateName;
                idleStateName = profile.IdleStateName;
                rotateVisualTowardBearing = profile.RotateVisualTowardBearing;
                crossFadeSeconds = profile.GestureCrossFadeSeconds;

                if (!string.IsNullOrWhiteSpace(profile.UpperBodyPointStateName))
                    upperBodyPointStateName = profile.UpperBodyPointStateName;

                if (!string.IsNullOrWhiteSpace(profile.UpperBodyLayerName))
                    upperBodyLayerName = profile.UpperBodyLayerName;
            }

            if (questGiver != null && !string.IsNullOrWhiteSpace(questGiver.IdleAnimationStateName))
                idleStateName = questGiver.IdleAnimationStateName;

            if (usePointGestureModeOverride)
                pointGestureMode = pointGestureModeOverride;

            cachedUpperBodyLayerIndex = -1;
        }

        /// <summary>
        /// Turn toward the resolved aim / bearing (when enabled) and begin the point gesture.
        /// Yields once the point has started so callers can close UI, then restores idle in the background.
        /// </summary>
        public IEnumerator PlayPointUntilStarted(float bearingDegrees, Vector3 aimPosition = default)
        {
            if (restoreRoutine != null)
            {
                StopCoroutine(restoreRoutine);
                restoreRoutine = null;
            }

            EnsureAnimator();
            if (animator == null)
                yield break;

            int gestureLayer = ResolvePointLayerIndex(out string stateName);
            if (gestureLayer < 0 || string.IsNullOrWhiteSpace(stateName))
                yield break;

            BeginUnscaledAnimator();

            if (ShouldRotateVisual())
                yield return RotateVisualTowardAim(bearingDegrees, aimPosition);

            PlayGesture(stateName, gestureLayer);
            yield return new WaitForSecondsRealtime(Mathf.Max(0.05f, crossFadeSeconds + 0.1f));

            // Hold through menu close + post-point 1.5s wait before restoring idle.
            ScheduleRestore(PointHoldSeconds, gestureLayer);
        }

        public void PlayPoint(float bearingDegrees, Vector3 aimPosition = default)
        {
            StartCoroutine(PlayPointUntilStarted(bearingDegrees, aimPosition));
        }

        public void PlayShrug()
        {
            int gestureLayer = ResolveShrugLayerIndex(out string stateName);
            if (gestureLayer < 0 || string.IsNullOrWhiteSpace(stateName))
                return;

            BeginUnscaledAnimator();
            PlayGesture(stateName, gestureLayer);
            ScheduleRestore(ShrugHoldSeconds, gestureLayer);
        }

        private bool ShouldRotateVisual()
        {
            // Upper-body-only still turns the visual root toward the bearing (seated torso aim).
            return rotateVisualTowardBearing;
        }

        private IEnumerator RotateVisualTowardAim(float bearingDegrees, Vector3 aimPosition)
        {
            Transform root = ResolveRotationTarget();
            Quaternion target = ResolveFacingRotation(root.position, bearingDegrees, aimPosition);
            float rotateTimer = 0f;
            Quaternion start = root.rotation;
            while (rotateTimer < RotateDurationSeconds)
            {
                rotateTimer += Time.unscaledDeltaTime;
                root.rotation = Quaternion.Slerp(start, target, Mathf.Clamp01(rotateTimer / RotateDurationSeconds));
                yield return null;
            }

            root.rotation = target;
        }

        private Transform ResolveRotationTarget()
        {
            if (pointGestureMode == PptPointGestureMode.FullBody)
                return transform;

            if (visualRoot != null)
                return visualRoot;

            return transform;
        }

        private static Quaternion ResolveFacingRotation(Vector3 fromPosition, float bearingDegrees, Vector3 aimPosition)
        {
            if (aimPosition != Vector3.zero)
            {
                Vector3 flat = aimPosition - fromPosition;
                flat.y = 0f;
                if (flat.sqrMagnitude > 0.01f)
                    return Quaternion.LookRotation(flat.normalized, Vector3.up);
            }

            return Quaternion.Euler(0f, bearingDegrees, 0f);
        }

        private int ResolvePointLayerIndex(out string stateName)
        {
            if (pointGestureMode == PptPointGestureMode.UpperBodyOnly)
            {
                stateName = upperBodyPointStateName;
                return ResolveUpperBodyLayerIndex(stateName);
            }

            stateName = pointStateName;
            return HasStateOnLayer(0, stateName) ? 0 : -1;
        }

        private int ResolveShrugLayerIndex(out string stateName)
        {
            stateName = shrugStateName;
            if (pointGestureMode == PptPointGestureMode.UpperBodyOnly)
                return ResolveUpperBodyLayerIndex(stateName);

            return HasStateOnLayer(0, stateName) ? 0 : -1;
        }

        private int ResolveUpperBodyLayerIndex(string stateName)
        {
            EnsureAnimator();
            if (animator == null)
                return -1;

            int layerIndex = GetUpperBodyLayerIndex();
            if (layerIndex < 0)
                return -1;

            return HasStateOnLayer(layerIndex, stateName) ? layerIndex : -1;
        }

        private int GetUpperBodyLayerIndex()
        {
            if (cachedUpperBodyLayerIndex >= 0)
                return cachedUpperBodyLayerIndex;

            EnsureAnimator();
            if (animator == null || string.IsNullOrWhiteSpace(upperBodyLayerName))
                return -1;

            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetLayerName(i) != upperBodyLayerName)
                    continue;

                cachedUpperBodyLayerIndex = i;
                return i;
            }

            return -1;
        }

        private bool HasStateOnLayer(int layerIndex, string stateName)
        {
            EnsureAnimator();
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return false;

            return animator.HasState(layerIndex, Animator.StringToHash(stateName));
        }

        private void PlayGesture(string stateName, int layerIndex)
        {
            EnsureAnimator();
            if (animator == null || string.IsNullOrWhiteSpace(stateName) || layerIndex < 0)
                return;

            if (!HasStateOnLayer(layerIndex, stateName))
                return;

            if (layerIndex > 0)
                animator.SetLayerWeight(layerIndex, 1f);

            animator.CrossFadeInFixedTime(stateName, crossFadeSeconds, layerIndex, 0f);
        }

        private void ScheduleRestore(float delay, int gestureLayer)
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            restoreRoutine = StartCoroutine(RestoreIdleAfter(delay, gestureLayer));
        }

        private IEnumerator RestoreIdleAfter(float delay, int gestureLayer)
        {
            yield return new WaitForSecondsRealtime(delay);
            RestoreIdle(gestureLayer);
            EndUnscaledAnimator();
            restoreRoutine = null;
        }

        private void RestoreIdle(int gestureLayer)
        {
            EnsureAnimator();
            if (animator == null)
                return;

            if (!string.IsNullOrWhiteSpace(idleStateName) && HasStateOnLayer(0, idleStateName))
                animator.CrossFadeInFixedTime(idleStateName, crossFadeSeconds, 0, 0f);

            if (gestureLayer > 0)
                animator.SetLayerWeight(gestureLayer, 0f);
        }

        private void BeginUnscaledAnimator()
        {
            EnsureAnimator();
            if (animator == null || updateModeOverridden)
                return;

            cachedUpdateMode = animator.updateMode;
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;
            updateModeOverridden = true;
        }

        private void EndUnscaledAnimator()
        {
            if (!updateModeOverridden || animator == null)
                return;

            animator.updateMode = cachedUpdateMode;
            updateModeOverridden = false;
        }

        private void EnsureAnimator()
        {
            if (animator != null)
                return;

            if (visualRoot != null)
                animator = visualRoot.GetComponentInChildren<Animator>();

            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }

        private void OnDisable()
        {
            EndUnscaledAnimator();
        }
    }
}
