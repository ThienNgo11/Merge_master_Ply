using System;
using DatdevUlts.Ults;
using UnityEngine;
using UnityEngine.Rendering;

namespace DatdevUlts.AnimationUtils
{
    public class AnimatedSortingGroup : MonoBehaviour
    {
        [SerializeField] private SortingGroup _sortingGroup;
        [SortingLayerAtt] [SerializeField] private string _layer;
        [SerializeField] private int _order;

        private void Update()
        {
            Apply();
        }

        private void OnDrawGizmos()
        {
            if (!Application.isPlaying)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (!enabled)
            {
                return;
            }

            if (_sortingGroup.sortingOrder != _order)
            {
                _sortingGroup.sortingOrder = _order;
            }
        }
    }
}