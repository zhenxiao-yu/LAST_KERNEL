// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Events;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Add this as a CHILD to a UIDocument.
    /// This allows you to add event listeners via Unity Events.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Focus Event Handler")]
    public class UIFocusEventHandler : UIComponentBase<VisualElement>
    {
        [Header("Events")]

        public UnityEvent<FocusEvent> OnFocusWithEvent;
        public UnityEvent OnFocus;

        public UnityEvent<BlurEvent> OnBlurWithEvent;
        public UnityEvent OnBlur;

        public override void OnAttach()
        {
            base.OnAttach();
            RegisterEvents();
        }

        public override void OnDetach()
        {
            UnregisterEvents();
            base.OnDetach();
        }

        public void RegisterEvents()
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                if (OnFocusWithEvent != null) ele.RegisterCallback<FocusEvent>(onFocus);
                if (OnBlurWithEvent != null) ele.RegisterCallback<BlurEvent>(onBlur);
            }
        }

        public void UnregisterEvents()
        {
            if (Elements.Count == 0)
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                if (OnFocusWithEvent != null) ele.UnregisterCallback<FocusEvent>(onFocus);
                if (OnBlurWithEvent != null) ele.UnregisterCallback<BlurEvent>(onBlur);
            }
        }

        protected virtual void onFocus(FocusEvent evt)
        {
            OnFocusWithEvent?.Invoke(evt);
            OnFocus?.Invoke();
        }

        protected virtual void onBlur(BlurEvent evt)
        {
            OnBlurWithEvent?.Invoke(evt);
            OnBlur?.Invoke();
        }

        public void LogTest(string msg)
        {
            Debug.Log(msg);
        }
    }
}
#endif
