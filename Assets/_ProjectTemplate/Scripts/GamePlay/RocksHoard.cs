using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class RocksHoard : MonoBehaviour
	{
		[SerializeField]
		private TextMeshProUGUI _rocksCaption;

		private int _rocksAmount;

		public void Stock(int jewelsAmount)
		{
			_rocksAmount = jewelsAmount;
			if (_rocksCaption != null)
			{
				_rocksCaption.text = "+" + jewelsAmount;
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
