using Pool;
using Unity.Profiling;
using UnityEngine;

public class ReturnToPoolAfterDelay : MonoBehaviour, IPoolable
{
    static readonly ProfilerMarker RETURNTOPOOL_DESTROY = new ProfilerMarker("ReturnToPoolAfterDelay.Destroy");
    
    [SerializeField] private float m_delay = 1.0f;

    private PooledGameObject m_pooledObject;
    private float m_timer = 0.0f;

    public void OnSpawned()
    {
        if (!m_pooledObject)
        {
            m_pooledObject = GetComponent<PooledGameObject>();
        }

        m_timer = 0.0f;
    }

    public void OnDespawned()
    {
    }

    private void Update()
    {
        m_timer += Time.deltaTime;
        if (m_timer > m_delay)
        {
            using (RETURNTOPOOL_DESTROY.Auto())
            {
                if (m_pooledObject)
                {
                    m_pooledObject.ReturnToPool();
                }
                else
                {
                    Destroy(gameObject);
                }
            }
        }
    }
}