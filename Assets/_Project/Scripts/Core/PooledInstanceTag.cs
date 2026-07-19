using UnityEngine;

namespace Project.Core
{
    /// <summary>
    /// Marker auto-attached by <see cref="GameObjectPool"/> to every instance it hands out, so call
    /// sites can release an instance with just <c>PoolManager.Release(instance)</c> — matching the
    /// ergonomics of a plain <c>Destroy(instance)</c> — without needing to also pass the source prefab.
    /// </summary>
    public class PooledInstanceTag : MonoBehaviour
    {
        public GameObjectPool SourcePool { get; set; }
    }
}
