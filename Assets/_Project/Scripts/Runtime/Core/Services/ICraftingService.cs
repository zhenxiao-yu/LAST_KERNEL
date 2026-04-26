using System.Collections.Generic;

namespace Markyu.LastKernel
{
    public interface ICraftingService
    {
        event System.Action<string> OnRecipeDiscovered;
        event System.Action<CardDefinition> OnCraftingFinished;
        event System.Action<CardDefinition> OnExplorationFinished;

        List<RecipeDefinition> AllRecipes { get; }
        HashSet<string> DiscoveredRecipes { get; }

        void NotifyCraftingFinished(CardDefinition definition);
        void NotifyExplorationFinished(CardDefinition definition);
        void CheckForRecipe(CardStack stack);
        void PauseCraftingTask(CardStack stack);
        void ResumeCraftingTask(CardStack stack);
        void StopCraftingTask(CardStack stack);
        bool CanJoinActiveCraft(CardStack targetStack, CardDefinition incomingCard);
        void ValidateAndResumeTask(CardStack stack);

        bool IsRecipeDiscovered(string recipeId);
        void MarkRecipeAsDiscovered(RecipeDefinition recipe);

        RecipeDefinition GetRecipeById(string recipeId);
        CraftingTask GetCraftingTask(CardStack stack);
        string GetFormattedIngredients(RecipeDefinition recipe);
        void RestoreCraftingTask(CardStack stack, string recipeId, float progress);
    }
}
