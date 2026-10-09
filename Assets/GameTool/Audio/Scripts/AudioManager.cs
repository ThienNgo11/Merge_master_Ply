using System.Collections.Generic;
using DatdevUlts.Ults;
using GameTool.Assistants.DesignPattern;
using GameToolSample.Audio;
using UnityEngine;

namespace GameTool.Audio.Scripts
{
    public class AudioManager : SingletonMonoBehaviour<AudioManager>
    {
        private MixerAudioManager mixerAudioManager => AudioPlayer.Instance.mixerAudioManager;

        private AudioSource musicSpeaker => AudioPlayer.Instance.musicSpeaker;
        private AudioSource soundSpeaker => AudioPlayer.Instance.soundSpeaker;

        [SerializeField] public AudioAsset Serializers;

        private readonly Dictionary<eMusicName, List<AudioClip>> MusicTracks =
            new Dictionary<eMusicName, List<AudioClip>>();

        private readonly Dictionary<eSoundName, List<AudioClip>> SoundTracks =
            new Dictionary<eSoundName, List<AudioClip>>();

        protected override void Awake()
        {
            base.Awake();
            UpdateKey();
        }

        private void Start()
        {
            ChangeMasterVolume(1);
            ChangeMusicVolume(1);
            ChangeSFXVolume(1);
        }


        private void UpdateKey()
        {
            MusicTracks.Clear();
            SoundTracks.Clear();
            if (Serializers)
            {
                foreach (var serializer in Serializers.musicAsset)
                {
                    if (!MusicTracks.ContainsKey(serializer.key))
                    {
                        MusicTracks.Add(serializer.key, serializer.listAudio);
                    }
                }

                foreach (var serializer in Serializers.soundAsset)
                {
                    if (!SoundTracks.ContainsKey(serializer.key))
                    {
                        SoundTracks.Add(serializer.key, serializer.listAudio);
                    }
                }
            }
        }

        public void PlayMusic(eMusicName filename, bool loop = true)
        {
            UpdateMusic(filename);
            musicSpeaker.clip = (MusicTracks[filename].GetRandom());
            musicSpeaker.Play();
            musicSpeaker.loop = loop;
        }

        public bool IsSameMusicClip(AudioClip clip)
        {
            if (musicSpeaker.clip != null)
            {
                return musicSpeaker.clip == clip;
            }

            return false;
        }

        public void Shot(eSoundName filename, float volume = 1)
        {
            UpdateSound(filename);
            soundSpeaker.PlayOneShot(SoundTracks[filename].GetRandom(), volume);
        }

        public void UpdateSound(eSoundName filename)
        {
            if (!SoundTracks.ContainsKey(filename))
            {
                var table = Resources.Load<AudioTable>("AudioTable");
                var list = table.soundTracksSerializers.Find(sound => sound.key == filename.ToString());
                List<AudioClip> listAudio = new List<AudioClip>();
                for (int i = 0; i < list.track.resourcePaths.Count; i++)
                {
                    listAudio.Add(Resources.Load<AudioClip>(list.track.resourcePaths[i]));
                }
                SoundTracks.Add(filename, listAudio);
            }
        }

        public void UpdateMusic(eMusicName filename)
        {
            if (!MusicTracks.ContainsKey(filename))
            {
                var table = Resources.Load<AudioTable>("AudioTable");
                var list = table.musicTracksSerializers.Find(music => music.key == filename.ToString());
                List<AudioClip> listAudio = new List<AudioClip>();
                for (int i = 0; i < list.track.resourcePaths.Count; i++)
                {
                    listAudio.Add(Resources.Load<AudioClip>(list.track.resourcePaths[i]));
                }
                MusicTracks.Add(filename, listAudio);
            }
        }

        public void Shot(AudioClip clip, float volume = 1)
        {
            soundSpeaker.PlayOneShot(clip, volume);
        }

        public void ShotWithIndex(eSoundName filename, int index = 0, float volume = 1f)
        {
            UpdateSound(filename);
            soundSpeaker.PlayOneShot(SoundTracks[filename][index], volume);
        }

        public void Fade(float volume, float duration = 1f)
        {
            mixerAudioManager.FadeMusicVolume(volume, duration);
        }

        public void PauseMusic()
        {
            musicSpeaker.Pause();
        }

        public void StopMusic()
        {
            musicSpeaker.Stop();
        }

        public void ResumeMusic()
        {
            musicSpeaker.UnPause();
        }

        public void StopSFX()
        {
            soundSpeaker.Stop();
        }

        public AudioClip GetAudioClip(eMusicName s, int index = -1)
        {
            UpdateMusic(s);
            return index < 0 ? MusicTracks[s].GetRandom() : MusicTracks[s][index];
        }

        public AudioClip GetAudioClip(eSoundName s, int index = -1)
        {
            UpdateSound(s);
            return index < 0 ? SoundTracks[s].GetRandom() : SoundTracks[s][index];
        }
      
        public float LengthOfAudioClip(eMusicName s, int index = 0)
        {
            return GetAudioClip(s, index).length;
        }

        public float LengthOfAudioClip(eSoundName s, int index = 0)
        {
            return GetAudioClip(s, index).length;
        }

        public void Mute(bool value)
        {
            SetMasterVolume(value ? 0f : 1f);
        }

        public void SetMasterVolume(float value)
        {
            ChangeMasterVolume(value);
        }

        private void ChangeMasterVolume(float value)
        {
            mixerAudioManager.ChangeMasterVolume(value);
        }

        public void SetMusic(bool value)
        {
            SetMusicVolume(value ? 1f : 0f);
        }

        public void SetMusicVolume(float value)
        {
            ChangeMusicVolume(value);
        }

        private void ChangeMusicVolume(float value)
        {
            mixerAudioManager.ChangeMusicVolume(value);
        }

        public void SetSFX(bool value)
        {
            SetSFXVolume(value ? 1f : 0f);
        }

        public void SetSFXVolume(float value)
        {
            ChangeSFXVolume(value);
        }

        private void ChangeSFXVolume(float value)
        {
            mixerAudioManager.ChangeSFXVolume(value);
        }

        public void ChangePitch(float pitch)
        {
            musicSpeaker.pitch = pitch;
        }
    }
}