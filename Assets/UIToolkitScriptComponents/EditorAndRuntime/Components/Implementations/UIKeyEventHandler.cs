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
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Key Event Handler")]
    public class UIKeyEventHandler : UIComponentBase<VisualElement>
    {
        [Header("Events")]

        public UnityEvent<KeyDownEvent> OnKeyDownWithEvent;
        public UnityEvent<KeyCode> OnKeyDown;

        public UnityEvent<KeyUpEvent> OnKeyUpWithEvent;
        public UnityEvent<KeyCode> OnKeyUp;

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

                if (OnKeyDownWithEvent != null) ele.RegisterCallback<KeyDownEvent>(onKeyDown);
                if (OnKeyUpWithEvent != null) ele.RegisterCallback<KeyUpEvent>(onKeyUp);
            }
        }

        public void UnregisterEvents()
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                if (OnKeyDownWithEvent != null) ele.UnregisterCallback<KeyDownEvent>(onKeyDown);
                if (OnKeyUpWithEvent != null) ele.UnregisterCallback<KeyUpEvent>(onKeyUp);
            }
        }

        protected virtual void onKeyDown(KeyDownEvent evt)
        {
            OnKeyDownWithEvent?.Invoke(evt);
            OnKeyDown?.Invoke(evt.keyCode);
        }

        protected virtual void onKeyUp(KeyUpEvent evt)
        {
            OnKeyUpWithEvent?.Invoke(evt);
            OnKeyUp?.Invoke(evt.keyCode);
        }

        public void LogTest(KeyCode keyCode)
        {
            Debug.Log(keyCode.ToString());
        }
    }
}
#endif
