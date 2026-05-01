using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScrollViewPro
{
    public class ChildExitEvent : EventBase<ChildExitEvent>
    {
        public ScrollViewPro scrollView;

        public static void Dispatch(VisualElement target, ScrollViewPro scrollView)
        {
            using (ChildExitEvent evt = ChildExitEvent.GetPooled())
            {
                evt.scrollView = scrollView;
                evt.target = target;
                evt.bubbles = true;

                target.SendEvent(evt);
            }
        }
    }
}