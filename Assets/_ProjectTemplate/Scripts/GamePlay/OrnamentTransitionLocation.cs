using DG.Tweening;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class OrnamentTransitionLocation : OrnamentTransitionBasis
	{
		[SerializeField]
		private Vector3 fromLocation;

		[SerializeField]
		private Vector3 toLocation;

		public override void SetStatusToPristine()
		{
			TerminateTransition();
			transform.localPosition = fromLocation;
		}

		public override void SetStatusToFinal()
		{
			TerminateTransition();
			transform.localPosition = toLocation;
		}

		public override void Perform(float pause = 0f)
		{
			TerminateTransition();
			transform.localPosition = fromLocation;
			BeginTransition(transform.DOLocalMove(toLocation, motionSpan), pause);
		}
	}
}
