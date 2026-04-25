using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Markyu.FortStack.Tests
{
    public class RecipeMatcherTests
    {
        [Test]
        public void DoesStackMatchRecipe_RejectsExtraNonIngredientInStrictRecipe()
        {
            CardDefinition wood = CreateCard("wood", CardCategory.Material);
            CardDefinition stone = CreateCard("stone", CardCategory.Material);
            RecipeDefinition recipe = CreateRecipe("plank", allowExcessIngredients: false, 1f, (wood, 2));

            bool matches = RecipeMatcher.DoesStackMatchRecipe(
                new[] { wood, wood, stone },
                recipe);

            Assert.That(matches, Is.False);
        }

        [Test]
        public void DoesStackMatchRecipe_AllowsExtraResourceCounts()
        {
            CardDefinition tree = CreateCard("tree", CardCategory.Resource);
            RecipeDefinition recipe = CreateRecipe("wood_from_tree", allowExcessIngredients: false, 1f, (tree, 1));

            bool matches = RecipeMatcher.DoesStackMatchRecipe(
                new[] { tree, tree },
                recipe);

            Assert.That(matches, Is.True);
        }

        [Test]
        public void DoesStackMatchRecipe_AllowsExtraCardsForWorkstationRecipes()
        {
            CardDefinition sawmill = CreateCard("sawmill", CardCategory.Structure);
            CardDefinition wood = CreateCard("wood", CardCategory.Material);
            CardDefinition plank = CreateCard("plank", CardCategory.Material);
            RecipeDefinition recipe = CreateRecipe("sawmill_plank", allowExcessIngredients: true, 1f, (sawmill, 1), (wood, 2));

            bool matches = RecipeMatcher.DoesStackMatchRecipe(
                new[] { sawmill, wood, wood, plank },
                recipe);

            Assert.That(matches, Is.True);
        }

        [Test]
        public void PickWeightedRecipe_SelectsRecipeByRoll()
        {
            RecipeDefinition lowWeight = CreateRecipe("low", allowExcessIngredients: false, 1f);
            RecipeDefinition highWeight = CreateRecipe("high", allowExcessIngredients: false, 3f);
            var recipes = new List<RecipeDefinition> { lowWeight, highWeight };

            Assert.That(RecipeMatcher.PickWeightedRecipe(recipes, 0.5f), Is.SameAs(lowWeight));
            Assert.That(RecipeMatcher.PickWeightedRecipe(recipes, 1.1f), Is.SameAs(highWeight));
            Assert.That(RecipeMatcher.PickWeightedRecipe(recipes, 3.9f), Is.SameAs(highWeight));
        }

        private static CardDefinition CreateCard(string id, CardCategory category)
        {
            var card = ScriptableObject.CreateInstance<CardDefinition>();
            card.SetId(id);
            card.SetDisplayName(id);
            SetField(card, "category", category);
            return card;
        }

        private static RecipeDefinition CreateRecipe(
            string id,
            bool allowExcessIngredients,
            float randomWeight,
            params (CardDefinition card, int count)[] ingredients)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeDefinition>();
            SetField(recipe, "id", id);
            SetField(recipe, "displayName", id);
            SetField(recipe, "allowExcessIngredients", allowExcessIngredients);
            SetField(recipe, "randomWeight", randomWeight);

            var requiredIngredients = new List<RecipeDefinition.Ingredient>();
            foreach (var ingredient in ingredients)
            {
                requiredIngredients.Add(new RecipeDefinition.Ingredient
                {
                    card = ingredient.card,
                    count = ingredient.count,
                    consumptionMode = IngredientConsumption.Consume
                });
            }

            SetField(recipe, "requiredIngredients", requiredIngredients);
            return recipe;
        }

        private static void SetField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Expected field '{fieldName}' on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
