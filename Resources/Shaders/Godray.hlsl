#ifndef GODRAY_INCLUDED
#define GODRAY_INCLUDED

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
TEXTURE2D_SAMPLER2D(_GodrayTex, sampler_GodrayTex);
float2 _LightSourcePos; // light source position in screen space
half3 _Tint;
float _Decay;
float _Intensity;
float _Distance;
float2 _LightPos;

half4 FragGodray(VaryingsDefault i) : SV_Target
{
    if (distance(i.texcoord, _LightPos) <= 0.01)
    {
        return float4(1, 0, 0, 1);
    }
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord).r;
    depth = Linear01Depth(depth);

    float2 deltaTexcoord = i.texcoord - _LightPos; // tmp hard-coded light pos
    half godray = 0;
    float intensity = _Intensity;

    for (int u = 0; u < GODRAY_ITERATION; u++)
    {
        float2 shiftUV = i.texcoord - deltaTexcoord * (u + 1) / GODRAY_ITERATION * _Distance;
        float lightColor = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, shiftUV).r;
        lightColor = Linear01Depth(lightColor);
        godray += lightColor * intensity;
        intensity *= _Decay;
    }

    godray /= GODRAY_ITERATION;
    //godray = pow(godray, _Decay);

    return half4(_Tint * godray, 1);
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
    return result / 8;
}

half4 FragCombineGodray(VaryingsDefault i) : SV_Target
{
    half4 godray = SAMPLE_TEXTURE2D(_GodrayTex, sampler_GodrayTex, i.texcoord);
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
    //return godray;
    return c + godray;
}
#endif // GODRAY_INCLUDED