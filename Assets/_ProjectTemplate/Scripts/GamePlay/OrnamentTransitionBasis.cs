using DG.Tweening;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public abstract class OrnamentTransitionBasis : MonoBehaviour
	{
		[SerializeField]
		protected float motionSpan;

		private Tween _transition;

		public abstract void SetStatusToPristine();

		public abstract void SetStatusToFinal();

		public abstract void Perform(float pause = 0f);

		internal void TerminateTransition()
		{
			if (_transition != null)
			{
				_transition.Kill();
				_transition = null;
			}
		}

		internal void BeginTransition(Tween transition, float pause)
		{
			TerminateTransition();
			Tween constructed = transition.SetDelay(pause).SetEase(Ease.OutBack).SetTarget(this);
			constructed.OnKill(() =>
			{
				if (_transition == constructed)
				{
					_transition = null;
				}
			});
			_transition = constructed;
		}

		private void OnDestroy()
		{
			TerminateTransition();
		}
	}
}
