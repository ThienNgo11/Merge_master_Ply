using GameTool.ObjectPool.Scripts;
using UnityEditor;
using UnityEngine;

namespace DatdevUlts
{
    public class SpinePooling : BasePooling
    {
        [SerializeField] private SpineUtils _spineUtils;
        [SerializeField] private string _nameAnim;
        [SerializeField] private bool _loop;
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] private bool _autoDisable = true;

        public SpineUtils SpineUtils => _spineUtils;

        public string NameAnim => _nameAnim;

        public bool Loop => _loop;

        public bool AutoPlay => _autoPlay;

        public bool AutoDisable => _autoDisable;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!_spineUtils)
            {
                _spineUtils = _spineUtils.GetComponent<SpineUtils>();
                EditorUtility.SetDirty(this);
            }
        }
#endif

        protected override void OnEnable()
        {
            base.OnEnable();
            if (_autoPlay)
            {
                #if USE_SPINE
                _spineUtils.SetAnimProtect(_nameAnim, _loop);
                #endif
            }
        }
    }
}