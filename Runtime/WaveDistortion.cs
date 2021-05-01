using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Kumu
{
    [System.Serializable]
    [PostProcess(typeof(WaveDistortionRenderer), PostProcessEvent.AfterStack, "Kumu/WaveDistortion")]
    public sealed class WaveDistortion : PostProcessEffectSettings
    {
        public FloatParameter period = new FloatParameter();
        public FloatParameter speed = new FloatParameter();
        public FloatParameter posterization = new FloatParameter();
        public FloatParameter amplitude = new FloatParameter();
    }

    sealed class WaveDistortionRenderer : PostProcessEffectRenderer<WaveDistortion>
    {
        static class ShaderIDs
        {
            internal static readonly int Period = Shader.PropertyToID("_Period");
            internal static readonly int Speed = Shader.PropertyToID("_Speed");
            internal static readonly int Posterization = Shader.PropertyToID("_Poisterization"); 
            internal static readonly int Amplitude = Shader.PropertyToID("_Amplitude");
        }

        public override void Init()
        {
            // though the name is fitToScreenRatio we actually fit it to the camera's ratio
        }

        public override void Render(PostProcessRenderContext context)
        {

            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/WaveDistortion"));

            sheet.properties.SetFloat(ShaderIDs.Period, settings.period);
            sheet.properties.SetFloat(ShaderIDs.Speed, settings.speed);
            sheet.properties.SetFloat(ShaderIDs.Amplitude, settings.amplitude);
            sheet.properties.SetFloat(ShaderIDs.Posterization, settings.posterization);

            var cmd = context.command;
            cmd.BeginSample("WaveDistortion");
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            cmd.EndSample("WaveDistortion");

        }
    }
}