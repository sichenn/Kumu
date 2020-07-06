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
        TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
        float2 _LightSourcePos; // light source position in screen space
        half3 _Tint;
        float _Decay;
        float _Intensity;
        float _Distance;
        float2 _LightPos;
        
        inline float easeOutCubic(float x)
        {
            return 1 - pow(1 - x, 3);
        }

        half4 FragRadialBlur(VaryingsDefault i) : SV_Target
        {
            float2 deltaTexcoord = i.texcoord - _LightPos; // tmp hard-coded light pos
            float4 result = 0;
            for (int u = 0; u < 8; u++)
            {
                float2 shiftUV = i.texcoord - deltaTexcoord * (u + 1) / 8 * 0.1;
                half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, shiftUV);
                result += c;
            }
            //return float4(1, 0, 0, 1);
            return result / 8;
        }
    ENDHLSL

    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass // 0
        {
            Name "Godray (High)"
            HLSLPROGRAM
                #define GODRAY_ITERATION 32

                #include "Godray.hlsl"

                #pragma vertex VertDefault
                #pragma fragment FragGodray
            ENDHLSL
        }

        Pass // 1
        {
            Name "Godray (Mid)"
            HLSLPROGRAM
                #define GODRAY_ITERATION 16
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragGodray
            ENDHLSL
        }

        Pass // 2
        {
            Name "Godray (Low)"
            HLSLPROGRAM
                #define GODRAY_ITERATION 8
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragGodray
            ENDHLSL
        }

        Pass // 3
        {
            Name "Radial Blur"
            HLSLPROGRAM 
                #pragma vertex VertDefault
                #pragma fragment FragRadialBlur
            ENDHLSL
        }
        
    }
}
