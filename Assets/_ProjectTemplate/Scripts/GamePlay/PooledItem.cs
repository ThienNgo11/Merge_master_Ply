using System;
using System.Collections;
using UnityEngine;

namespace GameTool.ObjectPool.Scripts
{
    public class PooledItem : MonoBehaviour
    {
        [NonSerialized] private int _prefabKey;
        [NonSerialized] private Transform _parentPool;
        [NonSerialized] private bool _isInPool;
        private Coroutine _delayCoroutine;

        public int PrefabKey
        {
            get => _prefabKey;
            set => _prefabKey = value;
        }

        public Transform ParentPool
        {
            get => _parentPool;
            set => _parentPool = value;
        }

        public bool IsInPool
        {
            get => _isInPool;
            set => _isInPool = value;
        }

        public void Despawn()
        {
            if (_isInPool)
            {
                return;
            }

            CancelDelay();
            PoolingManager.Instance.Despawn(gameObject);
        }

        public void Despawn(float delay)
        {
            if (_isInPool)
            {
                return;
            }

            if (delay <= 0f)
            {
                Despawn();
                return;
            }

            CancelDelay();
            _delayCoroutine = StartCoroutine(CoWaitDespawn(delay));
        }

        public void CancelDelay()
        {
            if (_delayCoroutine != null)
            {
                StopCoroutine(_delayCoroutine);
                _delayCoroutine = null;
            }
        }

        private IEnumerator CoWaitDespawn(float delay)
        {
            yield return new WaitForSeconds(delay);
            _delayCoroutine = null;
            Despawn();
        }

        private void OnDisable()
        {
            CancelDelay();
        }
    }
}
