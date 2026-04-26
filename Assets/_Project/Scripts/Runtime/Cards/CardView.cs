using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Optional visual adapter for card presentation.
    /// Existing card visuals should keep working without this component.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] private TextMeshPro titleText;
        [SerializeField] private TextMeshPro priceText;
        [SerializeField] private TextMeshPro nutritionText;
        [SerializeField] private TextMeshPro healthText;
        [SerializeField] private MeshRenderer artRenderer;
        [SerializeField] private string artTextureProperty = "_OverlayTex";

        public void SetTitle(string value)
        {
            if (titleText != null)
            {
                titleText.text = value;
            }
        }

        public void SetArt(Sprite sprite)
        {
            SetArt(sprite != null ? sprite.texture : null);
        }

        public void SetArt(Texture texture)
        {
            if (artRenderer == null || string.IsNullOrWhiteSpace(artTextureProperty))
            {
                return;
            }

            Material material = artRenderer.material;
            if (material != null && material.HasProperty(artTextureProperty))
            {
                material.SetTexture(artTextureProperty, texture);
            }
        }

        public void SetStats(string price, string nutrition, string health)
        {
            if (priceText != null)
            {
                priceText.text = price;
            }

            if (nutritionText != null)
            {
                nutritionText.text = nutrition;
            }

            if (healthText != null)
            {
                healthText.text = health;
            }
        }

        public void SetHighlighted(bool value)
        {
        }
    }
}
