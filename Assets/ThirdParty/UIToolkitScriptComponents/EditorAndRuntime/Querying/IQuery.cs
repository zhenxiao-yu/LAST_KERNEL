// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    public interface IQuery
    {
        IList<VisualElement> GetElements();
        void Execute();
        GameObject gameObject { get; }
    }
}
#endif
