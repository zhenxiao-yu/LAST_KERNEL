using System.Collections.Generic;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CustomEditor(typeof(RecipeDefinition), true)]
    public class RecipeDefinitionEditor : OdinEditor
    {
        // Caches all other recipes once to avoid per-frame FindAssets calls.
        private List<RecipeDefinition> _allOtherRecipes;
        private List<RecipeDefinition> _conflictingRecipes;
        private bool _isDirty = true;

        protected override void OnEnable()
        {
            base.OnEnable();
            FetchAllRecipes();
        }

        private void FetchAllRecipes()
        {
            _allOtherRecipes = new List<RecipeDefinition>();
            var currentTarget = (RecipeDefinition)target;

            string[] guids = AssetDatabase.FindAssets("t:RecipeDefinition");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var r = AssetDatabase.LoadAssetAtPath<RecipeDefinition>(path);
                if (r != null && r != currentTarget)
                    _allOtherRecipes.Add(r);
            }
            _isDirty = true;
        }

        protected override void DrawTree()
        {
            EditorGUI.BeginChangeCheck();
            base.DrawTree();
            if (EditorGUI.EndChangeCheck())
                _isDirty = true;

            if (_isDirty)
            {
                FindConflicts((RecipeDefinition)target);
                _isDirty = false;
            }

            DrawConflictInfoSection((RecipeDefinition)target);
        }

        private void FindConflicts(RecipeDefinition current)
        {
            _conflictingRecipes = new List<RecipeDefinition>();
            var currentMap = GetIngredientMap(current.RequiredIngredients);

            foreach (var other in _allOtherRecipes)
            {
                if (AreIngredientsIdentical(currentMap, GetIngredientMap(other.RequiredIngredients)))
                    _conflictingRecipes.Add(other);
            }
        }

        private Dictionary<CardDefinition, int> GetIngredientMap(List<RecipeDefinition.Ingredient> list)
        {
            var map = new Dictionary<CardDefinition, int>();
            if (list == null) return map;

            foreach (var item in list)
            {
                if (item.card == null) continue;
                if (map.ContainsKey(item.card)) map[item.card] += item.count;
                else map[item.card] = item.count;
            }
            return map;
        }

        private static bool AreIngredientsIdentical(
            Dictionary<CardDefinition, int> a, Dictionary<CardDefinition, int> b)
        {
            if (a.Count != b.Count) return false;
            foreach (var kvp in a)
            {
                if (!b.TryGetValue(kvp.Key, out int v) || v != kvp.Value) return false;
            }
            return true;
        }

        private void DrawConflictInfoSection(RecipeDefinition current)
        {
            if (_conflictingRecipes == null || _conflictingRecipes.Count == 0) return;

            float total = current.RandomWeight;
            foreach (var r in _conflictingRecipes) total += r.RandomWeight;

            EditorGUILayout.Space(6);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField(
                $"⚠ Shared Ingredients Detected ({_conflictingRecipes.Count + 1} recipes)",
                EditorStyles.boldLabel);
            EditorGUILayout.LabelField(
                "These recipes require the same ingredients and compete by RandomWeight.",
                EditorStyles.miniLabel);
            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Result",  EditorStyles.boldLabel, GUILayout.Width(130));
            EditorGUILayout.LabelField("Weight",  EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.LabelField("Chance",  EditorStyles.boldLabel, GUILayout.Width(55));
            EditorGUILayout.EndHorizontal();

            DrawConflictRow(current, current.RandomWeight, total, isSelf: true);
            foreach (var r in _conflictingRecipes)
                DrawConflictRow(r, r.RandomWeight, total, isSelf: false);

            EditorGUILayout.EndVertical();
        }

        private static void DrawConflictRow(RecipeDefinition r, float weight, float total, bool isSelf)
        {
            EditorGUILayout.BeginHorizontal(isSelf ? "box" : GUIStyle.none);

            string label = r.ResultingCard != null ? r.ResultingCard.name : r.name;
            if (isSelf) label += " (this)";
            EditorGUILayout.LabelField(label, GUILayout.Width(130));
            EditorGUILayout.LabelField(weight.ToString("0.##"), GUILayout.Width(55));

            float chance = total > 0f ? weight / total * 100f : 0f;
            EditorGUILayout.LabelField($"{chance:F1}%", GUILayout.Width(55));

            if (!isSelf && GUILayout.Button("Select", EditorStyles.miniButton, GUILayout.Width(55)))
            {
                EditorGUIUtility.PingObject(r);
                Selection.activeObject = r;
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
