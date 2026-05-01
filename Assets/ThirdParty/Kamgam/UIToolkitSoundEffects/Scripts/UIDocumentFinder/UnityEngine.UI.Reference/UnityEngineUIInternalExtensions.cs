using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects.UnityUIInternals
{
    /// <summary>
    /// It uses the UnityEngine.UI Assembly to gain access to the internals of the UI code, see:
    /// https://github.com/Unity-Technologies/UnityCsReference/blob/master/Modules/UIElements/AssemblyInfo.cs
    /// and: https://discussions.unity.com/t/how-to-access-useful-unity-editor-engine-internal-methods/251479/3
    /// This works because the text core is visible to UnityEngine.UI and thus is we reference UnityEngine.UI we also have
    /// access to the internals.
    /// </summary>
    internal static class UnityEngineUIInternalExtensions
    {
        internal static PanelSettings GetPanelSettings(this VisualElement element)
        {
            if (element == null)
                return null;

            if (element.panel is IRuntimePanel runtimePanel)
            {
                return runtimePanel.panelSettings;
            }

            return null;
        }

        /// <summary>
        /// Adds the found ui documents to the results list.
        /// NOTICE: It does NOT clear the list before adding though it creates a list if results is null.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="results"></param>
        /// <returns></returns>
        internal static List<UIDocument> GetUIDocuments(this VisualElement element, List<UIDocument> results = null)
        {
            if (element == null)
                return results;

            if (results == null)
                results = new List<UIDocument>();

            if (element.panel is IRuntimePanel runtimePanel)
            {
#if UNITY_6000_5_OR_NEWER
                if (runtimePanel.panelSettings == null
                    || runtimePanel.panelSettings.m_AttachedPanelComponentsList == null
                    || runtimePanel.panelSettings.m_AttachedPanelComponentsList.m_AttachedPanelComponents == null)
                {
                    return results;
                }

                foreach (var comp in runtimePanel.panelSettings.m_AttachedPanelComponentsList.m_AttachedPanelComponents)
                {
                    if (comp == null || comp.gameObject == null)
                        continue;
                    
                    var doc = comp.gameObject.GetComponent<UIDocument>();
                    results.Add(doc);
                    UnityEngine.Debug.Log(doc);
                }
#else
                if (runtimePanel.panelSettings == null
                    || runtimePanel.panelSettings.m_AttachedUIDocumentsList == null
                    || runtimePanel.panelSettings.m_AttachedUIDocumentsList.m_AttachedUIDocuments == null)
                {
                    return results;
                }

                results.AddRange(runtimePanel.panelSettings.m_AttachedUIDocumentsList.m_AttachedUIDocuments);
#endif
            }

            return results;
        }
    }
}