using System;
using System.Collections.Generic;
using System.Linq;
using GameToolSample.Audio;
using UnityEditor;
using UnityEngine;

namespace GameTool.Audio.Scripts
{
    [CreateAssetMenu(fileName = "AudioAsset", menuName = "ScriptableObject/AudioAsset", order = 0)]
    public class AudioAsset : ScriptableObject
    {
        public List<MusicAssetItem> musicAsset;
        public List<SoundAssetItem> soundAsset;
        
#if UNITY_EDITOR
        [ContextMenu("Re Update")]
        public void OnValidate()
        {
            var table = Resources.Load<AudioTable>("AudioTable");
            for (int i = 0; i < musicAsset.Count; i++)
            {
                try
                {
                    musicAsset[i].listAudio = table.musicTracksSerializers
                        .Find(music => music.key == musicAsset[i].key.ToString()).track.listAudio.ToList();
                }
                catch
                {
                    musicAsset[i].listAudio.Clear();
                }
                
            }
            
            for (int i = 0; i < soundAsset.Count; i++)
            {
                try
                {
                    soundAsset[i].listAudio = table.soundTracksSerializers
                        .Find(sound => sound.key == soundAsset[i].key.ToString()).track.listAudio.ToList();
                }
                catch
                {
                    soundAsset[i].listAudio.Clear();
                }
            }
            EditorUtility.SetDirty(this);
        }
#endif
    }

    [Serializable]
    public class MusicAssetItem
    {
        public eMusicName key;
        public List<AudioClip> listAudio;
    }

    [Serializable]
    public class SoundAssetItem
    {
        public eSoundName key;
        public List<AudioClip> listAudio;
    }
}