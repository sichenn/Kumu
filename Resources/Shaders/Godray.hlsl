#ifndef GODRAY_INCLUDED
#define GODRAY_INCLUDED

half4 FragGodray(VaryingsDefault i) : SV_Target
{
    if (distance(i.texcoord, _LightPos) <= 0.01)
    {
        return float4(1, 0, 0, 1);
    }
    half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
    float depth = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, i.texcoord).r;
    depth = Linear01Depth(depth);

    float2 deltaTexcoord = i.texcoord - _LightPos; // tmp hard-coded light pos
    half godray = 0;
    float intensity = _Intensity;

    [unroll(GODRAY_ITERATION)]
    for (int u = 0; u < GODRAY_ITERATION; u++)
    {
        float2 shiftUV = i.texcoord - deltaTexcoord * (u + 1) / GODRAY_ITERATION * _Distance;
        float lightColor = SAMPLE_TEXTURE2D(_CameraDepthTexture, sampler_CameraDepthTexture, shiftUV).r;
        lightColor = Linear01Depth(lightColor);
        godray += lightColor * intensity;
        intensity *= _Decay;
    }
    godray /= GODRAY_ITERATION;
    color.rgb += _Tint * godray * (1 - depth);


    return color;
}

#endif // GODRAY_INCLUDED