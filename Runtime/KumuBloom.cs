using UnityEngine;
using System;
using UnityEngine.Rendering.PostProcessing;

[System.Serializable]
[PostProcess(typeof(KumuBloomRenderer), PostProcessEvent.AfterStack, "Kumu/Bloom")]
public sealed class KumuBloom : PostProcessEffectSettings
{
    public FloatParameter intensity = new FloatParameter();

    [Range(1, 16)]
    public IntParameter iterations = new IntParameter() { value = 4 };

    [Range(0, 10)]
    public FloatParameter threshold = new FloatParameter() { value = 1 };

    [Range(0, 1)]
    public FloatParameter softThreshold = new FloatParameter(){value = 0.5f};

    public BoolParameter debug = new BoolParameter();
}

sealed class KumuBloomRenderer : PostProcessEffectRenderer<KumuBloom>
{
    static class ShaderIDs
    {
        internal static readonly int Filter = Shader.PropertyToID("_Filter");
        internal static readonly int Intensity = Shader.PropertyToID("_Intensity");
    }

    readonly int BoxDownPrefilterPass = 0;
    readonly int BoxDownPass = 1;
    readonly int BoxUpPass = 2;
    readonly int ApplyBloomPass = 3;
    readonly int DebugBloomPass = 4;
    RenderTexture[] textures = new RenderTexture[16];

    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Kumu/Bloom"));

        float knee = settings.threshold.value * settings.softThreshold.value;
        Vector4 filter;
        filter.x = settings.threshold.value;
        filter.y = filter.x - knee;
        filter.z = 2f * knee;
        filter.w = 0.25f / (knee + 0.00001f);
        sheet.properties.SetVector(ShaderIDs.Filter, filter);
        sheet.properties.SetFloat(ShaderIDs.Intensity, settings.intensity.value);

        int width = context.width / 2;
        int height = context.height / 2;
        
        RenderTextureFormat format = context.sourceFormat;

        var cmd = context.command;
        RenderTexture currentDestination = textures[0] =
            RenderTexture.GetTemporary(width, height, 0, format);
        cmd.BlitFullscreenTriangle(context.source, currentDestination, sheet, BoxDownPrefilterPass);
        RenderTexture currentSource = currentDestination;

        
        cmd.BeginSample("BoxBloom");
        int i = 1;
        for (; i < settings.iterations.value; i++)
        {
            width /= 2;
            height /= 2;
            if (height < 2)
            {
                break;
            }
            currentDestination = textures[i] =
                RenderTexture.GetTemporary(width, height, 0, format);
            cmd.BlitFullscreenTriangle(currentSource, currentDestination, sheet, BoxDownPass);
            currentSource = currentDestination;
        }
        
        for (i -= 2; i >= 0; i--)
        {
            currentDestination = textures[i];
            textures[i] = null;
            cmd.BlitFullscreenTriangle(currentSource, currentDestination, sheet, BoxUpPass);
            RenderTexture.ReleaseTemporary(currentSource);
            currentSource = currentDestination;
        }

        cmd.EndSample("BoxBloom");

        if (settings.debug.value)
        {
            cmd.BeginSample("Debug Bloom");
            cmd.BlitFullscreenTriangle(currentSource, context.destination, sheet, DebugBloomPass);
            cmd.EndSample("Debug Bloom");
        }
        else
        {
            cmd.SetGlobalTexture("_SourceTex", context.source);
            cmd.BlitFullscreenTriangle(currentSource, context.destination, ApplyBloomPass);
        }
        RenderTexture.ReleaseTemporary(currentSource);
    }
}