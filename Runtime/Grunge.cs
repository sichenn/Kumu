using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
[PostProcess(typeof(GrungeRenderer), PostProcessEvent.AfterStack, "Kumu/Grunge")]
public sealed class Grunge : PostProcessEffectSettings
{
    [Range(0, 1)] public FloatParameter strength = new FloatParameter();
    public TextureParameter blendTexture = new TextureParameter();
    public Vector2Parameter tiling = new Vector2Parameter() { value = new Vector2(1, 1) };
    [Tooltip("Adjust the texture's UV tiling to avoid stretching")]
    public BoolParameter fitToScreenRatio = new BoolParameter();
}

sealed class GrungeRenderer : PostProcessEffectRenderer<Grunge>
{
    static class ShaderIDs
    {
        internal static readonly int Strength = Shader.PropertyToID("_Strength");
        internal static readonly int BlendTex = Shader.PropertyToID("_BlendTex");
        internal static readonly int Tiling = Shader.PropertyToID("_Tiling");
    }

    private static Shader s_Shader;

    public override void Init()
    {
        s_Shader = Shader.Find("Hidden/Kumu/Grunge");
    }

    public override void Render(PostProcessRenderContext context)
    {
        if (settings.blendTexture.value == null)
        {
            context.command.Blit(context.source, context.destination);
            return;
        }
        var sheet = context.propertySheets.Get(s_Shader);

		sheet.properties.SetVector(ShaderIDs.Tiling, settings.tiling);
        sheet.properties.SetFloat(ShaderIDs.Strength, settings.strength);
        sheet.properties.SetTexture(ShaderIDs.BlendTex, settings.blendTexture);

        var cmd = context.command;
        cmd.BeginSample("Grunge");
        cmd.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
        cmd.EndSample("Grunge");

    }


}
