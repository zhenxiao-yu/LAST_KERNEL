using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation.Extensions
{
    public static class TextAnimationExtensions
    {
        // Manipulator
        
        public static void TAUpdateAfterClassChange(this VisualElement element)
        {
            if (element == null)
                return;
            
            TextAnimationManipulator.UpdateAfterClassChange(element as TextElement);
        }
        
        public static void TAEnableTextAnimation(this VisualElement element)
        {
            if (element == null)
                return;
            
            // Add text animation base class name if needed.
            if (!element.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME))
                element.AddToClassList(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);

            TAUpdateAfterClassChange(element);
        }
        
        public static void TADisableTextAnimation(this VisualElement element)
        {
            if (element == null)
                return;
            
            // Add text animation base class name if needed.
            if (element.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME))
                element.RemoveFromClassList(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);

            TAUpdateAfterClassChange(element);
        }
        
        /// <summary>
        /// Adds the class name to the class list and activates text animation on the element by
        /// adding the "text-animation" class in addition to the specified class names.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="classNames"></param>
        public static void TAAddToClassList(this VisualElement element, params string[] classNames)
        {
            if (element == null)
                return;
            
            // Add text animation base class name if needed.
            if (!element.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME))
                element.AddToClassList(TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME);
                
            foreach (var name in classNames)
            {
                element.AddToClassList(name);   
            }
            
            element.TAUpdateAfterClassChange();
        }
        
        /// <summary>
        /// Removes the text-animation class and updates (removes) the manipulator.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="classNames"></param>
        public static void TARemoveFromClassList(this VisualElement element, params string[] classNames)
        {
            if (element == null)
                return;
            
            foreach (var name in classNames)
            {
                element.RemoveFromClassList(name);   
            }
            
            element.TAUpdateAfterClassChange();
        }
        
        public static TextAnimationManipulator TAGetManipulator(this VisualElement element)
        {
            return TextAnimationManipulator.GetManipulator(element);
        }
        
        public static void TATryGetManipulator(this VisualElement element, out TextAnimationManipulator manipulator)
        {
            TextAnimationManipulator.TryGetManipulator(element, out manipulator);
        }
        
        public static void TAPause(this VisualElement element)
        {
            if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
            {
                manipulator.Pause();
            }
        }

        public static void TAResume(this VisualElement element)
        {
            TAPlay(element);
        }

        public static void TAPlay(this VisualElement element)
        {
            if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
            {
                manipulator.Play();
            }
        }

        public static void TARestart(this VisualElement element, bool paused = false, float time = 0f)
        {
            if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
            {
                manipulator.Restart(paused, time);
            }
        }
        
        public static void TASetAutoPlay(this VisualElement element, bool autoPlay)
        {
            if (!autoPlay && element.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME))
            {
                element.TAAddToClassList(TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME);
            }
            else if (autoPlay && !element.ClassListContains(TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME))
            {
                element.TARemoveFromClassList(TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME);
            }
        }


        // Animations
        
        public static bool TAHasCharacterAnimation(this VisualElement element)
        {
            var manipulator = TextAnimationManipulator.GetManipulator(element);
            if (manipulator != null)
                return manipulator.HasCharacterAnimations();
            return false;
        }
        
        public static bool TAHasTypewriterAnimations(this VisualElement element)
        {
            var manipulator = TextAnimationManipulator.GetManipulator(element);
            if (manipulator != null)
                return manipulator.HasTypewriterAnimations();
            return false;
        }
        
        /// <summary>
        /// Please remember that you should not change the animation parameters on the manipulator. They are ephemeral. Instead use the TextAnimationsProvider to change the configs there.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="animationIndex"></param>
        /// <returns></returns>
        public static TextAnimation TAGetAnimation(this VisualElement element, string animationId, int animationIndex = 0)
        {
            return TextAnimationManipulator.GetAnimation(element, animationId, animationIndex);
        }
        
        /// <summary>
        /// Please remember that you should not change the animation parameters on the manipulator. They are ephemeral. Instead use the TextAnimationsProvider to change the configs there.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="animationIndex"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static T TAGetAnimation<T>(this VisualElement element, string animationId, int animationIndex = 0) where T : TextAnimation
        {
            return TextAnimationManipulator.GetAnimation<T>(element, animationId, animationIndex);
        }
        
        /// <summary>
        /// Please remember that you should not change the animation parameters on the manipulator. They are ephemeral. Instead use the TextAnimationsProvider to change the configs there.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="config"></param>
        public static void TATryGetAnimation(this VisualElement element, string animationId, out TextAnimation config)
        {
            TextAnimationManipulator.TryGetAnimation(element, animationId, out config);
        }
        
        /// <summary>
        /// Please remember that you should not change the animation parameters on the manipulator. They are ephemeral. Instead use the TextAnimationsProvider to change the configs there.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="animationId"></param>
        /// <param name="config"></param>
        /// <typeparam name="T"></typeparam>
        public static void TATryGetAnimation<T>(this VisualElement element, string animationId, out T config) where T : TextAnimation
        {
            TextAnimationManipulator.TryGetAnimation<T>(element, animationId, out config);
        }

        // Pause Play
        public static void TAPause(this VisualElement element, string animationId)
        {
            if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
            {
                animation.Pause();
            }
        }

        public static void TAResume(this VisualElement element, string animationId)
        {
            TAPlay(element, animationId);
        }

        public static void TAPlay(this VisualElement element, string animationId)
        {
            if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
            {
                animation.Play();
            }
        }

        public static void TARestart(this VisualElement element, string animationId, bool paused = false, float time = 0f)
        {
            if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
            {
                animation.Restart(paused, time);
            }
        }
        
        // Queries
        public static void TAPause<T>(this UQueryBuilder<T> queryBuilder) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
                {
                    manipulator.Pause();
                }    
            }
        }
        
        public static void TAResume<T>(this UQueryBuilder<T> queryBuilder) where T : VisualElement
        {
            TAPlay(queryBuilder);
        }
        
        public static void TAPlay<T>(this UQueryBuilder<T> queryBuilder) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
                {
                    manipulator.Resume();
                }    
            }
        }
        
        public static void TARestart<T>(this UQueryBuilder<T> queryBuilder, bool paused = false, float time = 0f) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetManipulator(element, out var manipulator))
                {
                    manipulator.Restart(paused, time);
                }    
            }
        }
        
        
        public static void TAPause<T>(this UQueryBuilder<T> queryBuilder, string animationId) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
                {
                    animation.Pause();
                }    
            }
        }
        
        public static void TAResume<T>(this UQueryBuilder<T> queryBuilder, string animationId) where T : VisualElement
        {
            TAPlay(queryBuilder, animationId);
        }
        
        public static void TAPlay<T>(this UQueryBuilder<T> queryBuilder, string animationId) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
                {
                    animation.Resume();
                }    
            }
        }
        
        public static void TARestart<T>(this UQueryBuilder<T> queryBuilder, string animationId, bool paused = false, float time = 0f) where T : VisualElement
        {
            var state = queryBuilder.Build();
            foreach (var element in state)
            {
                if (TextAnimationManipulator.TryGetAnimation(element, animationId, out var animation))
                {
                    animation.Restart(paused, time);
                }    
            }
        }
    }
}