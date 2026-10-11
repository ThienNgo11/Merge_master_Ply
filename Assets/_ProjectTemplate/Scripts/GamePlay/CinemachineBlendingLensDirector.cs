using System;
using Cinemachine;
using DG.Tweening;
using GameTool.Assistants.DesignPattern;
using UnityEngine;

namespace Funlab.MergeMaster
{
	public class CinemachineBlendingLensDirector : SingletonMonoBehaviour<CinemachineBlendingLensDirector>
	{
		[SerializeField]
		private CinemachineVirtualCameraBase[] childCameras;

		[SerializeField]
		private Camera mainCamera;

		public int activeLens { get; private set; }

		public int LensTally => childCameras != null ? childCameras.Length : 0;

		private Tween _activeChain;

		protected override void Awake()
		{
			base.Awake();
			if (Instance != this)
			{
				return;
			}
			ResolveMainCamera();
			GuaranteeChildCameras();
			activeLens = 0;
		}

		private void ResolveMainCamera()
		{
			if (mainCamera == null)
			{
				mainCamera = Camera.main;
				if (mainCamera == null)
				{
					mainCamera = FindObjectOfType<Camera>();
				}
			}
		}

		private void GuaranteeChildCameras()
		{
			if (childCameras == null || childCameras.Length < 3)
			{
				childCameras = new CinemachineVirtualCameraBase[3];
				var found = GetComponentsInChildren<CinemachineVirtualCameraBase>(true);
				foreach (var cam in found)
				{
					if (cam == null) continue;
					string n = cam.gameObject.name;
					if (n.Contains("Unit")) childCameras[2] = cam;
					else if (n.Contains("Env")) childCameras[1] = cam;
					else if (n.Contains("Main")) childCameras[0] = cam;
				}
				for (int i = 0; i < childCameras.Length && i < found.Length; i++)
				{
					if (childCameras[i] == null) childCameras[i] = found[i];
				}
			}

			if (childCameras.Length > 2 && childCameras[2] != null)
			{
				var unitCam = childCameras[2];
				if (unitCam.LookAt == null)
				{
					Transform ft = transform.Find("FollowTarget");
					if (ft != null) unitCam.LookAt = ft;
				}
				if (unitCam.Follow == null)
				{
					unitCam.Follow = unitCam.LookAt;
				}
			}
		}

		public CinemachineVirtualCameraBase GetLens(int ordinal)
		{
			GuaranteeChildCameras();
			if (childCameras == null || ordinal < 0 || ordinal >= childCameras.Length)
			{
				return null;
			}
			return childCameras[ordinal];
		}

		public void AssignLens(int ordinal, float span = 1f, Ease smoothing = Ease.Linear)
		{
			GuaranteeChildCameras();
			ResolveMainCamera();
			activeLens = ordinal;
			_activeChain?.Kill();
			_activeChain = null;

			if (childCameras == null || ordinal < 0 || ordinal >= childCameras.Length)
			{
				return;
			}

			var destCam = childCameras[ordinal];
			if (destCam == null || mainCamera == null)
			{
				return;
			}

			if (ordinal == 2)
			{
				destCam.UpdatePose();
			}

			Vector3 targetPos = destCam.transform.position;
			Quaternion targetRot = destCam.transform.rotation;
			float targetFov = destCam.m_Lens.FieldOfView > 0f ? destCam.m_Lens.FieldOfView : (ordinal == 2 ? GameTuning.lensExtent : GameTuning.arenaLensExtent);

			if (span <= 0f)
			{
				mainCamera.transform.position = targetPos;
				mainCamera.transform.rotation = targetRot;
				mainCamera.fieldOfView = targetFov;
				return;
			}

			Vector3 startPos = mainCamera.transform.position;
			Quaternion startRot = mainCamera.transform.rotation;
			float startFov = mainCamera.fieldOfView;

			_activeChain = DOVirtual.Float(0f, 1f, span, portion =>
			{
				if (mainCamera == null) return;
				mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, portion);
				mainCamera.transform.rotation = Quaternion.Slerp(startRot, targetRot, portion);
				mainCamera.fieldOfView = Mathf.Lerp(startFov, targetFov, portion);
			}).SetEase(smoothing).OnComplete(() =>
			{
				if (mainCamera != null)
				{
					mainCamera.transform.position = targetPos;
					mainCamera.transform.rotation = targetRot;
					mainCamera.fieldOfView = targetFov;
				}
				_activeChain = null;
			});
		}

		private void LateUpdate()
		{
			if (_activeChain != null || mainCamera == null)
			{
				return;
			}
			if (childCameras != null && activeLens >= 0 && activeLens < childCameras.Length)
			{
				var activeCam = childCameras[activeLens];
				if (activeCam != null)
				{
					if (activeLens == 2)
					{
						activeCam.UpdatePose();
					}
					mainCamera.transform.position = activeCam.transform.position;
					mainCamera.transform.rotation = activeCam.transform.rotation;
					if (activeCam.m_Lens.FieldOfView > 0f)
					{
						mainCamera.fieldOfView = activeCam.m_Lens.FieldOfView;
					}
				}
			}
		}
	}
}
