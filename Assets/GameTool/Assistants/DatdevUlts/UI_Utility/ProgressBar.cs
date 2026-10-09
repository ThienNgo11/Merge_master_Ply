using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace DatdevUlts.UI_Utility
{
    public class ProgressBar : MonoBehaviour
    {
        [SerializeField] private Image _current;
        [FormerlySerializedAs("_fill"),SerializeField] private Image _max;
        [SerializeField] private bool _horizontal = true;
        [SerializeField] private bool _vertical;

        public Image Current => _current;

        public Image Max => _max;

        public RectTransform RectCurrent => Current.transform as RectTransform;

        public RectTransform RectMax => Max.transform as RectTransform;

        public float GetFillAmount(float current, float max)
        {
            return Mathf.Clamp(current, 0, max) / max;
        }

        public Vector2 GetSizeDelta(float current, float max)
        {
            current = Mathf.Clamp(current, 0, max);
            var size = RectMax.rect.size;
            if (_horizontal)
            {
                size.x *= current / max;
                size.y = RectCurrent.sizeDelta.y;
            }

            if (_vertical)
            {
                size.x = RectCurrent.sizeDelta.x;
                size.y *= current / max;
            }

            return size;
        }
    }
}