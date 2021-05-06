#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
int _Repeat;
float _Divisor;
float _Offset;
float _Roll;


half4 FragTexture(VaryingsDefault i) : SV_Target
{
    // Convert to the polar coordinate.
        float2 sc = i.texcoord - 0.5;
        float phi = atan2(sc.y, sc.x);
        float r = sqrt(dot(sc, sc));

        // Angular repeating.
        phi += _Offset;
        phi = phi - _Divisor * floor(phi / _Divisor);
        #if SYMMETRY_ON
        phi = min(phi, _Divisor - phi);
        #endif
        phi += _Roll - _Offset;

        // Convert back to the texture coordinate.
        float2 uv = float2(cos(phi), sin(phi)) * r + 0.5;

        // Reflection at the border of the screen.
        uv = max(min(uv, 2.0 - uv), -uv);

        return SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex, uv);
}

half GetAngle(half2 texcoord)
{
    return atan2(texcoord.y, texcoord.x);
}

half2 GetPivotVector(half2 texcoord)
{
    return texcoord - 0.5;
}

void RotateVector(inout half2 v, half angle)
{
    half radian = radians(angle);
    v.x = v.x * cos(radian) - v.y * sin(radian);
    v.y = v.x * sin(radian) + v.y * cos(radian);
}
