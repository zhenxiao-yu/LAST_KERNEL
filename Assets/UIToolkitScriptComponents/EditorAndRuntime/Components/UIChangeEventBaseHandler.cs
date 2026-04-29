// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER
using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Events;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Baseclass for event handlers.
    /// </summary>
    public class UIChangeEventBaseHandler<T> : UIComponentBase<VisualElement>
    {
        [Header("Events")]

        public UnityEvent<T> OnChange;
        public UnityEvent<ChangeEvent<T>> OnChangeWithEvent;

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

                if (OnChangeWithEvent != null || OnChange != null)
                    ele.RegisterCallback<ChangeEvent<T>>(onChange);
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

                if (OnChangeWithEvent != null || OnChange != null)
                    ele.UnregisterCallback<ChangeEvent<T>>(onChange);
            }
        }

        protected virtual void onChange(ChangeEvent<T> evt)
        {
            OnChangeWithEvent?.Invoke(evt);
            OnChange?.Invoke(evt.newValue);
        }

        public void LogTest(T value)
        {
            Debug.Log(value);
        }

        public void LogTest(IChangeEvent evt)
        {
            Debug.Log(evt.ToString());
        }
    }
}
#endif
