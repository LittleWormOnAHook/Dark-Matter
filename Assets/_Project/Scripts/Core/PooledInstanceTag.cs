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

        public int BumpLease()
        {
            unchecked
            {
                LeaseId++;
            }

            return LeaseId;
        }
    }
}
