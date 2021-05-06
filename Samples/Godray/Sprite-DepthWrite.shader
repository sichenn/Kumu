Shader "Kumu/Sprite (Pixel Lit)"
{
	Properties
	{
		_MainTex("Main Texture", 2D) = "white" {}
		_Color("Color", Color) = (1,1,1,1)

		[MaterialToggle] PixelSnap("Pixel snap", Float) = 0
		[PerRendererData] _AlphaTex("External Alpha", 2D) = "white" {}
		[PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0

		_ZWrite("Depth Write", Float) = 1.0
		_Cutoff("Depth alpha cutoff", Range(0,1)) = 0.5

		_ShadowAlphaCutoff("Shadow alpha cutoff", Range(0,1)) = 0.1
		_CustomRenderQueue("Custom Render Queue", Float) = 0.0

		[HideInInspector] _SrcBlend("__src", Float) = 1.0
		[HideInInspector] _DstBlend("__dst", Float) = 0.0
		[HideInInspector] _RenderQueue("__queue", Float) = 0.0
		[HideInInspector] _Cull("__cull", Float) = 0.0
	}

		SubShader
		{
			Tags { "Queue" = "Transparent" "RenderType" = "Sprite" "AlphaDepth" = "False" "CanUseSpriteAtlas" = "True" "IgnoreProjector" = "True" }
			LOD 200

			Pass
			{
				Name "FORWARD"
				Tags { "LightMode" = "ForwardBase" }
				Blend[_SrcBlend][_DstBlend]
				ZWrite[_ZWrite]
				ZTest LEqual
				Cull[_Cull]

				CGPROGRAM
					#include "UnityCG.cginc"
					#include "AutoLight.cginc"
					uniform sampler2D _MainTex;

					#if defined(_ALPHA_CLIP) 
					uniform fixed _Cutoff;
					#define ALPHA_CLIP(pixel, color) clip((pixel.a * color.a) - _Cutoff);
					#else
					#define ALPHA_CLIP(pixel, color)
					#endif

					#if ETC1_EXTERNAL_ALPHA
					//External alpha texture for ETC1 compression
					uniform sampler2D _AlphaTex;
					#endif //ETC1_EXTERNAL_ALPHA

					CBUFFER_START(UnityPerDrawSprite)
					#ifndef UNITY_INSTANCING_ENABLED
					fixed4 _RendererColor;
					float4 _Flip;
					#endif
					float _EnableExternalAlpha;
					CBUFFER_END


					uniform fixed4 _MainTex_ST;

					struct VertexOutput
					{
						float4 pos : SV_POSITION;
						fixed4 color : COLOR;
						float2 texcoord : TEXCOORD0;
						float4 posWorld : TEXCOORD1;
						UNITY_VERTEX_OUTPUT_STEREO
					};

					struct VertexInput
					{
						float4 vertex : POSITION;
						float4 texcoord : TEXCOORD0;
						float4 color : COLOR;
						UNITY_VERTEX_INPUT_INSTANCE_ID
					};

					inline fixed4 calculateTexturePixel(float2 texcoord)
					{
						fixed4 pixel = tex2D(_MainTex, texcoord);

						#if ETC1_EXTERNAL_ALPHA
						fixed4 alpha = tex2D(_AlphaTex, texcoord);
						pixel.a = lerp(pixel.a, alpha.r, _EnableExternalAlpha);
						#endif

						return pixel;
					}

					inline float4 calculateLocalPos(float4 vertex)
					{
#ifdef UNITY_INSTANCING_ENABLED
						vertex.xy *= _Flip.xy;
#endif

						float4 pos = UnityObjectToClipPos(vertex);

#ifdef PIXELSNAP_ON
						pos = UnityPixelSnap(pos);
#endif

						return pos;
					}

					VertexOutput vert(VertexInput v)
					{
						VertexOutput output;

						UNITY_SETUP_INSTANCE_ID(input);
						UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

						output.pos = calculateLocalPos(v.vertex);
						output.color = v.color;
						output.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);;
						output.posWorld = mul(unity_ObjectToWorld, v.vertex);

						TRANSFER_VERTEX_TO_FRAGMENT(output)

						return output;
					}


					fixed4 fragBase(VertexOutput input) : SV_Target
					{
						fixed4 texureColor = calculateTexturePixel(input.texcoord);
						ALPHA_CLIP(texureColor, input.color)

						return texureColor;
					}

					#pragma target 3.0

					#pragma shader_feature _ _ALPHABLEND_ON _ALPHAPREMULTIPLY_ON _ADDITIVEBLEND _ADDITIVEBLEND_SOFT _MULTIPLYBLEND _MULTIPLYBLEND_X2
					#pragma shader_feature _ _FIXED_NORMALS_VIEWSPACE _FIXED_NORMALS_VIEWSPACE_BACKFACE _FIXED_NORMALS_MODELSPACE  _FIXED_NORMALS_MODELSPACE_BACKFACE
					#pragma shader_feature _ _SPECULAR _SPECULAR_GLOSSMAP
					#pragma shader_feature _ALPHA_CLIP
					#pragma shader_feature _FOG

					#pragma multi_compile_fwdbase
					#pragma fragmentoption ARB_precision_hint_fastest
					#pragma multi_compile_fog
					#pragma multi_compile _ PIXELSNAP_ON
					#pragma multi_compile _ ETC1_EXTERNAL_ALPHA

					#pragma vertex vert
					#pragma fragment fragBase
				ENDCG
			}
			Pass
			{
				Name "ShadowCaster"
				Tags { "LightMode" = "ShadowCaster" }
				Offset 1, 1

				Fog { Mode Off }
				ZWrite On
				ZTest LEqual
				Cull Off
				Lighting Off

				CGPROGRAM
					#pragma fragmentoption ARB_precision_hint_fastest
					#pragma multi_compile_shadowcaster
					#pragma multi_compile _ PIXELSNAP_ON
					#pragma multi_compile _ ETC1_EXTERNAL_ALPHA

					#pragma vertex vert
					#pragma fragment frag

					#include "CGIncludes/SpriteShadows.cginc"
				ENDCG
			}
		}
		CustomEditor "SpriteShaderGUI"
}