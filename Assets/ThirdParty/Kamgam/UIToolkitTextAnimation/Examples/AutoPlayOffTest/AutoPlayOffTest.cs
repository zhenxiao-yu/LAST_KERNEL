using System.Collections;
using Kamgam.UIToolkitTextAnimation.Extensions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class AutoPlayOffTest : MonoBehaviour
    {
        protected UIDocument _document;

        public UIDocument Document
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

        IEnumerator Start()
        {
            Debug.Log("Will start playing in 3 seconds.");
            yield return new WaitForSeconds(1);

            Debug.Log("Will start playing in 2 seconds.");
            yield return new WaitForSeconds(1);

            Debug.Log("Will start playing in 1 second.");
            yield return new WaitForSeconds(1);

            var label = Document.rootVisualElement.Query<Label>();
            label.TAPlay(); // Play ALL label animations. Uses extensions methods on the query builder.
        }
    }
}