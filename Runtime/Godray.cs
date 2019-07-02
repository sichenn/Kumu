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
    }

    sealed class GodrayRenderer : PostProcessEffectRenderer<Godray>
    {
        static class ShaderIDs
        {
            static readonly int Intensity = Shader.PropertyToID("_Intensity");
        }

        public override void Render(PostProcessRenderContext context)
        {
            var cmd = context.command;
            var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Godray"));
        }
    }


}
