using System;
using DG.Tweening;
using GameTool.Assistants.DesignPattern;
using UnityEngine;

namespace Cinemachine
{
	[Serializable]
	public struct LensSettings
	{
		public float FieldOfView;
	}

	public class CinemachineVirtualCameraBase : MonoBehaviour
	{
		public Transform LookAt;
		public Transform Follow;
		public bool PreviousStateIsValid;
		public LensSettings m_Lens;

		public virtual T GetCinemachineComponent<T>() where T : Component
		{
			return GetComponent<T>();
		}
	}

	public class CinemachineVirtualCamera : CinemachineVirtualCameraBase
	{
		public override T GetCinemachineComponent<T>()
		{
			return GetComponent<T>();
		}
	}

	public class CinemachineTransposer : MonoBehaviour
	{
		public Vector3 m_FollowOffset;
	}
}

namespace Funlab.MergeMaster
{
	using Cinemachine;

	public class CinemachineBlendingLensDirector : SingletonMonoBehaviour<CinemachineBlendingLensDirector>
	{
		[SerializeField]
		private CinemachineVirtualCameraBase[] childCameras;

		public int activeLens { get; private set; }

		public int LensTally => childCameras != null ? childCameras.Length : 0;

		private Tween _activeChain;

		protected override void Awake()
		{
			base.Awake();
			if (childCameras == null || childCameras.Length == 0)
			{
				childCameras = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
			}
			activeLens = 0;
		}

		public CinemachineVirtualCameraBase GetLens(int ordinal)
		{
			if (childCameras == null || ordinal < 0 || ordinal >= childCameras.Length)
			{
				return null;
			}
			return childCameras[ordinal];
		}

		public void AssignLens(int ordinal, float span = 1f, Ease smoothing = Ease.Linear)
		{
			activeLens = ordinal;
			_activeChain?.Kill();
			_activeChain = null;

			if (childCameras == null || ordinal < 0 || ordinal >= childCameras.Length)
			{
				return;
			}

			for (int i = 0; i < childCameras.Length; i++)
			{
				if (childCameras[i] != null)
				{
					childCameras[i].gameObject.SetActive(i == ordinal);
				}
			}
		}
	}
}
