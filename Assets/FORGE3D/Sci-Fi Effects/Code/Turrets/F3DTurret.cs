using UnityEngine;
using System.Collections;
using System;

namespace FORGE3D
{
    public class F3DTurret : MonoBehaviour
    {
        [HideInInspector] public bool destroyIt;

        public GameObject Mount;
        public GameObject Swivel;

        private Vector3 defaultDir;
        private Quaternion defaultRot;

        private Transform headTransform;
        private Transform barrelTransform;

        public float HeadingTrackingSpeed = 2f;
        public float ElevationTrackingSpeed = 2f;

        private Vector3 targetPos;
        [HideInInspector] public Vector3 headingVetor;

        private float curHeadingAngle;
        private float curElevationAngle;

        public Vector2 HeadingLimit;
        public Vector2 ElevationLimit;

        public bool smoothControlling;

        public bool DebugDraw;

        public Transform DebugTarget; 
        
        private bool fullAccess;
        public Animator[] Animators;
        
        

        private void Awake()
        {
            if (Swivel != null)
                headTransform = Swivel.GetComponent<Transform>();
            if (Mount != null)
                barrelTransform = Mount.GetComponent<Transform>();
        }

        public void PlayAnimation()
        {
            if (Animators == null)
                return;
            for (int i = 0; i < Animators.Length; i++)
                Animators[i].SetTrigger("FireTrigger");
        }

        public void PlayAnimationLoop()
        {
            if (Animators == null)
                return;
            for (int i = 0; i < Animators.Length; i++)
                Animators[i].SetBool("FireLoopBool", true);
        }

        public void StopAnimation()
        {
            if (Animators == null)
                return;
            for (int i = 0; i < Animators.Length; i++)
                Animators[i].SetBool("FireLoopBool", false);
        }

        // Use this for initialization
        private void Start()
        {
            if (headTransform == null && Swivel != null)
                headTransform = Swivel.transform;
            if (barrelTransform == null && Mount != null)
                barrelTransform = Mount.transform;
            if (headTransform == null || barrelTransform == null)
                return;
            targetPos = headTransform.transform.position + headTransform.transform.forward * 100f;
            defaultDir = Swivel.transform.forward;
            defaultRot = Quaternion.FromToRotation(transform.forward, defaultDir);
            if (HeadingLimit.y - HeadingLimit.x >= 359.9f)
                fullAccess = true;
            StopAnimation();
        }

        // Autotrack
        public void SetNewTarget(Vector3 _targetPos)
        {
            targetPos = _targetPos;
        }

        public void HoldCurrentAim()
        {
            EnsureTransforms();
            if (barrelTransform == null)
                return;
            targetPos = barrelTransform.position + barrelTransform.forward * 100f;
        }

        public void SyncTrackingFromPose()
        {
            EnsureTransforms();
            if (headTransform == null || barrelTransform == null)
                return;
            curHeadingAngle = SignedEuler(headTransform.localEulerAngles.y);
            curElevationAngle = SignedEuler(barrelTransform.localEulerAngles.x);
            HoldCurrentAim();
        }

        public void ApplyLookDelta(float headingDelta, float elevationDelta)
        {
            EnsureTransforms();
            if (headTransform == null || barrelTransform == null)
                return;

            if (HeadingLimit.x <= -180f && HeadingLimit.y >= 180f || fullAccess)
            {
                curHeadingAngle += headingDelta;
                headTransform.rotation = headTransform.rotation * Quaternion.Euler(0f, headingDelta, 0f);
            }
            else
            {
                float next = Mathf.Clamp(curHeadingAngle + headingDelta, HeadingLimit.x, HeadingLimit.y);
                float step = next - curHeadingAngle;
                if (Mathf.Abs(step) > 0.0001f)
                {
                    curHeadingAngle = next;
                    headTransform.rotation = headTransform.rotation * Quaternion.Euler(0f, step, 0f);
                }
            }

            float nextElevation = Mathf.Clamp(curElevationAngle + elevationDelta, ElevationLimit.x, ElevationLimit.y);
            float elevationStep = nextElevation - curElevationAngle;
            if (Mathf.Abs(elevationStep) > 0.0001f)
            {
                curElevationAngle = nextElevation;
                barrelTransform.rotation = barrelTransform.rotation * Quaternion.Euler(elevationStep, 0f, 0f);
            }

            HoldCurrentAim();
        }

        private void EnsureTransforms()
        {
            if (headTransform == null && Swivel != null)
                headTransform = Swivel.transform;
            if (barrelTransform == null && Mount != null)
                barrelTransform = Mount.transform;
        }

        private static float SignedEuler(float angle)
        {
            if (angle > 180f)
                angle -= 360f;
            return angle;
        }

        // Angle between mount and target
        public float GetAngleToTarget()
        {
            if (Mount == null)
                return 0f;
            return Vector3.Angle(Mount.transform.forward, targetPos - Mount.transform.position);
        }

        private void Update()
        {
            if (headTransform == null || barrelTransform == null)
                return;
            if (DebugTarget != null)
                targetPos = DebugTarget.transform.position;
            
            if (!smoothControlling)
            {
                if (barrelTransform != null)
                {
                    /////// Heading
                    headingVetor =
                        Vector3.Normalize(F3DMath.ProjectVectorOnPlane(headTransform.up,
                            targetPos - headTransform.position));
                    float headingAngle =
                        F3DMath.SignedVectorAngle(headTransform.forward, headingVetor, headTransform.up);
                    float turretDefaultToTargetAngle = F3DMath.SignedVectorAngle(defaultRot * headTransform.forward,
                        headingVetor, headTransform.up);
                    float turretHeading = F3DMath.SignedVectorAngle(defaultRot * headTransform.forward,
                        headTransform.forward, headTransform.up);

                    float headingStep = HeadingTrackingSpeed * Time.deltaTime;

                    // Heading step and correction
                    // Full rotation
                    if (HeadingLimit.x <= -180f && HeadingLimit.y >= 180f)
                        headingStep *= Mathf.Sign(headingAngle);
                    else // Limited rotation
                        headingStep *= Mathf.Sign(turretDefaultToTargetAngle - turretHeading);

                    // Hard stop on reach no overshooting
                    if (Mathf.Abs(headingStep) > Mathf.Abs(headingAngle))
                        headingStep = headingAngle;

                    // Heading limits
                    if (curHeadingAngle + headingStep > HeadingLimit.x &&
                        curHeadingAngle + headingStep < HeadingLimit.y ||
                        HeadingLimit.x <= -180f && HeadingLimit.y >= 180f || fullAccess)
                    {
                        curHeadingAngle += headingStep;
                        headTransform.rotation = headTransform.rotation * Quaternion.Euler(0f, headingStep, 0f);
                    }

                    /////// Elevation
                    Vector3 elevationVector =
                        Vector3.Normalize(F3DMath.ProjectVectorOnPlane(headTransform.right,
                            targetPos - barrelTransform.position));
                    float elevationAngle =
                        F3DMath.SignedVectorAngle(barrelTransform.forward, elevationVector, headTransform.right);

                    // Elevation step and correction
                    float elevationStep = Mathf.Sign(elevationAngle) * ElevationTrackingSpeed * Time.deltaTime;
                    if (Mathf.Abs(elevationStep) > Mathf.Abs(elevationAngle))
                        elevationStep = elevationAngle;

                    // Elevation limits
                    if (curElevationAngle + elevationStep < ElevationLimit.y &&
                        curElevationAngle + elevationStep > ElevationLimit.x)
                    {
                        curElevationAngle += elevationStep;
                        barrelTransform.rotation = barrelTransform.rotation * Quaternion.Euler(elevationStep, 0f, 0f);
                    }
                }
            }
            else
            {
                if (Swivel == null)
                    return;
                Transform barrelX = barrelTransform;
                Transform barrelY = Swivel.transform;

                //finding position for turning just for X axis (down-up)
                Vector3 targetX = targetPos - barrelX.transform.position;
                Quaternion targetRotationX = Quaternion.LookRotation(targetX, headTransform.up);

                barrelX.transform.rotation = Quaternion.Slerp(barrelX.transform.rotation, targetRotationX,
                    HeadingTrackingSpeed * Time.deltaTime);
                barrelX.transform.localEulerAngles = new Vector3(barrelX.transform.localEulerAngles.x, 0f, 0f);

                //checking for turning up too much
                if (barrelX.transform.localEulerAngles.x >= 180f &&
                    barrelX.transform.localEulerAngles.x < (360f - ElevationLimit.y))
                {
                    barrelX.transform.localEulerAngles = new Vector3(360f - ElevationLimit.y, 0f, 0f);
                }

                //down
                else if (barrelX.transform.localEulerAngles.x < 180f &&
                         barrelX.transform.localEulerAngles.x > -ElevationLimit.x)
                {
                    barrelX.transform.localEulerAngles = new Vector3(-ElevationLimit.x, 0f, 0f);
                }

                //finding position for turning just for Y axis
                Vector3 targetY = targetPos;
                targetY.y = barrelY.position.y;

                Quaternion targetRotationY = Quaternion.LookRotation(targetY - barrelY.position, barrelY.transform.up);

                barrelY.transform.rotation = Quaternion.Slerp(barrelY.transform.rotation, targetRotationY,
                    ElevationTrackingSpeed * Time.deltaTime);
                barrelY.transform.localEulerAngles = new Vector3(0f, barrelY.transform.localEulerAngles.y, 0f);

                if (!fullAccess)
                {
                    //checking for turning left
                    if (barrelY.transform.localEulerAngles.y >= 180f &&
                        barrelY.transform.localEulerAngles.y < (360f - HeadingLimit.y))
                    {
                        barrelY.transform.localEulerAngles = new Vector3(0f, 360f - HeadingLimit.y, 0f);
                    }

                    //right
                    else if (barrelY.transform.localEulerAngles.y < 180f &&
                             barrelY.transform.localEulerAngles.y > -HeadingLimit.x)
                    {
                        barrelY.transform.localEulerAngles = new Vector3(0f, -HeadingLimit.x, 0f);
                    }
                }
            }

            if (DebugDraw)
                Debug.DrawLine(barrelTransform.position,
                    barrelTransform.position +
                    barrelTransform.forward * Vector3.Distance(barrelTransform.position, targetPos), Color.red);
        }
    }
}