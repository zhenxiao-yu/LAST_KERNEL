using Sirenix.OdinInspector.Editor;
using UnityEditor;

namespace Markyu.LastKernel
{
    // Odin now handles all field display via BoxGroup/ListDrawerSettings/ValidateInput
    // attributes on PackDefinition. This editor just ensures Odin's inspector runs.
    [CustomEditor(typeof(PackDefinition))]
    public class PackDefinitionEditor : OdinEditor { }
}
