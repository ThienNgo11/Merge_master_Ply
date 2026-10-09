using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Audio;

namespace GameTool.Audio.Scripts
{
    public class MixerAudioManager : MonoBehaviour
    {
        public AudioMixer masterMixer;
        string masterMixerGroupName = "Master";
        string musicMixerGroupName = "Music";
        string soundFXMixerGroupName = "SoundFX";

        public void ChangeMasterVolume(float value)
        {
            if (value <= 0)
            {
                value = 0.0001f;
            }
            else if (value >= 1f)
            {
                value = 1f;
            }

            masterMixer.SetFloat(masterMixerGroupName, Mathf.Log10(value) * 20);
        }

        public void FadeMasterVolume(float value, float duration = 1f, Action callback = null)
        {
            if (value <= 0)
            {
                value = 0.0001f;
            }
            else if (value >= 1f)
            {
                value = 1f;
            }

            masterMixer.DOSetFloat(masterMixerGroupName, Mathf.Log10(value) * 20, duration).SetEase(Ease.Linear).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }

        public void ChangeMusicVolume(float value)
        {
            if (value <= 0)
            {
                value = 0.0001f;
            }
            else if (value >= 1f)
            {
                value = 1f;
            }

            masterMixer.SetFloat(musicMixerGroupName, Mathf.Log10(value) * 20);
        }

        public void FadeMusicVolume(float value, float duration = 1f, Action callback = null)
        {
            if (value <= 0)
            {
                value = 0.0001f;
            }
            else if (value >= 1f)
            {
                value = 1f;
            }

            masterMixer.DOSetFloat(musicMixerGroupName, Mathf.Log10(value) * 20, duration).SetEase(Ease.Linear).OnComplete(() =>
            {
                callback?.Invoke();
            });
        }

        public void ChangeSFXVolume(float value)
        {
            if (value <= 0)
            {
                value = 0.0001f;
            }
            else if (value >= 1f)
            {
                value = 1f;
            }

            masterMixer.SetFloat(soundFXMixerGroupName, Mathf.Log10(value) * 20);
        }

        public float GetMusicVolumn()
        {
            float tmp;
            masterMixer.GetFloat(musicMixerGroupName, out tmp);
            Debug.Log(tmp);
            return Remap(tmp);
        }

        public float GetSoundFXVolumn()
        {
            float tmp;
            masterMixer.GetFloat(soundFXMixerGroupName, out tmp);
            Debug.Log(tmp);
            return Remap(tmp);
        }

        float Remap(float value)
        {
            return (value + 80f) * 1 / 80;
        }
    }
}