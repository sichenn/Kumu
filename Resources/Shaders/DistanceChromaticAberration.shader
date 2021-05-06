Shader "Hidden/Kumu/DistanceChromaticAberration"
{
    HLSLINCLUDE
    // for Unity 2018.2 or Newer
		#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
	// for below Unity 2018.2 
		// #include "PostProcessing/Shaders/StdLib.hlsl"
    
    #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/Builtins/DiskKernels.hlsl"

    TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
    float4 _MainTex_TexelSize;

    TEXTURE2D_SAMPLER2D(_CameraDepthTexture, sampler_CameraDepthTexture);
    TEXTURE2D_SAMPLER2D(_SpecturalLut, sampler_SpecturalLut);
    TEXTURE2D_SAMPLER2D(_DepthOfFieldTex, sampler_DepthOfFieldTex);
    float4 _DepthOfFieldTex_TexelSize;

    half _Intensity;
    half _Power;
    half _Polarize;
    half _Distance;
    half _LensCoeff;
    float _MaxCoC;
    half _RcpMaxCoC;
    half _RcpAspect;

    // takes a value (0-1) and polarize it with given factor (0-1) 
    // a value smaller than 0.5 gets closer to 0
    // a value equal or greater than 0.5 gets closer to 1 
    float polarize(float value, float factor)
    {
        if(value < 0.5)
        {
            return lerp(value, 0, factor);
        }
        else
        {
            return lerp(value, 1, factor);
        }
    }

    float4 Frag(VaryingsDefault i) : SV_Target
    {
        // float near = _ProjectionParams.y;
        // float far = _ProjectionParams.z;
        // float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord);
        // float correctedDepth = 1 - depth;
        // return correctedDepth;

        float depth = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoordStereo));
        half coc = (depth - _Distance) * _LensCoeff / max(depth, 1e-5);
        half factor = saturate(coc * 0.5 * _RcpMaxCoC + 0.5);
        // return factor;
        factor = pow(factor, _Power);
        return factor;
        // use depth as a factor in intensity
        _Intensity *= factor;

        // Calculate foreground
        float2 disp = kDiskKernel[0] * _MaxCoC;
        float dist = length(disp);

        float2 duv = float2(disp.x * _RcpAspect, disp.y);
        half4 samp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord + duv);
        // Compare the CoC to the sample distance.
        // Add a small margin to smooth out.
        const half margin = _MainTex_TexelSize.y * 2;
        half fgWeight = saturate((-coc - dist + margin) / margin);
        fgWeight *= step(_MainTex_TexelSize.y, -coc);

        // BG: Calculate the alpha value only based on the center CoC.
        // This is a rather aggressive approximation but provides stable results.
        return fgWeight;
        // Cut influence from focused areas because they're darkened by CoC
        // premultiplying. This is only needed for near field.
        // fgWeight *= step(_MainTex_TexelSize.y, -coc);


        _Intensity = min(_Intensity, 1 - fgWeight);
        // return 1 - fgWeight;
        return fgWeight;
        // return _Intensity;
        float2 uvR = float2(i.texcoord.x - _Intensity, i.texcoord.y);
        float2 uvG = float2(i.texcoord.x + _Intensity, i.texcoord.y);
        float2 uvB = float2(i.texcoord.x, i.texcoord.y - _Intensity);

        float color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
        float colorR = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvR).r;
        float colorG = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvG).g;
        float colorB = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uvB).b;
        
        
        // return depth;
        // return lerp(float4(colorR, colorG, colorB, 1), color, factor);
        return float4(colorR, colorG, colorB, 1);
    }

    ENDHLSL
    
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always
        
        Pass
        {
            HLSLPROGRAM
                #define KERNEL_SMALL
                #pragma vertex VertDefault
                #pragma fragment Frag
            ENDHLSL
        }
    }
}
