using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Kumu
{
    public class ShuffleGrunge : MonoBehaviour
    {
        public Texture2D[] textures;
        [Tooltip("Seconds until a grunge texture is shuffled")]
        [Range(0, 1)]
        public float intervalSeconds = 1;

        private PostProcessVolume m_Volume;
        private PostProcessEffectSettings m_ProfileSettings;
        private Grunge m_Grunge;
        private int m_CurrentTextureIndex = 0;
        private Coroutine m_CurrentCoroutine;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before
        /// any of the Update methods is called the first time.
        /// </summary>
        void Start()
        {
            m_Volume = GetComponent<PostProcessVolume>();
            if (m_Volume.sharedProfile.TryGetSettings<Grunge>(out m_Grunge))
            {
                // m_Grunge.enabled.Override(true);
            }
            m_CurrentCoroutine = StartCoroutine(ChangeTextureCoroutine());
        }

        private IEnumerator ChangeTextureCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(intervalSeconds);
                ChangeTexture();
            }
        }

        public void Refresh()
        {
            if (m_CurrentCoroutine != null)
            {
                StopCoroutine(m_CurrentCoroutine);
                m_CurrentCoroutine = StartCoroutine(ChangeTextureCoroutine());
            }
        }

        private void ChangeTexture()
        {
            if (m_Grunge == null)
            {
                return;
            }

            if (m_CurrentTextureIndex == textures.Length - 1)
            {
                m_CurrentTextureIndex = 0;
            }
            else
            {
                m_CurrentTextureIndex++;
            }

            m_Grunge.blendTexture.value = textures[m_CurrentTextureIndex];
        }

    }
}