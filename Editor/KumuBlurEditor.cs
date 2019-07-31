using UnityEditor.Rendering.PostProcessing;
using Kumu;

namespace KumuEditor
{
    [PostProcessEditor(typeof(KumuBlur))]
    public sealed class KumuBlurEditor : PostProcessEffectEditor<KumuBlur>
    {
        SerializedParameterOverride m_BlurType;
        SerializedParameterOverride m_Blend;
        SerializedParameterOverride m_Downsample;
        SerializedParameterOverride m_Iterations;
        SerializedParameterOverride m_Diffusion;
        SerializedParameterOverride m_Threshold;
        SerializedParameterOverride m_Angle;

        public override void OnEnable()
        {
            m_Blend = FindParameterOverride(x => x.blend);
            m_BlurType = FindParameterOverride(x => x.blurType);
            m_Downsample = FindParameterOverride(x => x.downsample);
            m_Iterations = FindParameterOverride(x => x.iterations);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_Threshold = FindParameterOverride(x => x.threshold);
            m_Angle = FindParameterOverride(x => x.angle);
        }

        public override void OnInspectorGUI()
        {
            DrawCommonSettings();
            int blurType = m_BlurType.value.intValue;
            if (blurType == (int)BlurType.OneDimensional)
            {
                PropertyField(m_Angle);
            }
        }

        private void DrawCommonSettings()
        {
            PropertyField(m_BlurType);
            PropertyField(m_Blend);
            PropertyField(m_Diffusion);
            PropertyField(m_Downsample);
            PropertyField(m_Iterations);
        }

    }

}
