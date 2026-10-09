using System;
using System.Collections.Generic;

namespace UnityEngine.UI.Extensions
{
	[RequireComponent(typeof(CanvasRenderer))]
	public class UIPrimitiveBase : MaskableGraphic, ILayoutElement, ICanvasRaycastFilter
	{
		protected static Material s_ETC1DefaultUI;

		private List<Vector2> outputList = new List<Vector2>();

		[SerializeField]
		private Sprite m_Sprite;

		[NonSerialized]
		private Sprite m_OverrideSprite;

		internal float m_EventAlphaThreshold = 1f;

		[SerializeField]
		private ResolutionMode m_improveResolution;

		[SerializeField]
		protected float m_Resolution;

		[SerializeField]
		private bool m_useNativeSize;

		public Sprite sprite
		{
			get
			{
				return m_Sprite;
			}
			set
			{
				if (m_Sprite == value)
				{
					return;
				}
				m_Sprite = value;
				GeneratedUVs();
				SetAllDirty();
			}
		}

		public Sprite overrideSprite
		{
			get
			{
				return activeSprite;
			}
			set
			{
				if (m_OverrideSprite == value)
				{
					return;
				}
				m_OverrideSprite = value;
				GeneratedUVs();
				SetAllDirty();
			}
		}

		protected Sprite activeSprite => m_OverrideSprite != null ? m_OverrideSprite : sprite;

		public float eventAlphaThreshold
		{
			get
			{
				return m_EventAlphaThreshold;
			}
			set
			{
				m_EventAlphaThreshold = value;
			}
		}

		public ResolutionMode ImproveResolution
		{
			get
			{
				return m_improveResolution;
			}
			set
			{
				m_improveResolution = value;
				SetAllDirty();
			}
		}

		public float Resolution
		{
			get
			{
				return m_Resolution;
			}
			set
			{
				m_Resolution = value;
				SetAllDirty();
			}
		}

		public bool UseNativeSize
		{
			get
			{
				return m_useNativeSize;
			}
			set
			{
				m_useNativeSize = value;
				SetAllDirty();
			}
		}

		public static Material defaultETC1GraphicMaterial
		{
			get
			{
				if (s_ETC1DefaultUI == null)
				{
					s_ETC1DefaultUI = Canvas.GetETC1SupportedCanvasMaterial();
				}
				return s_ETC1DefaultUI;
			}
		}

		public override Texture mainTexture
		{
			get
			{
				Sprite active = activeSprite;
				return active != null ? active.texture : s_WhiteTexture;
			}
		}

		public bool hasBorder
		{
			get
			{
				Sprite active = activeSprite;
				return active != null && active.border.sqrMagnitude > 0f;
			}
		}

		public float pixelsPerUnit
		{
			get
			{
				float spritePixelsPerUnit = 100f;
				Sprite active = activeSprite;
				if (active != null)
				{
					spritePixelsPerUnit = active.pixelsPerUnit;
				}
				float referencePixelsPerUnit = 100f;
				if (canvas != null)
				{
					referencePixelsPerUnit = canvas.referencePixelsPerUnit;
				}
				return spritePixelsPerUnit / referencePixelsPerUnit;
			}
		}

		public override Material material
		{
			get
			{
				if (m_Material != null)
				{
					return m_Material;
				}
				Sprite active = activeSprite;
				if (active != null && active.associatedAlphaSplitTexture != null)
				{
					return defaultETC1GraphicMaterial;
				}
				return defaultMaterial;
			}
			set
			{
				base.material = value;
			}
		}

		public virtual float minWidth => 0f;

		public virtual float preferredWidth
		{
			get
			{
				Sprite active = activeSprite;
				if (active == null)
				{
					return 0f;
				}
				return active.rect.size.x / pixelsPerUnit;
			}
		}

		public virtual float flexibleWidth => -1f;

		public virtual float minHeight => 0f;

		public virtual float preferredHeight
		{
			get
			{
				Sprite active = activeSprite;
				if (active == null)
				{
					return 0f;
				}
				return active.rect.size.y / pixelsPerUnit;
			}
		}

		public virtual float flexibleHeight => -1f;

		public virtual int layoutPriority => 0;

		protected UIPrimitiveBase()
		{
			useLegacyMeshGeneration = false;
		}

		protected UIVertex[] SetVbo(Vector2[] vertices, Vector2[] uvs)
		{
			UIVertex[] vbo = new UIVertex[4];
			for (int i = 0; i < vertices.Length && i < 4; i++)
			{
				UIVertex vert = UIVertex.simpleVert;
				vert.color = color;
				vert.position = vertices[i];
				vert.uv0 = uvs != null && i < uvs.Length ? uvs[i] : Vector2.zero;
				vbo[i] = vert;
			}
			return vbo;
		}

		protected Vector2[] IncreaseResolution(Vector2[] input)
		{
			return IncreaseResolution(new List<Vector2>(input)).ToArray();
		}

		protected List<Vector2> IncreaseResolution(List<Vector2> input)
		{
			outputList.Clear();
			switch (ImproveResolution)
			{
				case ResolutionMode.PerLine:
				{
					float totalDistance = 0f;
					for (int i = 0; i < input.Count - 1; i++)
					{
						totalDistance += Vector2.Distance(input[i], input[i + 1]);
					}
					ResolutionToNativeSize(totalDistance);
					float increments = m_Resolution > 0f ? totalDistance / m_Resolution : 0f;
					int incrementCount = 0;
					for (int i = 0; i < input.Count - 1; i++)
					{
						Vector2 p1 = input[i];
						outputList.Add(p1);
						Vector2 p2 = input[i + 1];
						float segmentDistance = Vector2.Distance(p1, p2);
						float steps = increments > 0f ? segmentDistance / increments : 0f;
						for (int j = 0; j < steps; j++)
						{
							outputList.Add(Vector2.Lerp(p1, p2, j * (1f / steps)));
							incrementCount++;
						}
						outputList.Add(p2);
					}
					break;
				}
				case ResolutionMode.PerSegment:
				{
					for (int i = 0; i < input.Count - 1; i++)
					{
						Vector2 p1 = input[i];
						outputList.Add(p1);
						Vector2 p2 = input[i + 1];
						ResolutionToNativeSize(Vector2.Distance(p1, p2));
						float steps = m_Resolution > 0f ? 1f / m_Resolution : 0f;
						for (float j = 1f; j < steps; j++)
						{
							outputList.Add(Vector2.Lerp(p1, p2, j * m_Resolution));
						}
						outputList.Add(p2);
					}
					break;
				}
				default:
					outputList.AddRange(input);
					break;
			}
			return new List<Vector2>(outputList);
		}

		protected virtual void GeneratedUVs()
		{
		}

		protected virtual void ResolutionToNativeSize(float distance)
		{
		}

		public virtual void CalculateLayoutInputHorizontal()
		{
		}

		public virtual void CalculateLayoutInputVertical()
		{
		}

		public virtual bool IsRaycastLocationValid(Vector2 screenPoint, Camera eventCamera)
		{
			return true;
		}

		private Vector2 MapCoordinate(Vector2 local, Rect rect)
		{
			Sprite active = activeSprite;
			if (active == null)
			{
				return local;
			}
			Rect spriteRect = active.rect;
			return new Vector2(local.x * spriteRect.width / rect.width, local.y * spriteRect.height / rect.height);
		}

		private Vector4 GetAdjustedBorders(Vector4 border, Rect rect)
		{
			for (int axis = 0; axis <= 1; axis++)
			{
				float combinedBorders = border[axis] + border[axis + 2];
				if (rect.size[axis] < combinedBorders && combinedBorders != 0f)
				{
					float borderScaleRatio = rect.size[axis] / combinedBorders;
					border[axis] *= borderScaleRatio;
					border[axis + 2] *= borderScaleRatio;
				}
			}
			return border;
		}

		protected override void OnEnable()
		{
			base.OnEnable();
			SetAllDirty();
		}
	}
}
