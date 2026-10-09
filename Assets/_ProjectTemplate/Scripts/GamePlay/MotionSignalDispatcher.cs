using UnityEngine;

namespace Funlab.MergeMaster
{
	public class MotionSignalDispatcher : MonoBehaviour
	{
		public MotionSignalResponder responder;

		public void SignalResponder(string kind)
		{
			if (responder != null)
			{
				responder.MotionSignal(kind);
			}
		}
	}
}
