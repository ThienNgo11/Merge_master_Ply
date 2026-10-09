using System;
using UnityEngine;

namespace Funlab.MergeMaster
{
	[Serializable]
	public struct ActorTemplate
	{
		public ActorKind kind;

		[Range(1f, 1024f)]
		public int worth;

		[Header("This health will be used if it is set to a value greater than zero.")]
		public int vitality;

		[Header("This DPS will be used if it is set to a value greater than zero.")]
		public int tailoredHarmRate;

		public GameObject template;
	}
}
