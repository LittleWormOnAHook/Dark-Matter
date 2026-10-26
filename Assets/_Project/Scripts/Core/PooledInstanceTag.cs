using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Marker auto-attached by <see cref="GameObjectPool"/> to every instance it hands out.
    /// <see cref="LeaseId"/> increments on each Spawn so delayed releases can ignore stale timers.
    /// </summary>
    public class PooledInstanceTag : MonoBehaviour
    {
        public GameObjectPool SourcePool { get; set; }
        public int LeaseId { get; private set; }
        /// <summary>2026-10-25d: Time.frameCount of the last Spawn (death cleanup keeps same-frame killing-blow FX).</summary>
        public int SpawnFrame { get; private set; }

        public int BumpLease()
        {
            unchecked
            {
                LeaseId++;
            }
            SpawnFrame = Time.frameCount;

            return LeaseId;
        }
    }
}
