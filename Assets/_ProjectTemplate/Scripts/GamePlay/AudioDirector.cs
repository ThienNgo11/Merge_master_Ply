using System;
using System.Collections.Generic;
using GameTool.Audio.Scripts;
using GameToolSample.Audio;
using UnityEngine;
using UnityEngine.Audio;

namespace Funlab.MergeMaster
{
	public class AudioDirector : MonoBehaviour
	{
		private const string MasterVolumeOption = "OtWVUHN";

		private const float SubduedLoudness = 0.5f;

		[SerializeField]
		private AudioMixer masterBlender;

		[Range(0f, 100f)]
		[SerializeField]
		private float masterLoudness;

		[SerializeField]
		private AudioObjectController combatantEmitter;

		[SerializeField]
		private AudioObjectController repeatingEmitter;

		public List<PersonAudio> personCues;

		public List<SaurianAudio> saurianCues;

		public List<SaurianAudioKit> uiCues;

		[SerializeField]
		private eMusicName clashMusic;

		[Header("Playable audio")]
		[SerializeField] private AudioClip clashMusicClip;
		[SerializeField] private AudioClip victoryClip;

		[SerializeField]
		[Range(0f, 100f)]
		private float clashLoudness = 70f;

		[SerializeField]
		private List<eSoundName> preloadedSounds;

		private readonly Dictionary<int, Dictionary<AudioCue, eSoundName>> saurianCuesCatalog = new Dictionary<int, Dictionary<AudioCue, eSoundName>>();

		private readonly Dictionary<int, Dictionary<AudioCue, eSoundName>> personCuesCatalog = new Dictionary<int, Dictionary<AudioCue, eSoundName>>();

		private readonly float repeatUniqueGap = 0.08f;

		private readonly Dictionary<AudioClip, float> momentLimits = new Dictionary<AudioClip, float>();

		private readonly Dictionary<AudioCue, eSoundName> uiAudioCatalog = new Dictionary<AudioCue, eSoundName>();

		private readonly Dictionary<AudioCue, eSoundName> miniatureCuesCatalog = new Dictionary<AudioCue, eSoundName>();

		private readonly List<int> _sortedSaurianIdentifiers = new List<int>();

		private bool _booted;

		private bool _subscribed;

		private bool _soundtrackRequested;
		private bool _soundtrackPending;
		private bool _awaitingMusicGesture = true;

		private bool[] _audibleSounds;

		private bool _latestSoundtrackLooping = true;

		private eSoundName _repeatingSound;

		private float MasterVolume => masterLoudness / 100f;

		private void Awake()
		{
			AssembleCatalogs();
			Subscribe();
		}

		private void Start()
		{
			AssembleCatalogs();
			Subscribe();
			AdjustMasterLoudness(masterLoudness);
			PreloadSounds();
		}

		private void Update()
		{
			if (!_soundtrackRequested || !GameTuning.isSoundtrackOn) return;
			bool firstGesture = _awaitingMusicGesture && (Input.GetMouseButtonDown(0) ||
				(Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began));
			if (firstGesture) _awaitingMusicGesture = false;
			if (_soundtrackPending || firstGesture)
			{
				PerformSoundtrack(_latestSoundtrackLooping, firstGesture);
			}
		}

		private void AdjustMasterLoudness(float loudnessPercent)
		{
			if (masterBlender == null)
			{
				return;
			}
			float attenuationDb = loudnessPercent <= 0.01f ? -80f : 20f * Mathf.Log10(loudnessPercent / 100f);
			masterBlender.SetFloat(MasterVolumeOption, attenuationDb);
		}

		private void PreloadSounds()
		{
			if (!AudioManager.IsInstanceValid())
			{
				return;
			}
			AudioManager audioManager = AudioManager.Instance;
			PreloadSounds(audioManager, personCuesCatalog);
			PreloadSounds(audioManager, saurianCuesCatalog);
			foreach (eSoundName sound in uiAudioCatalog.Values)
			{
				if (IsAudible(sound))
				{
					audioManager.UpdateSound(sound);
				}
			}
			if (preloadedSounds != null)
			{
				foreach (eSoundName sound in preloadedSounds)
				{
					if (sound != eSoundName.None && IsAudible(sound))
					{
						audioManager.UpdateSound(sound);
					}
				}
			}
			if (clashMusic != eMusicName.None)
			{
				audioManager.UpdateMusic(clashMusic);
			}
		}

		private void PreloadSounds(AudioManager audioManager, Dictionary<int, Dictionary<AudioCue, eSoundName>> catalog)
		{
			foreach (Dictionary<AudioCue, eSoundName> byEffect in catalog.Values)
			{
				foreach (eSoundName sound in byEffect.Values)
				{
					if (IsAudible(sound))
					{
						audioManager.UpdateSound(sound);
					}
				}
			}
		}

		private bool IsAudible(eSoundName sound)
		{
			if (_audibleSounds == null)
			{
				int peak = 0;
				foreach (eSoundName value in (eSoundName[])Enum.GetValues(typeof(eSoundName)))
				{
					peak = Mathf.Max(peak, (int)value);
				}
				_audibleSounds = new bool[peak + 1];
				AudioTable table = Resources.Load<AudioTable>("AudioTable");
				if (table != null && table.soundTracksSerializers != null)
				{
					foreach (var serializer in table.soundTracksSerializers)
					{
						if (serializer == null || serializer.track == null || serializer.track.resourcePaths == null || serializer.track.resourcePaths.Count == 0)
						{
							continue;
						}
						if (Enum.TryParse(serializer.key, out eSoundName listed) && (int)listed >= 0 && (int)listed < _audibleSounds.Length)
						{
							_audibleSounds[(int)listed] = true;
						}
					}
				}
			}
			int ordinal = (int)sound;
			return ordinal >= 0 && ordinal < _audibleSounds.Length && _audibleSounds[ordinal];
		}

		private void OnDestroy()
		{
			if (!_subscribed)
			{
				return;
			}
			GameTuning.audioSignal.RemoveListener(AudioSignalResponder);
			_subscribed = false;
		}

		private void Subscribe()
		{
			if (_subscribed)
			{
				return;
			}
			GameTuning.audioSignal.AddListener(AudioSignalResponder);
			_subscribed = true;
		}

		private void AssembleCatalogs()
		{
			if (_booted)
			{
				return;
			}
			_booted = true;
			if (personCues != null)
			{
				foreach (PersonAudio personAudio in personCues)
				{
					if (personAudio.sound == eSoundName.None)
					{
						continue;
					}
					GetSoundSeries(personCuesCatalog, personAudio.worth)[personAudio.audioCue] = personAudio.sound;
				}
			}
			if (saurianCues != null)
			{
				foreach (SaurianAudio saurianAudio in saurianCues)
				{
					if (saurianAudio.worths == null || saurianAudio.cues == null)
					{
						continue;
					}
					foreach (int worth in saurianAudio.worths)
					{
						foreach (SaurianAudioKit kit in saurianAudio.cues)
						{
							if (kit.sound == eSoundName.None)
							{
								continue;
							}
							GetSoundSeries(saurianCuesCatalog, worth)[kit.audioCue] = kit.sound;
						}
					}
				}
			}
			_sortedSaurianIdentifiers.AddRange(saurianCuesCatalog.Keys);
			_sortedSaurianIdentifiers.Sort();
			if (uiCues != null)
			{
				foreach (SaurianAudioKit uiKit in uiCues)
				{
					if (uiKit.sound == eSoundName.None || uiKit.audioCue == AudioCue.Unset)
					{
						continue;
					}
					uiAudioCatalog[uiKit.audioCue] = uiKit.sound;
					if (uiKit.audioCue == AudioCue.MiniatureAssemble || uiKit.audioCue == AudioCue.MiniatureCheckpointUnsealed || uiKit.audioCue == AudioCue.MiniatureFinished || uiKit.audioCue == AudioCue.RouletteSpin)
					{
						miniatureCuesCatalog[uiKit.audioCue] = uiKit.sound;
					}
				}
			}
		}

		private static Dictionary<AudioCue, eSoundName> GetSoundSeries(Dictionary<int, Dictionary<AudioCue, eSoundName>> catalog, int worth)
		{
			if (!catalog.TryGetValue(worth, out Dictionary<AudioCue, eSoundName> byEffect))
			{
				byEffect = new Dictionary<AudioCue, eSoundName>();
				catalog[worth] = byEffect;
			}
			return byEffect;
		}

		private void AudioSignalResponder(ActorKind kind, AudioCue audioCue, int worth)
		{
			if (!GameTuning.isAudioOn)
			{
				return;
			}
			switch (kind)
			{
				case ActorKind.Person:
					PerformArbitrary(ResolvePersonSound(audioCue, worth), audioCue == AudioCue.ReleaseBowstring ? SubduedLoudness : 1f);
					break;
				case ActorKind.Saurian:
				case ActorKind.Overlord:
					PerformArbitrary(ResolveSaurianSound(audioCue, worth), audioCue == AudioCue.Impact ? SubduedLoudness : 1f);
					break;
				default:
					PerformUICue(audioCue);
					break;
			}
		}

		private eSoundName ResolvePersonSound(AudioCue audioCue, int worth)
		{
			if (personCuesCatalog.TryGetValue(worth, out Dictionary<AudioCue, eSoundName> byWorth) && byWorth.TryGetValue(audioCue, out eSoundName sound))
			{
				return sound;
			}
			if (personCuesCatalog.TryGetValue(0, out Dictionary<AudioCue, eSoundName> general) && general.TryGetValue(audioCue, out sound))
			{
				return sound;
			}
			return eSoundName.None;
		}

		private eSoundName ResolveSaurianSound(AudioCue audioCue, int worth)
		{
			if (saurianCuesCatalog.TryGetValue(worth, out Dictionary<AudioCue, eSoundName> byWorth) && byWorth.TryGetValue(audioCue, out eSoundName sound))
			{
				return sound;
			}
			eSoundName beneath = eSoundName.None;
			eSoundName tiniest = eSoundName.None;
			foreach (int entry in _sortedSaurianIdentifiers)
			{
				if (!saurianCuesCatalog[entry].TryGetValue(audioCue, out sound))
				{
					continue;
				}
				if (tiniest == eSoundName.None)
				{
					tiniest = sound;
				}
				if (entry <= worth)
				{
					beneath = sound;
				}
			}
			return beneath != eSoundName.None ? beneath : tiniest;
		}

		private void PerformArbitrary(eSoundName sound, float loudness)
		{
			if (sound == eSoundName.None)
			{
				return;
			}
			Perform(sound, true, combatantEmitter, loudness);
		}

		public void PlayClashMusic()
		{
			_latestSoundtrackLooping = true;
			_soundtrackRequested = true;
			if (GameTuning.isSoundtrackOn)
			{
				PerformSoundtrack(true);
			}
		}

		public void ResumeMusicAfterToggle()
		{
			if (GameTuning.isSoundtrackOn && _soundtrackRequested)
			{
				PerformSoundtrack(_latestSoundtrackLooping);
			}
		}

		public void HaltMusic()
		{
			_soundtrackPending = false;
			HaltSoundtrack();
		}

		public void PerformUICue(AudioCue cue)
		{
			AssembleCatalogs();
			if (GameTuning.isAudioOn && uiAudioCatalog.TryGetValue(cue, out eSoundName sound))
			{
				Perform(sound, false, null);
			}
		}

		public void PerformMiniatureCue(AudioCue cue, bool repeat)
		{
			AssembleCatalogs();
			if (!GameTuning.isAudioOn || !miniatureCuesCatalog.TryGetValue(cue, out eSoundName sound))
			{
				return;
			}
			if (!repeat)
			{
				Perform(sound, false, null);
				return;
			}
			if (repeatingEmitter == null || !IsAudible(sound) || (repeatingEmitter.IsPlaying && _repeatingSound == sound))
			{
				return;
			}
			_repeatingSound = sound;
			repeatingEmitter.ShotAudioClip(sound, -1, MasterVolume * (cue == AudioCue.RouletteSpin ? SubduedLoudness : 1f));
		}

		public void HaltMiniatureCue(AudioCue cue)
		{
			if (repeatingEmitter != null && miniatureCuesCatalog.TryGetValue(cue, out eSoundName sound) && _repeatingSound == sound)
			{
				repeatingEmitter.StopAudioClip();
				_repeatingSound = eSoundName.None;
			}
		}

		private void PerformSoundtrack(bool isRepeating, bool restart = false)
		{
			AssembleCatalogs();
			if (!AudioPlayer.IsInstanceValid() || !AudioManager.IsInstanceValid())
			{
				_soundtrackPending = true;
				return;
			}
			AudioSource musicSpeaker = AudioPlayer.Instance.musicSpeaker;
			AudioClip clip = clashMusicClip != null ? clashMusicClip :
				(clashMusic != eMusicName.None ? AudioManager.Instance.GetAudioClip(clashMusic) : null);
			if (musicSpeaker == null || clip == null)
			{
				_soundtrackPending = false;
				Debug.LogWarning("Playable music source or clip is missing.");
				return;
			}
			_soundtrackPending = false;
			musicSpeaker.volume = clashLoudness / 100f * MasterVolume;
			if (!restart && musicSpeaker.isPlaying && musicSpeaker.clip == clip)
			{
				return;
			}
			musicSpeaker.Stop();
			musicSpeaker.clip = clip;
			musicSpeaker.loop = isRepeating;
			musicSpeaker.Play();
		}

		public void PlayVictorySound()
		{
			if (!GameTuning.isAudioOn) return;
			// Use the same source as combat effects, which is initialized with this scene.
			if (victoryClip != null && combatantEmitter != null && combatantEmitter.CanControlAudioSource())
			{
				combatantEmitter.AudioSource.PlayOneShot(victoryClip, MasterVolume);
				return;
			}
			PerformUISample(eSoundName.FL_Win);
		}

		private void HaltSoundtrack()
		{
			if (AudioManager.IsInstanceValid())
			{
				AudioManager.Instance.StopMusic();
			}
		}

		public void PerformUISample(eSoundName sample)
		{
			if (GameTuning.isAudioOn && sample != eSoundName.None)
			{
				Perform(sample, false, null);
			}
		}

		private void Perform(eSoundName sound, bool verifyMomentLimit, AudioObjectController emitter, float loudness = 1f)
		{
			if (!AudioManager.IsInstanceValid() || !IsAudible(sound))
			{
				return;
			}
			AudioManager audioManager = AudioManager.Instance;
			AudioClip sample = audioManager.GetAudioClip(sound);
			if (sample == null)
			{
				return;
			}
			if (verifyMomentLimit)
			{
				float presentMoment = Time.unscaledTime;
				if (momentLimits.TryGetValue(sample, out float blockedUntil) && presentMoment < blockedUntil)
				{
					return;
				}
				momentLimits[sample] = presentMoment + repeatUniqueGap;
			}
			if (emitter != null)
			{
				if (loudness < 1f && emitter.CanControlAudioSource())
				{
					emitter.AudioSource.PlayOneShot(sample, loudness);
					return;
				}
				emitter.ShotAudioClipNoAdd(sample);
				return;
			}
			audioManager.Shot(sample, MasterVolume);
		}
	}
}
