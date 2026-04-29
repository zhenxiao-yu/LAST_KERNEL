using System;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Marks a UIToolkitScreenController subclass with the UXML path and sorting
    /// order it owns.  UIScreenAutoWirer uses this to auto-create and wire scene
    /// GameObjects; UIToolkitScreenController reads it to set Document.sortingOrder.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class UIScreenAttribute : Attribute
    {
        /// <summary>Project-relative path to the UXML asset (e.g. "Assets/…/View.uxml").</summary>
        public string UxmlPath { get; }

        /// <summary>UIDocument sorting order — higher renders on top.</summary>
        public int SortingOrder { get; }

        public UIScreenAttribute(string uxmlPath, int sortingOrder = 0)
        {
            UxmlPath      = uxmlPath;
            SortingOrder  = sortingOrder;
        }
    }
}
