using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace Kumu
{
    [System.Serializable]
    [PostProcess(typeof(GodrayRenderer), PostProcessEvent.AfterStack, "Kumu/Godray")]
    public sealed class Godray : PostProcessEffectSettings
    {
        public FloatParameter intensity = new FloatParameter() { value = 1 };
        public ColorParameter tint = new ColorParameter() { value = Color.white };
        [Range(0, 1)]
        public FloatParameter decay = new FloatParameter() { value = 0.5f };
        [Range(-1, 1)]
        public FloatParameter distance = new FloatParameter() { value = 0.5f };

        public QualityParameter quality = new QualityParameter() { value = Quality.High };
        public Vector2Parameter lightPos = new Vector2Parameter() { value = new Vector2(.5f, .5f) };
    }

    [System.Serializable]
    public sealed class QualityParameter : ParameterOverride<Quality> { }
    public enum Quality
    {
        High,
        Mid,
        Low
    }

    sealed class GodrayRenderer : PostProcessEffectRenderer<Godray>
    {
        static class ShaderIDs
        {
            internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
            internal static readonly int Tint = Shader.PropertyToID("_Tint");
            internal static readonly int Decay = Shader.PropertyToID("_Decay");
            internal static readonly int Distance = Shader.PropertyToID("_Distance");
            internal static readonly int LightPos = Shader.PropertyToID("_LightPos");
        }

        enum Pass
        {
            HighQuality = 0,
            MidQuality = 1,
            LowQuality = 2,
            RadialBlur = 3
        }

        private static Shader s_Shader;

        public override void Init()
        {
            s_Shader = Shader.Find("Hidden/Kumu/Godray");
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            if (settings.intensity.value == 0.0f || settings.distance.value == 0.0f)
            {
                cmd.Blit(context.source, context.destination);
                return;
            }

            var sheet = context.propertySheets.Get(s_Shader);


            // configure properties
            sheet.properties.SetFloat(ShaderIDs.Intensity, settings.intensity);
            sheet.properties.SetFloat(ShaderIDs.Decay, settings.decay);
            sheet.properties.SetFloat(ShaderIDs.Distance, settings.distance);
            sheet.properties.SetColor(ShaderIDs.Tint, settings.tint);
            sheet.properties.SetVector(ShaderIDs.LightPos, settings.lightPos);

            // process image
            int firstPass = 0;
            cmd.BeginSample("Godray");
            Vector2Int textureSize = new Vector2Int(context.screenWidth, context.screenHeight);
            bool singlePassDoubleWide = (context.stereoActive &&
                                        (context.stereoRenderingMode == PostProcessRenderContext.StereoRenderingMode.SinglePass) &&
                                        (context.camera.stereoTargetEye == StereoTargetEyeMask.Both));
            int textureWidthStereo = singlePassDoubleWide ? textureSize.x * 2 : textureSize.x;
           

            if (settings.quality.value == Quality.High)
            {
                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.HighQuality);

            }
            else if (settings.quality.value == Quality.Mid)
            {
                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.MidQuality);
            }
            else
            {
                cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, (int)Pass.LowQuality);
            }

            //context.GetScreenSpaceTemporaryRT(
            //                           cmd, firstPass, 0, context.sourceFormat, RenderTextureReadWrite.Default,
            //                           FilterMode.Bilinear, textureWidthStereo, textureSize.y);
            //if (settings.quality.value == Quality.High)
            //{
            //    cmd.BlitFullscreenTriangle(context.source, firstPass, sheet, (int)Pass.HighQuality);

            //}
            //else if (settings.quality.value == Quality.Mid)
            //{
            //    cmd.BlitFullscreenTriangle(context.source, firstPass, sheet, (int)Pass.HighQuality);
            //}
            //else
            //{
            //    cmd.BlitFullscreenTriangle(context.source, firstPass, sheet, (int)Pass.HighQuality);
            //}

            //cmd.BlitFullscreenTriangle(firstPass, context.destination, sheet, (int)Pass.RadialBlur);
            //cmd.ReleaseTemporaryRT(firstPass);
            cmd.EndSample("Godray");
        }
    }


}
