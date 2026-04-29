using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// The Document and panel are split because we also use the Panel
    /// in the UI Builder which has no UIDocument to begin with (see UIEditorPanelObserver).
    /// </summary>
#if KAMGAM_VISUAL_SCRIPTING
    // Why? See: https://forum.unity.com/threads/unable-to-provide-a-default-for-getvalue-on-object-valueinput.1140022/#post-9138727
    [Unity.VisualScripting.Inspectable]
#endif
    public partial class TextAnimationPanel
    {
        [System.NonSerialized]
        public VisualElement RootVisualElement;

        [System.NonSerialized]
        public TextAnimations Configs;

        public TextAnimationPanel(VisualElement rootVisualElement, TextAnimations configs)
        {
            RootVisualElement = rootVisualElement;
            Configs = configs;
        }

        public void Enable()
        {
            if (!Panels.Contains(this))
                Panels.Add(this);

            AddOrRemoveManipulators();
        }

        public void Destroy()
        {
            RootVisualElement = null;
            Panels.Remove(this);
        }

        public TextAnimation GetConfigAt(int index)
        {
            if (index >= 0 && index < Configs.Animations.Count)
                return Configs.Animations[index];

            return null;
        }

        /// <summary>
        /// Goes through the whole visual tree and adds/removes animation manipulators.
        /// </summary>
        public void AddOrRemoveManipulators()
        {
            if (RootVisualElement == null)
                return;

            RootVisualElement
                .Query<TextElement>(className: TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME)
                .ForEach(AddOrRemoveManipulator);
        }
        
        /// <summary>
        /// Call this on any text element which you have recently added (or removed) an animation tag.
        /// </summary>
        /// <param name="textElement"></param>
        public void AddOrRemoveManipulator(TextElement textElement)
        {
            if (Configs == null)
                return;

            TextAnimationManipulator.AddOrRemoveManipulator(textElement);
        }
        
        public void UpdateManipulatorsAfterClassChange()
        {
            if (RootVisualElement == null)
                return;

            RootVisualElement
                .Query<TextElement>(className: TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME)
                .ForEach(UpdateManipulatorAfterClassChange);
        }
        
        /// <summary>
        /// Call this on any text element which you have recently added (or removed) an animation tag.
        /// </summary>
        /// <param name="textElement"></param>
        public void UpdateManipulatorAfterClassChange(TextElement textElement)
        {
            if (Configs == null)
                return;

            TextAnimationManipulator.UpdateAfterClassChange(textElement);
        }
    }
}