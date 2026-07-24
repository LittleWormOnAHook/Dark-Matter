using UnityEngine;

namespace Project.CameraFx
{
    /// <summary>
    /// Static one-liner API for combat, explosions, and environmental systems.
    /// </summary>
    public static class CameraShake
    {
        public static void AddTrauma(float amount)
        {
            CameraShakeService.EnsureExists()?.AddTrauma(amount);
        }

        public static void Shake(CameraShakeImpulse impulse)
        {
            CameraShakeService.EnsureExists()?.Shake(impulse);
        }

        public static void ShakeAt(Vector3 worldPosition, float trauma, float radius)
        {
            CameraShakeService.EnsureExists()?.ShakeAt(worldPosition, trauma, radius);
        }

        public static void ShakeDirectional(Vector3 worldDirection, float trauma)
        {
            CameraShakeService.EnsureExists()?.ShakeDirectional(worldDirection, trauma);
        }

        public static void Explosion(Vector3 origin, float strength, float radius = 40f)
        {
            CameraShakeService.EnsureExists()?.Explosion(origin, strength, radius);
        }

        public static void Impact(Vector3 origin, float strength, float radius = 12f)
        {
            CameraShakeService.EnsureExists()?.Impact(origin, strength, radius);
        }

        public static void Environmental(float trauma)
        {
            CameraShakeService.EnsureExists()?.Environmental(trauma);
        }

        public static void Sustain(float trauma)
        {
            CameraShakeService.EnsureExists()?.SustainTrauma(trauma);
        }
    }
}
