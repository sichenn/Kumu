using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
[PostProcess(typeof(KaleidoscopeRenderer), PostProcessEvent.AfterStack, "Kumu/Kaleidoscope")]
public class Kaleidoscope : PostProcessEffectSettings
{
    public IntParameter repeat = new IntParameter() { value = 1 };
    [Range(0, 360)] public FloatParameter offset = new FloatParameter() { value = 0 };
    [Range(0, 360)] public FloatParameter roll = new FloatParameter() { value = 0 };
    public BoolParameter symmetry = new BoolParameter() { value = true };
}

sealed class KaleidoscopeRenderer : PostProcessEffectRenderer<Kaleidoscope>
{
    static class ShaderIDs
    {
        internal static readonly int Repeat = Shader.PropertyToID("_Repeat");
        internal static readonly int Offset = Shader.PropertyToID("_Offset");
        internal static readonly int Roll = Shader.PropertyToID("_Roll");
    }
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Kaleidoscope"));
        var cmd = context.command;
        var div = Mathf.PI * 2 / Mathf.Max(1, settings.repeat);

        sheet.properties.SetFloat("_Divisor", div);
        sheet.properties.SetFloat("_Offset", settings.offset * Mathf.Deg2Rad);
        sheet.properties.SetFloat("_Roll", settings.roll * Mathf.Deg2Rad);
        if (settings.symmetry)
        {
            sheet.EnableKeyword("SYMMETRY_ON");
        }
        else
        {
            sheet.DisableKeyword("SYMMETRY_ON");
        }
        cmd.BeginSample("Kaleidoscope");
        cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        cmd.EndSample("Kaleidoscope");
    }
}
