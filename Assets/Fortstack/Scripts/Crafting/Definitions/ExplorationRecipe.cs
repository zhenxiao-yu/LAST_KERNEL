using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Special Recipes/Exploration Recipe", fileName = "Recipe_Exploration_")]
    public class ExplorationRecipe : RecipeDefinition
    {
        public override void Execute(CardStack stack)
        {
            // 1. Find the Area card (The one with Loot)
            var areaCard = stack.Cards
                .FirstOrDefault(c => c.Definition.Category == CardCategory.Area
                                && c.Definition.GetRandomLoot() != null);

            if (areaCard != null)
            {
                var loot = areaCard.Definition.GetRandomLoot();
                if (loot != null)
                {
                    CardManager.Instance?.CreateCardInstance(loot, stack.TargetPosition.Flatten(), stack);
                    CraftingManager.Instance?.NotifyExplorationFinished(areaCard.Definition);
                }
            }

            // Consume personnel and tools. The area card stays unless the recipe
            // explicitly marks it for consumption.
            var rules = GetIngredientRules();
            ConsumeIngredients(stack, rules);
        }
    }
}

