namespace Pool
{
    /// <summary>
    /// Interface to be able to receive callbacks for pooled objects. Allows for custom setup and reset functionality per-object.
    /// </summary>
    public interface IPoolable
    {
        /// <summary>
        /// Called after OnEnable, right before returning the pooled object from Pool.Instantiate
        /// </summary>
        void OnSpawned();
        
        /// <summary>
        /// Called immediately upon returning the instance to the pool, before OnDisable.
        /// </summary>
        void OnDespawned();
    }
}