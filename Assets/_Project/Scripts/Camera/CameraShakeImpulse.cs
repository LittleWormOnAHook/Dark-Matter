using UnityEngine;

namespace Project.CameraFx
{
    /// <summary>
    /// One-shot trauma request for <see cref="CameraShakeService"/>.
    /// </summary>
    public struct CameraShakeImpulse
    {
        public float Trauma;
        public float Radius;
        public Vector3 Origin;
        public bool HasOrigin;
        public Vector3 Direction;
        public bool HasDirection;

        public static CameraShakeImpulse Global(float trauma)
        {
            return new CameraShakeImpulse
            {
                Trauma = trauma,
                Radius = 0f,
                HasOrigin = false,
                HasDirection = false
            };
        }

        public static CameraShakeImpulse At(Vector3 worldPosition, float trauma, float radius)
        {
            return new CameraShakeImpulse
            {
                Trauma = trauma,
                Radius = Mathf.Max(0f, radius),
                Origin = worldPosition,
                HasOrigin = true,
                HasDirection = false
            };
        }

        public static CameraShakeImpulse Directional(Vector3 worldDirection, float trauma)
        {
            return new CameraShakeImpulse
            {
                Trauma = trauma,
                Radius = 0f,
                HasOrigin = false,
                Direction = worldDirection.sqrMagnitude > 0.0001f
                    ? worldDirection.normalized
                    : Vector3.forward,
                HasDirection = true
            };
        }
    }
}
