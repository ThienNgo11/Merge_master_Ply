using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class UIJewelReadout : MonoBehaviour
	{
		[SerializeField]
		private TMP_Text _caption;

		private void OnEnable()
		{
			PlayMechanics.JewelsAltered += OnJewelsAltered;
			ReviseCaption();
		}

		private void OnDisable()
		{
			PlayMechanics.JewelsAltered -= OnJewelsAltered;
		}

		private void OnJewelsAltered(long entity)
		{
			ReviseCaption();
		}

		private void ReviseCaption()
		{
			if (_caption != null)
			{
				_caption.text = Toolbox.CompactNumeralToThousands(PlayMechanics.jewels);
			}
		}
	}
}
