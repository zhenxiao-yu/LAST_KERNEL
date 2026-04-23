using UnityEditor;

namespace Markyu.FortStack
{
    [CustomEditor(typeof(ResearchDefinition))]
    public class ResearchDefinitionEditor : CardDefinitionEditor
    {
        protected override void DrawDerivedSection()
        {
            EditorGUILayout.HelpBox(
                "This is a research card, no further configurations required.",
                MessageType.Info
            );
        }
    }
}

