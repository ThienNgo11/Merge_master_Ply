// Toony Colors Pro 2 (v1 legacy) style mobile toon shader: ramp from threshold/smoothing or ramp texture,
// highlight/shadow tint, optional normal map.
Shader "Toony Colors Pro 2/Legacy/Mobile"
{
	Properties
	{
		_Color ("Color", Color) = (1,1,1,1)
		_HColor ("Highlight Color", Color) = (0.785,0.785,0.785,1)
		_SColor ("Shadow Color", Color) = (0.195,0.195,0.195,1)
		_MainTex ("Main Texture (RGB) Spec/MatCap Mask (A) ", 2D) = "white" {}
		[TCP2Gradient] _Ramp ("#RAMPT# Toon Ramp (RGB)", 2D) = "gray" {}
		_RampThreshold ("#RAMPF# Ramp Threshold", Range(0,1)) = 0.5
		_RampSmooth ("#RAMPF# Ramp Smoothing", Range(0.01,1)) = 0.1
		_BumpMap ("#NORM# Normal map (RGB)", 2D) = "bump" {}
	}

	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 200

		CGPROGRAM
		#pragma surface surf ToonyColors noforwardadd interpolateview halfasview
		#pragma target 2.0
		#pragma multi_compile __ TCP2_RAMPTEXT
		#pragma multi_compile __ TCP2_BUMP

		fixed4 _Color;
		sampler2D _MainTex;
		fixed4 _HColor;
		fixed4 _SColor;
		sampler2D _Ramp;
		fixed _RampThreshold;
		fixed _RampSmooth;
		#if TCP2_BUMP
		sampler2D _BumpMap;
		#endif

		struct Input
		{
			half2 uv_MainTex;
			#if TCP2_BUMP
			half2 uv_BumpMap;
			#endif
		};

		inline half4 LightingToonyColors (SurfaceOutput s, half3 lightDir, half atten)
		{
			#ifndef USING_DIRECTIONAL_LIGHT
			lightDir = normalize(lightDir);
			#endif
			fixed ndl = max(0, dot(s.Normal, lightDir) * 0.5 + 0.5);
			#if TCP2_RAMPTEXT
			fixed3 ramp = tex2D(_Ramp, fixed2(ndl, ndl)).rgb;
			#else
			fixed3 ramp = smoothstep(_RampThreshold - _RampSmooth * 0.5, _RampThreshold + _RampSmooth * 0.5, ndl).xxx;
			#endif
			ramp *= atten;
			fixed4 sColor = lerp(_HColor, _SColor, _SColor.a);
			ramp = lerp(sColor.rgb, _HColor.rgb, ramp);
			fixed4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * ramp;
			c.a = s.Alpha;
			return c;
		}

		void surf (Input IN, inout SurfaceOutput o)
		{
			fixed4 mainTex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = mainTex.rgb * _Color.rgb;
			o.Alpha = mainTex.a * _Color.a;
			#if TCP2_BUMP
			o.Normal = UnpackNormal(tex2D(_BumpMap, IN.uv_BumpMap));
			#endif
		}
		ENDCG
	}

	Fallback "Diffuse"
}
