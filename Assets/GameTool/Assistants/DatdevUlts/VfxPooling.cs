using GameTool.ObjectPool.Scripts;
using UnityEngine;

namespace DatdevUlts
{
    public class VfxPooling : BasePooling
    {
        [SerializeField] private ParticleSystem _particleSystem;
        [SerializeField] private ParticleSystem[] _childs;

        public ParticleSystem ParticleSystem => _particleSystem;

        public ParticleSystem[] Childs => _childs;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_particleSystem)
            {
                _particleSystem = GetComponent<ParticleSystem>();
            }
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_particleSystem)
                _particleSystem.Play();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            gameObject.SetActive(false);
        }
    }
}