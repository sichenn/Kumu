Shader "Hidden/Kumu/Blur" 
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

        #include "Sampling.hlsl"

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BlurTex, sampler_BlurTex);
		float4 _MainTex_TexelSize;

        float   _Intensity;
        int     _Iterations;
        float   _SampleScale;
        float   _Threshold;

        half4 Combine(half4 bloom, float2 uv)
        {
            half4 color = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv);
            return (bloom + color);
        }

        half4 FragDownsampleStandard(VaryingsDefault i) : SV_Target
        {
            half4 color = DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy);
            return color;
        }

        half4 FragUpsampleStandard(VaryingsDefault i) : SV_Target
        {
            half4 color = UpsampleTent(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale);                      
            return color;
        }

		half4 FragDownsample4(VaryingsDefault i) : SV_Target
        {
            
            half4 color = DownsampleBox4Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), 
                                            i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy);
            return color;
        }
		
		half4 FragUpsampleBox(VaryingsDefault i) : SV_Target
        {
            half4 color = UpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale);
            return color; 
        }

        half4 HorizontalBlur(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float sampleScale)
        {
            //get uv coordinate of sample
            float2 offsetUV = uv + float2(sampleScale, 0);
            half4 col = SAMPLE_TEXTURE2D(tex, samplerTex, uv);
            //add color at position to color
            half4 blur = SAMPLE_TEXTURE2D(tex, samplerTex, offsetUV);
            return (col + blur) / 2;
        }

        half4 FragHorizontalBlur(VaryingsDefault i) : SV_Target
        {
            return HorizontalBlur(  TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, 
                                    UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale);
        }

        half4 FragCombine(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            half4 blur = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, i.texcoord);
            
            // just blur
            return lerp(color, blur, _Intensity);
            // kinda psychedelic
            return lerp(blur, color, 
            smoothstep(0, 0.25, saturate(color) - _Threshold)); 
            // kinda like drunk
            return lerp(color, blur, 
            smoothstep(0, 1, saturate(color) - _Threshold)); 
        }

	ENDHLSL

	SubShader {
		Cull Off ZTest Always ZWrite Off
		
        // 0: Downsample Standard
        Pass 
        {
            Name "Downsample Standard"
            HLSLPROGRAM 
                #pragma vertex VertDefault
                #pragma fragment FragDownsampleStandard
            ENDHLSL
        }

		// 1: Downsample 4 taps
        Pass
        {
            Name "Downsample Box"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragDownsample4
            ENDHLSL
        }

        // 2: Upsample Standard
        Pass
        {
            Name "Upsample Standard"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragUpsampleStandard
            ENDHLSL
        }

		// 3: Upsample box 
        Pass
        {
            Name "Upsample Box"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragUpsampleBox
            ENDHLSL
        }

        // 4: Combine/output texture
        Pass
        {
            Name "Combine"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragCombine
            ENDHLSL
        }

        // 5: Horizontal Blur
        Pass
        {
            Name "Horizontal"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragHorizontalBlur
            ENDHLSL
        }
	}
}