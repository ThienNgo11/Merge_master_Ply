using GameTool.ObjectPool.Scripts;
using UnityEngine;

namespace DatdevUlts.AnimationUtils
{
    public class AnimatorPooling : BasePooling
    {
        [SerializeField] private AnimatorController _animatorController;

        public AnimatorController AnimatorController => _animatorController;

        private void OnValidate()
        {
            _animatorController = GetComponentInChildren<AnimatorController>();
        }
    }
}
