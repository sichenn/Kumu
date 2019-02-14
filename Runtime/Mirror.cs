using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
[PostProcess(typeof(MirrorRenderer), PostProcessEvent.AfterStack, "Kumu/Mirror")]
public class Mirror : PostProcessEffectSettings
{
    public enum MirrorAxis { None, Vertical, Horizontal, VerticalAndHorizontal }
    [System.Serializable] 
    public sealed class MirrorAxisParameter : ParameterOverride<MirrorAxis> { }
    public MirrorAxisParameter mirrorAxis = new MirrorAxisParameter { value = MirrorAxis.None };
}

sealed class MirrorRenderer : PostProcessEffectRenderer<Mirror>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Mirror"));
        var cmd = context.command;
        int pass = (int)settings.mirrorAxis.value;
        cmd.BeginSample("Mirror");
        cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, pass);
        cmd.EndSample("Mirror");
    }
}
