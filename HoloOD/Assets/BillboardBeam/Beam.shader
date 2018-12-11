// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Volumetric Billboard Beam/Beam" {
	Properties {
		_MainTex ("Base (RGB)", 2D) = "white" {}
	}
	CGINCLUDE
	#include "UnityCG.cginc"
	struct v2f {
		float4 pos : POSITION;
		float2 tex : TEXCOORD0;
		float4 col : COLOR;
	};
	sampler2D _MainTex;
	v2f vert (float4 pos : POSITION, float2 tex : TEXCOORD0, float4 col : COLOR) {
		v2f o;
		o.pos = UnityObjectToClipPos(pos);
		o.tex = tex;
		o.col = col;
		return o;
	}
	float4 frag (v2f i) : COLOR {
		return tex2D(_MainTex, i.tex) * i.col;
	}
	ENDCG 
	SubShader {
		Tags { "RenderType" = "Transparent" "Queue" = "Transparent" }
		Blend One One
		ZTest LEqual
		Cull Off
		Fog { Mode off }
		Pass {    
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
	Fallback Off
}
