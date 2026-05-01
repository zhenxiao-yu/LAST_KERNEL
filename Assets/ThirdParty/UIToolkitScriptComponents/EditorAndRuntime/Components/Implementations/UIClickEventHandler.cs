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
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Click Event Handler")]
    public class UIClickEventHandler : UIComponentBase<VisualElement>
    {
        [Header("Events")]

        public UnityEvent<ClickEvent> OnClickWithEvent;
        public UnityEvent OnClick;

        public override void OnAttach()
        {
            base.OnAttach();
            RegisterEvents();
        }

        public override void OnDetach()
        {
            base.OnDetach();
            UnregisterEvents();
        }

        public void RegisterEvents()
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                if (OnClick != null)
                    ele.RegisterCallback<ClickEvent>(onClick);
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

                if (OnClick != null) 
                    ele.UnregisterCallback<ClickEvent>(onClick);
            }
        }

        protected virtual void onClick(ClickEvent evt)
        {
            OnClickWithEvent?.Invoke(evt);
            OnClick?.Invoke();
        }

        public void LogTest(string msg)
        {
            Debug.Log(msg);
        }
    }
}
#endif
