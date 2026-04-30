using System.Collections;
using Kamgam.UIToolkitTextAnimation.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class CodeOnlyDemo : MonoBehaviour
    {
        protected UIDocument _doc;

        public UIDocument Doc
        {
            get
            {
                if (_doc == null)
                {
                    _doc = this.GetComponent<UIDocument>();
                }

                return _doc;
            }
        }

        void Awake()
        {
            // If you use Awake then you have to use TextAnimationDocument.RegisterOnEnable() because
            // the manipulators may not have been added yet (depending on the "random" initialization
            // order. My recommendation: Do not use Awake for this.
        }

        IEnumerator Start()
        {
            var root = Doc.rootVisualElement;


            yield return new WaitForSeconds(1f);
            Debug.Log("-- Demo Start --");

            // ADDING A WAVE ANIMATION TO LABEL 1
            // The cumbersome way
            {
                var label1 = root.Q<Label>(name: "Label1");

                // Let's add a typewriter animation to the first label
                // We start by activating text animations on the label by adding the 'text-animation' class
                label1.AddToClassList("text-animation");

                // HINT:
                // This does the same thing:
                // label1.AddToClassList(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);

                // But that does not do anything yet (except for making it bold due to the USS).
                // We also have to add a 'text-typewriter-..' class to specify which animation to use.
                label1.AddToClassList("text-typewriter-w-wave");

                Debug.Log("Hm, still nothing?");
                yield return new WaitForSeconds(2f);

                // Oh right, we have to inform the text animation system of the class list changes.
                // Sadly Unity does not (yet) have events for that.
                // So we have to call TAUpdateAfterClassChange() on the element.
                TextAnimationManipulator.UpdateAfterClassChange(label1);

                // HINT:
                // label1.TAUpdateAfterClassChange(); // <- Does the same with extension methods.
            }

            // ADDING A WAVE ANIMATION TO LABEL 2
            // The easy way:
            var label2 = root.Q<Label>(name: "Label2");
            label2.TAAddToClassList(TextAnimationsProvider.GetTypewriterClassName("w-wave"));

            yield return new WaitForSeconds(1f);
            Debug.Log("-- Demo End --");
        }
    }
}
