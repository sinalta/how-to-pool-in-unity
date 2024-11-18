using System.Collections.Generic;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Pool
{
    /// <summary>
    /// Component to be added to any prefab which is intended to be pooled.
    /// Manages collecting and interacting with all the IPoolables on an instance, as well as providing access to the pool which created it.  
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledGameObject : MonoBehaviour
    {
        static readonly ProfilerMarker ONSPAWNED_MARKER = new ("PooledGameObject.OnSpawned");
        static readonly ProfilerMarker ONSPAWNED_COMPONENTS_MARKER = new ("PooledGameObject.OnSpawned.Components");
        static readonly ProfilerMarker ONSPAWNED_POOLABLES_MARKER = new ("PooledGameObject.OnSpawned.Poolables");
        
        static readonly ProfilerMarker ONDESPAWNED_MARKER = new ("PooledGameObject.OnDespawned");
        static readonly ProfilerMarker ONDESPAWNED_COMPONENTS_MARKER = new ("PooledGameObject.OnDespawned.Components");
        static readonly ProfilerMarker ONDESPAWNED_POOLABLES_MARKER = new ("PooledGameObject.OnDespawned.Poolables");
        
        public GameObjectPoolSystem.SingleObjectPool OwningPool { get; private set; }
        
        [Tooltip("These components will be disabled when the instance is in the pool, and re-enabled when removed from the pool.")]
        [SerializeField] private List<Component> m_syncedComponents = new ();
        
        private List<IPoolable> m_poolables = new(); 

        public void Initialize(GameObjectPoolSystem.SingleObjectPool owningPool)
        {
            OwningPool = owningPool;
            m_poolables.AddRange(GetComponentsInChildren<IPoolable>(true));
        }

        public void OnSpawned()
        {
            using var _ = ONSPAWNED_MARKER.Auto();

            using (ONSPAWNED_COMPONENTS_MARKER.Auto())
            {
                foreach (var component in m_syncedComponents)
                {
                    switch (component)
                    {
                        case Behaviour behaviour:
                            behaviour.enabled = true;
                            break;

                        case Renderer renderer:
                            renderer.enabled = true;
                            break;
                    }
                }
            }

            using (ONSPAWNED_POOLABLES_MARKER.Auto())
            {
                foreach (var poolable in m_poolables)
                {
                    poolable.OnSpawned();
                }
            }
        }

        public void OnDespawned()
        {
            using var _ = ONDESPAWNED_MARKER.Auto();

            using (ONDESPAWNED_POOLABLES_MARKER.Auto())
            {
                foreach (var poolable in m_poolables)
                {
                    poolable.OnDespawned();
                }
            }

            using (ONDESPAWNED_COMPONENTS_MARKER.Auto())
            {
                foreach (var component in m_syncedComponents)
                {
                    switch (component)
                    {
                        case Behaviour behaviour:
                            behaviour.enabled = false;
                            break;

                        case Renderer renderer:
                            renderer.enabled = false;
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Directly returns this object to it's owning pool, skipping the GameObjectPoolSystem.
        /// </summary>
        public void ReturnToPool()
        {
            OwningPool.ReturnToPool(gameObject);
        }
    }
}