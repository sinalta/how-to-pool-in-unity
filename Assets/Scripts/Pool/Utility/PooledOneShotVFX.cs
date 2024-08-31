using UnityEngine;

namespace Pool.Utility
{
    [RequireComponent(typeof(PooledGameObject))]
    [RequireComponent(typeof(ParticleSystem))]
    public class PooledOneShotVFX : MonoBehaviour, IPoolable
    {
        private ParticleSystem m_particleSystem;
        private PooledGameObject m_pooledObject;

        private void Awake()
        {
            m_particleSystem = GetComponent<ParticleSystem>();
            m_pooledObject = GetComponent<PooledGameObject>();
        }

        private void OnParticleSystemStopped()
        {
            m_pooledObject.ReturnToPool();
        }

        public void OnSpawned()
        {
            m_particleSystem.Play(true);
        }

        public void OnDespawned()
        {
            m_particleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }
}