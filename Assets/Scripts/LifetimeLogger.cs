using System;
using UnityEngine;

namespace DefaultNamespace
{
    public class LifetimeLogger : MonoBehaviour
    {
        private void Awake()
        {
            Debug.Log($"[{Time.frameCount} {GetInstanceID()}] Awake {transform.position} {transform.parent.name} {gameObject.activeInHierarchy}");
        }
        
        private void OnEnable()
        {
            Debug.Log($"[{Time.frameCount} {GetInstanceID()}] OnEnable {transform.position} {transform.parent.name} {gameObject.activeInHierarchy}");
        }
        
        private void Start()
        {
            Debug.Log($"[{Time.frameCount} {GetInstanceID()}] Start {transform.position} {transform.parent.name} {gameObject.activeInHierarchy}");
        }
        
        private void OnDisable()
        {
            Debug.Log($"[{Time.frameCount} {GetInstanceID()}] OnDisable {transform.position} {transform.parent.name} {gameObject.activeInHierarchy}");
        }
        
        private void OnDestroy()
        {
            Debug.Log($"[{Time.frameCount} {GetInstanceID()}] OnDestroy {transform.position} {transform.parent.name}");
        }
    }
}
