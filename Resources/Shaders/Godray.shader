Shader "Hidden/Kumu/Godray"
{
    HLSLINCLUDE
    #define GODRAY_ITERATION 1   
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
            Name "Radial Blur (High)"
            HLSLPROGRAM 
                #define RADIAL_BLUR_ITERATION 16
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragRadialBlur
            ENDHLSL
        }
        
        Pass // 4
        {
            Name "Radial Blur (Mid)"
            HLSLPROGRAM
                #define RADIAL_BLUR_ITERATION 8
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragRadialBlur
            ENDHLSL
        }

        Pass // 5
        {
            Name "Radial Blur (Low)"
            HLSLPROGRAM
                #define RADIAL_BLUR_ITERATION 4
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragRadialBlur
            ENDHLSL
        }

        Pass // 6
        {
            Name "Combine Godray"
            HLSLPROGRAM
                #include "Godray.hlsl"
                #pragma vertex VertDefault
                #pragma fragment FragCombineGodray
            ENDHLSL

        }
    }
}
