Shader "Hidden/Kumu/GradientOverlay"
{
	HLSLINCLUDE
	// for Unity 2018.2 or Newer
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
	// for below Unity 2018.2 
		// #include "PostProcessing/Shaders/StdLib.hlsl"
		#include "PSBlend.hlsl"

		float _Strength;
		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
		TEXTURE2D_SAMPLER2D(_GradientTex, sampler_GradientTex);


		float Posterize(float In, float Steps)
		{
			return floor(In / (1 / Steps)) * (1 / Steps);
		}

		float4 Frag(VaryingsDefault i) : SV_Target
		{
			float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
			float4 gradient = SAMPLE_TEXTURE2D(_GradientTex, sampler_GradientTex, i.texcoord);

			return gradient * _Strength + color;
        }
	ENDHLSL
	SubShader
	{
		ZWrite Off Cull Off ZTest Always

		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment Frag
			ENDHLSL
		}
	}
}
