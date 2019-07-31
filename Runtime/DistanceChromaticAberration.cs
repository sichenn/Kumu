using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Kumu
{
    [System.Serializable]
    [PostProcess(typeof(DistanceChromaticAberrationRenderer), PostProcessEvent.AfterStack, "Kumu/Distance Chromatic Aberration")]
    public sealed class DistanceChromaticAberration : PostProcessEffectSettings
    {
        public TextureParameter specturalLut = new TextureParameter();
        [Range(0, 1)] public FloatParameter intensity = new FloatParameter();
        [UnityEngine.Rendering.PostProcessing.Min(0.1f), Tooltip("Distance to the point of focus.")]
        public FloatParameter focusDistance = new FloatParameter { value = 10f };
        [Range(1, 10)]
        public IntParameter power = new IntParameter { value = 1 };
        [Range(0, 1)]
        public FloatParameter polarize = new FloatParameter { value = 0 };
        [Range(0.05f, 32f), Tooltip("Ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.")]
        public FloatParameter aperture = new FloatParameter { value = 5.6f };
        [Range(1f, 300f), Tooltip("Distance between the lens and the film. The larger the value is, the shallower the depth of field is.")]
        public FloatParameter focalLength = new FloatParameter { value = 50f };
        [DisplayName("Max Blur Size"), Tooltip("Convolution kernel size of the bokeh filter, which determines the maximum radius of bokeh. It also affects performances (the larger the kernel is, the longer the GPU time is required).")]
        public KernelSizeParameter kernelSize = new KernelSizeParameter { value = KernelSize.Medium };
    }

    sealed class DistanceChromaticAberrationRenderer : PostProcessEffectRenderer<DistanceChromaticAberration>
    {
        // Height of the 35mm full-frame format (36mm x 24mm)
        // TODO: Will be set by a physical camera when Post processing v3 comes out
        const float k_FilmHeight = 0.024f;

        static class ShaderIDs
        {
            internal static readonly int SpecturalLut = Shader.PropertyToID("_SpecturalLut");
            internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int Power = Shader.PropertyToID("_Power");
            internal static readonly int Polarize = Shader.PropertyToID("_Polarize");
            internal static readonly int Distance = Shader.PropertyToID("_Distance");
            internal static readonly int LensCoeff = Shader.PropertyToID("_LensCoeff");
            internal static readonly int MaxCoC = Shader.PropertyToID("_MaxCoC");
            internal static readonly int RcpMaxCoC = Shader.PropertyToID("_RcpMaxCoC");
            internal static readonly int RcpAspect = Shader.PropertyToID("_RcpAspect");
            internal static readonly int DepthOfFieldTex = Shader.PropertyToID("_DepthOfFieldTex");
        }

        public override void Render(PostProcessRenderContext context)
        {
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/DistanceChromaticAberration"));

            // Material setup
            float scaledFilmHeight = k_FilmHeight * (context.height / 1080f);
            var f = settings.focalLength.value / 1000f;
            var s1 = Mathf.Max(settings.focusDistance.value, f);
            var aspect = (float)context.screenWidth / (float)context.screenHeight;
            var coeff = f * f / (settings.aperture.value * (s1 - f) * scaledFilmHeight * 2f);
            var maxCoC = CalculateMaxCoCRadius(context.screenHeight);

            if (settings.specturalLut.value != null)
            {
                sheet.properties.SetTexture(ShaderIDs.SpecturalLut, settings.specturalLut);
            }
            sheet.properties.SetFloat(ShaderIDs.Intensity, 1 * settings.intensity);
            sheet.properties.SetFloat(ShaderIDs.Power, settings.power);
            sheet.properties.SetFloat(ShaderIDs.Polarize, settings.polarize);
            sheet.properties.SetFloat(ShaderIDs.Distance, s1);
            sheet.properties.SetFloat(ShaderIDs.LensCoeff, coeff);
            sheet.properties.SetFloat(ShaderIDs.MaxCoC, maxCoC);
            sheet.properties.SetFloat(ShaderIDs.RcpMaxCoC, 1f / maxCoC);
            sheet.properties.SetFloat(ShaderIDs.RcpAspect, 1f / aspect);

            var cmd = context.command;
            var colorFormat = context.sourceFormat;

            cmd.BeginSample("Distance Chromatic Aberration");
            // context.GetScreenSpaceTemporaryRT(cmd, ShaderIDs.DepthOfFieldTex, 0, colorFormat, RenderTextureReadWrite.Default, FilterMode.Bilinear, context.width / 2, context.height / 2);
            // cmd.BlitFullscreenTriangle(context.source, ShaderIDs.DepthOfFieldTex, sheet, (int)Pass.DownsampleAndPrefilter);
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            cmd.EndSample("Distance Chromatic Aberration");
        }

        float CalculateMaxCoCRadius(int screenHeight)
        {
            // Estimate the allowable maximum radius of CoC from the kernel
            // size (the equation below was empirically derived).
            float radiusInPixels = (float)settings.kernelSize.value * 4f + 6f;

            // Applying a 5% limit to the CoC radius to keep the size of
            // TileMax/NeighborMax small enough.
            return Mathf.Min(0.05f, radiusInPixels / screenHeight);
        }
    }

}