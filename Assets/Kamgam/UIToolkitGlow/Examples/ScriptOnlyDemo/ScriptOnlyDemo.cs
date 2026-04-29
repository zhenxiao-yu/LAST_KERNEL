using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitGlow
{
    public class ScriptOnlyDemo : MonoBehaviour
    {
        protected GlowDocument _glowDocument;

        public GlowDocument GlowDocument
        {
            get
            {
                if (_glowDocument == null)
                {
                    _glowDocument = this.GetComponent<GlowDocument>();
                }

                return _glowDocument;
            }
        }

        public IEnumerator Start()
        {
            var doc = GlowDocument.Document;
            var lbl = doc.rootVisualElement.Q<Label>(name: "TestLabel");

            
            
            yield return new WaitForSeconds(2f);
            
            // Add Glow
            Debug.Log("Adding Glow");
            
            // This is the important part. It will generate and add (or remove) the manipulator(s).
            lbl.AddToClassList("g-glow");
            GlowDocument.UpdateGlowOnChildren();
            
            yield return new WaitForSeconds(2f);
            
            // Remove Glow
            lbl.RemoveFromClassList("g-glow");
            GlowDocument.UpdateGlowOnChildren();
            
            
            
            yield return new WaitForSeconds(2f);
            
            // Add Shadow
            Debug.Log("Adding Shadow");
            
            lbl.WrapInShadow();
            
            yield return new WaitForSeconds(2f);
            
            // Remove Shadow
            lbl.UnwrapFromShadow();
        }
    }

}