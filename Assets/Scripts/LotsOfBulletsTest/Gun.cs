using Pool;
using UnityEngine;

namespace LotsOfBulletsTest
{
	public class Gun : MonoBehaviour
	{
		public bool UsePool { get; set; } = true;
		
		[SerializeField] private GameObjectPoolSystem m_pool = null;
		[SerializeField] private GameObject m_bulletPrefab = null;
		[SerializeField] private float m_rateOfFire = 100.0f;
		[SerializeField] private float m_spawnOffset = 1.0f;

		private float m_fireTimer = 0.0f;

		private void Update()
		{
			var fireDelta = 1.0f / m_rateOfFire;
			m_fireTimer += Time.deltaTime;
			if (m_fireTimer >= fireDelta)
			{
				m_fireTimer -= fireDelta;

				if (UsePool)
				{
					m_pool.Instantiate(m_bulletPrefab, transform.position + transform.up * m_spawnOffset, transform.rotation, null);
				}
				else
				{
					Instantiate(m_bulletPrefab, transform.position + transform.up * m_spawnOffset, transform.rotation, null);
				}
			}
		}
	}
}
