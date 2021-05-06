#include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"

TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);

half4 FragTexture(VaryingsDefault i) : SV_Target
{
    half2 mirrorTexcoord = i.texcoord;

    #if defined(Mirror_Horizontal)
        mirrorTexcoord.x = 0.5 - abs(0.5-mirrorTexcoord.x );
    #endif
    #if defined(Mirror_Vertical)
        mirrorTexcoord.y = 0.5 - abs(0.5-mirrorTexcoord.y);
    #endif
    
    half4 c = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mirrorTexcoord);
    return c;
}