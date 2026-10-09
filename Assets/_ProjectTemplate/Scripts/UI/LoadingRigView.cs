using System.Collections;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class LoadingRigView : MonoBehaviour
	{
		private static readonly string[] LoadingCaptionFrames =
		{
			"Loading<alpha=#00>...",
			"Loading.<alpha=#00>..",
			"Loading..<alpha=#00>.",
			"Loading..."
		};

		[SerializeField]
		private TMP_Text loadingCaption;

		[SerializeField]
		private float loadingCaptionBeat = 0.35f;

		private Vector3 _appliedParentScale;

		private WaitForSecondsRealtime _loadingCaptionPause;

		private void OnEnable()
		{
			_appliedParentScale = Vector3.zero;
			NeutralizeParentScale();
			if (loadingCaption != null)
			{
				StartCoroutine(CycleLoadingCaption());
			}
		}

		private void LateUpdate()
		{
			NeutralizeParentScale();
		}

		private IEnumerator CycleLoadingCaption()
		{
			if (_loadingCaptionPause == null)
			{
				_loadingCaptionPause = new WaitForSecondsRealtime(loadingCaptionBeat);
			}
			int frame = 0;
			while (true)
			{
				loadingCaption.SetText(LoadingCaptionFrames[frame]);
				frame = (frame + 1) % LoadingCaptionFrames.Length;
				yield return _loadingCaptionPause;
			}
		}

		private void NeutralizeParentScale()
		{
			Transform parent = transform.parent;
			if (parent == null)
			{
				return;
			}
			Vector3 parentScale = parent.lossyScale;
			if (parentScale == _appliedParentScale || parentScale.x <= 0f || parentScale.y <= 0f || parentScale.z <= 0f)
			{
				return;
			}
			_appliedParentScale = parentScale;
			transform.localScale = new Vector3(1f / parentScale.x, 1f / parentScale.y, 1f / parentScale.z);
		}
	}
}
