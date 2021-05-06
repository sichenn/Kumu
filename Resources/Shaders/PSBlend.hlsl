half4 blendNormal(half4 from, half4 to)
{
	return lerp(from, to, 0.5);
}

half4 blendNormal(half4 from, half4 to, half intensity)
{
	return lerp(from, to, intensity);
}

 
float4 blendSoftLight(float4 from, float4 to)
{
    float4 result = float4(0.0, 0.0, 0.0, 1.0);
    if (from.r > 0.5)
    {
        result.r = (1 - (1 - from.r) * (1 - (to.r - 0.5)));
    }
    else
    {
        result.r = from.r * (to.r + 0.5);
    }
    if (from.g > 0.5)
    {
        result.g = (1 - (1 - from.g) * (1 - (to.g - 0.5)));
    }
    else
    {
        result.g = from.g * (to.g + 0.5);
    }
    if (from.b > 0.5)
    {
        result.b = (1 - (1 - from.b) * (1 - (to.b - 0.5)));
    }
    else
    {
        result.b = from.b * (to.b + 0.5);
    }
    return result;
}

float4 blendSoftLight(float4 from, float4 to, float intensity)
{
    return lerp(from, blendSoftLight(from,to), intensity);
}