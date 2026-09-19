using UnityEngine;

namespace Project.World
{
    public enum DMSplinePrefabUpAxis
    {
        PositiveY = 0,
        NegativeY = 1,
        PositiveX = 2,
        NegativeX = 3,
        PositiveZ = 4,
        NegativeZ = 5
    }

    public static class DMSplineAxisUtility
    {
        public static Vector3 ToVector(DMSplinePrefabUpAxis axis)
        {
            switch (axis)
            {
                case DMSplinePrefabUpAxis.NegativeY: return Vector3.down;
                case DMSplinePrefabUpAxis.PositiveX: return Vector3.right;
                case DMSplinePrefabUpAxis.NegativeX: return Vector3.left;
                case DMSplinePrefabUpAxis.PositiveZ: return Vector3.forward;
                case DMSplinePrefabUpAxis.NegativeZ: return Vector3.back;
                default: return Vector3.up;
            }
        }

        public static Vector3 ResolveUpAxis(DMSplinePrefabUpAxis globalAxis, DMSplineAnchorSettings settings)
        {
            if (settings != null && settings.overrideUpAxis)
                return ToVector(settings.upAxis);
            return ToVector(globalAxis);
        }

        /// <summary>
        /// Builds a rotation that maps model up/forward to desired world up/forward (yaw around up).
        /// </summary>
        public static Quaternion RotationFromUpAndForward(
            Vector3 modelUp,
            Vector3 modelForward,
            Vector3 desiredWorldUp,
            Vector3 desiredWorldForward)
        {
            modelUp.Normalize();
            desiredWorldUp.Normalize();
            desiredWorldForward = Vector3.ProjectOnPlane(desiredWorldForward, desiredWorldUp);
            modelForward = Vector3.ProjectOnPlane(modelForward, modelUp);

            if (desiredWorldForward.sqrMagnitude < 0.0001f)
                desiredWorldForward = Vector3.Cross(desiredWorldUp, Vector3.forward);
            if (desiredWorldForward.sqrMagnitude < 0.0001f)
                desiredWorldForward = Vector3.Cross(desiredWorldUp, Vector3.right);
            desiredWorldForward.Normalize();

            if (modelForward.sqrMagnitude < 0.0001f)
                modelForward = Vector3.Cross(modelUp, Vector3.forward);
            if (modelForward.sqrMagnitude < 0.0001f)
                modelForward = Vector3.Cross(modelUp, Vector3.right);
            modelForward.Normalize();

            Quaternion alignUp = Quaternion.FromToRotation(modelUp, desiredWorldUp);
            Vector3 rotatedForward = alignUp * modelForward;
            float yaw = Vector3.SignedAngle(rotatedForward, desiredWorldForward, desiredWorldUp);
            return Quaternion.AngleAxis(yaw, desiredWorldUp) * alignUp;
        }
    }
}
