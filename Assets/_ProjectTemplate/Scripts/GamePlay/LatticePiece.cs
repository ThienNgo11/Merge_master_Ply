using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	// Level-design helper: labels the enemy slot in the editor. StageLayout prefabs are only read as data at runtime.
	[ExecuteInEditMode]
	public class LatticePiece : MonoBehaviour
	{
		public ActorKind kind;

		public int worth;

#pragma warning disable 0414
		private readonly int[] worths = GameTuning.LatticeWorths;
#pragma warning restore 0414

		private TextMeshPro kindText;

		private TextMeshPro worthText;

		private void OnEnable()
		{
#if UNITY_EDITOR
			if (Application.isPlaying)
			{
				return;
			}
			Transform kindDescendant = transform.Find("TypeTxt");
			Transform worthDescendant = transform.Find("ValueTxt");
			kindText = kindDescendant != null ? kindDescendant.GetComponent<TextMeshPro>() : null;
			worthText = worthDescendant != null ? worthDescendant.GetComponent<TextMeshPro>() : null;
			RenewTags();
#endif
		}

		private void Update()
		{
#if UNITY_EDITOR
			if (!Application.isPlaying)
			{
				RenewTags();
			}
#endif
		}

#if UNITY_EDITOR
		private void RenewTags()
		{
			// Same format as the authored labels: "HUMAN"/"DINO"/"NONE" and the raw value.
			string kindTag = kind.ToLabel().ToUpperInvariant();
			string worthTag = worth.ToString();
			if (kindText != null && kindText.text != kindTag)
			{
				kindText.text = kindTag;
			}
			if (worthText != null && worthText.text != worthTag)
			{
				worthText.text = worthTag;
			}
		}
#endif
	}
}
