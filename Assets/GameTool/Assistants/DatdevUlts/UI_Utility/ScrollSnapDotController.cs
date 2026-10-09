using System.Collections.Generic;
using UnityEngine;

namespace DatdevUlts.UI_Utility
{
    public class ScrollSnapDotController : MonoBehaviour
    {
        [SerializeField] private List<ScrollSnapDot> _listDots = new List<ScrollSnapDot>();
        [SerializeField] private ScrollSnap _scrollSnap;

        public List<ScrollSnapDot> ListDots => _listDots;

        public ScrollSnap ScrollSnap => _scrollSnap;

        private void Awake()
        {
            ScrollSnap.OnPageChangeEvent.AddListener(() =>
            {
                for (int i = 0; i < ListDots.Count; i++)
                {
                    ListDots[i].Switch(ScrollSnap.CurrentPageIndex == i);
                }
            });
        }
    }
}