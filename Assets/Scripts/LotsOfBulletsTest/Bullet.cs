using Pool;
using UnityEngine;

namespace LotsOfBulletsTest
{
	public class Bullet : MonoBehaviour
	{
		[SerializeField] private float m_speed = 20.0f;
		[SerializeField] private float m_lifetime = 2.0f;

		private PooledGameObject m_pooledObject = null;
		private float m_timeUntilDespawn = 0.0f;

		private void Awake()
		{
			m_pooledObject = GetComponent<PooledGameObject>();
		}

		private void OnEnable()
		{
			m_timeUntilDespawn = m_lifetime;
		}

		private void Update()
		{
			m_timeUntilDespawn -= Time.deltaTime;
			if (m_timeUntilDespawn <= 0.0f)
			{
				if (m_pooledObject.OwningPool != null)
				{
					m_pooledObject.ReturnToPool();
				}
				else
				{
					Destroy(gameObject);
				}
				
				return;
			}
			
			transform.position += transform.up * (m_speed * Time.deltaTime);
		}
	}
}