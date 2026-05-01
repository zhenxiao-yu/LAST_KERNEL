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
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Change Long Handler")]
    public class UIChangeEventLongHandler : UIChangeEventBaseHandler<long> { }
}
#endif
