using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.Tests
{
    /// <summary>
    /// Validates all RecipeDefinition assets and RecipeMatcher logic.
    /// </summary>
    public class RecipeValidationTests
    {
        private RecipeDefinition[] _allRecipes;

        [SetUp]
        public void SetUp()
        {
            _allRecipes = AssetDatabase
                .FindAssets("t:RecipeDefinition", new[] { "Assets/_Project" })
                .Select(g => AssetDatabase.LoadAssetAtPath<RecipeDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(r => r != null)
                .ToArray();
        }

        [Test]
        public void RecipeIds_AreUnique()
        {
            var seen = new Dictionary<string, string>();
            var duplicates = new List<string>();
            foreach (RecipeDefinition recipe in _allRecipes)
            {
                string path = AssetDatabase.GetAssetPath(recipe);
                if (string.IsNullOrEmpty(recipe.Id))
                {
                    duplicates.Add($"'{path}' has no ID");
                    continue;
                }
                if (seen.TryGetValue(recipe.Id, out string other))
                    duplicates.Add($"'{recipe.Id}' duplicated at '{path}' and '{other}'");
                else
                    seen[recipe.Id] = path;
            }
            Assert.IsEmpty(duplicates, string.Join("\n", duplicates));
        }

        [Test]
        public void AllRecipes_HaveIngredients()
        {
            var failing = _allRecipes
                .Where(r => r.RequiredIngredients == null || r.RequiredIngredients.Count == 0)
                .Select(r => AssetDatabase.GetAssetPath(r))
                .ToList();

            Assert.IsEmpty(failing, "Recipes with no ingredients:\n" + string.Join("\n", failing));
        }

        [Test]
        public void AllRecipes_HaveOutput()
        {
            var failing = _allRecipes
                .Where(r => r.RequiresResultingCard && r.ResultingCard == null)
                .Select(r => AssetDatabase.GetAssetPath(r))
                .ToList();

            Assert.IsEmpty(failing, "Recipes with no output card:\n" + string.Join("\n", failing));
        }

        [Test]
        public void AllRecipes_HavePositiveDuration()
        {
            var failing = _allRecipes
                .Where(r => r.CraftingDuration <= 0f)
                .Select(r => $"{AssetDatabase.GetAssetPath(r)} (duration={r.CraftingDuration})")
                .ToList();

            Assert.IsEmpty(failing, "Recipes with duration <= 0:\n" + string.Join("\n", failing));
        }

        [Test]
        public void AllRecipes_IngredientsAreNotNull()
        {
            var failing = new List<string>();
            foreach (RecipeDefinition recipe in _allRecipes)
            {
                if (recipe.RequiredIngredients == null) continue;
                for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
                {
                    if (recipe.RequiredIngredients[i].card == null)
                        failing.Add($"{AssetDatabase.GetAssetPath(recipe)} ingredient[{i}] is null");
                }
            }
            Assert.IsEmpty(failing, string.Join("\n", failing));
        }

        // ─── RecipeMatcher Unit Tests ─────────────────────────────────────────

        [Test]
        public void RecipeMatcher_RejectsExtraIngredient_InStrictRecipe()
        {
            CardDefinition wood = MakeCard("wood", CardCategory.Material);
            CardDefinition stone = MakeCard("stone", CardCategory.Material);
            RecipeDefinition recipe = MakeRecipe("plank", allowExcess: false, (wood, 2));

            bool matches = RecipeMatcher.DoesStackMatchRecipe(new[] { wood, wood, stone }, recipe);

            Assert.IsFalse(matches);
            Cleanup(wood, stone, recipe);
        }

        [Test]
        public void RecipeMatcher_AllowsExtraCount_ForResourceCards()
        {
            CardDefinition tree = MakeCard("tree", CardCategory.Resource);
            RecipeDefinition recipe = MakeRecipe("wood_from_tree", allowExcess: false, (tree, 1));

            bool matches = RecipeMatcher.DoesStackMatchRecipe(new[] { tree, tree }, recipe);

            Assert.IsTrue(matches);
            Cleanup(tree, recipe);
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private static CardDefinition MakeCard(string id, CardCategory category)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetId(id);
            card.SetDisplayName(id);
            SetField(card, "category", category);
            return card;
        }

        private static RecipeDefinition MakeRecipe(
            string id, bool allowExcess, params (CardDefinition card, int count)[] ingredients)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            SetField(recipe, "id", id);
            SetField(recipe, "displayName", id);
            SetField(recipe, "allowExcessIngredients", allowExcess);
            SetField(recipe, "craftingDuration", 5f);
            SetField(recipe, "randomWeight", 1f);

            var list = new List<RecipeDefinition.Ingredient>();
            foreach (var (card, count) in ingredients)
                list.Add(new RecipeDefinition.Ingredient { card = card, count = count, consumptionMode = IngredientConsumption.Consume });
            SetField(recipe, "requiredIngredients", list);
            return recipe;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Assert.IsNotNull(field, $"Field '{name}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }

        private static void Cleanup(params Object[] objects)
        {
            foreach (Object obj in objects) Object.DestroyImmediate(obj);
        }
    }
}
