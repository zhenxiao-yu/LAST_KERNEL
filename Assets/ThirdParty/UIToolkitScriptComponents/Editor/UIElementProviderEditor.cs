using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Kamgam.UIToolkitScriptComponents
{
    [CustomEditor(typeof(UIElementProvider))]
    public class UIElementProviderEditor : Editor
    {
        protected UIElementProvider _provider;

        public void OnEnable()
        {
            _provider = target as UIElementProvider;
        }

        public override void OnInspectorGUI()
        {
            // Do not show anyhting
        }
    }
}
