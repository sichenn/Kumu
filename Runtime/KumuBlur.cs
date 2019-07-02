using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;


namespace Kumu
{
    /// <summary>
    /// Convolution kernel size for the Depth of Field effect.
    /// </summary>
    public enum BlurType
    {

        /// <summary>
        /// Gaussian Blur
        /// </summary>
        Standard = 0,
        /// <summary>
        /// Box Blur.
        /// </summary>
        Box = 1,
        /// <summary>
        /// Blur on one dimension
        /// </summary>
        OneDimensional = 2,
        /// <summary>
        /// Radial Blur
        /// </summary>
        Radial = 4
    }

    public enum DownSample
    {
        None = 1,
        x2 = 2,
        x4 = 4,
        x8 = 8,
        x16 = 16
    }

    [System.Serializable]
    public sealed class BloomTypeParameter : ParameterOverride<BlurType> { }
    [System.Serializable]
    public sealed class DownsampleParameter : ParameterOverride<DownSample> { }

    [System.Serializable]
    [PostProcess(typeof(KumuBlurRenderer), PostProcessEvent.AfterStack, "Kumu/Blur")]
    public sealed class KumuBlur : PostProcessEffectSettings
    {
        public BloomTypeParameter blurType = new BloomTypeParameter();
        [Range(0, 1)]
        public FloatParameter blend = new FloatParameter() { value = 1 };
        public DownsampleParameter downsample = new DownsampleParameter() { value = DownSample.None };
        [Range(1, 16)]
        public IntParameter iterations = new IntParameter() { value = 1 };
        /// <summary>
        /// The radius at which to blur
        /// </summary>
        /// <returns></returns>
        [Range(0, 100)]
        public FloatParameter diffusion = new FloatParameter() { value = 1 };
        [Range(0, 1)]
        public FloatParameter threshold = new FloatParameter() { value = 1 };
        /// <summary>
        /// Controls one directional blur's direction
        /// </summary>
        /// <returns></returns>
        [Range(0, 360)]
        public FloatParameter angle = new FloatParameter() { value = 0 };
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
            OneDimensional = 4,
            Radial = 5,
            Combine = 6,
            CombineCumulative = 7,
        }

        static class ShaderIDs
        {
            internal static readonly int Filter = Shader.PropertyToID("_Filter");
            internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int Radius = Shader.PropertyToID("_Radius");
            internal static readonly int BlurTex = Shader.PropertyToID("_BlurTex");
            internal static readonly int SampleScale = Shader.PropertyToID("_SampleScale");
            internal static readonly int Iterations = Shader.PropertyToID("_Iterations");
        }

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
        Vector2 m_SampleScale;

        public override void Init()
        {
            m_Pyramid = new Level[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                m_Pyramid[i] = new Level
                {
                    down = Shader.PropertyToID("_BlurMipDown" + i),
                    up = Shader.PropertyToID("_BlurMipUp" + i)
                };
            }
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Blur"));

            // set shader parameters
            sheet.properties.SetFloat(ShaderIDs.Intensity, settings.blend.value);

            // for some blurs, the unnecessary middle blur is omitted
            sheet.properties.SetFloat(ShaderIDs.Iterations, settings.iterations.value);
            sheet.properties.SetFloat("_Threshold", settings.threshold.value);


            switch (settings.blurType.value)
            {
                case BlurType.OneDimensional:
                    BlurOneDimensional(context, cmd, sheet);
                    break;
                case BlurType.Standard:
                    Blur(context, cmd, sheet);
                    break;
                case BlurType.Box:
                    Blur(context, cmd, sheet);
                    break;
                case BlurType.Radial:
                    BlurRadial(context, cmd, sheet);
                    break;
            }
        }

        private void BlurRadial(in PostProcessRenderContext context,
                                    in UnityEngine.Rendering.CommandBuffer command,
                                    in PropertySheet sheet)
        {
            command.BeginSample("Blur Radial");

            // Calculate texture size
            Vector2Int textureSize = new Vector2Int(
                Mathf.FloorToInt(context.screenWidth / ((int)settings.downsample.value)),
                Mathf.FloorToInt(context.screenHeight / ((int)settings.downsample.value))
            );

            bool singlePassDoubleWide = (context.stereoActive &&
                                        (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) &&
                                        (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int textureWidthStereo = singlePassDoubleWide ? textureSize.x * 2 : textureSize.x;

            int iterations = settings.iterations.value;
            m_SampleScale.x = settings.diffusion.value;
            m_SampleScale.y = settings.diffusion.value;


            RenderTargetIdentifier lastBlur = context.source;

            if (iterations == 1)
            {
                int blurID = m_Pyramid[0].down;

                sheet.properties.SetVector(ShaderIDs.SampleScale, -0.5f * m_SampleScale);
                command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
                context.GetScreenSpaceTemporaryRT(
                                    command, blurID, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                                    FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                command.BlitFullscreenTriangle(context.source, blurID, sheet, (int)Pass.Radial);

                lastBlur = blurID;
            }
            else
            {
                for (int i = 0; i < iterations / 2; i++)
                {
                    int blurID = m_Pyramid[i].down;
                    // sheet.properties.SetVector(ShaderIDs.SampleScale, m_SampleScale);
                    sheet.properties.SetVector(ShaderIDs.SampleScale, (i / (float)iterations - 0.5f) * m_SampleScale);
                    context.GetScreenSpaceTemporaryRT(
                                        command, blurID, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                                        FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                    command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
                    command.BlitFullscreenTriangle(context.source, blurID, sheet, (int)Pass.Radial);

                    lastBlur = blurID;
                }

                for (int i = iterations / 2; i < iterations; i++)
                {
                    int blurID = m_Pyramid[i].down;
                    // sheet.properties.SetVector(ShaderIDs.SampleScale, m_SampleScale);
                    sheet.properties.SetVector(ShaderIDs.SampleScale, ((i + 1) / (float)iterations - 0.5f) * m_SampleScale);
                    context.GetScreenSpaceTemporaryRT(
                                        command, blurID, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                                        FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                    command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
                    command.BlitFullscreenTriangle(context.source, blurID, sheet, (int)Pass.Radial);

                    lastBlur = blurID;
                }
            }



            command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
            command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.CombineCumulative);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                command.ReleaseTemporaryRT(m_Pyramid[i].down);
            }
            command.EndSample("Blur Radial");
        }

        private void BlurOneDimensional(in PostProcessRenderContext context,
                                    in UnityEngine.Rendering.CommandBuffer command,
                                    in PropertySheet sheet)
        {
            command.BeginSample("Blur One Dimensional");
            // Calculate texture size
            Vector2Int textureSize = new Vector2Int(
                Mathf.FloorToInt(context.screenWidth / ((int)settings.downsample.value)),
                Mathf.FloorToInt(context.screenHeight / ((int)settings.downsample.value))
            );

            bool singlePassDoubleWide = (context.stereoActive &&
                                        (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) &&
                                        (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int textureWidthStereo = singlePassDoubleWide ? textureSize.x * 2 : textureSize.x;

            int iterations = settings.iterations.value;
            m_SampleScale.x = Mathf.Cos(settings.angle * Mathf.Deg2Rad) * settings.diffusion.value;
            m_SampleScale.y = Mathf.Sin(settings.angle * Mathf.Deg2Rad) * settings.diffusion.value;

            RenderTargetIdentifier lastBlur = context.source;

            // special case when there's only 1 iteration 
            if (iterations == 1)
            {
                int blurID = m_Pyramid[0].down;

                sheet.properties.SetVector(ShaderIDs.SampleScale, m_SampleScale);
                command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
                context.GetScreenSpaceTemporaryRT(
                                    command, blurID, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                                    FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                command.BlitFullscreenTriangle(context.source, blurID, sheet, (int)Pass.OneDimensional);

                lastBlur = blurID;
            }
            else
            {
                for (int i = 0; i < iterations; i++)
                {
                    int blurID = m_Pyramid[i].down;

                    sheet.properties.SetVector(ShaderIDs.SampleScale, ((float)i / (iterations - 1) - 0.5f) * m_SampleScale);

                    context.GetScreenSpaceTemporaryRT(
                                        command, blurID, 0, context.sourceFormat, RenderTextureReadWrite.Default,
                                        FilterMode.Bilinear, textureWidthStereo, textureSize.y);
                    command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
                    command.BlitFullscreenTriangle(context.source, blurID, sheet, (int)Pass.OneDimensional);

                    lastBlur = blurID;
                }
            }

            command.SetGlobalTexture(ShaderIDs.BlurTex, lastBlur);
            command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.CombineCumulative);

            // Cleanup
            for (int i = 0; i < iterations; i++)
            {
                command.ReleaseTemporaryRT(m_Pyramid[i].down);
            }
            command.EndSample("Blur One Dimensional");
        }

        private void Blur(in PostProcessRenderContext context,
                            in UnityEngine.Rendering.CommandBuffer command,
                            in PropertySheet sheet)
        {
            command.BeginSample("Blur");
            Vector2Int textureSize = new Vector2Int(
                Mathf.FloorToInt(context.screenWidth / 2f),
                Mathf.FloorToInt(context.screenHeight / 2f)
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
            command.SetGlobalTexture(ShaderIDs.BlurTex, lastUp);

            // Cleanup
            for (int i = 0; i < m_Iterations; i++)
            {
                if (m_Pyramid[i].down != lastUp)
                    command.ReleaseTemporaryRT(m_Pyramid[i].down);
                if (m_Pyramid[i].up != lastUp)
                    command.ReleaseTemporaryRT(m_Pyramid[i].up);
            }

            command.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.Combine);
            command.EndSample("Blur");
        }
    }
}
