using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CustomEditor(typeof(StackingRulesMatrix))]
    public class StackingRulesMatrixEditor : OdinEditor
    {
        private StackingRulesMatrix matrix;

        private static class Styles
        {
            public static readonly GUIStyle rightLabel = new GUIStyle("RightLabel");
            public static readonly GUIStyle ruleIcon;

            static Styles()
            {
                ruleIcon = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 16,
                    fixedWidth = 20,
                    fixedHeight = 20,
                };
            }
        }

        private static readonly GUIContent[] ruleIcons =
        {
            new GUIContent("", "No stacking allowed"),
            new GUIContent("✓", "Category-wide stacking"),
            new GUIContent("≡", "Same-definition stacking")
        };

        protected override void OnEnable()
        {
            base.OnEnable();
            matrix = (StackingRulesMatrix)target;
        }

        public override void OnInspectorGUI()
        {
            var categories = System.Enum.GetValues(typeof(CardCategory));
            int size = categories.Length;

            const int checkboxSize = 20;
            int labelSize = 110;
            const int indent = 10;

            foreach (var cat in categories)
            {
                var dim = GUI.skin.label.CalcSize(new GUIContent(cat.ToString()));
                if (labelSize < dim.x) labelSize = (int)dim.x;
            }

            // Column headers (rotated -90°)
            Rect topRect = GUILayoutUtility.GetRect(labelSize + indent + size * checkboxSize, labelSize);
            for (int x = 0; x < size; x++)
            {
                string cat = categories.GetValue(x).ToString();
                Vector2 pivot = new Vector2(topRect.x + labelSize + indent + x * checkboxSize, topRect.y + labelSize);
                GUIUtility.RotateAroundPivot(-90, pivot);
                GUI.Label(new Rect(pivot.x, pivot.y, labelSize, checkboxSize), cat);
                GUI.matrix = Matrix4x4.identity;
            }

            // Rows
            for (int y = 0; y < size; y++)
            {
                Rect rowRect = GUILayoutUtility.GetRect(indent + labelSize + size * checkboxSize, checkboxSize);
                GUI.Label(new Rect(rowRect.x + indent, rowRect.y, labelSize, checkboxSize),
                    categories.GetValue(y).ToString(), Styles.rightLabel);

                for (int x = 0; x < size; x++)
                {
                    var current = matrix.GetRule((CardCategory)y, (CardCategory)x);
                    var cellRect = new Rect(rowRect.x + labelSize + indent + x * checkboxSize, rowRect.y, checkboxSize, checkboxSize);
                    if (GUI.Button(cellRect, ruleIcons[(int)current], Styles.ruleIcon))
                    {
                        Undo.RecordObject(matrix, "Change stacking rule");
                        matrix.SetRule((CardCategory)y, (CardCategory)x,
                            (StackingRule)(((int)current + 1) % System.Enum.GetValues(typeof(StackingRule)).Length));
                        EditorUtility.SetDirty(matrix);
                    }
                }
            }

            EditorGUILayout.Space(8);

            // Renders [Button] methods on StackingRulesMatrix (Bulk Operations group).
            base.OnInspectorGUI();
        }
    }
}
