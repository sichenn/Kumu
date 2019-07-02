Shader "Hidden/Kumu/Godray"
{
    HLSLINCLUDE
        #if UNITY_VERSION > 201820
		    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
			#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Sampling.hlsl"
			#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Colors.hlsl"
		#else 
		    #include "PostProcessing/Shaders/StdLib.hlsl"
		    #include "PostProcessing/Shaders/Sampling.hlsl"
		    #include "PostProcessing/Shaders/Colors.hlsl"
        #endif

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

        half4 FragGodray(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            
            return color;
        }
    ENDHLSL

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name"Godray"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragGodray
            ENDHLSL
        }
    }
}
