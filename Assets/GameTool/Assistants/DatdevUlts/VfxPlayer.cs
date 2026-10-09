using UnityEngine;

namespace DatdevUlts
{
    public class VfxPlayer : MonoBehaviour
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

        protected void OnEnable()
        {
            if (_particleSystem)
                _particleSystem.Play();
        }

        protected void OnDisable()
        {
            gameObject.SetActive(false);
        }
    }
}