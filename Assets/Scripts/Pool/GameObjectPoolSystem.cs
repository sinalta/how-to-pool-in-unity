﻿using System;
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

        public enum DryBehavior
        {
            Expand,
            ReuseOldest
        }
        
        public enum HibernationStyle
        {
            Inactive,
            Active
        }
        
        [Serializable]
        public class SingleObjectPoolSettings
        {
            public GameObject Prefab;
            [Min(1)] public int DefaultSize = 5;
            public DryBehavior DryBehavior = DryBehavior.Expand;
            public HibernationStyle HibernationStyle = HibernationStyle.Inactive;
        }
        
        /// <summary>
        /// The SingleObject pool handles most of the logic of the system, with the GameObjectPoolSystem just filtering on the specific prefab being used.
        /// </summary>
        public class SingleObjectPool : IDisposable
        {
            private int Capacity => m_availableInstances.Count + m_activeInstances.Count;
            
            private SingleObjectPoolSettings m_settings;
            private Stack<GameObject> m_availableInstances;
            private List<GameObject> m_activeInstances;
            private Transform m_container;
            private Vector3 m_safeSpace;

            public SingleObjectPool(SingleObjectPoolSettings settings, Transform container, Vector3 safeSpace)
            {
                m_settings = settings;
                m_availableInstances = new Stack<GameObject>(m_settings.DefaultSize);
                m_activeInstances = new List<GameObject>(m_settings.DefaultSize);
                m_container = container;
                m_safeSpace = safeSpace;
                
                Expand(m_settings.DefaultSize);
            }

            /// <summary>
            /// Functionally similar to Object.Instantiate, by the time the object is return from this function you can assume it is in an equivalent state.
            /// Invokes PooledGameObject.OnSpawned before returning the instance.
            /// If the pool is empty, will act as specified by the DryBehaviour specified in the settings.
            /// </summary>
            /// <param name="position">World space position for the new object.</param>
            /// <param name="rotation">World space orientation of the new object.</param>
            /// <param name="parent">Parent that will be assigned to the new object.</param>
            /// <returns>An instance of the tracked prefab.</returns>
            public GameObject GetInstance(Vector3 position, Quaternion rotation, Transform parent)
            {
                if (m_availableInstances.Count == 0)
                {
                    switch (m_settings.DryBehavior)
                    {
                        case DryBehavior.Expand:
                            Expand(Capacity + 1);
                            Debug.LogWarning($"{m_settings.Prefab.name} pool size increased to {Capacity}");
                            break;
                        
                        case DryBehavior.ReuseOldest:
                            ReturnToPool(m_activeInstances[0]);
                            break;
                    }
                }
                
                var instance = m_availableInstances.Pop();
                m_activeInstances.Add(instance);
                
                var instanceTransform = instance.transform;

                if (parent != instanceTransform.parent)
                {
                    instanceTransform.SetParent(parent);
                }

                instanceTransform.SetPositionAndRotation(position, rotation);

                if (m_settings.HibernationStyle == HibernationStyle.Inactive)
                {
                    instance.SetActive(true);
                }

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

                if (m_settings.HibernationStyle == HibernationStyle.Inactive)
                {
                    instance.SetActive(false);
                }

                var instanceTransform = instance.transform;
                instanceTransform.SetPositionAndRotation(m_safeSpace, Quaternion.identity);

#if UNITY_EDITOR
                if (instanceTransform.parent != m_container)
                {
                    instanceTransform.SetParent(m_container);
                }
#else
                if (instanceTransform.parent)
                {
                    instanceTransform.SetParent(null);
                }
#endif
                

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
#if UNITY_EDITOR
                    var instance = Object.Instantiate(m_settings.Prefab, m_safeSpace, Quaternion.identity, m_container);
#else
                    var instance = Object.Instantiate(m_settings.Prefab, m_safeSpace, Quaternion.identity);
#endif

                    if (m_settings.HibernationStyle == HibernationStyle.Inactive)
                    {
                        instance.SetActive(false);
                    }

                    if (!instance.TryGetComponent(out PooledGameObject pooledGameObject))
                    {
                        pooledGameObject = instance.AddComponent<PooledGameObject>();
                    }
                    
                    pooledGameObject.Initialize(this);
                    pooledGameObject.OnDespawned();
                    
                    m_availableInstances.Push(instance);
                }
            }
        }
        
        #endregion

        #region Serialized Fields
        
        [SerializeField] private List<SingleObjectPoolSettings> m_poolSettings = new ();
        [Space]
        [SerializeField] private Vector3 m_poolSafeSpace = new (5000.0f, 5000.0f, 5000.0f);
        
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
            Debug.LogWarning($"Attempting to Instantiate {prefab.name} without a valid pool." +
                             $"\nFalling back to default behaviour." +
                             $"\n[{instance.GetInstanceID()}]");
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
            
            Debug.LogWarning($"Attempting to Destroy non-pooled object: " +
                             $"\n[{instance.GetInstanceID()}] {instance.name}." +
                             $"\nFalling back to default behaviour.");
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
                    },
                };
                m_pools.Add(settings.Prefab, new SingleObjectPool(settings, container.transform, m_poolSafeSpace));
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
