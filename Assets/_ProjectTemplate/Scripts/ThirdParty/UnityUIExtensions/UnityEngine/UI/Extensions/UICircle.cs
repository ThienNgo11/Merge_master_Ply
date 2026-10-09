using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
	[AddComponentMenu("UI/Extensions/Primitives/UI Circle")]
	public class UICircle : UIPrimitiveBase
	{
		[Tooltip("The Arc Invert property will invert the construction of the Arc.")]
		public bool ArcInvert;

		[Tooltip("The Arc property is a percentage of the entire circumference of the circle.")]
		[Range(0f, 1f)]
		public float Arc;

		[Tooltip("The Arc Steps property defines the number of segments that the Arc will be divided into.")]
		[Range(0f, 1000f)]
		public int ArcSteps;

		[Tooltip("The Arc Rotation property permits adjusting the geometry orientation around the Z axis.")]
		[Range(0f, 360f)]
		public int ArcRotation;

		[Tooltip("The Progress property allows the primitive to be used as a progression indicator.")]
		[Range(0f, 1f)]
		public float Progress;

		private float _progress;

		public Color ProgressColor;

		public bool Fill;

		public float Thickness;

		public int Padding;

		private List<int> indices = new List<int>();

		private List<UIVertex> vertices = new List<UIVertex>();

		private Vector2 uvCenter = new Vector2(0.5f, 0.5f);

		protected override void OnPopulateMesh(VertexHelper vh)
		{
			vh.Clear();
			indices.Clear();
			vertices.Clear();

			Rect rect = GetPixelAdjustedRect();
			float outer = Mathf.Min(rect.width, rect.height) * 0.5f - Padding;
			if (outer <= 0f)
			{
				return;
			}
			float inner = Fill ? 0f : Mathf.Max(0f, outer - Thickness);
			Vector2 center = rect.center;
			Vector2 size = new Vector2(Mathf.Max(0.0001f, rect.width), Mathf.Max(0.0001f, rect.height));

			int steps = Mathf.Max(1, ArcSteps);
			float sweep = Mathf.Clamp01(Arc) * 360f;
			float direction = ArcInvert ? -1f : 1f;
			// ArcRotation 270 = 12 o'clock; ArcInvert = clockwise.
			float startAngle = 90f - (ArcRotation - 270f);
			_progress = Mathf.Clamp01(Progress);
			int progressSegments = Mathf.RoundToInt(steps * _progress);

			Color32 baseColor = color;
			Color32 progressColor = ProgressColor * new Color(1f, 1f, 1f, color.a);
			uvCenter = new Vector2(0.5f, 0.5f);

			for (int s = 0; s < steps; s++)
			{
				float a0 = (startAngle + direction * s * sweep / steps) * Mathf.Deg2Rad;
				float a1 = (startAngle + direction * (s + 1) * sweep / steps) * Mathf.Deg2Rad;
				Vector2 d0 = new Vector2(Mathf.Cos(a0), Mathf.Sin(a0));
				Vector2 d1 = new Vector2(Mathf.Cos(a1), Mathf.Sin(a1));
				Color32 segmentColor = s < progressSegments ? progressColor : baseColor;

				int baseIndex = vertices.Count;
				if (Fill)
				{
					vertices.Add(MakeVertex(center, segmentColor, rect, size));
					vertices.Add(MakeVertex(center + d0 * outer, segmentColor, rect, size));
					vertices.Add(MakeVertex(center + d1 * outer, segmentColor, rect, size));
					indices.Add(baseIndex);
					indices.Add(baseIndex + 1);
					indices.Add(baseIndex + 2);
				}
				else
				{
					vertices.Add(MakeVertex(center + d0 * inner, segmentColor, rect, size));
					vertices.Add(MakeVertex(center + d0 * outer, segmentColor, rect, size));
					vertices.Add(MakeVertex(center + d1 * outer, segmentColor, rect, size));
					vertices.Add(MakeVertex(center + d1 * inner, segmentColor, rect, size));
					indices.Add(baseIndex);
					indices.Add(baseIndex + 1);
					indices.Add(baseIndex + 2);
					indices.Add(baseIndex + 2);
					indices.Add(baseIndex + 3);
					indices.Add(baseIndex);
				}
			}
#if !UNITY_LUNA
			vh.AddUIVertexStream(vertices, indices);
#else
			int start = vh.currentVertCount;
			for (int i = 0; i < vertices.Count; i++)
			{
				vh.AddVert(vertices[i]);
			}
			for (int i = 0; i < indices.Count; i += 3)
			{
				vh.AddTriangle(start + indices[i], start + indices[i + 1], start + indices[i + 2]);
			}
#endif
		}

		private static UIVertex MakeVertex(Vector2 position, Color32 vertexColor, Rect rect, Vector2 size)
		{
			UIVertex vertex = UIVertex.simpleVert;
			vertex.position = position;
			vertex.color = vertexColor;
			vertex.uv0 = new Vector2((position.x - rect.xMin) / size.x, (position.y - rect.yMin) / size.y);
			return vertex;
		}

		public void SetProgress(float progress)
		{
			Progress = Mathf.Clamp01(progress);
			SetVerticesDirty();
		}

		public void SetArcSteps(int steps)
		{
			ArcSteps = steps;
			SetVerticesDirty();
		}

		public void SetInvertArc(bool invert)
		{
			ArcInvert = invert;
			SetVerticesDirty();
		}

		public void SetArcRotation(int rotation)
		{
			ArcRotation = rotation;
			SetVerticesDirty();
		}

		public void SetFill(bool fill)
		{
			Fill = fill;
			SetVerticesDirty();
		}

		public void SetBaseColor(Color color)
		{
			this.color = color;
			SetVerticesDirty();
		}

		public void UpdateBaseAlpha(float value)
		{
			Color c = color;
			c.a = value;
			color = c;
			SetVerticesDirty();
		}

		public void SetProgressColor(Color color)
		{
			ProgressColor = color;
			SetVerticesDirty();
		}

		public void UpdateProgressAlpha(float value)
		{
			ProgressColor.a = value;
			SetVerticesDirty();
		}

		public void SetPadding(int padding)
		{
			Padding = padding;
			SetVerticesDirty();
		}

		public void SetThickness(int thickness)
		{
			Thickness = thickness;
			SetVerticesDirty();
		}
	}
}
