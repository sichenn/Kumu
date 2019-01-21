Shader "Hidden/TP/PostProcessing/Grunge"
{
	HLSLINCLUDE
	// for Unity 2018.2 or Newer
		// #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
	// for below Unity 2018.2 
		#include "PostProcessing/Shaders/StdLib.hlsl"

		#include "Assets/Plugins/TP/Scripts/Shaders/PostProcessing/PSBlending/PSBlend.hlsl"


        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BlendTex, sampler_BlendTex);
		half _Strength;
		half2 _Tiling;



        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
			float2 blendTexUV = i.texcoord * _Tiling;
			float4 blendCol = SAMPLE_TEXTURE2D(_BlendTex, sampler_BlendTex, blendTexUV);

			// return float4(blendTexUV.x ,blendTexUV.y,0,1);
			// return blendCol;
            return blendSoftLight(color, blendCol, _Strength);
        }
	ENDHLSL
	SubShader
	{
		ZWrite Off
		Cull Off
		ZTest Always


		Pass
		{
			HLSLPROGRAM
				#pragma vertex VertDefault
				#pragma fragment Frag
			ENDHLSL
		}
	}
}
