using UnityEngine;

namespace Project.PPT
{
    public sealed class PptNpcGestureController : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Transform visualRoot;

        private string pointStateName = "Point";
        private string shrugStateName = "Shrug";
        private float crossFadeSeconds = 0.15f;
        private string idleStateName = "Idle";
        private Coroutine restoreRoutine;

        public void Configure(PptNpcProfile profile)
        {
            if (profile == null)
                return;

            pointStateName = profile.PointStateName;
            shrugStateName = profile.ShrugStateName;
            crossFadeSeconds = profile.GestureCrossFadeSeconds;
        }

        public void PlayPoint(float bearingDegrees)
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            StartCoroutine(PointRoutine(bearingDegrees));
        }

        private System.Collections.IEnumerator PointRoutine(float bearingDegrees)
        {
            Transform root = visualRoot != null ? visualRoot : transform;
            Quaternion target = Quaternion.Euler(0f, bearingDegrees, 0f);
            float rotateTimer = 0f;
            Quaternion start = root.rotation;
            while (rotateTimer < 0.4f)
            {
                rotateTimer += Time.deltaTime;
                root.rotation = Quaternion.Slerp(start, target, rotateTimer / 0.4f);
                yield return null;
            }

            PlayGesture(pointStateName);
            yield return new WaitForSeconds(2.5f);

            EnsureAnimator();
            if (animator != null && !string.IsNullOrWhiteSpace(idleStateName))
            {
                int hash = Animator.StringToHash(idleStateName);
                if (animator.HasState(0, hash))
                    animator.CrossFadeInFixedTime(idleStateName, crossFadeSeconds, 0, 0f);
            }
        }

        public void PlayShrug()
        {
            PlayGesture(shrugStateName);
            ScheduleRestore(2f);
        }

        private void PlayGesture(string stateName)
        {
            EnsureAnimator();
            if (animator == null || string.IsNullOrWhiteSpace(stateName))
                return;

            int hash = Animator.StringToHash(stateName);
            if (animator.HasState(0, hash))
                animator.CrossFadeInFixedTime(stateName, crossFadeSeconds, 0, 0f);
        }

        private void ScheduleRestore(float delay)
        {
            if (restoreRoutine != null)
                StopCoroutine(restoreRoutine);

            restoreRoutine = StartCoroutine(RestoreIdleAfter(delay));
        }

        private System.Collections.IEnumerator RestoreIdleAfter(float delay)
        {
            yield return new WaitForSeconds(delay);
            EnsureAnimator();
            if (animator != null && !string.IsNullOrWhiteSpace(idleStateName))
            {
                int hash = Animator.StringToHash(idleStateName);
                if (animator.HasState(0, hash))
                    animator.CrossFadeInFixedTime(idleStateName, crossFadeSeconds, 0, 0f);
            }

            restoreRoutine = null;
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
    }
}
