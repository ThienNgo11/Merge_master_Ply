using UnityEngine;
using System;
using DG.Tweening;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Audio;
using GameToolSample.Scripts.Enum;

namespace GameTool.Audio.Scripts
{
    public class AudioObjectController : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;

        public AudioSource AudioSource
        {
            get
            {
                if (!HaveAudioSource()) return null;
                return audioSource;
            }
            set { audioSource = value; }
        }

        private void Start()
        {
            this.RegisterListener(EventID.PauseGame, PauseGameListener);
            this.RegisterListener(EventID.ContinueGame, ContinueGameListener);
            this.RegisterListener(EventID.Mute, MuteEventRegisterListener);
            this.RegisterListener(EventID.Unmute, UnmuteEventRegisterListener);

            CheckAudioSourceStart();
        }

        private void OnDestroy()
        {
            this.RemoveListener(EventID.PauseGame, PauseGameListener);
            this.RemoveListener(EventID.ContinueGame, ContinueGameListener);
            this.RemoveListener(EventID.Mute, MuteEventRegisterListener);
            this.RemoveListener(EventID.Unmute, UnmuteEventRegisterListener);
        }

        /// <summary>
        /// Xử lý sự kiện khi tạm dừng trò chơi.
        /// </summary>
        void PauseGameListener(Component o, object[] sender = null)
        {
            Pause();
        }

        /// <summary>
        /// Xử lý sự kiện khi tiếp tục trò chơi.
        /// </summary>
        void ContinueGameListener(Component o, object[] sender = null)
        {
            UnPause();
        }

        /// <summary>
        /// Xử lý sự kiện tắt âm.
        /// </summary>
        void MuteEventRegisterListener(Component component, object[] obj = null)
        {
            Mute = true;
        }

        /// <summary>
        /// Xử lý sự kiện bật âm.
        /// </summary>
        void UnmuteEventRegisterListener(Component component, object[] obj = null)
        {
            Mute = false;
        }

        /// <summary>
        /// Kiểm tra thành phần audioSource khi bắt đầu trò chơi.
        /// </summary>
        void CheckAudioSourceStart()
        {
            if (!audioSource)
            {
                if (gameObject.TryGetComponent(out AudioSource audio))
                {
                    audioSource = audio;
                }
                else
                {
                    Debug.LogError("AudioSource is Null");
                }
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh với âm lượng và bằng cách gán vào audioSource.clip.
        /// Khuyến cáo không nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        /// <param name="volume">Giá trị âm lượng</param>
        public void ShotAudioClip(eSoundName name, int index = -1, float volume = 1f)
        {
            if (!CanControlAudioSource()) return;

            AudioClip clip = AudioManager.Instance.GetAudioClip(name, index);

            if (clip != null)
            {
                audioSource.volume = volume;
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh với âm lượng và bằng cách gán vào audioSource.clip.
        /// Khuyến cáo không nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        /// <param name="volume">Giá trị âm lượng</param>
        public void ShotAudioClip(eMusicName name, int index = -1, float volume = 1f)
        {
            if (!CanControlAudioSource()) return;

            AudioClip clip = AudioManager.Instance.GetAudioClip(name, index);

            if (clip != null)
            {
                audioSource.volume = volume;
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh với âm lượng và bằng cách gán vào audioSource.clip.
        /// Khuyến cáo không nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="clip">Âm thanh</param>
        /// <param name="volume">Giá trị âm lượng</param>
        public void ShotAudioClip(AudioClip clip, float volume = 1f)
        {
            if (!CanControlAudioSource()) return;

            if (clip != null)
            {
                audioSource.volume = volume;
                audioSource.clip = clip;
                audioSource.Play();
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh nhưng không gán vào audioSource.clip.
        /// Nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        public void ShotAudioClipNoAdd(eSoundName name, int index = -1)
        {
            if (!CanControlAudioSource()) return;

            AudioClip clip = AudioManager.Instance.GetAudioClip(name, index);

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh nhưng không gán vào audioSource.clip.
        /// Nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        public void ShotAudioClipNoAdd(eMusicName name, int index = -1)
        {
            if (!CanControlAudioSource()) return;

            AudioClip clip = AudioManager.Instance.GetAudioClip(name, index);

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Chơi một đoạn âm thanh nhưng không gán vào audioSource.clip.
        /// Nên sử dụng với những âm thanh sử dụng nhiều, liên tục với số lượng lớn như tiếng bước chân, tiếng súng bắn...
        /// </summary>
        /// <param name="clip">Âm thanh</param>
        public void ShotAudioClipNoAdd(AudioClip clip)
        {
            if (!CanControlAudioSource()) return;

            if (clip != null)
            {
                audioSource.PlayOneShot(clip);
            }
        }

        /// <summary>
        /// Xoá bỏ đoạn âm thanh đang nằm trong audioSource.clip.
        /// </summary>
        public void ClearAudioClip()
        {
            if (!HaveAudioSource()) return;
            audioSource.clip = null;
        }

        /// <summary>
        /// Dừng toàn bộ âm thanh đang chơi.
        /// </summary>
        public void StopAudioClip()
        {
            if (!HaveAudioSource()) return;
            audioSource.Stop();
        }

        /// <summary>
        /// Chơi đoạn âm thanh.
        /// </summary>
        public void Play()
        {
            if (!HaveAudioSource()) return;
            audioSource.Play();
        }

        /// <summary>
        /// Tạm dừng audio
        /// </summary>
        public void Pause()
        {
            if (!HaveAudioSource()) return;
            audioSource.Pause();
        }

        /// <summary>
        /// Tiếp tục audio
        /// </summary>
        public void UnPause()
        {
            if (!HaveAudioSource()) return;
            audioSource.UnPause();
        }

        /// <summary>
        /// Kiểm tra tắt bật im lặng
        /// </summary>
        public bool Mute
        {
            get => HaveAudioSource() ? audioSource.mute : false;
            set
            {
                if (!HaveAudioSource()) return;
                audioSource.mute = value;
            }
        }

        /// <summary>
        /// Kiểm tra có đang chạy âm thanh hay không
        /// </summary>
        public bool IsPlaying
        {
            get => HaveAudioSource() ? audioSource.isPlaying : false;
        }

        /// <summary>
        /// Kiểm tra Loop.
        /// </summary>
        public bool Loop
        {
            get => HaveAudioSource() ? audioSource.loop : false;
            set
            {
                if (!HaveAudioSource()) return;
                audioSource.loop = value;
            }
        }

        /// <summary>
        /// Kiểm tra Volume.
        /// </summary>
        public float Volume
        {
            get => HaveAudioSource() ? audioSource.volume : 0f;
            set { audioSource.volume = value; }
        }

        /// <summary>
        /// Kiểm tra giá trị hiệu ứng âm thanh không gian (2D hay 3D).
        /// </summary>
        public float SpatialBlend
        {
            get => HaveAudioSource() ? audioSource.spatialBlend : 0f;
            set { audioSource.spatialBlend = Mathf.Clamp(value, 0f, 1f); }
        }

        /// <summary>
        /// Hiệu ứng nhỏ dần hoặc lớn dần âm lượng.
        /// </summary>
        /// <param name="volume">Giá trị âm lượng cần đạt tới</param>
        /// <param name="fadeSpeed">Tốc độ</param>
        /// <param name="callback">Hành động khi hiệu ứng hoàn thành</param>
        public void FadeVolume(float volume, float fadeSpeed = 1f, Action callback = null)
        {
            if (!HaveAudioSource()) return;
            audioSource.DOKill();
            float value = Mathf.Clamp(volume, 0f, 1f);
            audioSource.DOFade(value, fadeSpeed).SetEase(Ease.Linear).OnComplete(() => { callback?.Invoke(); });
        }

        /// <summary>
        /// Kiểm tra xem có thể điều khiển được audioSource không.
        /// </summary>
        public bool CanControlAudioSource()
        {
            if (!HaveAudioSource())
            {
                return false;
            }
            else
            {
                if (!(audioSource.enabled && audioSource.gameObject.activeSelf))
                {
                    Debug.LogError("AudioSource is Disable");
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Kiểm tra xem audioSource có tồn tại hay không.
        /// </summary>
        public bool HaveAudioSource()
        {
            if (audioSource == null)
            {
                Debug.LogError("AudioSource is Null");
            }

            return audioSource != null;
        }

        /// <summary>
        /// Trả về độ dài của đoạn âm thanh trong AudioManager.
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        public float LengthOfClip(eSoundName name, int index = 0)
        {
            return AudioManager.Instance.GetAudioClip(name, index).length;
        }

        /// <summary>
        /// Trả về độ dài của đoạn âm thanh trong AudioManager.
        /// </summary>
        /// <param name="name">Tên của âm thanh (enum)</param>
        public float LengthOfClip(eMusicName name, int index = 0)
        {
            return AudioManager.Instance.GetAudioClip(name, index).length;
        }
    }
}