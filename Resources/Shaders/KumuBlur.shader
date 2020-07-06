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

		TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        TEXTURE2D_SAMPLER2D(_BlurTex, sampler_BlurTex);
		float4 _MainTex_TexelSize;

        float   _Intensity;
        int     _Iterations;
        float2  _SampleScale;
        float   _Threshold;

        // rotate the uv by radian angle
        float2 rotate_rad(float2 st, float angle)
        {
            float2x2 mat = float2x2(cos(angle), -sin(angle),
            sin(angle), cos(angle));
            st -= 0.5;
            st = mul(mat, st);
            st += 0.5;
            return st;
        }

        // rotate the uv by degree angle
        float2 rotate_deg(float2 uv, float angle)
        {
            return rotate_rad(uv, radians(angle));
        }

        // convert uv from regular coordinate to polar coordinate
        float2 CartesianToPolar(float2 uv, float2 center, float tile)
        {
            float2 offsetUV = uv - center;
            float angle = atan2(offsetUV.y, offsetUV.x) * 0.5;
            // shorthand equation to get the distance (r) from center
            float r = sqrt(dot(offsetUV, offsetUV));
            return float2(r, tile * angle / PI);
        }

        float2 PolarToCartesian(float2 uv, float2 center, float tile)
        {
            float r = uv.x;
            float angle = uv.y * PI / tile * 2;
            return float2(cos(angle) * r + center.x, sin(angle) * r + center.y);
        }

        half4 Combine(half4 bloom, float2 uv)
        {
            half4 color = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv);
            return (bloom + color);
        }

        half4 OneDimensionalBlur(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float2 sampleScale)
        {
            float2 offsetUV = uv + sampleScale;
            half4 col = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv);
            half4 blur = SAMPLE_TEXTURE2D(tex, samplerTex, offsetUV);
            return col + blur;
        }

        half4 RadialBlur(TEXTURE2D_ARGS(tex, samplerTex), float2 uv, float2 texelSize, float2 sampleScale)
        {
            half4 col = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, uv);
            half4 blur = SAMPLE_TEXTURE2D(tex, samplerTex, rotate_deg(uv, sampleScale.x));

            return col + blur;
            // half4 acc =0;
            // for(int i =0; i<4; i++)
            // {
            //     acc += SAMPLE_TEXTURE2D(tex, samplerTex, rotate_deg(uv, (i/4.0 - 0.5) * sampleScale.x));
            // }
            // return acc / 4;
        }

        half4 FragDownsampleStandard(VaryingsDefault i) : SV_Target
        {
            half4 color = DownsampleBox13Tap(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy);
            return color;
        }

        half4 FragUpsampleStandard(VaryingsDefault i) : SV_Target
        {
            half4 color = UpsampleTent(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale.x);                      
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
            half4 color = UpsampleBox(TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale.x);
            return color; 
        }

        half4 FragOneDimensionalBlur(VaryingsDefault i) : SV_Target
        {
            return OneDimensionalBlur(  TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, 
                                    UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale);
        }

        half4 FragRadialBlur(VaryingsDefault i) : SV_Target
        {
            return RadialBlur(  TEXTURE2D_PARAM(_MainTex, sampler_MainTex), i.texcoord, 
                                UnityStereoAdjustedTexelSize(_MainTex_TexelSize).xy, _SampleScale);
        }

        half4 FragCombine(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            half4 blur = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, i.texcoord);
            
            // just blur
            return lerp(color, blur, _Intensity);
            // kinda psychedelic
            // return lerp(blur, color, 
            // smoothstep(0, 0.25, saturate(color) - _Threshold)); 
            
            // kinda like drunk effect
            // return lerp(color, blur, 
            // smoothstep(0, 1, saturate(color) - _Threshold)); 
        }

        half4 FragCombineCumulative(VaryingsDefault i) : SV_Target
        {
            half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            half4 blur = SAMPLE_TEXTURE2D(_BlurTex, sampler_BlurTex, i.texcoord);
            
            // just blur
            return lerp(color, blur, _Intensity) / (_Iterations + 1);
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

        // 4: One Dimensional Blur
        Pass
        {
            Name "One Dimensional Blur"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragOneDimensionalBlur
            ENDHLSL
        }

        // 5: Radial Blur
        Pass
        {
            Name "Radial Blur"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragRadialBlur
            ENDHLSL
        }

        // 6: Combine/output texture
        Pass
        {
            Name "Combine"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragCombine
            ENDHLSL
        }

        // 7: Combine/output texture
        Pass
        {
            Name "Combine Cumulative"
            HLSLPROGRAM
                #pragma vertex VertDefault
                #pragma fragment FragCombineCumulative
            ENDHLSL
        }
	}
}