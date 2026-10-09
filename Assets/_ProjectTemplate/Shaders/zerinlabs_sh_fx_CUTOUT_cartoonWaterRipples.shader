// Unlit alpha-tested foam ripples: a scrolling, distorted foam map masked by the ring mesh's vertex colour R.
Shader "zerinlabs/sh_fx_CUTOUT_cartoonWaterRipples"
{
	Properties
	{
		_colFoam ("Foam color", Color) = (1,1,1,1)
		_TEX ("Foam map", 2D) = "white" {}
		_NM ("Distorsion map (NormalMap)", 2D) = "white" {}
		_TEX1_speedX ("Foam speed X:", Float) = 0.3
		_TEX1_speedY ("Foam speed Y:", Float) = 0.3
		_NM_speedX ("Distorsion speed X:", Float) = 1
		_NM_speedY ("Distorsion speed Y:", Float) = 1
		_Dist ("Distorsion amount", Range(-1, 1)) = 0.03
		_amount ("Foam amount", Float) = 2
		_EdgeSmooth ("Foam smoothness", Range(0, 0.5)) = 0.05
		_Cutoff ("Alpha cutoff", Range(0, 1)) = 0.5
	}

	SubShader
	{
		Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" "IgnoreProjector"="True" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_fog
			#pragma multi_compile_instancing

			#include "UnityCG.cginc"

			fixed4 _colFoam;
			sampler2D _TEX;
			float4 _TEX_ST;
			sampler2D _NM;
			float4 _NM_ST;
			float _TEX1_speedX;
			float _TEX1_speedY;
			float _NM_speedX;
			float _NM_speedY;
			float _Dist;
			float _amount;
			float _EdgeSmooth;
			float _Cutoff;

			struct appdata
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color    : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos   : SV_POSITION;
				float4 uv    : TEXCOORD0;
				fixed4 color : COLOR;
				UNITY_FOG_COORDS(1)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv.xy = TRANSFORM_TEX(v.texcoord, _TEX);
				o.uv.zw = TRANSFORM_TEX(v.texcoord, _NM);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 distortion = UnpackNormal(tex2D(_NM, i.uv.zw + _Time.x * float2(_NM_speedX, _NM_speedY))).xy * _Dist;
				float foam = tex2D(_TEX, i.uv.xy + _Time.x * float2(_TEX1_speedX, _TEX1_speedY) + distortion).r * i.color.r * _amount;

				float smoothness = max(_EdgeSmooth, 0.0001);
				float alpha = smoothstep(_Cutoff - smoothness, _Cutoff + smoothness, foam);
				clip(alpha - 0.01);

				fixed4 col = fixed4(_colFoam.rgb, 1.0);
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
