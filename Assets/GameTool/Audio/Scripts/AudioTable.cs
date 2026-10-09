using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameTool.Audio.Scripts
{
    [CreateAssetMenu(fileName = "AudioTable", menuName = "ScriptableObject/AudioTable", order = 0)]
    public class AudioTable : ScriptableObject
    {
        public List<SerializerMusic> musicTracksSerializers = new List<SerializerMusic>();
        public List<SerializerSound> soundTracksSerializers = new List<SerializerSound>();

#if UNITY_EDITOR
        public void OnValidate()
        {
            var dict = new Dictionary<string, SerializerMusic>();
            var dictSound = new Dictionary<string, SerializerSound>();

            for (int i = 0; i < musicTracksSerializers.Count; i++)
            {
                var musicTracksSerializer = musicTracksSerializers[i];
                musicTracksSerializer.isDuplicated = false;

                var key = musicTracksSerializer.key;
                #if UNITY_2021_1_OR_NEWER
                if (!dict.TryAdd(key, musicTracksSerializer))
                {
                    dict[key].isDuplicated = true;
                    musicTracksSerializer.isDuplicated = true;
                }
                #else
                if (!dict.ContainsKey(key))
                {
                    dict.Add(key, musicTracksSerializer);
                    dict[key].isDuplicated = false;
                    musicTracksSerializer.isDuplicated = false;
                }
                else
                {
                    dict[key].isDuplicated = true;
                    musicTracksSerializer.isDuplicated = true;
                }
                #endif

                var trackAudio = musicTracksSerializer.track;
                trackAudio.resourcePaths.Clear();
                for (int j = 0; j < trackAudio.listAudio.Count; j++)
                {
                    trackAudio.resourcePaths
                        .Add(GetResPath(trackAudio.listAudio[j]));
                }
            }

            for (int i = 0; i < soundTracksSerializers.Count; i++)
            {
                var soundTracksSerializer = soundTracksSerializers[i];
                soundTracksSerializer.isDuplicated = false;

                var key = soundTracksSerializer.key;
                #if UNITY_2021_1_OR_NEWER
                if (!dictSound.TryAdd(key, soundTracksSerializer))
                {
                    dictSound[key].isDuplicated = true;
                    soundTracksSerializer.isDuplicated = true;
                }
                #else
                if (!dictSound.ContainsKey(key))
                {
                    dictSound.Add(key, soundTracksSerializer);
                    dictSound[key].isDuplicated = false;
                    soundTracksSerializer.isDuplicated = false;
                }
                else
                {
                    dictSound[key].isDuplicated = true;
                    soundTracksSerializer.isDuplicated = true;
                }
                #endif

                var trackAudio = soundTracksSerializer.track;
                trackAudio.resourcePaths.Clear();
                for (int j = 0; j < trackAudio.listAudio.Count; j++)
                {
                    trackAudio.resourcePaths
                        .Add(GetResPath(trackAudio.listAudio[j]));
                }
            }

            EditorUtility.SetDirty(this);
        }

        public string GetResPath(AudioClip clip)
        {
            var str = AssetDatabase.GetAssetPath(clip);
            var index = str.LastIndexOf("Resources", StringComparison.Ordinal);
            if (index >= 0)
            {
                str = str.Substring(index);
                str = str.Remove(0, "Resources/".Length);

                index = str.LastIndexOf(".", StringComparison.Ordinal);
                str = str.Remove(index);

                return str;
            }

            return "";
        }

        public void Improve()
        {
            for (int i = 0; i < musicTracksSerializers.Count; i++)
            {
                for (int j = 0; j < musicTracksSerializers[i].track.listAudio.Count; j++)
                {
                    var audio = musicTracksSerializers[i].track.listAudio[j];
                    string path = AssetDatabase.GetAssetPath(audio);
                    ImproveSetting(path, audio, true, false);
                }
            }

            for (int i = 0; i < soundTracksSerializers.Count; i++)
            {
                for (int j = 0; j < soundTracksSerializers[i].track.listAudio.Count; j++)
                {
                    var audio = soundTracksSerializers[i].track.listAudio[j];
                    string path = AssetDatabase.GetAssetPath(audio);
                    ImproveSetting(path, audio, false, soundTracksSerializers[i].isFrequently);
                }
            }

            EditorUtility.SetDirty(this);
            AssetDatabase.Refresh();
            AssetDatabase.SaveAssets();
        }

        public static void ImproveSetting(string filePath, AudioClip audio, bool isMusic, bool isFrequently)
        {
            AudioImporter importer = AssetImporter.GetAtPath(filePath) as AudioImporter;
            var lengLong = 15;
            if (importer)
            {
                importer.forceToMono = true;
                if (isFrequently && !isMusic && audio.length < lengLong)
                {
                    importer.loadInBackground = true;
                    #if !UNITY_2022_1_OR_NEWER
                    importer.preloadAudioData = true;
                    #endif

                    importer.SetOverrideSampleSettings("Android", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.DecompressOnLoad,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate, 
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("iOS", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.DecompressOnLoad,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("Standalone", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.DecompressOnLoad,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });
                }
                else if (!isFrequently && !isMusic && audio.length < lengLong)
                {
                    importer.loadInBackground = false;

                    importer.SetOverrideSampleSettings("Android", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("iOS", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("Standalone", new AudioImporterSampleSettings()
                    {
                        loadType = AudioClipLoadType.CompressedInMemory,
                        compressionFormat = AudioCompressionFormat.ADPCM,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });
                }
                else
                {
                    importer.loadInBackground = false;

                    importer.SetOverrideSampleSettings("Android", new AudioImporterSampleSettings()
                    {
#if UNITY_WEBGL
                        loadType = AudioClipLoadType.CompressedInMemory,
#else
                        loadType = AudioClipLoadType.Streaming,
#endif
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = audio.length > lengLong ? 0.5f : 1f,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("iOS", new AudioImporterSampleSettings()
                    {
#if UNITY_WEBGL
                        loadType = AudioClipLoadType.CompressedInMemory,
#else
                        loadType = AudioClipLoadType.Streaming,
#endif
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = audio.length > lengLong ? 0.5f : 1f,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });

                    importer.SetOverrideSampleSettings("Standalone", new AudioImporterSampleSettings()
                    {
#if UNITY_WEBGL
                        loadType = AudioClipLoadType.CompressedInMemory,
#else
                        loadType = AudioClipLoadType.Streaming,
#endif
                        compressionFormat = AudioCompressionFormat.Vorbis,
                        quality = audio.length > lengLong ? 0.5f : 1f,
                        sampleRateSetting = AudioSampleRateSetting.PreserveSampleRate,
                        #if UNITY_2022_1_OR_NEWER
                        preloadAudioData = true
                        #endif
                    });
                }

                Debug.Log(filePath);
                importer.SaveAndReimport();
            }
        }
#endif
    }

    [Serializable]
    public class TrackAudio
    {
#if UNITY_EDITOR
        public List<AudioClip> listAudio;
#endif
        public List<string> resourcePaths;
    }

    [Serializable]
    public class SerializerMusic
    {
        public string key;
        public TrackAudio track;
#if UNITY_EDITOR
        [HideInInspector] public bool isDuplicated;
#endif
    }

    [Serializable]
    public class SerializerSound
    {
        public string key;
        public TrackAudio track;

        [Tooltip(
            @"Âm thanh được phát với số lượng lớn (ví dụ: âm thanh vũ khí, tiếng bước chân, âm thanh va chạm, v.v.). Hoạt động tốt nhất với các cài đặt sau (cũng phù hợp với âm thanh ngắn dưới 10 giây)")]
        public bool isFrequently;
#if UNITY_EDITOR
        [HideInInspector] public bool isDuplicated;
#endif
    }
}