// Unlit cartoon waterfall: water and waterfall maps (both scrolling, distorted by a scrolling normal map) blended
// by vertex colour R, foam edge from vertex colour G broken up by an edge-noise map.
Shader "zerinlabs/sh_fx_SOLID_cartoonWaterFall"
{
	Properties
	{
		_TEX1 ("Water map", 2D) = "white" {}
		_TEX1_speedX ("Water speed X:", Float) = 0.3
		_TEX1_speedY ("Water speed Y:", Float) = 0.3
		_TEX2 ("Waterfall map", 2D) = "white" {}
		_TEX2_speedX ("Waterfall speed X:", Float) = 0.3
		_TEX2_speedY ("Waterfall speed Y:", Float) = 0.3
		_TEX3 ("Edge noise map", 2D) = "white" {}
		_EdgeColor ("Edge color", Color) = (1,1,1,1)
		_EdgeThick ("Edge thickness", Range(0, 1)) = 0.5
		_EdgeSmooth ("Edge smoothness", Range(0, 0.5)) = 0.05
		_NM ("Distorsion map (NormalMap)", 2D) = "white" {}
		_NM_speedX ("Distorsion speed X:", Float) = 1
		_NM_speedY ("Distorsion speed Y:", Float) = 1
		_Dist ("Distorsion amount", Range(-1, 1)) = 0.03
		_Emission ("Emissive color", Color) = (0,0,0,1)
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" }
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

			sampler2D _TEX1;
			float4 _TEX1_ST;
			float _TEX1_speedX;
			float _TEX1_speedY;
			sampler2D _TEX2;
			float4 _TEX2_ST;
			float _TEX2_speedX;
			float _TEX2_speedY;
			sampler2D _TEX3;
			float4 _TEX3_ST;
			fixed4 _EdgeColor;
			float _EdgeThick;
			float _EdgeSmooth;
			sampler2D _NM;
			float4 _NM_ST;
			float _NM_speedX;
			float _NM_speedY;
			float _Dist;
			fixed4 _Emission;

			struct appdata
			{
				float4 vertex   : POSITION;
				float2 texcoord : TEXCOORD0;
				fixed4 color    : COLOR;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f
			{
				float4 pos      : SV_POSITION;
				float4 uvWater  : TEXCOORD0;
				float4 uvEdgeNM : TEXCOORD1;
				fixed4 color    : COLOR;
				UNITY_FOG_COORDS(2)
				UNITY_VERTEX_OUTPUT_STEREO
			};

			v2f vert(appdata v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uvWater.xy = TRANSFORM_TEX(v.texcoord, _TEX1);
				o.uvWater.zw = TRANSFORM_TEX(v.texcoord, _TEX2);
				o.uvEdgeNM.xy = TRANSFORM_TEX(v.texcoord, _TEX3);
				o.uvEdgeNM.zw = TRANSFORM_TEX(v.texcoord, _NM);
				o.color = v.color;
				UNITY_TRANSFER_FOG(o, o.pos);
				return o;
			}

			fixed4 frag(v2f i) : SV_Target
			{
				float2 distortion = UnpackNormal(tex2D(_NM, i.uvEdgeNM.zw + _Time.x * float2(_NM_speedX, _NM_speedY))).xy * _Dist;
				fixed4 water = tex2D(_TEX1, i.uvWater.xy + _Time.x * float2(_TEX1_speedX, _TEX1_speedY) + distortion);
				fixed4 fall = tex2D(_TEX2, i.uvWater.zw + _Time.x * float2(_TEX2_speedX, _TEX2_speedY) + distortion);
				fixed4 col = lerp(water, fall, i.color.r);

				float edge = i.color.g + (tex2D(_TEX3, i.uvEdgeNM.xy + distortion).r - 0.5) * 0.1;
				float smoothness = max(_EdgeSmooth, 0.0001);
				float foam = 1.0 - smoothstep(_EdgeThick - smoothness, _EdgeThick + smoothness, edge);

				col.rgb = lerp(col.rgb, _EdgeColor.rgb, foam) + _Emission.rgb;
				col.a = 1.0;
				UNITY_APPLY_FOG(i.fogCoord, col);
				return col;
			}
			ENDCG
		}
	}

	Fallback "Diffuse"
}
