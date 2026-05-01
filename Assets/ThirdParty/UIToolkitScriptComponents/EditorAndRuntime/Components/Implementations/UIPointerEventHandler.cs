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
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Pointer Event Handler")]
    public class UIPointerEventHandler : UIComponentBase<VisualElement>
    {
        [Header("Events")]

        public UnityEvent<PointerDownEvent> OnPointerDownWithEvent;
        public UnityEvent<Vector3> OnPointerDown;

        public UnityEvent<PointerUpEvent> OnPointerUpWithEvent;
        public UnityEvent<Vector3> OnPointerUp;

        public UnityEvent<PointerEnterEvent> OnPointerEnterWithEvent;
        public UnityEvent<Vector3> OnPointerEnter;

        public UnityEvent<PointerLeaveEvent> OnPointerLeaveWithEvent;
        public UnityEvent<Vector3> OnPointerLeave;

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

                if (OnPointerDown != null) ele.RegisterCallback<PointerDownEvent>(onPointerDown);
                if (OnPointerUp != null) ele.RegisterCallback<PointerUpEvent>(onPointerUp);
                if (OnPointerEnter != null) ele.RegisterCallback<PointerEnterEvent>(onPointerEnter);
                if (OnPointerLeave != null) ele.RegisterCallback<PointerLeaveEvent>(onPointerLeave);
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

                if (OnPointerDown != null) ele.UnregisterCallback<PointerDownEvent>(onPointerDown);
                if (OnPointerUp != null) ele.UnregisterCallback<PointerUpEvent>(onPointerUp);
                if (OnPointerEnter != null) ele.UnregisterCallback<PointerEnterEvent>(onPointerEnter);
                if (OnPointerLeave != null) ele.UnregisterCallback<PointerLeaveEvent>(onPointerLeave);
            }
        }

        protected virtual void onPointerDown(PointerDownEvent evt)
        {
            OnPointerDownWithEvent?.Invoke(evt);
            OnPointerDown?.Invoke(evt.position);
        }

        protected virtual void onPointerUp(PointerUpEvent evt)
        {
            OnPointerUpWithEvent?.Invoke(evt);
            OnPointerUp?.Invoke(evt.position);
        }

        protected virtual void onPointerEnter(PointerEnterEvent evt)
        {
            OnPointerEnterWithEvent?.Invoke(evt);
            OnPointerEnter?.Invoke(evt.position);
        }

        protected virtual void onPointerLeave(PointerLeaveEvent evt)
        {
            OnPointerLeaveWithEvent?.Invoke(evt);
            OnPointerLeave?.Invoke(evt.position);
        }

        public void LogTest(IPointerEvent evt)
        {
            Debug.Log(evt);
        }

        public void LogTest(Vector3 pos)
        {
            Debug.Log(pos);
        }
    }
}
#endif
