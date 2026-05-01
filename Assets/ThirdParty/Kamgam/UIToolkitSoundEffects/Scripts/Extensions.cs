using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    public static class Extensions
    {
        public static SoundEffectsDocument SECreateOrUpdateHierarchy(this GameObject gameObject)
        {
            if (gameObject == null)
                return null;
            
            return SECreateOrUpdateHierarchy(gameObject.GetComponent<UIDocument>());
        }
        
        /// <summary>
        /// Adds a SoundEffectsDocument to the given UIDocument if there is none yet.<br />
        /// Creates or updates SoundEffectManipulators to any UI Element that has the sfx class.
        /// </summary>
        /// <param name="document"></param>
        /// <returns></returns>
        public static SoundEffectsDocument SECreateOrUpdateHierarchy(this UIDocument document)
        {
            return SoundEffectsDocument.AddToUIDocument(document);
        }
        
        /// <summary>
        /// Adds a SoundEffectsManipulator to the given element and initializes the sound effects system on it.<br />
        /// Creates or updates the SoundEffectManipulators for the given element and all children of the element if they have the sfx class.
        /// </summary>
        /// <param name="visualElement"></param>
        /// <returns></returns>
        public static SoundEffect SECreateOrUpdateHierarchy(this VisualElement visualElement)
        {
            return SoundEffect.CreateOrUpdateHierarchy(visualElement);
        }

        private static List<UIDocument> s_tmpUIDocuments = new List<UIDocument>();

        public static SoundEffect SEAddEffect(this VisualElement visualElement, string id = null, ElementEventType eventType = ElementEventType.OnPointerClick, AudioClip clip = null, float volume = 1f, UIDocument document = null)
        {
            // Add SoundEffectDocument to the document of the visual element.
            if (document == null)
            {
                s_tmpUIDocuments.Clear();
                UIDocumentFinder.UIDocumentFinder.GetUIDocuments(visualElement, s_tmpUIDocuments);
                foreach (var doc in s_tmpUIDocuments)
                {
                    if (doc == null)
                        continue;
                    
                    document = doc;
                    SoundEffectsDocument.AddToUIDocument(document);
                }
                s_tmpUIDocuments.Clear();
            }
            else
            {
                SoundEffectsDocument.AddToUIDocument(document);
            }
            // Last resort, add to all ui documents in all scenes.
            if (document == null)
                SoundEffectsDocument.AddToAllUIDocumentsInAllScenes(allowAbort: true);
            
            // And the add the sound effect to the element itself.            
            return SoundEffect.AddToElement(visualElement, id, eventType, clip, volume);
        }
        
        public static void SERemoveEffect(this VisualElement visualElement)
        {
            SoundEffect.RemoveFromElement(visualElement);
        }
        
        public static SoundEffect SEGetEffect(this VisualElement visualElement)
        {
            var effects = SoundEffects.GetOrCreate();
            if (effects == null)
                return null;
                
            var id = SoundEffect.GetIdFromClassList(visualElement);
            return effects.GetEffect(id);
        }
        
        public static void SEPlay(this VisualElement visualElement, EventType eventType)
        {
            var effect = visualElement.SEGetEffect();
            effect.Play();
        }
        
        public static void SEStop(this VisualElement visualElement, EventType eventType)
        {
            var effect = visualElement.SEGetEffect();
            effect.Stop();
        }
    }
}