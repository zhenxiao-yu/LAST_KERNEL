// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Applies the text transform OnEnable and (if this is enabled) applies it automatically whenever the text is changed.<br />
    /// Cally .Apply() to trigger it manually.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Display")]
    public class UIDisplay : UIComponentBase<VisualElement>
    {
        public void SetEnabled(bool enable)
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                ele.SetDisplayEnabled(enable);
            }
        }

        public bool GetEnabled(int index = 0)
        {
            if (!HasElements() || Elements.Count <= index)
                return false;

            return Elements[index].GetDisplayEnabled();
        }

        public void Toggle()
        {
            executeOnEveryElement(toggle);
        }

        protected void toggle(VisualElement ele)
        {
            bool enabled = ele.GetDisplayEnabled();
            ele.SetDisplayEnabled(!enabled);
        }
    }
}

#endif
