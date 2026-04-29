using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitParticles
{
    public class ParticlesDemo7Scripting : MonoBehaviour
    {
        public UIDocument Document;
        public Texture Texture;
        
        void Start()
        {
            var img = new ParticleImage();
            
            // Some properties can be change directly on the image
            // like assigning a texture.
            img.Texture = Texture;

            // Others are controlled by the particle system but
            // we have to wait until the ParticleSystem has been created.
            img.OnInitialized += () =>
            {
                var main = img.ParticleSystemForImage.ParticleSystem.main;
                main.simulationSpeed = 5f;
            };

            // Add to root.
            Document.rootVisualElement.Add(img);
        }
    }
}
