using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    public partial class SoundEffect
    {
        public static string CLASSNAME = "kamgam-sfx";
        public static string CLASSNAME_ID_PREFX = "kamgam-sfx-id-";
        public static readonly CustomStyleProperty<string> CustomStyleSoundEffectProperty = new ("--kamgam-sfx-id");
        
        private static Stack<SoundEffect> s_pool = new Stack<SoundEffect>();

        public static SoundEffect GetFromPool()
        {
            if (s_pool.Count > 0)
            {
                // Null tolerant pool because some instance here may be real assets
                // and if deleted in the editor they become null.
                SoundEffect effect;
                do
                {
                    effect = s_pool.Pop();
                }
                while (effect == null);
                
                return effect;
            }
            else
            {
                var effect = CreateInstance<SoundEffect>();
                return effect;
            }
        }

        public static SoundEffect GetCopyFromPool(SoundEffect baseObject)
        {
            if (baseObject == null)
                return null;
            
            if (s_pool.Count > 0 )
            {
                var copy = s_pool.Pop();
                copy.CopyValuesFrom(baseObject);
                copy.IsCopy = true;
                baseObject.Copies.Add(copy);
                return copy;
            }
            else
            {
                var newCopy = GetFromPool();
                newCopy.CopyValuesFrom(baseObject);
                newCopy.IsCopy = true;
                baseObject.Copies.Add(newCopy);
                return newCopy;
            }
        }
        
        public static void ReturnToPool(SoundEffect effect)
        {
            if (effect == null)
                return;

            effect.Clear();

            s_pool.Push(effect);
        }
        
        public static void ReturnToPool(IList<SoundEffect> configs)
        {
            if (configs == null)
                return;
            
            for (int i = configs.Count - 1; i >= 0; i--)
            {
                ReturnToPool(configs[i]);
            }

            configs.Clear();
        }
        
        public static string GetIdFromClassList(VisualElement element)
        {
            var classes = element.GetClasses();
            foreach (var className in classes)
            {
                if (!className.StartsWith(SoundEffect.CLASSNAME_ID_PREFX))
                    continue;

                string id = className.Replace(SoundEffect.CLASSNAME_ID_PREFX, "");
                return id;
            }

            return null;
        }
        
        /// <summary>
        /// Adds a SoundEffectsManipulator to the given element and initializes the sound effects system on it.<br />
        /// Creates or updates the SoundEffectManipulators for the given element and all children of the element if they have the sfx class.
        /// </summary>
        /// <param name="visualElement"></param>
        /// <returns></returns>
        public static SoundEffect CreateOrUpdateHierarchy(VisualElement visualElement)
        {
            if (visualElement == null)
                return null;

            // Descendants
            SoundEffectManipulator manipulator = null;
            visualElement.Query<VisualElement>(className: SoundEffect.CLASSNAME).ForEach((ve) =>
            {
                SoundEffectManipulator.CreateOrUpdate(ve);
                var m = SoundEffectManipulator.GetManipulator(ve);
                if (m != null)
                    manipulator = m;
            });
            
            // Itself
            SoundEffectManipulator.CreateOrUpdate(visualElement);
            var m = SoundEffectManipulator.GetManipulator(visualElement);
            if (m != null)
                manipulator = m;
            
            if (manipulator == null)
                return null;

            return manipulator.SoundEffect;
        }

        public static string GetNewId()
        {
            return System.Guid.NewGuid().ToString();
        }
        
        public static SoundEffect AddToElement(VisualElement visualElement, string id = null,
            ElementEventType eventType = ElementEventType.OnPointerClick, AudioClip clip = null, float volume = 1f)
        {
            if (visualElement == null)
                return null;
            
            // Add base class
            if (!visualElement.ClassListContains(CLASSNAME))
                visualElement.AddToClassList(CLASSNAME);

            // Remove previous one if existing.
            var existingId = SoundEffect.GetIdFromClassList(visualElement);
            if (existingId != null)
            {
                RemoveFromElement(visualElement);
            }
            
            if (string.IsNullOrEmpty(id))
                id = SoundEffect.GetNewId();
            
            // Add ID class
            if (!visualElement.ClassListContains(CLASSNAME_ID_PREFX + id))
                visualElement.AddToClassList(CLASSNAME_ID_PREFX + id);
            
            // Get/Create Effect
            var effects = SoundEffects.GetOrCreate();
            if (effects == null)
                return null;

            bool preExistingId = effects.GetEffect(id) != null; 

            var effect = effects.GetOrCreateEffect(id, addFirstEvent: true);
            if (effect == null)
                return null;

            if (!preExistingId)
            {
                effect.Events[0].eventType = eventType;
                effect.Events[0].AudioSourceSettings.Volume = volume;
                effect.Events[0].Clips.Add(clip);
            }

            // Add manipulator
            var manipulator = SoundEffectManipulator.CreateOrUpdate(visualElement);
            if (manipulator == null)
                return null;
            
            manipulator.ConnectToAudioEffect(id);

            return manipulator.SoundEffect;
        }
        
        public static void RemoveFromElement(VisualElement visualElement)
        {
            if (visualElement == null)
                return;

            if (!visualElement.ClassListContains(SoundEffect.CLASSNAME))
                return;
            
            // Remove base class.
            visualElement.RemoveFromClassList(SoundEffect.CLASSNAME);

            var existingId = SoundEffect.GetIdFromClassList(visualElement);
            if (existingId == null)
                return;

            // Remove id class
            visualElement.RemoveFromClassList(SoundEffect.CLASSNAME_ID_PREFX + existingId);
            
            // Remove manipulator
            var manipulator = SoundEffectManipulator.GetManipulator(visualElement);
            visualElement.RemoveManipulator(manipulator);
        }
    }
}