using GameTool.Assistants.DesignPattern;
using GameToolSample.Scripts.Enum;
using UnityEngine;

namespace DatdevUlts.UI_Utility
{
    public class FollowSafeArea : MonoBehaviour
    {
        [Tooltip("RectTrsf cầm safe area, stretch toàn màn hình, cùng cấp với this rect (this rect, _rectFull cũng phải thế)")]
        [SerializeField] private RectTransform _rectSafe;
        
        [Tooltip("Xem tooltip Rect Safe")]
        [SerializeField] private RectTransform _rectFull;

        [SerializeField] private bool _followX;
        [SerializeField] private bool _followY;

        private RectTransform _thisRect;
        private Vector3 _newScale;

        private void Awake()
        {
            _thisRect = GetComponent<RectTransform>();
            this.RegisterListener(EventID.SafeAreaChange, OnSafeAreaChange);
            Follow();
        }

        private void OnEnable()
        {
            Follow();
        }

        private void OnSafeAreaChange(Component sender, object[] arg2)
        {
            if (sender.gameObject == _rectSafe.gameObject)
            {
                Follow();
            }
        }

        private void OnDisable()
        {
            this.RemoveListener(EventID.SafeAreaChange, OnSafeAreaChange);
        }

        private void Follow()
        {
            if (!_thisRect)
            {
                return;
            }

            float scale = 1;
            if (_followX)
            {
                scale = Mathf.Min(scale, _rectSafe.rect.width / _rectFull.rect.width);
            }

            if (_followY)
            {
                scale = Mathf.Min(scale, _rectSafe.rect.height / _rectFull.rect.height);
            }

            _newScale.x = scale;
            _newScale.y = scale;
            _newScale.z = scale;
            
            if (_newScale != _thisRect.localScale)
            {
                _thisRect.localScale = _newScale;
                _thisRect.position = _rectSafe.position;
            }
        }
    }
}