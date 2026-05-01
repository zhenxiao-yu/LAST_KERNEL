// AIPlanner — Generates a scored list of AIJobs for one villager given the colony state.
//
// Pure static class — no state, fully deterministic for the same inputs.
// Called by:
//   • ColonyAIManager.RunPlanningTick()  — assigns the best job to each free worker.
//   • VillagerBrain (standalone mode)    — generates its own job when no manager is present.
//
// Decision order (highest priority first):
//   1. Survival — food is critical → food-producing recipes get urgency bonus
//   2. Crafting — join a recipe stack; or fetch a missing ingredient
//   3. Organisation — consolidate scattered food / gold / materials
//   4. Sell excess — move an over-limit card to a trade zone   (gated by setting)
//   5. Idle — wander near colony centre

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class AIPlanner
    {
        // ── Public entry point ────────────────────────────────────────────────

        // Build and return a scored, descending-priority list of candidate jobs
        // for a single villager.  The caller should pick jobs[0] as the best option.
        public static List<AIJob> GenerateJobsForVillager(
            CardAI           villager,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            var jobs = new List<AIJob>();
            if (villager == null || settings == null || state == null) return jobs;

            var card = villager.GetComponent<CardInstance>();
            if (card == null || card.IsBeingDragged || villager.IsLocked) return jobs;

            // Never plan during the end-of-day modal pipeline (feeding / selling / night combat).
            if (state.IsDayCycleRunning) return jobs;

            // Block planning only during active Night phase.
            // Allow Dawn and Dusk: RunStateData can default to Dawn before Bind() is called,
            // and blocking those phases would silently starve villagers of all work.
            if (state.CurrentPhase == GamePhase.Night) return jobs;

            Vector3 pos = card.transform.position;

            // --- Generate candidates ---
            GenerateCraftingJobs   (jobs, villager, card, pos, state, settings);
            GenerateOrganisationJobs(jobs, villager, pos, state, settings);

            if (settings.AllowAIToSellCards)
                GenerateSellJobs(jobs, villager, pos, state, settings);

            if (settings.AllowAIToBuyPacks)
                GenerateBuyPackJobs(jobs, villager, pos, state, settings);

            // Idle is always added as the lowest-priority fallback.
            jobs.Add(BuildIdleJob(pos, state, settings));

            jobs.Sort((a, b) => b.Score.CompareTo(a.Score));
            return jobs;
        }

        // ── Job generators ────────────────────────────────────────────────────

        // ─ Crafting ───────────────────────────────────────────────────────────
        // For each known recipe, try two job types:
        //   A) JoinRecipeStack   — villager walks to an existing stack that only needs
        //                          this villager to trigger the recipe.
        //   B) FetchIngredient   — exactly one non-villager ingredient is missing from
        //                          a stack; villager fetches it from elsewhere.
        private static void GenerateCraftingJobs(
            List<AIJob>         jobs,
            CardAI              villager,
            CardInstance        card,
            Vector3             pos,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            if (CraftingManager.Instance == null || CardManager.Instance == null) return;
            if (state.AllRecipes == null || state.AllRecipes.Count == 0)          return;

            var reservations = AIReservationSystem.Instance;
            var myDef        = card.Definition;

            foreach (var recipe in state.AllRecipes)
            {
                if (recipe?.RequiredIngredients == null || recipe.RequiredIngredients.Count == 0)
                    continue;

                // Skip undiscovered recipes unless the player has opted in.
                bool isDiscovered = state.DiscoveredRecipeIds.Contains(recipe.Id);
                if (!isDiscovered && !settings.AllowAIToCraftUnknownRecipes) continue;

                float baseScore = isDiscovered
                    ? settings.DiscoveredRecipeWeight
                    : settings.UnknownRecipeWeight;

                // Boost urgency for food-producing recipes when the colony is hungry.
                float urgency = 1f;
                if (state.IsFoodCritical
                    && recipe.ResultingCard != null
                    && recipe.ResultingCard.Category == CardCategory.Consumable)
                {
                    urgency = settings.FoodUrgencyMultiplier;
                }

                bool villagerIsIngredient = recipe.RequiredIngredients.Any(ing => ing.card == myDef);

                // ── Job A: Join an existing stack directly ──────────────────────
                foreach (var dest in state.AllStacks)
                {
                    if (dest == card.Stack)      continue;
                    if (dest.IsCrafting)          continue;
                    if (dest.Cards.Count == 0)   continue;
                    // Don't target a stack another villager has already reserved.
                    if (reservations.IsStackReserved(dest)) continue;

                    // Hypothetical: what recipes match if this villager joins?
                    var hypo = dest.Cards.Select(c => c.Definition).Append(myDef);
                    if (RecipeMatcher.FindMatchingRecipes(hypo, state.AllRecipes).Count > 0)
                    {
                        float dist  = Vector3.Distance(pos, dest.TargetPosition);
                        float score = baseScore * urgency - dist * settings.DistancePenaltyPerUnit;

                        string resultName = recipe.ResultingCard != null
                            ? recipe.ResultingCard.DisplayName : "?";
                        string topName = dest.TopCard?.Definition?.DisplayName ?? "stack";

                        jobs.Add(new AIJob
                        {
                            Type               = AIJobType.JoinRecipeStack,
                            Score              = score,
                            Urgency            = urgency,
                            DestinationStack   = dest,
                            Recipe             = recipe,
                            NeedsVillagerToJoin = true,
                            Description        = $"Join '{topName}' → craft {resultName}"
                        });
                    }
                }

                // ── Job B: Fetch a missing ingredient ───────────────────────────
                // Only fires when exactly one ingredient of count 1 is still missing
                // from a destination stack.  Multi-missing setups are too complex to
                // solve in a single trip.
                foreach (var dest in state.AllStacks)
                {
                    if (dest == card.Stack)     continue;
                    if (dest.IsCrafting)         continue;
                    if (dest.Cards.Count == 0)  continue;
                    if (reservations.IsStackReserved(dest)) continue;

                    // Include the villager in the hypothetical composition if it's an ingredient.
                    IEnumerable<CardDefinition> baseHypo = villagerIsIngredient
                        ? dest.Cards.Select(c => c.Definition).Append(myDef)
                        : dest.Cards.Select(c => c.Definition);

                    var missing = GetMissingIngredients(baseHypo, recipe);
                    // We only handle single-card shortfalls here.
                    if (missing == null || missing.Count != 1 || missing[0].count != 1) continue;

                    var neededDef = missing[0].def;

                    // Look for that card in another idle stack.
                    foreach (var src in state.AllStacks)
                    {
                        if (src == dest || src == card.Stack || src.IsCrafting) continue;

                        var candidate = src.Cards.FirstOrDefault(c =>
                            c.Definition == neededDef &&
                            !c.IsBeingDragged &&
                            !reservations.IsCardReserved(c));
                        if (candidate == null) continue;

                        // Score penalises total travel (villager→source + source→dest).
                        float travel = Vector3.Distance(pos, src.TargetPosition)
                                     + Vector3.Distance(src.TargetPosition, dest.TargetPosition);
                        float score  = baseScore * urgency - travel * settings.DistancePenaltyPerUnit;

                        string resultName = recipe.ResultingCard != null
                            ? recipe.ResultingCard.DisplayName : "?";

                        jobs.Add(new AIJob
                        {
                            Type               = AIJobType.FetchIngredient,
                            Score              = score,
                            Urgency            = urgency,
                            SourceCard         = candidate,
                            SourceStack        = src,
                            DestinationStack   = dest,
                            Recipe             = recipe,
                            NeedsVillagerToJoin = villagerIsIngredient,
                            Description        = $"Fetch '{neededDef.DisplayName}' → craft {resultName}"
                        });
                    }
                }
            }
        }

        // ─ Organisation ───────────────────────────────────────────────────────
        // Consolidate scattered single-card stacks of the same type.
        // Lower priority than crafting but keeps the board tidy.
        private static void GenerateOrganisationJobs(
            List<AIJob>         jobs,
            CardAI              villager,
            Vector3             pos,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            var reservations = AIReservationSystem.Instance;

            // Food consolidation gets an urgency boost when food is low.
            float foodUrgency = state.IsFoodCritical ? settings.FoodUrgencyMultiplier : 1f;
            TryAddConsolidationJob(jobs, pos, state.FoodCards,     AIJobType.OrganizeFood,
                settings.OrganizationWeight * foodUrgency, settings, reservations, "Consolidate food");

            TryAddConsolidationJob(jobs, pos, state.GoldCards,     AIJobType.OrganizeGold,
                settings.OrganizationWeight, settings, reservations, "Consolidate gold");

            TryAddConsolidationJob(jobs, pos, state.MaterialCards, AIJobType.OrganizeMaterials,
                settings.OrganizationWeight, settings, reservations, "Consolidate materials");

            // Expose any valuable cards buried under junk in mixed stacks.
            // Scored the same as consolidation so urgency bonuses (food-critical) still apply.
            TryAddExposeJobs(jobs, pos, state.FoodCards,     settings.OrganizationWeight * foodUrgency, settings, reservations);
            TryAddExposeJobs(jobs, pos, state.GoldCards,     settings.OrganizationWeight,               settings, reservations);
            TryAddExposeJobs(jobs, pos, state.MaterialCards, settings.OrganizationWeight,               settings, reservations);
        }

        // Finds two separate solo stacks of the same card type and adds a job to
        // move one onto the other.
        private static void TryAddConsolidationJob(
            List<AIJob>          jobs,
            Vector3              pos,
            List<CardInstance>   cards,
            AIJobType            jobType,
            float                baseScore,
            VillagerAISettings   settings,
            AIReservationSystem  reservations,
            string               description)
        {
            // We need at least two separate single-card stacks to merge.
            var soloCards = cards
                .Where(c => c != null
                         && c.Stack != null
                         && c.Stack.Cards.Count == 1
                         && !c.IsBeingDragged
                         && !reservations.IsCardReserved(c))
                .ToList();

            if (soloCards.Count < 2) return;

            // Pick the closest source card to the villager.
            var src = soloCards
                .OrderBy(c => Vector3.Distance(pos, c.transform.position))
                .First();

            // Pick the closest destination that is not the source.
            var dst = soloCards
                .Where(c => c != src && c.Stack != src.Stack)
                .OrderBy(c => Vector3.Distance(src.transform.position, c.transform.position))
                .FirstOrDefault();

            if (dst == null) return;

            float dist  = Vector3.Distance(pos, src.transform.position);
            float score = baseScore - dist * settings.DistancePenaltyPerUnit;

            jobs.Add(new AIJob
            {
                Type             = jobType,
                Score            = score,
                Urgency          = 1f,
                SourceCard       = src,
                SourceStack      = src.Stack,
                DestinationStack = dst.Stack,
                Description      = description
            });
        }

        // ─ Expose buried cards ────────────────────────────────────────────────
        // Generates an ExposeCard job for every valuable card that is buried (index > 0)
        // inside a mixed stack that is not actively crafting.  The villager walks to the
        // stack, calls SplitAt to free the card into its own stack, and is done — no
        // delivery.  This handles the common "supply card covered by scrap after pack open"
        // scenario even when there is no consolidation partner to merge with.
        private static void TryAddExposeJobs(
            List<AIJob>         jobs,
            Vector3             pos,
            List<CardInstance>  cards,
            float               baseScore,
            VillagerAISettings  settings,
            AIReservationSystem reservations)
        {
            foreach (var card in cards)
            {
                if (card?.Stack == null)               continue;
                if (card.Stack.IsCrafting)             continue;
                if (card.IsBeingDragged)               continue;
                if (reservations.IsCardReserved(card)) continue;

                int index = card.Stack.Cards.IndexOf(card);
                if (index <= 0) continue; // Already the top card — nothing to expose.

                // The card directly on top has the same category → this is a valid consolidation
                // stack, not junk-on-valuable.  Exposing it would undo consolidation and loop.
                var cardAbove = card.Stack.Cards[index - 1];
                if (cardAbove?.Definition?.Category == card.Definition?.Category) continue;

                float dist  = Vector3.Distance(pos, card.Stack.TargetPosition);
                float score = baseScore - dist * settings.DistancePenaltyPerUnit;

                jobs.Add(new AIJob
                {
                    Type        = AIJobType.ExposeCard,
                    Score       = score,
                    Urgency     = 1f,
                    SourceCard  = card,
                    SourceStack = card.Stack,
                    Description = $"Expose '{card.Definition?.DisplayName ?? "card"}' from stack"
                });
            }
        }

        // ─ Sell excess ────────────────────────────────────────────────────────
        // Carries one sellable card to the CardBuyer zone per job.
        // Protects food when the colony is hungry; boosts urgency at card-cap pressure.
        private static void GenerateSellJobs(
            List<AIJob>         jobs,
            CardAI              villager,
            Vector3             pos,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            if (state.SellZone == null) return;

            var reservations = AIReservationSystem.Instance;

            // Never sell gold (needed for packs) or villagers.
            // Never sell food when the colony is already hungry.
            var sellable = state.AllCards
                .Where(c => c                      != null
                         && c.Stack                != null
                         && !c.Stack.IsCrafting
                         && c.Definition.IsSellable
                         && c.Definition.Category != CardCategory.Character
                         && c.Definition.Category != CardCategory.Currency
                         && !(state.IsFoodCritical && c.Definition.Category == CardCategory.Consumable)
                         && !c.IsBeingDragged
                         && !reservations.IsCardReserved(c))
                .ToList();

            if (sellable.Count == 0) return;

            // Protect recipe ingredients from being sold before they can be used.
            // A supply cache, wood, or ore may have no crafting partner on the board right now
            // but selling it destroys future recipe potential.  In a desperate state (food
            // critical or card-cap pressure) this protection lifts so the colony can survive.
            bool desperate = state.IsFoodCritical || state.IsCardCapPressure;
            if (!desperate)
                sellable = sellable
                    .Where(c => !IsIngredientInAnyRecipe(c.Definition, state.AllRecipes))
                    .ToList();

            if (sellable.Count == 0) return;

            float urgency    = state.IsCardCapPressure ? settings.CardCapUrgencyMultiplier : 1f;
            float baseScore  = settings.SellWeight * urgency;
            Vector3 zonePos  = state.SellZone.transform.position;

            // Sell the card that minimises total travel (villager → card → zone).
            var best = sellable
                .OrderBy(c => Vector3.Distance(pos, c.Stack.TargetPosition)
                            + Vector3.Distance(c.Stack.TargetPosition, zonePos))
                .First();

            float travel = Vector3.Distance(pos, best.Stack.TargetPosition)
                         + Vector3.Distance(best.Stack.TargetPosition, zonePos);
            float score  = baseScore - travel * settings.DistancePenaltyPerUnit;

            jobs.Add(new AIJob
            {
                Type            = AIJobType.SellExcessCard,
                Score           = score,
                Urgency         = urgency,
                SourceCard      = best,
                SourceStack     = best.Stack,
                DestinationZone = state.SellZone,
                Description     = $"Sell '{best.Definition.DisplayName}'"
            });
        }

        // ─ Buy pack ───────────────────────────────────────────────────────────
        // Carries one gold card to the active PackVendor closest to being fully paid.
        // Multiple trips accumulate payment; the vendor spawns the pack automatically.
        private static void GenerateBuyPackJobs(
            List<AIJob>         jobs,
            CardAI              villager,
            Vector3             pos,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            if (state.PackVendors == null || state.PackVendors.Count == 0) return;

            var reservations = AIReservationSystem.Instance;

            // Find an available gold card (not carried, not reserved, not in a crafting stack).
            var gold = state.GoldCards
                .Where(c => c                 != null
                         && c.Stack           != null
                         && !c.Stack.IsCrafting
                         && !c.IsBeingDragged
                         && !reservations.IsCardReserved(c))
                .OrderBy(c => Vector3.Distance(pos, c.Stack.TargetPosition))
                .FirstOrDefault();

            if (gold == null) return;

            // Prefer the vendor with the smallest remaining cost (closest to completion).
            var vendor = state.PackVendors
                .OrderBy(v => v.RemainingCost)
                .First();

            float travel = Vector3.Distance(pos, gold.Stack.TargetPosition)
                         + Vector3.Distance(gold.Stack.TargetPosition, vendor.transform.position);
            float score  = settings.BuyPackWeight - travel * settings.DistancePenaltyPerUnit;

            jobs.Add(new AIJob
            {
                Type            = AIJobType.BuyPack,
                Score           = score,
                Urgency         = 1f,
                SourceCard      = gold,
                SourceStack     = gold.Stack,
                DestinationZone = vendor,
                Description     = $"Buy pack — pay '{vendor.PackId}' ({vendor.RemainingCost} left)"
            });
        }

        // ─ Idle ───────────────────────────────────────────────────────────────
        private static AIJob BuildIdleJob(
            Vector3             pos,
            ColonyStateSnapshot state,
            VillagerAISettings  settings)
        {
            // Drift toward the average position of all villagers (colony centre).
            Vector3 colonyCenter = pos;
            if (state.Villagers.Count > 0)
            {
                colonyCenter = Vector3.zero;
                foreach (var v in state.Villagers) colonyCenter += v.transform.position;
                colonyCenter /= state.Villagers.Count;
            }

            return new AIJob
            {
                Type           = AIJobType.Idle,
                Score          = settings.IdleWeight,
                Urgency        = 0f,
                TargetPosition = colonyCenter,
                Description    = "Idle near colony centre"
            };
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        // Returns which ingredients are still missing from `stackDefs` to satisfy `recipe`.
        // Returns null when the stack composition is incompatible with a strict recipe
        // (i.e. the stack has foreign cards the recipe does not accept).
        internal static List<(CardDefinition def, int count)> GetMissingIngredients(
            IEnumerable<CardDefinition> stackDefs,
            RecipeDefinition            recipe)
        {
            if (recipe?.RequiredIngredients == null) return null;

            // Count how many of each definition the hypothetical stack already has.
            var haveMap = stackDefs
                .Where(d => d != null)
                .GroupBy(d => d)
                .ToDictionary(g => g.Key, g => g.Count());

            // Strict recipe: a single foreign card disqualifies the stack entirely.
            if (!recipe.AllowExcessIngredients)
            {
                foreach (var def in haveMap.Keys)
                    if (!recipe.RequiredIngredients.Any(ing => ing.card == def))
                        return null;
            }

            var missing = new List<(CardDefinition def, int count)>();
            foreach (var ing in recipe.RequiredIngredients)
            {
                if (ing.card == null) continue;
                int have = haveMap.TryGetValue(ing.card, out int c) ? c : 0;
                int need = ing.count - have;
                if (need > 0) missing.Add((ing.card, need));
            }
            return missing;
        }

        // Returns true if `def` appears as a required ingredient in any recipe.
        // Used by GenerateSellJobs to protect recipe ingredients from premature sale.
        // Intentional foreach (not LINQ) to avoid per-tick allocations.
        private static bool IsIngredientInAnyRecipe(CardDefinition def, List<RecipeDefinition> recipes)
        {
            if (def == null || recipes == null) return false;
            foreach (var recipe in recipes)
            {
                if (recipe?.RequiredIngredients == null) continue;
                foreach (var ing in recipe.RequiredIngredients)
                    if (ing.card == def) return true;
            }
            return false;
        }
    }
}
