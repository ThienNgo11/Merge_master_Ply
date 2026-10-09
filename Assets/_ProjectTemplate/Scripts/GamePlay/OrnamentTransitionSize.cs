using DG.Tweening;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class OrnamentTransitionSize : OrnamentTransitionBasis
	{
		[SerializeField]
		private Vector3 fromSize;

		[SerializeField]
		private Vector3 toSize;

		public Vector3 FinalSize => toSize;

		public override void SetStatusToPristine()
		{
			TerminateTransition();
			transform.localScale = fromSize;
		}

		public override void SetStatusToFinal()
		{
			TerminateTransition();
			transform.localScale = toSize;
		}

		public override void Perform(float pause = 0f)
		{
			TerminateTransition();
			transform.localScale = fromSize;
			BeginTransition(transform.DOScale(toSize, motionSpan), pause);
		}
	}
}
