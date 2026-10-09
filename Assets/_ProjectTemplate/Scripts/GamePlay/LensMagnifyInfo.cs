using System;
using UnityEngine;

namespace Funlab.MergeMaster
{
	[Serializable]
	public struct LensMagnifyInfo
	{
		public Vector3 Location;

		public float AOV;

		public LensMagnifyInfo(Vector3 location, float viewAngle)
		{
			Location = location;
			AOV = viewAngle;
		}
	}
}
