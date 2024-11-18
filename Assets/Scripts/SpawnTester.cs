using Pool;
using Unity.Profiling;
using UnityEngine;

public class SpawnTester : MonoBehaviour
{
    static readonly ProfilerMarker SPAWNTESTER_INSTANTIATE = new ProfilerMarker("SpawnTester.Instantiate");
    
    [SerializeField] private GameObjectPoolSystem m_pool;
    [SerializeField] private GameObject m_prefab;
    [SerializeField] private float m_spawnDelay = 1.0f;
    [SerializeField] private float m_radius = 5.0f;

    private float m_timeUntilSpawn = 0.0f;

    public void SetPrefab(GameObject prefab)
    {
        m_prefab = prefab;
    }

    private void Start()
    {
        m_timeUntilSpawn = m_spawnDelay;
    }

    private void Update()
    {
        m_timeUntilSpawn -= Time.deltaTime;
        while (m_timeUntilSpawn < 0.0f)
        {
            m_timeUntilSpawn += m_spawnDelay;

            using (SPAWNTESTER_INSTANTIATE.Auto())
            {
                var instance = m_pool.Instantiate(
                    m_prefab,
                    Random.onUnitSphere * m_radius,
                    Quaternion.identity,
                    transform);
            }
        }
    }
}