using System.Collections.Generic;
using UnityEngine;

namespace Pool
{
    /// <summary>
    /// Component to be added to any prefab which is intended to be pooled.
    /// Manages collecting and interacting with all the IPoolables on an instance, as well as providing access to the pool which created it.  
    /// </summary>
    [DisallowMultipleComponent]
    public class PooledGameObject : MonoBehaviour
    {
        public GameObjectPoolSystem.SingleObjectPool OwningPool { get; private set; }
        
        private List<IPoolable> m_poolables = new(); 

        public void Initialize(GameObjectPoolSystem.SingleObjectPool owningPool)
        {
            OwningPool = owningPool;
            m_poolables.AddRange(GetComponentsInChildren<IPoolable>(true));
        }

        public void OnSpawned()
        {
            foreach (var poolable in m_poolables)
            {
                poolable.OnSpawned();
            }
        }

        public void OnDespawned()
        {
            foreach (var poolable in m_poolables)
            {
                poolable.OnDespawned();
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