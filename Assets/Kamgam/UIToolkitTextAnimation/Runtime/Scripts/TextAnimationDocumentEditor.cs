using UnityEngine;

namespace Kamgam.UIToolkitTextAnimation
{
#if UNITY_EDITOR
    [UnityEditor.CustomEditor(typeof(TextAnimationDocument))]
    public class TextAnimationDocumentEditor : UnityEditor.Editor
    {
        TextAnimationDocument obj;

        public void OnEnable()
        {
            obj = target as TextAnimationDocument;
        }

        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();

            if (GUILayout.Button( new GUIContent("Refresh Preview", "Sometimes the preview in UI Builder or the Game View does not update automatically. Use this to force and update in the UI Builder and the Game View.")))
            {
                if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    obj.AddOrRemoveManipulators();
                }
                else
                {
                    UIEditorPanelObserver.ForceRefresh();
                }
            }
        }
    }
#endif

}