// Adapted from Keijiro's Kino
// https://github.com/keijiro/KinoMirror
Shader "Hidden/Kumu/Kaleidoscope"
{
	SubShader
	{
		ZWrite Off Cull Off ZTest Always
		
		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment FragTexture
				#pragma multi_compile _ SYMMETRY_ON
				#include "Kaleidoscope.hlsl"
			ENDHLSL
		}
	}
}
