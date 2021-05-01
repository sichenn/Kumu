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
        public FloatParameter pow = new FloatParameter();
        [Space()]
        public BoolParameter debug = new BoolParameter();
    }

    sealed class WaveDistortionRenderer : PostProcessEffectRenderer<WaveDistortion>
    {
        static class ShaderIDs
        {
            internal static readonly int Period = Shader.PropertyToID("_Period");
            internal static readonly int Speed = Shader.PropertyToID("_Speed");
            internal static readonly int Posterization = Shader.PropertyToID("_Poisterization"); 
            internal static readonly int Amplitude = Shader.PropertyToID("_Amplitude");
            internal static readonly int Debug = Shader.PropertyToID("_Debug");
            internal static readonly int Pow = Shader.PropertyToID("_Pow");

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
            sheet.properties.SetFloat(ShaderIDs.Pow, settings.pow);

            sheet.properties.SetInt(ShaderIDs.Debug, settings.debug ? 1 : 0);


            var cmd = context.command;
            cmd.BeginSample("WaveDistortion");
            cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
            cmd.EndSample("WaveDistortion");

        }
    }
}