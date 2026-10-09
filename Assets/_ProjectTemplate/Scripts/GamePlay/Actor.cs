using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Funlab.MergeMaster
{
	public class Actor : MotionSignalResponder
	{
		private const string SignalFunctionTitle = "SignalResponder";

		private const float CrossDissolveSpan = 0.1f;

		private static readonly string[] AssaultStatuses = { "Attack", "Attack2", "Attack3" };

		private static readonly int RestingHash = Animator.StringToHash("Idle");

		private static readonly int CelebrateHash = Animator.StringToHash("Dance");

		// Human attack clips: absolute "Shoot" times (all clips are 1 s long).
		private const float PersonShootMoment = 0.633f;
		private const float PersonShootMoment2 = 0.533f;
		private const float PersonShootMoment3 = 0.483f;
		private const float WizardShootMoment = 0.667f;
		private const float DrawBowstringMoment = 0.1f;

		// Dino attack clips: "Hit" time as a fraction of the clip length (keyed by clip-name prefix).
		private static readonly KeyValuePair<string, float>[] SaurianImpactPortions =
		{
			// Measured on the original (first bite after contact): Vraptorman 0.38 s of 1.317 s, Triceramon 0.355 s of
			// 1.083 s, Ankylosaurus ~0.31 s of 0.967 s; higher tiers not measured, the same ~0.31 fraction is assumed.
			new KeyValuePair<string, float>("KL01", 0.29f),
			new KeyValuePair<string, float>("KL02", 0.33f),
			new KeyValuePair<string, float>("KL03", 0.31f),
			new KeyValuePair<string, float>("KL04", 0.32f),
			new KeyValuePair<string, float>("KL05", 0.31f),
			new KeyValuePair<string, float>("KL06", 0.31f),
			new KeyValuePair<string, float>("KL07", 0.31f),
			new KeyValuePair<string, float>("KL08", 0.31f),
			new KeyValuePair<string, float>("KL09", 0.31f),
			new KeyValuePair<string, float>("KL10", 0.31f),
			new KeyValuePair<string, float>("KL11", 0.31f),
			new KeyValuePair<string, float>("KLBoss", 0.33f)
		};

		public int lane;

		public int line;

		[Header("External")]
		public GameplayCore gameplayCore;

		[Header("Internal")]
		public Transform core;

		public ActorFaction faction;

		public ActorKind kind;

		public Transform projectileBeginMark;

		protected Transform vitalityMeterLocation;

		protected GameObject actor;

		protected SphereCollider sphereHitbox;

		private int _worth;

		public Animator motionRig;

		protected Animator coreMotionRig;

		public GameObject meshEntity;

		protected GameObject dart;

		private ActorStatus _actorStatus;

		private int assaultSampleTally;

		private bool didAssaultBefore;

		protected bool hasHoldMotion;

		private string _activeAssaultStatus = "Attack";

		public int worth
		{
			get
			{
				return _worth;
			}
			set
			{
				_worth = value;
				AssembleActor();
			}
		}

		public ActorStatus actorStatus
		{
			get
			{
				return _actorStatus;
			}
			set
			{
				if (_actorStatus == value)
				{
					return;
				}
				_actorStatus = value;
				ImposeStatus(true);
			}
		}

		internal static float MotionPaceScalar
		{
			get
			{
				float pace = GameTuning.actorMotionPace;
				return pace > 0f ? pace : 1f;
			}
		}

		internal int AssaultSampleTally => assaultSampleTally;

		internal GameObject Figure => actor;

		internal virtual void GuaranteeBoot()
		{
		}

		internal virtual void OnActorAssembled()
		{
		}

		internal void PerformUpcomingAssaultVariant()
		{
			if (_actorStatus != ActorStatus.Assault || assaultSampleTally <= 1)
			{
				return;
			}
			_activeAssaultStatus = AssaultStatuses[Random.Range(0, Mathf.Min(assaultSampleTally, AssaultStatuses.Length))];
			PerformMotion(_activeAssaultStatus);
		}

		private void AssembleActor()
		{
			GuaranteeBoot();
			if (actor != null)
			{
				actor.SetActive(false);
				Destroy(actor);
				actor = null;
			}
			motionRig = null;
			meshEntity = null;
			dart = null;
			vitalityMeterLocation = null;
			sphereHitbox = null;
			hasHoldMotion = false;
			assaultSampleTally = 0;
			didAssaultBefore = false;

			if (gameplayCore == null || core == null)
			{
				return;
			}
			GameObject template = gameplayCore.GetActorForWorth(faction, kind, _worth);
			if (template == null)
			{
				return;
			}

			actor = Instantiate(template, core);
			Transform figurePivot = actor.transform;
			figurePivot.localPosition = Vector3.zero;
			figurePivot.localRotation = Quaternion.identity;
			figurePivot.localScale = Vector3.one * GameTuning.GetTroopStature(kind, _worth);

			motionRig = actor.GetComponentInChildren<Animator>(true);
			meshEntity = SeekNodeInDescendantWithTag(figurePivot, "CharacterBody");
			vitalityMeterLocation = figurePivot.Find("HealthBarLocation");
			Transform beginMark = figurePivot.Find("BulletStartPoint");
			projectileBeginMark = beginMark != null ? beginMark : transform;
			sphereHitbox = actor.GetComponent<SphereCollider>();
			dart = SeekNodeInDescendantWithTag(figurePivot, "Arrow");
			if (dart != null && dart.GetComponent<SkinnedMeshRenderer>() != null)
			{
				dart = null;
			}
			SkinnedMeshRenderer[] smrs = actor.GetComponentsInChildren<SkinnedMeshRenderer>(true);
			for (int idx = 0; idx < smrs.Length; idx++)
			{
#if !UNITY_LUNA
				smrs[idx].updateWhenOffscreen = true;
#endif
			}

			if (motionRig != null)
			{
				motionRig.speed = MotionPaceScalar;
				hasHoldMotion = HasMotionRigStatus("Hold");
				for (int idx = 0; idx < AssaultStatuses.Length; idx++)
				{
					if (HasMotionRigStatus(AssaultStatuses[idx]))
					{
						assaultSampleTally++;
					}
				}
				AppendMotionSignals(actor.name);
				MotionSignalDispatcher dispatcher = motionRig.GetComponent<MotionSignalDispatcher>();
				if (dispatcher == null)
				{
					dispatcher = motionRig.gameObject.AddComponent<MotionSignalDispatcher>();
				}
				dispatcher.responder = this;
			}

			OnActorAssembled();
			PresentActor();
			if (_actorStatus != ActorStatus.Resting)
			{
				ImposeStatus(false);
			}
		}

		private bool HasMotionRigStatus(string status)
		{
			if (motionRig == null || motionRig.runtimeAnimatorController == null)
			{
				return false;
			}
			if (motionRig.isActiveAndEnabled)
			{
				return motionRig.HasState(0, Animator.StringToHash(status));
			}
			string ending = "_" + status;
			foreach (AnimationClip sample in motionRig.runtimeAnimatorController.animationClips)
			{
				if (sample != null && sample.name.EndsWith(ending, StringComparison.Ordinal))
				{
					return true;
				}
			}
			return false;
		}

		private void ImposeStatus(bool withConquestPause)
		{
			if (motionRig == null)
			{
				return;
			}
			float pace = MotionPaceScalar;
			switch (_actorStatus)
			{
				case ActorStatus.Resting:
					motionRig.speed = pace;
					PerformMotion("Idle");
					break;
				case ActorStatus.Sprint:
					motionRig.speed = pace;
					PerformMotion("Run");
					break;
				case ActorStatus.Assault:
					motionRig.speed = pace * RemoteTuning.combat_assault_pace;
					_activeAssaultStatus = (!didAssaultBefore || assaultSampleTally <= 1) ? AssaultStatuses[0] : AssaultStatuses[Random.Range(0, Mathf.Min(assaultSampleTally, AssaultStatuses.Length))];
					didAssaultBefore = true;
					PerformMotion(_activeAssaultStatus);
					break;
				case ActorStatus.Perish:
					motionRig.speed = pace;
					PerformMotion("Die");
					break;
				case ActorStatus.Conquest:
					motionRig.speed = pace;
					if (withConquestPause && isActiveAndEnabled)
					{
						StartCoroutine(PerformMotionAfterPause("Victory", Random.Range(0f, 0.3f)));
					}
					else
					{
						PerformMotion("Victory");
					}
					break;
				case ActorStatus.Frozen:
					motionRig.speed = 0f;
					break;
				case ActorStatus.Celebrate:
					motionRig.speed = pace;
					string celebration = motionRig.isActiveAndEnabled && motionRig.HasState(0, CelebrateHash) ? "Dance" : "Victory";
					if (withConquestPause && isActiveAndEnabled)
					{
						StartCoroutine(PerformMotionAfterPause(celebration, Random.Range(0f, 0.3f)));
					}
					else
					{
						PerformMotion(celebration);
					}
					break;
			}
		}

		protected void PerformMotion(string status)
		{
			if (motionRig == null || !motionRig.isActiveAndEnabled || motionRig.runtimeAnimatorController == null)
			{
				return;
			}
			int statusHash = Animator.StringToHash(status);
			if (motionRig.HasState(0, statusHash))
			{
				motionRig.CrossFadeInFixedTime(statusHash, CrossDissolveSpan, 0);
			}
			else if (motionRig.HasState(0, RestingHash))
			{
				motionRig.Play("Idle", 0);
			}
		}

		private IEnumerator PerformMotionAfterPause(string status, float pause)
		{
			ActorStatus anticipated = _actorStatus;
			if (pause > 0f)
			{
				yield return new WaitForSeconds(pause);
			}
			if (_actorStatus == anticipated)
			{
				PerformMotion(status);
			}
		}

		protected virtual void StatusRevise()
		{
		}

		private void AppendMotionSignals(string actorTitle)
		{
			if (motionRig == null || motionRig.runtimeAnimatorController == null)
			{
				return;
			}
			if (kind != ActorKind.Person && kind != ActorKind.Saurian && kind != ActorKind.Overlord)
			{
				return;
			}
			HashSet<string> processed = GameTuning.actorsWithMotionSignalsAppended;
			if (processed != null && !processed.Add(actorTitle))
			{
				return;
			}

			bool appended = false;
			HashSet<AnimationClip> seen = new HashSet<AnimationClip>();
			foreach (AnimationClip sample in motionRig.runtimeAnimatorController.animationClips)
			{
				if (sample == null || !seen.Add(sample))
				{
					continue;
				}
				string sampleTitle = sample.name;
				if (sampleTitle.Contains("Attack"))
				{
					if (kind == ActorKind.Person)
					{
						bool isWizard = sampleTitle.Contains("Wizard");
						appended |= AppendMotionSignal(sample, "Shoot", GetShootMoment(sampleTitle));
						if (!isWizard)
						{
							appended |= AppendMotionSignal(sample, "PullString", DrawBowstringMoment);
						}
					}
					else
					{
						appended |= AppendMotionSignal(sample, "Hit", GetImpactPortion(sampleTitle) * sample.length);
					}
				}
				else if (sampleTitle.Contains("Die"))
				{
					appended |= AppendMotionSignal(sample, "DieComplete", sample.length - 0.01f);
				}
				else
				{
					continue;
				}
				GameTuning.motionSamplesWithSignal?.Add(sampleTitle);
			}
			if (appended && motionRig.isActiveAndEnabled)
			{
				// The model was just instantiated: rebind so its Animator picks up the new clip events.
				motionRig.Rebind();
			}
		}

		private static float GetShootMoment(string sampleTitle)
		{
			if (sampleTitle.Contains("Wizard"))
			{
				return WizardShootMoment;
			}
			if (sampleTitle.EndsWith("Attack3", StringComparison.Ordinal))
			{
				return PersonShootMoment3;
			}
			if (sampleTitle.EndsWith("Attack2", StringComparison.Ordinal))
			{
				return PersonShootMoment2;
			}
			return PersonShootMoment;
		}

		private static float GetImpactPortion(string sampleTitle)
		{
			for (int idx = 0; idx < SaurianImpactPortions.Length; idx++)
			{
				if (sampleTitle.StartsWith(SaurianImpactPortions[idx].Key, StringComparison.Ordinal))
				{
					return SaurianImpactPortions[idx].Value;
				}
			}
			return 0.5f;
		}

		private bool AppendMotionSignal(AnimationClip sample, string option, float moment)
		{
			// Clips are shared assets (and keep runtime events for the whole editor session): never add twice.
			foreach (UnityEngine.AnimationEvent priorSignal in sample.events)
			{
				if (priorSignal.functionName == SignalFunctionTitle && priorSignal.stringParameter == option)
				{
					return false;
				}
			}
			sample.AddEvent(new UnityEngine.AnimationEvent
			{
				functionName = SignalFunctionTitle,
				stringParameter = option,
				time = Mathf.Clamp(moment, 0f, sample.length)
			});
			return true;
		}

		protected void PresentActor()
		{
			if (actor == null)
			{
				return;
			}
			bool wasConcealed = !actor.activeSelf;
			actor.SetActive(true);
			if (wasConcealed && _actorStatus != ActorStatus.Resting)
			{
				// The model's Animator restarts from its default state when re-enabled.
				ImposeStatus(false);
			}
		}

		protected virtual void SenseOutline()
		{
		}

		protected virtual void Update()
		{
			StatusRevise();
			SenseOutline();
		}

		protected GameObject SeekNodeInDescendantWithTag(Transform holder, string wantedTag)
		{
			if (holder == null)
			{
				return null;
			}
			for (int idx = 0; idx < holder.childCount; idx++)
			{
				Transform descendant = holder.GetChild(idx);
				if (descendant.tag == wantedTag)
				{
					return descendant.gameObject;
				}
				GameObject located = SeekNodeInDescendantWithTag(descendant, wantedTag);
				if (located != null)
				{
					return located;
				}
			}
			return null;
		}
	}
}
