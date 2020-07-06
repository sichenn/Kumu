Shader "Hidden/Kumu/Mirror"
{
	SubShader
	{
		ZWrite Off Cull Off ZTest Always

		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment FragTexture
				#include "Mirror.hlsl"
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment FragTexture
				#define Mirror_Horizontal
				#include "Mirror.hlsl"
		
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment FragTexture
				#define Mirror_Vertical
				#include "Mirror.hlsl"
			ENDHLSL
		}

		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment FragTexture
				#define Mirror_Vertical
				#define Mirror_Horizont
				#include "Mirror.hlsl"
			ENDHLSL
		}
	}
}
