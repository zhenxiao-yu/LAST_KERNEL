// CraftingManager — Drives all card-stacking crafting and exploration tasks.
//
// Responsible for:
//   • Evaluating CardStacks for recipe matches (via RecipeMatcher)
//   • Starting, ticking, pausing, stopping, and completing CraftingTasks
//   • Displaying a ProgressUI above each active crafting stack
//   • Tracking discovered recipes across sessions (for quest + codex unlock)
//   • Restoring in-progress tasks after a save/load
//
// Key dependencies:
//   RecipeMatcher        — pure matching logic (also used by edit-mode tests)
//   RecipeDefinition     — Execute() performs the actual card transformation
//   CardManager          — stats notification after craft completion
//   WorldCanvas          — parent for spawned ProgressUI instances
//   GameDirector         — OnBeforeSave hook for discovered recipes
//
// NOTE: All card creation/destruction side effects live on RecipeDefinition.Execute,
//       not here. CraftingManager only manages task lifecycle and timing.

using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class CraftingManager : MonoBehaviour, ICraftingService
    {
        public static CraftingManager Instance { get; private set; }

        public event System.Action<string> OnRecipeDiscovered;

        public event System.Action<CardDefinition> OnCraftingFinished;
        public void NotifyCraftingFinished(CardDefinition definition) => OnCraftingFinished?.Invoke(definition);

        public event System.Action<CardDefinition> OnExplorationFinished;
        public void NotifyExplorationFinished(CardDefinition definition) => OnExplorationFinished?.Invoke(definition);

        [BoxGroup("References")]
        [SerializeField, Tooltip("UI prefab displayed above a card stack to show crafting progress.")]
        private ProgressUI progressUIPrefab;

        public List<RecipeDefinition> AllRecipes { get; private set; } = new();
        public HashSet<string> DiscoveredRecipes { get; private set; } = new();

        private readonly List<CraftingTask> activeCraftingTasks = new();
        private readonly Dictionary<CraftingTask, ProgressUI> activeCraftingUIs = new();
        private readonly Dictionary<string, RecipeDefinition> recipesById = new();

        // Guards against flooding the console when progressUIPrefab is intentionally
        // absent (e.g., in test scenes). The warning fires once, then goes silent.
        private bool warnedMissingProgressUI;

        #region Unity Lifecycle
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadRecipes();

            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnBeforeSave += HandleBeforeSave;

                var gameData = GameDirector.Instance.GameData;
                if (gameData != null)
                {
                    DiscoveredRecipes.UnionWith(gameData.DiscoveredRecipes);
                }
            }
        }

        private void OnDestroy()
        {
            if (GameDirector.Instance != null)
            {
                GameDirector.Instance.OnBeforeSave -= HandleBeforeSave;
            }
        }

        void Update()
        {
            // Reverse iteration: tasks that complete or cancel are removed inside the loop.
            // Going backwards avoids index shifting and the off-by-one errors that come with it.
            for (int i = activeCraftingTasks.Count - 1; i >= 0; i--)
            {
                var task = activeCraftingTasks[i];

                if (!task.IsCanceled) // Skip ticking canceled tasks
                {
                    task.UpdateProgress(Time.deltaTime);

                    if (activeCraftingUIs.TryGetValue(task, out ProgressUI ui))
                    {
                        ui.UpdateUI(task);
                    }
                }

                if (task.IsCanceled || task.IsComplete) // Remove on cancel or complete
                {
                    // Remove from tracking BEFORE PerformCraftingAction so that
                    // StartCraftingTask's GetCraftingTask guard doesn't block the repeat.
                    if (activeCraftingUIs.ContainsKey(task))
                    {
                        Destroy(activeCraftingUIs[task].gameObject);
                        activeCraftingUIs.Remove(task);
                    }

                    activeCraftingTasks.RemoveAt(i);

                    if (task.IsComplete)
                    {
                        PerformCraftingAction(task);
                    }
                }
            }
        }
        #endregion

        private void LoadRecipes()
        {
            AllRecipes = Resources.LoadAll<RecipeDefinition>("Recipes")
                .Where(recipe => recipe != null)
                .ToList();

            recipesById.Clear();

            foreach (var recipe in AllRecipes)
            {
                if (string.IsNullOrWhiteSpace(recipe.Id))
                {
                    Debug.LogWarning($"CraftingManager: Recipe '{recipe.name}' has an empty id and cannot be restored from saves.", recipe);
                    continue;
                }

                if (recipesById.ContainsKey(recipe.Id))
                {
                    Debug.LogWarning($"CraftingManager: Duplicate recipe id '{recipe.Id}' on '{recipe.name}'. Keeping the first recipe.", recipe);
                    continue;
                }

                recipesById.Add(recipe.Id, recipe);
            }
        }

        #region Save & Load
        private void HandleBeforeSave(GameData gameData)
        {
            gameData.DiscoveredRecipes.UnionWith(this.DiscoveredRecipes);
        }

        /// <summary>
        /// Re-initializes a crafting task from saved data, used when loading a game session. 
        /// It recreates the task and its associated UI, then sets the progress to the saved value.
        /// </summary>
        /// <param name="stack">The <see cref="CardStack"/> that was performing the craft.</param>
        /// <param name="recipeId">The unique string ID used to look up the <see cref="RecipeDefinition"/>.</param>
        /// <param name="progress">The normalized or raw time value representing how much of the craft was finished.</param>
        public void RestoreCraftingTask(CardStack stack, string recipeId, float progress)
        {
            RecipeDefinition recipe = GetRecipeById(recipeId);
            if (recipe == null)
            {
                Debug.LogWarning($"Could not find recipe with ID {recipeId} to restore.");
                return;
            }

            StartCraftingTask(stack, recipe);

            var task = GetCraftingTask(stack);
            if (task != null)
            {
                task.SetProgress(progress);

                if (activeCraftingUIs.TryGetValue(task, out ProgressUI ui))
                {
                    ui.UpdateUI(task);
                }
            }
        }
        #endregion

        #region Recipe Matching
        /// <summary>
        /// Evaluates a <see cref="CardStack"/> to identify any valid recipes it matches.
        /// If multiple recipes are valid, it performs a weighted random selection based on their 
        /// <see cref="RecipeDefinition.RandomWeight"/> and initiates a <see cref="CraftingTask"/>.
        /// </summary>
        /// <param name="stack">The stack of cards to evaluate for recipe ingredients.</param>
        /// <remarks>
        /// This method handles logic for:
        /// <list type="bullet">
        /// <item><description>Filtering <see cref="AllRecipes"/> against the current stack composition.</description></item>
        /// <item><description>Calculating total weights for probabilistic outcomes.</description></item>
        /// <item><description>Handling fallback selection if weights are zero.</description></item>
        /// </list>
        /// </remarks>
        public void CheckForRecipe(CardStack stack)
        {
            if (stack == null || stack.Cards.Count == 0) return;

            List<RecipeDefinition> matchingRecipes = RecipeMatcher.FindMatchingRecipes(
                stack.Cards.Select(card => card != null ? card.BaseDefinition : null),
                AllRecipes);

            if (matchingRecipes.Count == 0)
            {
                return;
            }

            RecipeDefinition chosenRecipe = RecipeMatcher.PickRandomWeightedRecipe(matchingRecipes);
            StartCraftingTask(stack, chosenRecipe);
        }

        private bool DoesStackMatchRecipe(CardStack stack, RecipeDefinition recipe)
        {
            return stack != null && RecipeMatcher.DoesStackMatchRecipe(
                stack.Cards.Select(card => card != null ? card.BaseDefinition : null),
                recipe);
        }
        #endregion

        #region Task Management
        private void StartCraftingTask(CardStack stack, RecipeDefinition recipe)
        {
            if (stack == null || recipe == null)
            {
                Debug.LogWarning("CraftingManager: Cannot start a crafting task without both a stack and a recipe.", this);
                return;
            }

            if (GetCraftingTask(stack) != null)
            {
                return;
            }

            // Create and add a new crafting task to the list
            var newTask = new CraftingTask(recipe, stack);
            activeCraftingTasks.Add(newTask);
            stack.SetCraftingState(true);

            TryCreateProgressUI(newTask, stack);

            if (!DiscoveredRecipes.Contains(recipe.Id))
            {
                DiscoveredRecipes.Add(recipe.Id);
                OnRecipeDiscovered?.Invoke(recipe.Id);
            }
        }

        public void PauseCraftingTask(CardStack stack)
        {
            CraftingTask task = GetCraftingTask(stack);
            task?.Pause();
        }

        public void ResumeCraftingTask(CardStack stack)
        {
            CraftingTask task = GetCraftingTask(stack);
            task?.Resume();
        }

        public void StopCraftingTask(CardStack stack)
        {
            CraftingTask task = GetCraftingTask(stack);

            if (task != null)
            {
                task.Cancel();
                stack.SetCraftingState(false);

                // Clean up the UI for the stopped task
                if (activeCraftingUIs.ContainsKey(task))
                {
                    Destroy(activeCraftingUIs[task].gameObject);
                    activeCraftingUIs.Remove(task);
                }
            }
        }

        private void PerformCraftingAction(CraftingTask task)
        {
            if (task == null || task.Recipe == null || task.TargetStack == null)
            {
                return;
            }

            var recipe = task.Recipe;
            var stack = task.TargetStack;

            recipe.Execute(stack);

            stack.SetCraftingState(false);
            CardManager.Instance?.NotifyStatsChanged();

            // Living-being results (characters, mobs) always stop the chain.
            // Auto-repeating a character recipe would allow unlimited free breeding,
            // which breaks resource pressure as a core game mechanic.
            bool resultIsLiving = recipe.ResultingCard != null &&
                (recipe.ResultingCard.Category == CardCategory.Character ||
                 recipe.ResultingCard.Category == CardCategory.Mob);

            if (resultIsLiving) return;

            // Determine whether the recipe should re-trigger automatically.
            bool shouldRepeat = false;

            if (recipe.IsContinuous)
            {
                // Explicit continuous producers (e.g. Recycler Yard) loop indefinitely
                // regardless of ingredient count.
                shouldRepeat = true;
            }
            else if (recipe.HasConsumableIngredients() && stack.Cards.Count > 0)
            {
                // Batch processors consumed some inputs but cards remain.
                // Re-check the stack — there may be enough left for another cycle.
                shouldRepeat = true;
            }
            else if (DoesStackMatchRecipe(stack, recipe))
            {
                // All-keep harvesting recipes (e.g. Recruit + Stone Deposit → Stone):
                // no ingredients were consumed, the stack is still valid, keep working.
                shouldRepeat = true;
            }

            if (shouldRepeat)
            {
                CheckForRecipe(stack);
            }
        }
        #endregion

        #region Validation
        /// <summary>
        /// Determines if an incoming card can be added to a stack that is already crafting.
        /// </summary>
        /// <param name="targetStack">The stack currently processing a craft.</param>
        /// <param name="incomingCard">The definition of the card attempting to join the stack.</param>
        /// <returns>
        /// True if the recipe allows excess ingredients (e.g., a Workstation) and the card is a valid ingredient; 
        /// otherwise, false to block interaction.
        /// </returns>
        public bool CanJoinActiveCraft(CardStack targetStack, CardDefinition incomingCard)
        {
            // 1. Get the task running on this stack
            var task = GetCraftingTask(targetStack);
            if (task == null) return false;

            if (incomingCard == null || task.Recipe == null)
            {
                return false;
            }

            if (task.Recipe.AllowExcessIngredients)
            {
                // Check if the incoming card is part of the recipe requirements
                bool isIngredient = task.Recipe.RequiredIngredients.Any(i => i.card == incomingCard);

                // Optional: Allow the "Result" card to stack 
                // (e.g. stacking Planks on top of the Sawmill output while it works)
                // bool isOutput = task.Recipe.ResultingCard == incomingCard;

                return isIngredient /*|| isOutput*/;
            }

            // 3. If strict recipe (not a workstation), we generally block interaction
            return false;
        }

        /// <summary>
        /// Re-evaluates an active crafting task after a card has been removed from its stack.
        /// </summary>
        /// <param name="stack">The stack whose composition has changed.</param>
        /// <remarks>
        /// If the remaining cards still satisfy the recipe requirements (common in "Workstation" recipes), 
        /// the task resumes. If the stack is no longer valid, the crafting task is stopped.
        /// </remarks>
        public void ValidateAndResumeTask(CardStack stack)
        {
            var task = GetCraftingTask(stack);
            if (task == null) return;

            // Checks if the stack (minus the card we just took away) still satisfies the recipe.
            if (DoesStackMatchRecipe(stack, task.Recipe))
            {
                // The remaining stack is still valid (e.g., Sawmill + 2 Wood).
                // Unpause the task!
                task.Resume();
            }
            else
            {
                // The remaining stack is broken (e.g., Sawmill + 1 Wood).
                // Stop the task.
                StopCraftingTask(stack);
            }
        }
        #endregion

        #region Recipe Discovery
        public bool IsRecipeDiscovered(string recipeId)
        {
            if (string.IsNullOrEmpty(recipeId))
            {
                // This isn't a valid recipe id, so it can't be 'undiscovered'.
                // Returning 'true' ensures it gets filtered out by the '!IsRecipeDiscovered' check.
                return true;
            }

            // Check if the list contains the ID.
            return DiscoveredRecipes.Contains(recipeId);
        }

        public void MarkRecipeAsDiscovered(RecipeDefinition recipe)
        {
            if (recipe != null && !string.IsNullOrEmpty(recipe.Id) && !DiscoveredRecipes.Contains(recipe.Id))
            {
                DiscoveredRecipes.Add(recipe.Id);
                OnRecipeDiscovered?.Invoke(recipe.Id);
            }
        }
        #endregion

        #region  Helper Methods
        public RecipeDefinition GetRecipeById(string recipeId)
        {
            return !string.IsNullOrWhiteSpace(recipeId) && recipesById.TryGetValue(recipeId, out var recipe)
                ? recipe
                : null;
        }

        public CraftingTask GetCraftingTask(CardStack stack)
        {
            return activeCraftingTasks.FirstOrDefault(task => task.TargetStack == stack);
        }

        /// <summary>
        /// Returns the ingredient list in readable string format.
        /// </summary>
        public string GetFormattedIngredients(RecipeDefinition recipe)
        {
            if (recipe == null || !AllRecipes.Contains(recipe))
            {
                return string.Empty;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < recipe.RequiredIngredients.Count; i++)
            {
                var ing = recipe.RequiredIngredients[i];
                if (ing.card == null) continue;

                sb.Append($"{ing.card.DisplayName} x{ing.count}");
                if (i < recipe.RequiredIngredients.Count - 1)
                    sb.Append(", ");
            }

            if (sb.Length > 0) sb.Append(".");

            return sb.ToString();
        }

        private void TryCreateProgressUI(CraftingTask task, CardStack stack)
        {
            if (progressUIPrefab == null)
            {
                if (!warnedMissingProgressUI)
                {
                    warnedMissingProgressUI = true;
                    Debug.LogWarning("CraftingManager: Progress UI prefab is not assigned. Crafting will continue without a visible progress indicator.", this);
                }

                return;
            }

            ProgressUI newUI = Instantiate(
                progressUIPrefab,
                stack.TargetPosition + progressUIPrefab.DisplayOffset,
                Quaternion.identity
            );

            newUI.transform.SetParent(WorldCanvas.Instance?.transform);
            newUI.transform.localRotation = Quaternion.identity;

            activeCraftingUIs.Add(task, newUI);
        }
        #endregion 
    }
}

