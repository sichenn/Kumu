using UnityEditor.Rendering.PostProcessing;
using Kumu;

namespace KumuBlurEditor
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

        public override void OnEnable()
        {
            m_Blend = FindParameterOverride(x => x.blend);
            m_BlurType = FindParameterOverride(x => x.blurType);
            m_Downsample = FindParameterOverride(x => x.downsample);
            m_Iterations = FindParameterOverride(x => x.iterations);
            m_Diffusion = FindParameterOverride(x => x.diffusion);
            m_Threshold = FindParameterOverride(x => x.threshold);
        }

        public override void OnInspectorGUI()
        {
            DrawCommonSettings();
            int blurType = m_BlurType.value.intValue;
            if(blurType == (int)BlurType.OneDimensional)
            {

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
