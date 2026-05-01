using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitCustomShaderImageURP.Demo
{
    public class CustomShaderImageDemo : MonoBehaviour
    {
        private UIDocument m_document;

        public UIDocument Document
        {
            get
            {
                if (m_document == null)
                {
                    m_document = this.GetComponent<UIDocument>();
                }

                return m_document;
            }
        }

        private CustomShaderImage m_outlineImg;
        private CustomShaderImage m_dissolveImg;
        private CustomShaderImage m_pixelateImg;
        private CustomShaderImage m_hsvImg;
        private CustomShaderImage m_blurImg;
        private CustomShaderImage m_greyScaleImg;

        void Start()
        {
            m_outlineImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImageOutline");
            m_dissolveImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImageGraphDissolve");
            m_pixelateImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImagePixelate");
            m_hsvImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImageHSV");
            m_blurImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImageBlur");
            m_greyScaleImg = Document.rootVisualElement.Q<CustomShaderImage>(name: "CustomShaderImageGreyScale");
        }

        void Update()
        {
            var outlineMaterial = m_outlineImg.Material;
            outlineMaterial.SetFloat("_OutlineThickness", (Mathf.Sin(Time.time * 3f) + 1.3f) * 14f);

            var dissolveMaterial = m_dissolveImg.Material;
            dissolveMaterial.SetFloat("_Progress", getPingPongProgress(speed: 0.5f));

            var pixelMaterial = m_pixelateImg.Material;
            pixelMaterial.SetFloat("_PixelSize", getPingPongProgress(speed: 0.25f) * 50f);

            var hsvMaterial = m_hsvImg.Material;
            hsvMaterial.SetFloat("_Hue", (Time.time * 0.2f) % 1f);

            var blurMaterial = m_blurImg.Material;
            blurMaterial.SetFloat("_BlurStrength", getPingPongProgress(speed: 0.4f) * 8f);

            var greyBlendMaterial = m_greyScaleImg.Material;
            greyBlendMaterial.SetColor("_Color", new Color(1f, 1f, 1f, getPingPongProgress(speed: 0.4f)));
        }

        private float getPingPongProgress(float speed)
        {
            var t = Time.time * speed;
            var pingPongProgress = Mathf.Sin(t * Mathf.PI) < 0f ? (t % 1f) : (1f - t % 1f);
            return pingPongProgress;
        }
    }
}