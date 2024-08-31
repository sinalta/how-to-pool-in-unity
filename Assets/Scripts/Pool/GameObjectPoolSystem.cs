using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using Object = UnityEngine.Object;

namespace Pool
{
    /// <summary>
    /// Configurable GameObject pooling setup.
    /// Allows the efficient re-use of multiple Prefab types, using the same interface as Object.Instantiate and Object.Destroy. 
    /// </summary>
    public class GameObjectPoolSystem : MonoBehaviour
    {
        #region Types
        
        [Serializable]
        public class SingleObjectPoolSettings
        {
            public GameObject Prefab;
            [Min(1)] public int DefaultSize = 5;
        }
        
        /// <summary>
        /// The SingleObject pool handles most of the logic of the system, with the GameObjectPoolSystem just filtering on the specific prefab being used.
        /// </summary>
        public class SingleObjectPool : IDisposable
        {
            private int Capacity => m_availableInstances.Count + m_activeInstances.Count;
            
            private SingleObjectPoolSettings m_settings;
            private Stack<GameObject> m_availableInstances;
            private HashSet<GameObject> m_activeInstances;
            private Transform m_container;

            public SingleObjectPool(SingleObjectPoolSettings settings, Transform container)
            {
                m_settings = settings;
                m_availableInstances = new Stack<GameObject>(m_settings.DefaultSize);
                m_activeInstances = new HashSet<GameObject>(m_settings.DefaultSize);
                m_container = container;
                
                Expand(m_settings.DefaultSize);
            }

            /// <summary>
            /// Functionally similar to Object.Instantiate, by the time the object is return from this function you can assume it is in an equivalent state.
            /// Invokes PooledGameObject.OnSpawned before returning the instance.
            /// If the pool is empty, will Instantiate a new one.
            /// </summary>
            /// <param name="position">World space position for the new object.</param>
            /// <param name="rotation">World space orientation of the new object.</param>
            /// <param name="parent">Parent that will be assigned to the new object.</param>
            /// <returns>An instance of the tracked prefab.</returns>
            public GameObject GetInstance(Vector3 position, Quaternion rotation, Transform parent)
            {
                if (m_availableInstances.Count == 0)
                {
                    Expand(Capacity + 1);
                    Debug.LogWarning($"{m_settings.Prefab.name} pool size increased to {Capacity}");
                }
                
                var instance = m_availableInstances.Pop();
                m_activeInstances.Add(instance);
                
                var instanceTransform = instance.transform;
                
                instanceTransform.SetParent(parent);
                instanceTransform.SetPositionAndRotation(position, rotation);
                
                instance.SetActive(true);
                
                var pooledObject = instance.GetComponent<PooledGameObject>();
                pooledObject.OnSpawned();

                return instance;
            }

            /// <summary>
            /// Functionally similar to Object.Destroy.
            /// Will return the instance to the list of instances available.
            /// </summary>
            /// <param name="instance">The object to return.</param>
            public void ReturnToPool(GameObject instance)
            {
                var pooledObject = instance.GetComponent<PooledGameObject>();
                pooledObject.OnDespawned();
                
                instance.SetActive(false);
                
                var instanceTransform = instance.transform;
                instanceTransform.SetParent(m_container);

                m_activeInstances.Remove(instance);
                m_availableInstances.Push(instance);
            }

            /// <summary>
            /// Utility function to forcefully return all active instances back to the pool.
            /// </summary>
            public void ReturnAllInstances()
            {
                using (ListPool<GameObject>.Get(out var activeList))
                {
                    activeList.AddRange(m_activeInstances);
                    foreach (var instance in activeList)
                    {
#if UNITY_EDITOR
                        //  Handle in editor shutdown, where the instances will have potentially been destroyed before the pool. 
                        if (!instance) continue;
#endif
                        ReturnToPool(instance);
                    }
                    
                    activeList.Clear();
                }
            }
            
            public void Dispose()
            {
                ReturnAllInstances();
                
                while (m_availableInstances.TryPop(out var instance))
                {
                    Object.Destroy(instance);
                }
            }

            private void Expand(int newSize)
            {
                while (Capacity < newSize)
                {
                    var instance = Object.Instantiate(m_settings.Prefab, m_container);
                    instance.SetActive(false);

                    if (!instance.TryGetComponent(out PooledGameObject pooledGameObject))
                    {
                        pooledGameObject = instance.AddComponent<PooledGameObject>();
                    }
                    
                    pooledGameObject.Initialize(this);
                    
                    m_availableInstances.Push(instance);
                }
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [SerializeField] private List<SingleObjectPoolSettings> m_poolSettings = new ();
        
        #endregion

        #region Private Fields
        
        private Dictionary<GameObject, SingleObjectPool> m_pools = new ();
        
        #endregion

        #region Public Interface
        
        /// <summary>
        /// Functionally similar to Object.Instantiate, by the time the object is return from this function you can assume it is in an equivalent state.
        /// Finds the SingleObjectPool for the requested prefab, and forwards the call to GetInstance.
        ///
        /// Forwards the call to Object.Instantiate if no matching SingleObjectPool is found.
        /// </summary>
        /// <param name="prefab">The prefab you'd like an instance of.</param>
        /// <param name="position">World space position for the new object.</param>
        /// <param name="rotation">World space orientation of the new object.</param>
        /// <param name="parent">Parent that will be assigned to the new object.</param>
        /// <returns></returns>
        public GameObject Instantiate(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (m_pools.TryGetValue(prefab, out var pool))
            {
                return pool.GetInstance(position, rotation, parent);
            }

            var instance = Object.Instantiate(prefab, position, rotation, parent);
            Debug.LogWarning($"Attempting to Instantiate {prefab.name} without a valid pool.\nFalling back to default behaviour.\n[{instance.GetInstanceID()}]");
            return instance;
        }

        /// <summary>
        /// Functionally similar to Object.Destroy. Returns the instance to the matching SingleObjectPool.
        /// If none is found, forwards the call to Object.Destroy.
        /// </summary>
        /// <param name="instance">The object to return.</param>
        public void Destroy(GameObject instance)
        {
            if (instance.TryGetComponent(out PooledGameObject pooledGameObject))
            {
                pooledGameObject.ReturnToPool();
            }
            
            Debug.LogWarning($"Attempting to Destroy non-pooled object: [{instance.GetInstanceID()}] {instance.name}.\nFalling back to default behaviour.");
            Object.Destroy(instance);
        }
        
        #endregion

        #region MonoBehaviour Interface
        
        private void Awake()
        {
            foreach (var settings in m_poolSettings)
            {
                var container = new GameObject($"{settings.Prefab.name} Pool")
                {
                    transform =
                    {
                        parent = transform
                    }
                };
                m_pools.Add(settings.Prefab, new SingleObjectPool(settings, container.transform));
            }
        }

        private void OnDestroy()
        {
            foreach (var pool in m_pools.Values)
            {
                pool.Dispose();
            }
            
            m_pools.Clear();
        }
        
        #endregion
    }
}
