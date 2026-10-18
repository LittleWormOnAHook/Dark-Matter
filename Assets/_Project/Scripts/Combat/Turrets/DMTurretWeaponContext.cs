using UnityEngine;

namespace Project.Combat.Turrets
{
    /// <summary>Stamps the firing turret onto FORGE3D pooled shots.</summary>
    public static class DMTurretWeaponContext
    {
        public static DMTurretRuntime Current { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void Reset() => Current = null;

        public static void BeginFire(DMTurretRuntime runtime)
        {
            Current = runtime;
        }

        public static void EndFire(DMTurretRuntime runtime)
        {
            if (Current == runtime)
                Current = null;
        }

        public static void NotifySpawned(Component spawned)
        {
            if (spawned == null || Current == null)
                return;

            DMTurretShotStamp stamp = spawned.GetComponent<DMTurretShotStamp>();
            if (stamp == null)
                stamp = spawned.gameObject.AddComponent<DMTurretShotStamp>();
            stamp.Owner = Current;
            Transform barrel = Current.Barrel;
            stamp.Origin = barrel != null ? barrel.position : spawned.transform.position;
            DMTurretLos.ApplyShotMask(spawned);
        }

        public static DMTurretRuntime Resolve(Component spawned)
        {
            if (spawned == null)
                return Current;

            DMTurretShotStamp stamp = spawned.GetComponent<DMTurretShotStamp>();
            if (stamp != null && stamp.Owner != null)
                return stamp.Owner;

            return Current;
        }
    }

    public sealed class DMTurretShotStamp : MonoBehaviour
    {
        public DMTurretRuntime Owner;
        public Vector3 Origin;
    }
}
