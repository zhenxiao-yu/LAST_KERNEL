using Kamgam.UIToolkitTextAnimation.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class TemplateDemo : MonoBehaviour
    {
        protected UIDocument _document;

        public UIDocument Doc
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponent<UIDocument>();
                }

                return _document;
            }
        }

        void Start()
        {
            // You could also simpy use "text-typewriter-w-bounce" directly but this shows how to retrieve it from a config by id.
            var typewriterConfig = TextAnimationsProvider.GetAnimation<TextAnimationTypewriter>("w-bounce");
            string bounceClassName = typewriterConfig.GetClassName();
            
            // Add this to the first label
            var labels = Doc.rootVisualElement.Query<Label>().Build().ToList();
            labels[0].AddToClassList(bounceClassName);
            // Ensure the text system is notified.
            labels[0].TAUpdateAfterClassChange();
        }
    }
}
