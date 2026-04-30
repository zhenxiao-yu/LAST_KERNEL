using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    public class SoundEffectManipulator : Manipulator
    {
        [System.NonSerialized]
        public static List<SoundEffectManipulator> Manipulators = new List<SoundEffectManipulator>();

        /// <summary>
        /// Removes all manipulators whose target is not attached to any panel.
        /// </summary>
        public static void DefragManipulators()
        {
            for (int i = Manipulators.Count-1; i >= 0; i--)
            {
                var manipulator = Manipulators[i];
                if (manipulator.target == null || manipulator.target.panel == null)
                {
                    Manipulators.RemoveAt(i);
                    
                    if (manipulator.target != null)
                        manipulator.target = null;
                }
            }
        }
        
        public static SoundEffectManipulator CreateOrUpdate(VisualElement element)
        {
            if (element == null || element.panel == null || element.panel.contextType != ContextType.Player)
                return null;
            
            // First try getting the id from the class list
            var id = SoundEffect.GetIdFromClassList(element);
            
            // Second try getting it from custom styles (which involves some waiting)
            if (string.IsNullOrEmpty(id))
            {
                // Usually the id is still empty after this because custom styles are resolved later (see below).
                element.customStyle.TryGetValue(SoundEffect.CustomStyleSoundEffectProperty, out id);
            }
            // Also make sure we got notified of future changes.
            if (element.ClassListContains(SoundEffect.CLASSNAME))
            {
                element.UnregisterCallback<CustomStyleResolvedEvent>(onCustomStyleResolved);
                element.RegisterCallback<CustomStyleResolvedEvent>(onCustomStyleResolved);
            }
                
            if (string.IsNullOrEmpty(id))
                return null;

            var manipulator = SoundEffectManipulator.GetManipulator(element);
            
            // Add new manipulator only if needed.
            if (manipulator == null)
            {
                manipulator = new SoundEffectManipulator();
                manipulator.target = element;
            }

            manipulator.ConnectToAudioEffect(id);

            return manipulator;
        }

        static void onCustomStyleResolved(CustomStyleResolvedEvent e)
        {
            var element = e.target as VisualElement;
            if (element.customStyle.TryGetValue(SoundEffect.CustomStyleSoundEffectProperty, out var id))
            {
                if (!string.IsNullOrEmpty(id))
                {
                    // If the element does no manipulator yet or a different id then create or update the manipulator.
                    var manipulator = GetManipulator(element);
                    if (manipulator == null || manipulator.SoundEffect.Id != id)
                    {
                        CreateOrUpdate(element);
                    }
                }
            }
        }

        public static SoundEffectManipulator GetManipulator(VisualElement element)
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator != null && manipulator.target == element)
                    return manipulator;
            }
            
            return null;
        }

        public static SoundEffectManipulator GetManipulator(SoundEffect effect, VisualElement element)
        {
            return GetManipulator(effect.Id, element);
        }

        /// <summary>
        /// Returns a list of matching manipulators (clears the list before).
        /// </summary>
        /// <param name="id"></param>
        /// <param name="results">Fills this list or generates an new one if NULL.</param>
        /// <returns></returns>
        public static List<SoundEffectManipulator> GetManipulators(string id, List<SoundEffectManipulator> results = null)
        {
            if (results == null)
                results = new List<SoundEffectManipulator>();
            
            results.Clear();

            foreach (var manipulator in Manipulators)
            {
                if (manipulator != null && manipulator.target != null)
                {
                    var mId = SoundEffect.GetIdFromClassList(manipulator.target);
                    if (mId == id)
                    {
                        results.Add(manipulator);
                    }
                }
            }

            return results;
        }

        public static SoundEffectManipulator GetManipulator(string id, VisualElement element)
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator != null && manipulator.target != null && manipulator.target == element)
                {
                    var mId = SoundEffect.GetIdFromClassList(manipulator.target);
                    if (!string.IsNullOrEmpty(mId) && mId == id)
                    {
                        return manipulator;
                    }
                }
            }
            
            return null;
        }
        
        public static SoundEffectManipulator GetFirstManipulator(string id)
        {
            foreach (var manipulator in Manipulators)
            {
                if (manipulator != null && manipulator.target != null)
                {
                    var mId = SoundEffect.GetIdFromClassList(manipulator.target);
                    if (!string.IsNullOrEmpty(mId) && mId == id)
                    {
                        return manipulator;
                    }
                }
            }
            
            return null;
        }

        public static void RemoveAllManipulators()
        {
            for (int i = Manipulators.Count-1; i >= 0; i--)
            {
                var manipulator = Manipulators[i];
                if (manipulator != null && manipulator.target != null)
                {
                    manipulator.target.RemoveManipulator(manipulator);
                }
            }
            
            Manipulators.Clear();
        }
        
        public SoundEffect SoundEffect;
        
        protected override void RegisterCallbacksOnTarget()
        {
            Manipulators.Add(this);
        }
        
        protected override void UnregisterCallbacksFromTarget()
        {
            Manipulators.Remove(this);

            if (SoundEffect != null)
            {
                SoundEffect.UnregisterCallbacks(target);
                SoundEffect = null;
            }
        }
        
        /// <summary>
        /// Connects or re-connects the visual element to the AudioEffect based on the audio-effect-id class.
        /// </summary>
        public void ConnectToAudioEffect(string id = null)
        {
            if (target == null)
                return;
                
            if (string.IsNullOrEmpty(id))
                id = SoundEffect.GetIdFromClassList(target);
            
            if (id == null)
                return;

            var effects = SoundEffects.GetOrCreate();
            if (effects == null)
                return;

            var effect = effects.GetEffect(id);
            
            // If effect changed (meaning id changed) then unregister callbacks.
            if (SoundEffect != null && SoundEffect != effect)
            {
                SoundEffect.UnregisterCallbacks(target);
                
                // Re-register on new effect object.
                SoundEffect = effect;
            }

            // Initial registration if effect was null.
            if (SoundEffect == null)
            {
                SoundEffect = effect;
                
                if (SoundEffect != null)
                {
                    SoundEffect.OnManipulatorAdded();
                }
            }
            
            // Update callbacks
            if (SoundEffect != null)
            {
                SoundEffect.UnregisterCallbacks(target);
                SoundEffect.RegisterCallbacks(target);
            }
        }
    }
}