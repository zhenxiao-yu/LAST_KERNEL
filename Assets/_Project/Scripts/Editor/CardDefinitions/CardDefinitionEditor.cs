using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Markyu.LastKernel
{
    // Odin now handles all field display via BoxGroup/FoldoutGroup/ShowIf/ValidateInput
    // attributes on CardDefinition. This editor just ensures Odin's inspector runs
    // for CardDefinition and its subclasses (InheritedTypes = true).
    [CustomEditor(typeof(CardDefinition), true)]
    public class CardDefinitionEditor : OdinEditor
    {
        // Subclasses can override DrawTree() to append custom sections below Odin's rendering.
        protected virtual void DrawDerivedSection() { }

        protected override void DrawTree()
        {
            base.DrawTree();
            DrawDerivedSection();
        }
    }
}
