namespace UnityEngine.UI.Extensions
{
	[ExecuteInEditMode]
	[RequireComponent(typeof(CanvasRenderer), typeof(ParticleSystem))]
	[AddComponentMenu("UI/Effects/Extensions/UIParticleSystem")]
	public class UIParticleSystem : MaskableGraphic
	{
		private const int MaxParticleCount = 14000;

		private const string FallbackShaderName = "UI Extensions/Particles/Additive";

		[Tooltip("Having this enabled run the system in LateUpdate rather than in Update making it faster but less precise (more clunky)")]
		public bool fixedTime;

		[Tooltip("Enables 3d rotation for the particles")]
		public bool use3dRotation;

		private Transform _transform;

		private ParticleSystem pSystem;

		private ParticleSystem.Particle[] particles;

		private UIVertex[] _quad = new UIVertex[4];

		private Vector4 imageUV = Vector4.zero;

		private ParticleSystem.TextureSheetAnimationModule textureSheetAnimation;

		private int textureSheetAnimationFrames;

		private Vector2 textureSheetAnimationFrameSize;

		private ParticleSystemRenderer pRenderer;

		private bool isInitialised;

		private Material currentMaterial;

		private Texture currentTexture;

		private ParticleSystem.MainModule mainModule;

		public override Texture mainTexture => currentTexture;

		protected bool Initialize()
		{
			if (_transform == null)
			{
				_transform = transform;
			}

			if (pSystem == null)
			{
				pSystem = GetComponent<ParticleSystem>();
				if (pSystem == null)
				{
					return false;
				}

				mainModule = pSystem.main;
				if (mainModule.maxParticles > MaxParticleCount)
				{
					mainModule.maxParticles = MaxParticleCount;
				}

				// The world-space renderer is hidden: the particles are drawn by this Graphic instead.
				pRenderer = pSystem.GetComponent<ParticleSystemRenderer>();
				if (pRenderer != null)
				{
					pRenderer.enabled = false;
				}

				if (m_Material == null && pRenderer != null && pRenderer.sharedMaterial != null)
				{
					material = pRenderer.sharedMaterial;
				}
				else if (material == null)
				{
					var foundShader = Shader.Find(FallbackShaderName);
					if (foundShader != null)
					{
						material = new Material(foundShader);
					}
				}

				currentMaterial = material;
				if (currentMaterial != null && currentMaterial.HasProperty("_MainTex"))
				{
					currentTexture = GetTexture(currentMaterial);
				}
				material = currentMaterial;

				mainModule.scalingMode = ParticleSystemScalingMode.Hierarchy;
				particles = null;
			}

			if (particles == null)
			{
				particles = new ParticleSystem.Particle[pSystem.main.maxParticles];
			}

			imageUV = new Vector4(0f, 0f, 1f, 1f);

			textureSheetAnimation = pSystem.textureSheetAnimation;
			textureSheetAnimationFrames = 0;
			textureSheetAnimationFrameSize = Vector2.zero;
			if (textureSheetAnimation.enabled)
			{
				textureSheetAnimationFrames = textureSheetAnimation.numTilesX * textureSheetAnimation.numTilesY;
				textureSheetAnimationFrameSize = new Vector2(1f / textureSheetAnimation.numTilesX, 1f / textureSheetAnimation.numTilesY);
			}

			return true;
		}

		protected override void Awake()
		{
			base.Awake();
			if (!Initialize())
			{
				enabled = false;
			}
		}

		protected override void OnEnable()
		{
			base.OnEnable();

			// Replays one-shot bursts (confetti, sparkles) every time the hosting popup is shown again.
			if (Application.isPlaying && pSystem != null && mainModule.playOnAwake)
			{
				pSystem.Simulate(0f, false, true, true);
			}
		}

		protected override void OnPopulateMesh(VertexHelper vh)
		{
#if UNITY_EDITOR
			if (!Application.isPlaying && !Initialize())
			{
				return;
			}
#endif
			vh.Clear();

			if (!gameObject.activeInHierarchy || pSystem == null || particles == null)
			{
				return;
			}

			if (!isInitialised && !pSystem.main.playOnAwake)
			{
				pSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
				isInitialised = true;
			}

			Vector2 temp = Vector2.zero;
			Vector2 corner1 = Vector2.zero;
			Vector2 corner2 = Vector2.zero;
			int count = pSystem.GetParticles(particles);

			for (int i = 0; i < count; ++i)
			{
				ParticleSystem.Particle particle = particles[i];

				Vector2 position = mainModule.simulationSpace == ParticleSystemSimulationSpace.Local
					? (Vector2)particle.position
					: (Vector2)_transform.InverseTransformPoint(particle.position);

				float rotation = -particle.rotation * Mathf.Deg2Rad;
				float rotation90 = rotation + Mathf.PI / 2f;
				Color32 color = particle.GetCurrentColor(pSystem);
				float size = particle.GetCurrentSize(pSystem) * 0.5f;

				if (mainModule.scalingMode == ParticleSystemScalingMode.Shape && canvas != null)
				{
					position /= canvas.scaleFactor;
				}

				Vector4 particleUV = imageUV;
				if (textureSheetAnimation.enabled && textureSheetAnimationFrames > 0)
				{
					particleUV = GetTextureSheetUV(particle);
				}

				temp.x = particleUV.x;
				temp.y = particleUV.y;
				_quad[0] = UIVertex.simpleVert;
				_quad[0].color = color;
				_quad[0].uv0 = temp;

				temp.x = particleUV.x;
				temp.y = particleUV.w;
				_quad[1] = UIVertex.simpleVert;
				_quad[1].color = color;
				_quad[1].uv0 = temp;

				temp.x = particleUV.z;
				temp.y = particleUV.w;
				_quad[2] = UIVertex.simpleVert;
				_quad[2].color = color;
				_quad[2].uv0 = temp;

				temp.x = particleUV.z;
				temp.y = particleUV.y;
				_quad[3] = UIVertex.simpleVert;
				_quad[3].color = color;
				_quad[3].uv0 = temp;

				if (rotation == 0f)
				{
					corner1.x = position.x - size;
					corner1.y = position.y - size;
					corner2.x = position.x + size;
					corner2.y = position.y + size;

					temp.x = corner1.x;
					temp.y = corner1.y;
					_quad[0].position = temp;
					temp.x = corner1.x;
					temp.y = corner2.y;
					_quad[1].position = temp;
					temp.x = corner2.x;
					temp.y = corner2.y;
					_quad[2].position = temp;
					temp.x = corner2.x;
					temp.y = corner1.y;
					_quad[3].position = temp;
				}
				else if (use3dRotation)
				{
					Vector3 pos3D = mainModule.simulationSpace == ParticleSystemSimulationSpace.Local
						? particle.position
						: _transform.InverseTransformPoint(particle.position);
					if (mainModule.scalingMode == ParticleSystemScalingMode.Shape && canvas != null)
					{
						pos3D /= canvas.scaleFactor;
					}

					Quaternion particleRotation = Quaternion.Euler(particle.rotation3D);
					_quad[0].position = pos3D + particleRotation * new Vector3(-size, -size, 0f);
					_quad[1].position = pos3D + particleRotation * new Vector3(-size, size, 0f);
					_quad[2].position = pos3D + particleRotation * new Vector3(size, size, 0f);
					_quad[3].position = pos3D + particleRotation * new Vector3(size, -size, 0f);
				}
				else
				{
					Vector2 right = new Vector2(Mathf.Cos(rotation), Mathf.Sin(rotation)) * size;
					Vector2 up = new Vector2(Mathf.Cos(rotation90), Mathf.Sin(rotation90)) * size;

					_quad[0].position = position - right - up;
					_quad[1].position = position - right + up;
					_quad[2].position = position + right + up;
					_quad[3].position = position + right - up;
				}

				vh.AddUIVertexQuad(_quad);
			}
		}

		private Vector4 GetTextureSheetUV(ParticleSystem.Particle particle)
		{
			float lifeProgress = particle.startLifetime > 0f ? 1f - particle.remainingLifetime / particle.startLifetime : 0f;
			float frameProgress = lifeProgress;

			var frameOverTime = textureSheetAnimation.frameOverTime;
			if (frameOverTime.curveMin != null)
			{
				frameProgress = frameOverTime.curveMin.Evaluate(lifeProgress);
			}
			else if (frameOverTime.curve != null)
			{
				frameProgress = frameOverTime.curve.Evaluate(lifeProgress);
			}
			else if (frameOverTime.constant > 0f)
			{
				frameProgress = frameOverTime.constant - (1f - lifeProgress);
			}

			frameProgress = Mathf.Repeat(frameProgress * textureSheetAnimation.cycleCount, 1f);

			int tilesX = textureSheetAnimation.numTilesX;
			int tilesY = textureSheetAnimation.numTilesY;
			int frame = 0;
			switch (textureSheetAnimation.animation)
			{
				case ParticleSystemAnimationType.WholeSheet:
					frame = Mathf.FloorToInt(frameProgress * textureSheetAnimationFrames);
					break;
				case ParticleSystemAnimationType.SingleRow:
					frame = Mathf.FloorToInt(frameProgress * tilesX);
					int row = textureSheetAnimation.rowIndex;
#if !UNITY_LUNA
					if (textureSheetAnimation.rowMode == ParticleSystemAnimationRowMode.Random && tilesY > 0)
					{
						row = (int)(particle.randomSeed % (uint)tilesY);
					}
#endif
					frame += row * tilesX;
					break;
			}

			frame %= textureSheetAnimationFrames;

			Vector4 uv;
			uv.x = (frame % tilesX) * textureSheetAnimationFrameSize.x;
			uv.y = 1f - ((frame / tilesX) + 1) * textureSheetAnimationFrameSize.y;
			uv.z = uv.x + textureSheetAnimationFrameSize.x;
			uv.w = uv.y + textureSheetAnimationFrameSize.y;
			return uv;
		}

		private void Update()
		{
			if (!fixedTime && Application.isPlaying && pSystem != null)
			{
				pSystem.Simulate(Time.unscaledDeltaTime, false, false, true);
				SetAllDirty();
				RefreshIfMaterialChanged();
			}
		}

		private void LateUpdate()
		{
			if (!Application.isPlaying)
			{
				SetAllDirty();
			}
			else if (fixedTime && pSystem != null)
			{
				pSystem.Simulate(Time.unscaledDeltaTime, false, false, true);
				SetAllDirty();
				RefreshIfMaterialChanged();
			}

			if (material == currentMaterial)
			{
				return;
			}

			pSystem = null;
			Initialize();
		}

		private void RefreshIfMaterialChanged()
		{
			if ((currentMaterial != null && currentMaterial.HasProperty("_MainTex") && currentTexture != GetTexture(currentMaterial)) ||
				(material != null && currentMaterial != null && material.shader != currentMaterial.shader))
			{
				pSystem = null;
				Initialize();
			}
		}

		private static Texture GetTexture(Material source)
		{
			Texture texture = source.mainTexture;
			return texture != null ? texture : Texture2D.whiteTexture;
		}

		protected override void OnDestroy()
		{
			currentMaterial = null;
			currentTexture = null;
			base.OnDestroy();
		}

		public void StartParticleEmission()
		{
			if (pSystem == null)
			{
				return;
			}
#if !UNITY_LUNA
			pSystem.time = 0f;
#else
			pSystem.Clear();
#endif
			pSystem.Play();
		}

		public void StopParticleEmission()
		{
			if (pSystem == null)
			{
				return;
			}
			pSystem.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
		}

		public void PauseParticleEmission()
		{
			if (pSystem == null)
			{
				return;
			}
			pSystem.Stop(false, ParticleSystemStopBehavior.StopEmitting);
		}
	}
}
