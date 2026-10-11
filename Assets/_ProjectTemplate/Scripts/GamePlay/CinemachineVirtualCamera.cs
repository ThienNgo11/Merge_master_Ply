using System;
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
			T comp = GetComponent<T>();
			if (comp == null)
			{
				comp = gameObject.AddComponent<T>();
			}
			return comp;
		}

		public virtual void UpdatePose()
		{
		}
	}

	public class CinemachineVirtualCamera : CinemachineVirtualCameraBase
	{
		private void Awake()
		{
			if (LookAt == null && transform.parent != null)
			{
				Transform ft = transform.parent.Find("FollowTarget");
				if (ft != null && gameObject.name.Contains("Unit"))
				{
					LookAt = ft;
				}
			}
			if (Follow == null && LookAt != null)
			{
				Follow = LookAt;
			}
		}

		public override void UpdatePose()
		{
			if (Follow != null)
			{
				var transposer = GetCinemachineComponent<CinemachineTransposer>();
				Vector3 offset = transposer != null && transposer.m_FollowOffset != Vector3.zero
					? transposer.m_FollowOffset
					: new Vector3(0.676f, 0.357f, 0.676f);
				transform.position = Follow.position + Follow.rotation * offset;
			}
			if (LookAt != null)
			{
				Vector3 lookPoint = LookAt.position + new Vector3(0f, 0.34f, 0f);
				Vector3 dir = lookPoint - transform.position;
				if (dir.sqrMagnitude > 0.0001f)
				{
					transform.rotation = Quaternion.LookRotation(dir, Vector3.up);
				}
			}
		}
	}

	public class CinemachineTransposer : MonoBehaviour
	{
		public Vector3 m_FollowOffset;
	}
}
