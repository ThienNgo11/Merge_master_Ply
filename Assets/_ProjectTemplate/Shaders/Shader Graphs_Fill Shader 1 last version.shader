// Diorama build-progress shader (last version): like "Fill Shader 1" plus a glowing band of height
// |_Transition_Offset| (in _Transition_Color) just below the fill line while the building is incomplete.
// Above the fill line the mesh is a flat, neutral dark-grey silhouette (original f_020/f_023: top faces
// (80,80,80), faces turned away from the sun ~(66,66,66), no texture, no tint, no coloured toon shadow),
// the same look as the unrevealed diorama ground pieces that use this shader.
Shader "Shader Graphs/Fill Shader 1 last version"
{
	Properties
	{
		_fill ("Fill", Range(0, 1)) = 0.5
		_height ("Height", Float) = 0
		_offset ("Offset", Float) = 0
		[NoScaleOffset] _texture2D ("Texture2D", 2D) = "white" {}
		[HDR] _Color ("Color", Color) = (0,0,0,0)
		_Tint ("Tint", Color) = (0,0,0,0)
		[ToggleUI] _Flip_Fill_Direction ("Flip Fill Direction", Float) = 0
		_Transition_Offset ("Transition_Offset", Float) = 0
		_Transition_Color ("Transition_Color", Color) = (0.8018868,0.1185178,0.1185178,0)
		[HideInInspector] _SilhouetteColor ("Silhouette Color", Color) = (0.314,0.314,0.314,1)
		[HideInInspector] _SilhouetteShade ("Silhouette Shade (away from light)", Range(0,1)) = 0.82
		[HideInInspector] _BUILTIN_QueueOffset ("Float", Float) = 0
		[HideInInspector] _BUILTIN_QueueControl ("Float", Float) = -1

		[HideInInspector] _HColor ("Highlight Color", Color) = (0.979,0.979,0.979,1)
		[HideInInspector] _SColor ("Shadow Color", Color) = (0.502,0.235,0.235,1)
		[HideInInspector] _RampThreshold ("Ramp Threshold", Range(0,1)) = 0.5
		[HideInInspector] _RampSmooth ("Ramp Smoothing", Range(0.01,1)) = 0.1
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" "Queue"="Geometry" "DisableBatching"="True" }
		LOD 200

		CGPROGRAM
		#pragma surface surf ToonFill fullforwardshadows vertex:vert addshadow
		#pragma target 3.0

		sampler2D _texture2D;
		half _fill;
		float _height;
		float _offset;
		fixed4 _Color;
		fixed4 _Tint;
		half _Flip_Fill_Direction;
		float _Transition_Offset;
		fixed4 _Transition_Color;
		fixed4 _SilhouetteColor;
		half _SilhouetteShade;
		fixed4 _HColor;
		fixed4 _SColor;
		half _RampThreshold;
		half _RampSmooth;

		struct Input
		{
			float2 fillUV;
			float objectY;
			float3 worldNormal;
		};

		void vert(inout appdata_full v, out Input o)
		{
			UNITY_INITIALIZE_OUTPUT(Input, o);
			o.fillUV = v.texcoord.xy;
			o.objectY = v.vertex.y;
		}

		inline half4 LightingToonFill(SurfaceOutput s, half3 lightDir, half atten)
		{
			#ifndef USING_DIRECTIONAL_LIGHT
			lightDir = normalize(lightDir);
			#endif
			half ndl = max(0, dot(s.Normal, lightDir) * 0.5 + 0.5);
			half3 ramp = smoothstep(_RampThreshold - _RampSmooth * 0.5, _RampThreshold + _RampSmooth * 0.5, ndl).xxx;
			ramp *= atten;
			half3 shadowColor = lerp(_HColor.rgb, _SColor.rgb, _SColor.a);
			ramp = lerp(shadowColor, _HColor.rgb, ramp);
			half4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp;
			c.a = s.Alpha;
			return c;
		}

		void surf(Input IN, inout SurfaceOutput o)
		{
			float fillY = _offset + _fill * _height;
			float d = IN.objectY - fillY;
			if (_Flip_Fill_Direction > 0.5)
			{
				d = -d;
			}
			half built = d <= 0.0 ? 1.0 : 0.0;
			half band = (built > 0.5 && d > _Transition_Offset && _fill < 0.999) ? 1.0 : 0.0;

			fixed4 tex = tex2D(_texture2D, IN.fillUV);
			// Unbuilt part: unlit flat grey with a simple two-tone shade so the silhouette keeps its volume.
			float3 lightDir = _WorldSpaceLightPos0.w > 0.5 ? float3(0, 1, 0) : normalize(_WorldSpaceLightPos0.xyz);
			half facing = saturate(dot(normalize(IN.worldNormal), lightDir));
			half3 silhouette = _SilhouetteColor.rgb * lerp(_SilhouetteShade, 1.0, facing);
			o.Albedo = tex.rgb * built;
			o.Emission = lerp(silhouette, _Color.rgb + _Transition_Color.rgb * band, built);
			o.Alpha = 1;
		}
		ENDCG
	}

	Fallback "Diffuse"
}
