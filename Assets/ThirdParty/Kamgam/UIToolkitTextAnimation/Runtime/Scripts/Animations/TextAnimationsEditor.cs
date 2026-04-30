#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace Kamgam.UIToolkitTextAnimation
{
    [UnityEditor.CustomEditor(typeof(TextAnimations))]
    public class TextAnimationsEditor : UnityEditor.Editor
    {
        TextAnimations obj;

        public void OnEnable()
        {
            obj = target as TextAnimations;
        }

        public override void OnInspectorGUI()
        {
            if (GUILayout.Button("<- Back to UI Document"))
            {
                var provider = TextAnimationDocument.Find(obj);
                if (provider != null)
                {
                    UnityEditor.Selection.objects = new GameObject[] { provider.gameObject };
                    UnityEditor.EditorGUIUtility.PingObject(provider.gameObject);
                }
            }

            base.OnInspectorGUI();

            if (GUILayout.Button(new GUIContent("Refresh Preview", "Sometimes the preview in UI Builder or the Game View does not update automatically. Use this to force and update in the UI Builder and the Game View.")))
            {
                if (!UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    UIEditorPanelObserver.ForceRefreshAndRebuild();
                }
            }
            
            GUILayout.Space(5);
            GUILayout.Label("Manipulator Classnames");
            
            DrawClassNameInfoWithCopyButton(
                "Enable Text Animations:",
                TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME,
                $"Add '{TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME}' your TextElement class list to enable " +
                $"text animations on this element. If you do not add this class then not animations will occur."
                );

            DrawClassNameInfoWithCopyButton(
                "Disable Auto-Play:",
                TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME,
                $"Add '{TextAnimationManipulator.TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME}' your TextElement class " +
                $"list to disable auto play. NOTICE: You will have to call .Play() on the manipulator to start the animations."
            );
            
            DrawClassNameInfoWithCopyButton(
                "Animation Tag:", $"<link anim=\"CONFIG_ID\">text</link>",
                $"Add '{TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME}' your TextElement class list and then " +
                $"use the tag '<link anim=\"CONFIG_ID\">animated text</link>' to use the animation CONFIG_ID in your text."
            );
            
            DrawClassNameInfoWithCopyButton(
                    "Delay Tag:", $"</noparse delay=\"2\">",
                    $"Add '{TextAnimationManipulator.TEXT_ANIMATION_CLASSNAME}' your TextElement class list and then " +
                    $"use the tag '</noparse delay=\"DELAY_IN_SEC\">' to add a delay to your animation.\n\n" +
                    $"Please notice that delays are only for the typewriter animations. Character animations will ignore them."
                );
        }

        private static void DrawClassNameInfoWithCopyButton(string label, string className, string description)
        {
            GUILayout.Space(5);
            GUILayout.Label(label);
            GUILayout.BeginHorizontal();
            GUILayout.TextField(className);
            if (GUILayout.Button("Copy", GUILayout.Width(70)))
            {
                EditorGUIUtility.systemCopyBuffer = className;
            }
            
            GUILayout.EndHorizontal();
            EditorGUILayout.HelpBox(new GUIContent(description));
        }
    }
}
#endif
