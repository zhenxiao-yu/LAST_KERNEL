using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public partial class TextAnimationManipulator
    {
        public const string TEXT_ANIMATION_CLASSNAME = "text-animation";
        public const string TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME = "text-animation-auto-play-off";
        public const string TEXT_TYPEWRITER_CLASSNAME = "text-typewriter-";

        // Best keep this to false since the UI Builder if buggy as hell (used only for testing).
        public static bool AddToUIBuilderElements = false;
        
        /// <summary>
        /// Contains all created manipulator objects. Some may not be bound to any element (target == null).
        /// Those will be reused.
        /// </summary>
        public static List<TextAnimationManipulator> ManipulatorRegistry = new List<TextAnimationManipulator>();

        public static bool CanBeReused(TextAnimationManipulator manipulator)
        {
            return !manipulator.HasValidTarget();
        }
        
        /// <summary>
        /// Call this on any element that you have changed the text-animation related class list. This will update
        /// (add or remove) the manipulators based on the class list.
        /// </summary>
        /// <param name="textElement"></param>
        public static void AddOrRemoveManipulator(TextElement textElement)
        {
            if (textElement == null)
                return;

            bool hasTextAnimationClass = textElement.ClassListContains(TEXT_ANIMATION_CLASSNAME);

            if (!ShouldAddManipulatorToThisPanel(textElement) && !hasTextAnimationClass)
                return;

            if (hasTextAnimationClass)
                GetOrCreateManipulator(textElement);
            else
                RemoveManipulator(textElement);
        }
        
        public static void UpdateAfterClassChange(TextElement textElement)
        {
            if (textElement == null)
                return;

            var manipulator = GetManipulator(textElement);
            if (manipulator != null)
            {
                manipulator.UpdateAfterClassChange();
            }
            else
            {
                var panel = TextAnimationPanel.GetPanel(textElement);
                if (panel != null)
                    panel.AddOrRemoveManipulator(textElement);
            }
        }

        private static bool ContainsTypewriterClassNames(TextElement textElement)
        {
            var classNames = textElement.GetClasses();
            foreach (var className in classNames)
            {
                if (className.StartsWith(TEXT_TYPEWRITER_CLASSNAME))
                    return true;
            }
            return false;
        }

        public static bool ShouldAddManipulatorToThisPanel(TextElement textElement)
        {
            return textElement.panel != null && (AddToUIBuilderElements || textElement.panel.contextType == ContextType.Player);
        }
        
        public static TextAnimationManipulator GetOrCreateManipulator(TextElement element)
        {
            // Get if already added.
            var existingManipulator = GetManipulator(element);
            if (existingManipulator != null)
            {
                return existingManipulator;
            }

            // Reset manipulators with invalid targets and make sure they stay in the registry.
            for (int i = ManipulatorRegistry.Count-1; i >= 0; i--)
            {
                var m = ManipulatorRegistry[i];
                
                // In the editor elements are often recreated without removing the manipulators from their elements.
                // This means we end up with manipulators that have not been reset yet.
                if (CanBeReused(m) && !m.HasValidTarget())
                {
                    m.Reset(); // <- this will trigger onManipulatorUnregistered which will remove the manipulator from the registry.
                    if (!ManipulatorRegistry.Contains(m))
                        ManipulatorRegistry.Add(m); // Thus we have to re-add it so we can reuse it.
                }
            }
            
            if (!ShouldAddManipulatorToThisPanel(element))
                return null;

            // Find a free manipulator to reuse.
            // This rarely happens (for example if the text-animation class is toggled on a text element).
            TextAnimationManipulator manipulator = null;
            foreach (var m in ManipulatorRegistry)
            {
                if (CanBeReused(m))
                {
                    manipulator = m;
                    break;
                }
            }

            // Create new if necessary
            if (manipulator == null)
            {
                manipulator = new TextAnimationManipulator();
                ManipulatorRegistry.Add(manipulator);
            }

            // This triggers RegisterCallbacksOnTarget() where we do all the initialization.
            element.AddManipulator(manipulator);
            
            return manipulator;
        }

        static void RemoveManipulator(VisualElement element)
        {
            if (TryGetManipulator(element, out var manipulator))
                RemoveManipulator(element, manipulator);
        }
        
        static void RemoveManipulator(VisualElement element, TextAnimationManipulator manipulator)
        {
            element.RemoveManipulator(manipulator);
            manipulator.Reset();
        }
        
        public static void TryPause(VisualElement element)
        {
            if (TryGetManipulator(element, out var manipulator))
            {
                manipulator.Pause();
            }
        }

        public static void TryResume(VisualElement element)
        {
            TryPlay(element);
        }

        public static void TryPlay(VisualElement element)
        {
            if (TryGetManipulator(element, out var manipulator))
            {
                manipulator.Play();
            }
        }

        public static void TryRestart(VisualElement element, bool paused = false, float time = 0f)
        {
            if (TryGetManipulator(element, out var manipulator))
            {
                manipulator.Restart(paused, time);
            }
        }
        
        public static TextAnimation GetAnimation(VisualElement element, string animationId, int index = 0)
        {
            foreach (var m in ActiveManipulators)
            {
                if (m != null && m.target != null && m.target == element)
                {
                    m.GetAnimation(animationId, index);
                }
            }

            return null;
        }
        
        /// <summary>
        /// Returns true if a animation was found.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animation"></param>
        /// <param name="animationId">If there are multiple animations with the same Id then the first will be used.</param>
        /// <returns></returns>
        public static bool TryGetAnimation(VisualElement element, string animationId, out TextAnimation animation)
        {
            animation = GetAnimation(element, animationId);
            return animation != null;
        }
        
        /// <summary>
        /// Returns true if a animation was found.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="index">If there are multiple animations with the same id then you can use the index to pick one.</param>
        /// <param name="animation"></param>
        /// <returns></returns>
        public static bool TryGetAnimation(VisualElement element, string animationId, int index, out TextAnimation animation)
        {
            animation = GetAnimation(element, animationId, index);
            return animation != null;
        }
        
        /// <summary>
        /// Returns the animation found by id.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="index">If there are multiple animations with the same id then you can use the index to pick one.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T GetAnimation<T>(VisualElement element, string animationId, int index = 0) where T : TextAnimation
        {
            foreach (var m in ActiveManipulators)
            {
                if (m != null && m.target != null && m.target == element)
                {
                    return m.GetAnimation(animationId, index) as T;
                }
            }

            return null;
        }
        
        /// <summary>
        /// Returns true if a animation was found.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animation"></param>
        /// <param name="animationId"></param>
        /// <returns></returns>
        public static bool TryGetAnimation<T>(VisualElement element, string animationId, out T animation) where T : TextAnimation
        {
            animation = GetAnimation<T>(element, animationId);
            return animation != null;
        }
        
        public static bool TryGetAnimation<T>(VisualElement element, string animationId, int index, out T animation) where T : TextAnimation
        {
            animation = GetAnimation<T>(element, animationId, index);
            return animation != null;
        }

        public static void RestartAllManipulators(float time, bool paused)
        {
            foreach (var manipulator in TextAnimationManipulator.ActiveManipulators)
            {
                manipulator.Restart(paused, time);
            }
        }
    }
}