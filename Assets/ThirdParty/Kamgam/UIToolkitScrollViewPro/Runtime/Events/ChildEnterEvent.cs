using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class ChildEnterEvent : EventBase<ChildEnterEvent>
    {
        public ScrollViewPro scrollView;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView)
        {
            using (ChildEnterEvent evt = ChildEnterEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}