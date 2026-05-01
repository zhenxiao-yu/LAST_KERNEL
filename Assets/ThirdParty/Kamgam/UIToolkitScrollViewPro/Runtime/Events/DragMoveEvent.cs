using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class DragMoveEvent : EventBase<DragMoveEvent>
    {
        public ScrollViewPro scrollView;
        public ScrollViewMode direction;
        public PointerMoveEvent pointerMoveEvent;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView, ScrollViewMode direction, PointerMoveEvent pointerMoveEvent)
        {
            using (var evt = DragMoveEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.direction = direction;
                evt.pointerMoveEvent = pointerMoveEvent;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}