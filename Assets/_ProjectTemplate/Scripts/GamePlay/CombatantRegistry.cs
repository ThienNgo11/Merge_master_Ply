using System;
using System.Collections.Generic;
using UnityEngine;

namespace Funlab.MergeMaster
{
	[Serializable]
	public class CombatantRegistry
	{
		private const int PeakTroopWorth = 1024;

		public List<Combatant> combatants;

		public List<Combatant> guardians;

		public List<Combatant> foes;

		private Dictionary<ActorKind, List<Combatant>> guardianCluster;

		private Dictionary<ActorKind, List<Combatant>> foeCluster;

		public List<Combatant> scrap;

		private readonly bool isClusteringAllowed;

		private Action<Combatant> m_onCombatantAppended;

		private Action<Combatant> m_onCombatantDiscarded;

		private HashSet<Combatant> _fusables;

		[NonSerialized]
		private List<Combatant> _fatalityRelay;

		public event Action<Combatant> onCombatantAppended
		{
			add
			{
				m_onCombatantAppended += value;
			}
			remove
			{
				m_onCombatantAppended -= value;
			}
		}

		public event Action<Combatant> onCombatantDiscarded
		{
			add
			{
				m_onCombatantDiscarded += value;
			}
			remove
			{
				m_onCombatantDiscarded -= value;
			}
		}

		public void Boot()
		{
			if (combatants == null) combatants = new List<Combatant>();
			if (guardians == null) guardians = new List<Combatant>();
			if (foes == null) foes = new List<Combatant>();
			if (scrap == null) scrap = new List<Combatant>();
			guardianCluster = new Dictionary<ActorKind, List<Combatant>>();
			foeCluster = new Dictionary<ActorKind, List<Combatant>>();
		}

		private void GuaranteeBoot()
		{
			if (combatants == null || guardians == null || foes == null || scrap == null || guardianCluster == null || foeCluster == null)
			{
				Boot();
			}
		}

		private List<Combatant> FactionSeries(ActorFaction faction)
		{
			switch (faction)
			{
				case ActorFaction.Guardian:
					return guardians;
				case ActorFaction.Foe:
					return foes;
				default:
					return null;
			}
		}

		private Dictionary<ActorKind, List<Combatant>> FactionCluster(ActorFaction faction)
		{
			return faction == ActorFaction.Guardian ? guardianCluster : faction == ActorFaction.Foe ? foeCluster : null;
		}

		public void Add(Combatant combatant)
		{
			if (combatant == null)
			{
				return;
			}
			GuaranteeBoot();
			if (!combatants.Contains(combatant))
			{
				combatants.Add(combatant);
			}
			List<Combatant> factionSeries = FactionSeries(combatant.faction);
			if (factionSeries != null && !factionSeries.Contains(combatant))
			{
				factionSeries.Add(combatant);
			}
			if (isClusteringAllowed)
			{
				Dictionary<ActorKind, List<Combatant>> cluster = FactionCluster(combatant.faction);
				if (cluster != null)
				{
					if (!cluster.TryGetValue(combatant.kind, out List<Combatant> series))
					{
						series = new List<Combatant>();
						cluster[combatant.kind] = series;
					}
					if (!series.Contains(combatant))
					{
						series.Add(combatant);
					}
				}
			}
			m_onCombatantAppended?.Invoke(combatant);
		}

		public void Discard(Combatant combatant, bool willScrap)
		{
			if (combatant == null)
			{
				return;
			}
			GuaranteeBoot();
			combatants.Remove(combatant);
			guardians.Remove(combatant);
			foes.Remove(combatant);
			foreach (List<Combatant> series in guardianCluster.Values)
			{
				series.Remove(combatant);
			}
			foreach (List<Combatant> series in foeCluster.Values)
			{
				series.Remove(combatant);
			}
			_fusables?.Remove(combatant);
			if (willScrap && !scrap.Contains(combatant))
			{
				scrap.Add(combatant);
			}
			m_onCombatantDiscarded?.Invoke(combatant);
		}

		private static bool IsInBattle(Combatant combatant)
		{
			return combatant != null && combatant.IsLiving;
		}

		public Combatant GetNearestOpposition(Combatant combatant)
		{
			if (combatant == null)
			{
				return null;
			}
			GuaranteeBoot();
			List<Combatant> rivals = combatant.faction == ActorFaction.Guardian ? foes : combatant.faction == ActorFaction.Foe ? guardians : null;
			if (rivals == null)
			{
				return null;
			}
			Vector3 home = combatant.transform.position;
			Combatant nearest = null;
			float shortest = float.MaxValue;
			for (int idx = 0; idx < rivals.Count; idx++)
			{
				Combatant contender = rivals[idx];
				if (contender == null || !Combatant.IsViableObjective(contender))
				{
					continue;
				}
				Vector3 difference = contender.transform.position - home;
				difference.y = 0f;
				float reach = difference.sqrMagnitude;
				if (reach < shortest)
				{
					shortest = reach;
					nearest = contender;
				}
			}
			return nearest;
		}

		public ActorFaction ReportFatality(Combatant fatality)
		{
			GuaranteeBoot();
			Discard(fatality, true);

			if (_fatalityRelay == null) _fatalityRelay = new List<Combatant>();
			bool relayShared = _fatalityRelay.Count == 0;
			List<Combatant> relay = relayShared ? _fatalityRelay : new List<Combatant>(combatants);
			if (relayShared)
			{
				relay.AddRange(combatants);
			}
			try
			{
				for (int idx = 0; idx < relay.Count; idx++)
				{
					Combatant combatant = relay[idx];
					if (combatant != null && combatant.IsLiving)
					{
						combatant.CombatantDidPerish(fatality);
					}
				}
			}
			finally
			{
				if (relayShared)
				{
					relay.Clear();
				}
			}

			if (TallyCombatants(foes) == 0)
			{
				return ActorFaction.Guardian;
			}
			if (TallyCombatants(guardians) == 0)
			{
				return ActorFaction.Foe;
			}
			return ActorFaction.Unset;
		}

		private static int TallyCombatants(List<Combatant> series)
		{
			int tally = 0;
			for (int idx = 0; idx < series.Count; idx++)
			{
				if (IsInBattle(series[idx]))
				{
					tally++;
				}
			}
			return tally;
		}

		public float GetLeftoverVitality(ActorFaction faction)
		{
			GuaranteeBoot();
			List<Combatant> series = FactionSeries(faction);
			if (series == null)
			{
				return 0f;
			}
			float overall = 0f;
			for (int idx = 0; idx < series.Count; idx++)
			{
				if (IsInBattle(series[idx]))
				{
					overall += series[idx].vitality;
				}
			}
			return overall;
		}

		public int GetLeftoverMight(ActorFaction faction)
		{
			GuaranteeBoot();
			List<Combatant> series = FactionSeries(faction);
			if (series == null)
			{
				return 0;
			}
			float overall = 0f;
			for (int idx = 0; idx < series.Count; idx++)
			{
				if (IsInBattle(series[idx]))
				{
					overall += series[idx].harmPerImpact;
				}
			}
			return Mathf.RoundToInt(overall);
		}

		public void AdjustVitalityByScalar(ActorFaction faction, float scalar)
		{
			GuaranteeBoot();
			List<Combatant> series = FactionSeries(faction);
			if (series == null)
			{
				return;
			}
			foreach (Combatant combatant in series)
			{
				MultiplyVitality(combatant, scalar);
			}
		}

		private static void MultiplyVitality(Combatant combatant, float scalar)
		{
			if (combatant == null)
			{
				return;
			}
			combatant.beginVitality *= scalar;
			combatant.vitality *= scalar;
			combatant.ReviseVitality();
		}

		public void VerifyFusables()
		{
			GuaranteeBoot();
			if (_fusables == null) _fusables = new HashSet<Combatant>();
			_fusables.Clear();
			for (int outer = 0; outer < guardians.Count; outer++)
			{
				for (int inner = outer + 1; inner < guardians.Count; inner++)
				{
					if (VerifyFuse(guardians[outer], guardians[inner]))
					{
						_fusables.Add(guardians[outer]);
						_fusables.Add(guardians[inner]);
					}
				}
			}
		}

		public int GetFusableTally()
		{
			VerifyFusables();
			return _fusables.Count;
		}

		public bool VerifyFuse(Combatant donor, Combatant recipient)
		{
			if (donor == null || recipient == null || donor == recipient)
			{
				return false;
			}
			if (donor.faction != ActorFaction.Guardian || recipient.faction != ActorFaction.Guardian)
			{
				return false;
			}
			if (donor.kind != recipient.kind || (donor.kind != ActorKind.Person && donor.kind != ActorKind.Saurian))
			{
				return false;
			}
			if (donor.worth != recipient.worth || donor.worth >= PeakTroopWorth)
			{
				return false;
			}
			return true;
		}

		public void PresentConquest(ActorFaction faction)
		{
			GuaranteeBoot();
			List<Combatant> series = FactionSeries(faction);
			if (series == null)
			{
				return;
			}
			Combatant speaker = null;
			foreach (Combatant combatant in series.ToArray())
			{
				if (!IsInBattle(combatant))
				{
					continue;
				}
				combatant.Halt();
				combatant.actorStatus = ActorStatus.Celebrate;
				if (speaker == null || (speaker.kind != ActorKind.Person && combatant.kind == ActorKind.Person))
				{
					speaker = combatant;
				}
			}
			if (speaker != null)
			{
				GameTuning.audioSignal?.Invoke(speaker.kind, AudioCue.Conquest, speaker.worth);
			}
		}

		public void Wipe()
		{
			GuaranteeBoot();
			HashSet<Combatant> every = new HashSet<Combatant>(combatants);
			every.UnionWith(scrap);
			foreach (Combatant combatant in every)
			{
				if (combatant != null)
				{
					combatant.gameObject.SetActive(false);
					UnityEngine.Object.Destroy(combatant.gameObject);
				}
			}
			combatants.Clear();
			guardians.Clear();
			foes.Clear();
			scrap.Clear();
			guardianCluster.Clear();
			foeCluster.Clear();
			_fusables?.Clear();
		}

		public void AdjustEveryVitalityByScalar(ActorFaction faction, float scalar)
		{
			GuaranteeBoot();
			HashSet<Combatant> every = new HashSet<Combatant>(combatants);
			every.UnionWith(scrap);
			foreach (Combatant combatant in every)
			{
				if (combatant != null && combatant.faction == faction)
				{
					MultiplyVitality(combatant, scalar);
				}
			}
		}
	}
}
