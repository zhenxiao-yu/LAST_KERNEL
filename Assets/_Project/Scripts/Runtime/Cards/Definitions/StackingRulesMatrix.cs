using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public enum StackingRule
    {
        None,           // No stacking allowed
        CategoryWide,   // Allowed across all definitions in category
        SameDefinition  // Allowed only if bottom.Definition == top.Definition
    }

    [CreateAssetMenu(menuName = "Last Kernel/Stacking Rules Matrix")]
    public class StackingRulesMatrix : ScriptableObject
    {
        // Drawn entirely by StackingRulesMatrixEditor — do not expose raw array in Inspector.
        [HideInInspector, SerializeField] private StackingRule[] rules;

#if UNITY_EDITOR
        [BoxGroup("Bulk Operations"), HorizontalGroup("Bulk Operations/Row")]
        [Button("All: None"), GUIColor(0.85f, 0.45f, 0.45f)]
        private void SetAllNone()
        {
            UnityEditor.Undo.RecordObject(this, "Stacking Rules: Set All None");
            SetAll(StackingRule.None);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [HorizontalGroup("Bulk Operations/Row")]
        [Button("All: Category-Wide"), GUIColor(0.45f, 0.8f, 0.45f)]
        private void SetAllCategoryWide()
        {
            UnityEditor.Undo.RecordObject(this, "Stacking Rules: Set All Category-Wide");
            SetAll(StackingRule.CategoryWide);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [HorizontalGroup("Bulk Operations/Row")]
        [Button("All: Same Definition"), GUIColor(0.45f, 0.65f, 0.9f)]
        private void SetAllSameDefinition()
        {
            UnityEditor.Undo.RecordObject(this, "Stacking Rules: Set All Same Definition");
            SetAll(StackingRule.SameDefinition);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void SetAll(StackingRule rule)
        {
            EnsureInitialized();
            int size = System.Enum.GetValues(typeof(CardCategory)).Length;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                    rules[y * size + x] = rule;
        }
#endif

        private void OnValidate()
        {
            Initialize();
        }

        public StackingRule GetRule(CardCategory bottom, CardCategory top)
        {
            EnsureInitialized();
            int size = System.Enum.GetValues(typeof(CardCategory)).Length;
            return rules[(int)bottom * size + (int)top];
        }

        public void SetRule(CardCategory bottom, CardCategory top, StackingRule value)
        {
            EnsureInitialized();
            int size = System.Enum.GetValues(typeof(CardCategory)).Length;
            rules[(int)bottom * size + (int)top] = value;
        }

        private void EnsureInitialized()
        {
            if (rules == null || rules.Length == 0)
                Initialize();
        }

        public void Initialize()
        {
            int newSize = System.Enum.GetValues(typeof(CardCategory)).Length;
            int newArrayLength = newSize * newSize;

            if (rules == null || rules.Length != newArrayLength)
            {
                StackingRule[] newRules = new StackingRule[newArrayLength];

                if (rules != null)
                {
                    int oldSize = (int)Mathf.Sqrt(rules.Length);

                    for (int y = 0; y < Mathf.Min(oldSize, newSize); y++)
                    {
                        for (int x = 0; x < Mathf.Min(oldSize, newSize); x++)
                        {
                            newRules[y * newSize + x] = rules[y * oldSize + x];
                        }
                    }
                }

                rules = newRules;
            }
        }
    }
}

