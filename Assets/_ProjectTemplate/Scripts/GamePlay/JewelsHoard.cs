using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class JewelsHoard : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _jewelsCaption;

		private int _jewelsAmount;

		public void Fill(int jewelsAmount)
		{
			_jewelsAmount = jewelsAmount;
			if (_jewelsCaption != null)
			{
				_jewelsCaption.text = "+" + jewelsAmount;
			}
		}

		public void Present()
		{
			transform.DOKill();
			gameObject.SetActive(true);
			transform.localScale = Vector3.zero;
			transform.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
		}

		public void Conceal()
		{
			transform.DOKill();
			transform.localScale = Vector3.one;
			gameObject.SetActive(false);
		}
	}
}
