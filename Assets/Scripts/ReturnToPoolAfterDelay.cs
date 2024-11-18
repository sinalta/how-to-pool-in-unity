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
        m_timer = 0.0f;
        enabled = true;
    }

    public void OnDespawned()
    {
        enabled = false;
    }

    private void Start()
    {
        m_pooledObject = GetComponent<PooledGameObject>();
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