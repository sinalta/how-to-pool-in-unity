using UnityEngine;

public class ChangeSpawnPrefab : MonoBehaviour
{
    [SerializeField] private SpawnTester m_spawnTester;
    [SerializeField] private GameObject m_prefab;
    
    public void OnToggleChanged(bool on)
    {
        if (!on) return;
        
        m_spawnTester.SetPrefab(m_prefab);
    }
}