using GameToolSample.GamePlay.Manager;
using UnityEngine;
using UnityEngine.UI;

namespace GameToolSample.IAP
{
	public class BuyIAP : MonoBehaviour
	{
		private void Awake()
		{
			Button btn = GetComponent<Button>();
			if (btn != null)
			{
				btn.onClick.AddListener(OnClickBuy);
			}
		}

		private void OnClickBuy()
		{
			GameManager.Instance.InstallFullGame();
		}
	}
}
