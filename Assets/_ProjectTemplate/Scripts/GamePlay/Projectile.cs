using GameTool.ObjectPool.Scripts;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class Projectile : MonoBehaviour
	{
		private const float ImpactReach = 0.1f;

		private const float AimElevation = 0.25f;

		private const float PeakLifespan = 3f;

		public GameplayCore gameplayCore;

		public Combatant firer;

		public Transform core;

		public GameObject[] models;

		public GameObject[] effects;

		public GameObject[] muzzles;

		public GameObject[] impacts;

		private Rigidbody physicsBody;

		private readonly float pace = 4.4f;

		// Radius around a fighter's root in which a projectile touches it (fighter sphere colliders are 0.14-0.2).
		private const float CoreImpactRadius = 0.2f;

		private bool hasImpacted;

		private Combatant objective;

		private Vector3 aimMark;

		private float lifespan;

		internal bool mimic;

		internal float harmShare = 1f;

		private bool armed;

		private TrailRenderer[] trails;

		private void Start()
		{
			if (!armed)
			{
				Arm();
			}
		}

		internal void Arm()
		{
			armed = true;
			hasImpacted = false;
			lifespan = 0f;
			if (physicsBody == null)
			{
				physicsBody = GetComponent<Rigidbody>();
			}
			if (physicsBody != null && !physicsBody.isKinematic)
			{
				physicsBody.velocity = Vector3.zero;
			}
			objective = firer != null ? firer.objective : null;
			aimMark = objective != null ? objective.transform.position + Vector3.up * AimElevation : transform.position + transform.forward;

			int stage = GradeSlot(firer);
			if (models != null)
			{
				if (models.Length > 0 && models[0] != null)
				{
					models[0].SetActive(stage == 0);
				}
				if (models.Length > 1 && models[1] != null)
				{
					models[1].SetActive(false);
				}
			}
			if (effects != null)
			{
				for (int slot = 0; slot < effects.Length; slot++)
				{
					if (effects[slot] != null)
					{
						effects[slot].SetActive(slot == stage);
					}
				}
			}
			FaceObjective();
			if (trails == null)
			{
				trails = GetComponentsInChildren<TrailRenderer>(true);
			}
			for (int idx = 0; idx < trails.Length; idx++)
			{
				trails[idx].Clear();
			}
		}

		private void HandleObjectiveImpact()
		{
			if (hasImpacted)
			{
				return;
			}
			hasImpacted = true;
			if (physicsBody != null && !physicsBody.isKinematic)
			{
				physicsBody.velocity = Vector3.zero;
			}
			GameObject impact = PickGradeVariant(impacts, firer);
			if (impact != null)
			{
				PoolingManager.Instance.SpawnParticle(impact, transform.position, transform.rotation);
			}
			if (firer != null && Combatant.IsViableObjective(objective) && (gameplayCore == null || gameplayCore.activity == ActivityStatus.Assaulting))
			{
				objective.DidTakeImpact(firer, harmShare);
				if (!mimic)
				{
					GameTuning.audioSignal?.Invoke(firer.kind, AudioCue.Impact, firer.worth);
				}
			}
			Discard();
		}

		internal static int GradeSlot(Combatant marksman)
		{
			return marksman != null ? Mathf.Clamp(PlayMechanics.GetActorGrade(marksman.worth) - 1, 0, 10) : 0;
		}

		internal static GameObject PickGradeVariant(GameObject[] variants, Combatant marksman)
		{
			int slot = GradeSlot(marksman);
			return variants != null && slot < variants.Length ? variants[slot] : null;
		}

		private Combatant SeekObstructingFoe()
		{
			if (firer == null || gameplayCore == null || gameplayCore.registry == null)
			{
				return null;
			}
			var rivals = firer.faction == ActorFaction.Guardian ? gameplayCore.registry.foes : gameplayCore.registry.guardians;
			if (rivals == null)
			{
				return null;
			}
			Vector3 location = transform.position;
			for (int idx = 0; idx < rivals.Count; idx++)
			{
				Combatant combatant = rivals[idx];
				if (combatant == null || combatant == objective || !Combatant.IsViableObjective(combatant))
				{
					continue;
				}
				Vector3 difference = combatant.transform.position - location;
				difference.y = 0f;
				if (difference.sqrMagnitude <= CoreImpactRadius * CoreImpactRadius)
				{
					return combatant;
				}
			}
			return null;
		}

		private void FaceObjective()
		{
			if (Combatant.IsViableObjective(objective))
			{
				aimMark = objective.transform.position + Vector3.up * AimElevation;
			}
			Vector3 heading = aimMark - transform.position;
			if (heading.sqrMagnitude > 0.000001f)
			{
				transform.rotation = Quaternion.LookRotation(heading);
			}
		}

		private void FixedUpdate()
		{
			if (hasImpacted)
			{
				return;
			}
			lifespan += Time.fixedDeltaTime;
			if (lifespan >= PeakLifespan)
			{
				hasImpacted = true;
				Discard();
				return;
			}
			FaceObjective();
			// Measured on the original (tl/w2, tl/w2b): a projectile hits the first enemy it touches on its way, e.g. a
			// raptor running at the shooter intercepts shots aimed at the Blackhawk behind it.
			Combatant barrier = SeekObstructingFoe();
			if (barrier != null)
			{
				objective = barrier;
				HandleObjectiveImpact();
				return;
			}
			float reach = Vector3.Distance(transform.position, aimMark);
			if (reach <= Mathf.Max(ImpactReach, pace * Time.fixedDeltaTime))
			{
				transform.position = aimMark;
				HandleObjectiveImpact();
				return;
			}
			Vector3 travel = transform.forward * pace;
			if (physicsBody != null && !physicsBody.isKinematic)
			{
				physicsBody.velocity = travel;
			}
			else
			{
				transform.position += travel * Time.fixedDeltaTime;
			}
		}

		private void Discard()
		{
			if (gameplayCore != null)
			{
				gameplayCore.DiscardProjectile(this);
			}
			else
			{
				Destroy(gameObject);
			}
		}
	}
}
