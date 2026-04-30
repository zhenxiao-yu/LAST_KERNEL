using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class TypewriterAppendTest : MonoBehaviour
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

        public IEnumerator Start()
        {
            var label = Document.rootVisualElement.Q<Label>();

            yield return new WaitForSeconds(2f);
            string[] additionalText = new string[]
            {
                "\nBut ", "wait ", "there ", "is ", "more.",
                "\nHere", " is some", " <link anim=\"weird,rainbow\">weird</link>", " <link anim=\"weird,rainbow\">rainbow</link>",
                " and", " a lot", " of Ds:\n"
            };

            foreach (var str in additionalText)
            {
                float delay = UnityEngine.Random.Range(0.2f, 0.8f);
                yield return new WaitForSeconds(delay);
                label.text += str;
            }

            for (int i = 0; i < 20; i++)
            {
                float delay = UnityEngine.Random.Range(0.15f, 0.3f);
                yield return new WaitForSeconds(delay);
                label.text += "D ";
            }
            yield return new WaitForSeconds(1);
            label.text = label.text.Substring(0, label.text.Length - 30);
            yield return new WaitForSeconds(1);
            label.text += " - Ups, we've lost some Ds.";
            yield return new WaitForSeconds(1.5f);
            
            label.text += "\n<link anim='wave'>That's it :-)</link>";
            yield return new WaitForSeconds(4);
            label.text += "\n<size=50%><link anim='swing'>Still here?</link></size>\n";
            yield return new WaitForSeconds(3);
            label.text += "<size=35%><link anim='jump'>>°What are you waiting for ??????????????????????????????????????????????????:._</link></size>";
        }
    }
}
