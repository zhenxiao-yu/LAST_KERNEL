// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    public interface ILink
    {
        VisualElement GetElement();
        void RefreshElement();
        GameObject gameObject { get; }
    }
}
#endif
