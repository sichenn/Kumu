Shader "Hidden/Kumu/WaveDistortion"
{
	HLSLINCLUDE
	// for Unity 2018.2 or Newer
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
	// for below Unity 2018.2 
		// #include "PostProcessing/Shaders/StdLib.hlsl"
		#include "PSBlend.hlsl"


        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BlendTex, sampler_BlendTex);

		half _Period;
		half _Poisterization;
		half _Speed;
		half _Amplitude;

		float Posterize(float In, float Steps)
		{
			return floor(In / (1 / Steps)) * (1 / Steps);
		}

		float4 Frag(VaryingsDefault i) : SV_Target
		{
			float distort = sin(_Time.x * -10 * _Speed + i.texcoord.y * _Period); 
			distort = Posterize(distort, _Poisterization) * 0.01f * _Amplitude * i.texcoord.y;

			float2 uv = float2(i.texcoord.x + distort, i.texcoord.y);
			float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);

			return color;
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
