using System;
using System.Collections;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Scripts.Enum;
using UnityEngine;

namespace DatdevUlts.UI_Utility
{
    public class BgFit : MonoBehaviour
    {
        [SerializeField] private RectTransform _rect;
        [SerializeField] private RectTransform _rectFit;
        [SerializeField] private Vector2 _matchSize = new Vector2(1080, 1920);

        private void OnDrawGizmos()
        {
            Check();
        }

        private void OnEnable()
        {
            this.RegisterListener(EventID.ChangeCamera, Callback);
            Check();
        }

        private void OnDisable()
        {
            this.RemoveListener(EventID.ChangeCamera, Callback);
        }

        private void Callback(Component arg1, object[] arg2)
        {
            StartCoroutine(Wait());

            IEnumerator Wait()
            {
                yield return null;
                yield return null;
                Check();
            }
        }

        [ContextMenu("Check")]
        public void Check()
        {
            if (!_rect)
            {
                _rect = GetComponent<RectTransform>();
            }

            if (_rect)
            {
                _rect.anchorMin = Vector2.one / 2;
                _rect.anchorMax = Vector2.one / 2;
                _rect.pivot = Vector2.one / 2;
                _rect.sizeDelta = _matchSize;

                _rect.localScale = Vector3.one;
                
                if (_rectFit)
                {
                    if (_rect.sizeDelta.x < _rectFit.rect.width)
                    {
                        _rect.localScale = Vector3.one * (_rectFit.rect.width / _rect.sizeDelta.x);
                    }

                    if (_rect.sizeDelta.y < _rectFit.rect.height)
                    {
                        _rect.localScale = Vector3.one * (_rectFit.rect.height / _rect.sizeDelta.y);
                    }
                }
            }
        }
    }
}