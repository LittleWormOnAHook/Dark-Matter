namespace Project.Core
{
    /// <summary>
    /// Optional hook for pooled prefabs that need to reset/restart internal state (particle
    /// systems, timers, cached fields) when handed out or taken back by <see cref="PoolManager"/>.
    /// Components without state (e.g. plain visual meshes) don't need to implement this — GameObjectPool
    /// only calls it if the instance actually has one.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>Called right after the instance is activated and repositioned by the pool.</summary>
        void OnSpawnedFromPool();

        /// <summary>Called right before the instance is deactivated and returned to the pool.</summary>
        void OnReturnedToPool();
    }
}
