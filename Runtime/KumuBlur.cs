using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Kumu
{
    /// <summary>
    /// Convolution kernel size for the Depth of Field effect.
    /// </summary>
    public enum BloomType
    {

        /// <summary>
        /// Gaussian Blur
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Box Blur.
        /// </summary>
        Box = 1,
    }

    [System.Serializable]
    public sealed class BloomTypeParameter : ParameterOverride<BloomType> { }

    [System.Serializable]
    [PostProcess(typeof(KumuBlurRenderer), PostProcessEvent.AfterStack, "Kumu/Blur")]
    public sealed class KumuBlur : PostProcessEffectSettings
    {
        [Range(0, 1)]
        public FloatParameter intensity = new FloatParameter() { value = 1};
        public BloomTypeParameter blurType = new BloomTypeParameter();
        /// <summary>
        /// The radius at which to blur
        /// </summary>
        /// <returns></returns>
        [Range(1f, 10f), Tooltip("Changes the extent of veiling effects. For maximum quality, use integer values. Because this value changes the internal iteration count, You should not animating it as it may introduce issues with the perceived radius.")]
        public FloatParameter diffusion = new FloatParameter() { value = 1 };

        [Range(0, 1)]
        public FloatParameter threshold = new FloatParameter() { value = 1 };

        /// <summary>
        /// Distorts the bloom to give an anamorphic look. Negative values distort vertically,
        /// positive values distort horizontally.
        /// </summary>
        [Range(-1f, 1f), Tooltip("Distorts the bloom to give an anamorphic look. Negative values distort vertically, positive values distort horizontally.")]
        public FloatParameter anamorphicRatio = new FloatParameter { value = 0f };


    }

    /// <summary>
    /// Bloom/blur postprocess adapted from Unity's Bloom
    /// </summary>
    sealed class KumuBlurRenderer : PostProcessEffectRenderer<KumuBlur>
    {
        enum Pass
        {
            DownSample = 0,
            Upsample = 2,
            Combine = 4
        }

        static class ShaderIDs
        {
            internal static readonly int Filter = Shader.PropertyToID("_Filter");
            internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int Radius = Shader.PropertyToID("_Radius");
            internal static readonly int BloomTex = Shader.PropertyToID("_BloomTex");
            internal static readonly int SampleScale = Shader.PropertyToID("_SampleScale");
            internal static readonly int Iterations = Shader.PropertyToID("_Iterations");
        }

        RenderTexture[] textures = new RenderTexture[16];

        /// <summary>
        /// Store each down/upsample's ID
        /// </summary>
        Level[] m_Pyramid;
        struct Level
        {
            internal int down;
            internal int up;
        }
        const int k_MaxPyramidSize = 16; // Just to make sure we handle 64k screens... Future-proof!
        int m_Iterations;

        public override void Init()
        {
            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BloomMipDown" + i),
                    up = Shader.PropertyToID("_BloomMipUp" + i)
                };
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Blur"));

            // set shader parameters
            cmd.BeginSample("Box Blur");
            sheet.properties.SetFloat(ShaderIDs.Intensity, settings.intensity.value);
            sheet.properties.SetFloat("_Threshold", settings.threshold.value);
            Blur(context, cmd, sheet);
            LensDirt();

            cmd.EndSample("Box Blur");
        }

        private void Blur(in PostProcessRenderContext context,
                            in UnityEngine.Rendering.CommandBuffer command,
                            in PropertySheet sheet)
        {
            // Negative anamorphic ratio values distort vertically - positive is horizontal
            float ratio = Mathf.Clamp(settings.anamorphicRatio, -1, 1);
            float ratioWidth = ratio < 0 ? -ratio : 0f;
            float ratioHeight = ratio > 0 ? ratio : 0f;


            Vector2Int textureSize = new Vector2Int(
                Mathf.FloorToInt(context.screenWidth / (2f - ratioWidth)),
                Mathf.FloorToInt(context.screenHeight / (2f - ratioHeight))
            );
            bool singlePassDoubleWide = (context.stereoActive &&
                                        (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) &&
                                        (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int textureWidthStereo = singlePassDoubleWide ? textureSize.x * 2 : textureSize.x;

            // Determines the iteration count
            int maxWidthHeight = Mathf.Max(textureSize.x, textureSize.y);
            float logs = Mathf.Log(maxWidthHeight, 2f) + Mathf.Min(settings.diffusion.value, 10f) - 10f;
            int logs_i = Mathf.FloorToInt(logs);
            m_Iterations = Mathf.Clamp(logs_i, 1, k_MaxPyramidSize);
            float sampleScale = 0.5f + logs - logs_i;
            sheet.properties.SetFloat(ShaderIDs.SampleScale, sampleScale);
            sheet.properties.SetInt(ShaderIDs.Iterations, m_Iterations);

            // Downsample
            var lastDown = context.source;
            for (int i = 0; i < m_Iterations; i++)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;
                context.GetScreenSpaceTemporaryRT(
                    command, mipDown, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                    FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                context.GetScreenSpaceTemporaryRT(
                    command, mipUp, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                    FilterMode.Bilinear, textureWidthStereo, textureSize.y);

                command.BlitFullscreenTriangle(
                        lastDown, mipDown, sheet,
                        (int)Pass.DownSample + (int)settings.blurType.value);

                lastDown = mipDown;

                // resize texture
                textureWidthStereo = (singlePassDoubleWide && ((textureWidthStereo / 2) % 2 > 0)) ?
                 1 + textureWidthStereo / 2 : textureWidthStereo / 2;
                textureWidthStereo = Mathf.Max(textureWidthStereo, 1);
                textureSize.y = Mathf.Max(textureSize.y / 2, 1);
            }

            // Upsample
            var lastUp = lastDown;
            for (int i = m_Iterations - 1; i >= 0; i--)
            {
                int mipDown = m_Pyramid[i].down;
                int mipUp = m_Pyramid[i].up;


                command.BlitFullscreenTriangle(lastUp, mipUp, sheet,
                    (int)Pass.Upsample + (int)settings.blurType.value);
                lastUp = mipUp;
            }
            command.SetGlobalTexture(ShaderIDs.BloomTex, lastUp);

            // Cleanup
            for (int i = 0; i < m_Iterations; i++)
            {
                if (m_Pyramid[i].down != lastUp)
                    command.ReleaseTemporaryRT(m_Pyramid[i].down);
                if (m_Pyramid[i].up != lastUp)
                    command.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Combine);
        }

        private void LensDirt()
        {

        }
    }
}
