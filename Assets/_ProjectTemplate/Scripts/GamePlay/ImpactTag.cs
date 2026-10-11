using GameTool.ObjectPool.Scripts;
using TMPro;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class ImpactTag : MonoBehaviour
	{
		public TextMeshPro tagText;

		private Animation _motion;

		private void Awake()
		{
			_motion = GetComponent<Animation>();
		}

		private void OnEnable()
		{
			if (_motion != null && _motion.clip != null)
			{
				_motion.Stop();
				_motion.Play();
			}
		}

		private void ImpactTagMotionFinish()
		{
			PoolingManager.Instance.Despawn(gameObject);
		}
	}
}
