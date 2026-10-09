using GameTool.Assistants.DesignPattern;
using UnityEngine;

namespace GameTool.Audio.Scripts
{
    public class AudioPlayer : SingletonMonoBehaviour<AudioPlayer>
    {
        public MixerAudioManager mixerAudioManager;

        public AudioSource musicSpeaker;
        public AudioSource soundSpeaker;
    }
}