using UnityEngine;

namespace Funlab.MergeMaster
{
	[ExecuteInEditMode]
	public class LensDepthMode : MonoBehaviour
	{
		private Camera lens;

		private void Start()
		{
			lens = GetComponent<Camera>();
			if (lens != null)
			{
				lens.depthTextureMode |= DepthTextureMode.Depth;
			}
		}
	}
}
