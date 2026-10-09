using DG.Tweening;
using GameTool.Audio.Scripts;
using GameToolSample.Audio;
using LunaTool;
using UnityEngine;
using UnityEngine.EventSystems;

namespace GameTool.UI.Scripts.ItemUI
{
    public class ButtonScaler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        public Vector3 startScale;
        public Vector3 endScale;
        public bool useAudio = true;
        public bool useScaleEffect = true;
        public AudioClip clip;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Application.isPlaying) return;
            if (useScaleEffect)
            {
                startScale = transform.localScale;
                endScale = new Vector3(startScale.x - 0.05f, startScale.y - 0.05f, startScale.z);
            }
            else
            {
                endScale = transform.localScale;
            }
        }
#endif
        public void OnPointerDown(PointerEventData eventData)
        {
            if (useScaleEffect)
            {
                transform.DOScale(endScale, 0.05f);
            }

            if (useAudio)
            {
                if (AudioManager.IsInstanceValid())
                {
                    if (!clip)
                    {
                        AudioManager.Instance.Shot(eSoundName.ButtonClick);
                    }
                    else
                    {
                        AudioManager.Instance.Shot(clip);
                    }
                }
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (useScaleEffect)
            {
                transform.DOScale(startScale, 0.1f);
            }
        }

        private void OnEnable()
        {
            if (useScaleEffect)
            {
                transform.localScale = startScale;
            }
        }
    }
}