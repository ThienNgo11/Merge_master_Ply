using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	[ExecuteAlways]
	public class UIRevisionReadout : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text revisionCaption;

		private void Start()
		{
			if (revisionCaption != null)
			{
				revisionCaption.text = $"v{Application.version}";
			}
		}
	}
}
