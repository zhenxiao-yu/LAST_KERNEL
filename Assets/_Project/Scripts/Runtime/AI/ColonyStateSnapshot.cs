// ColonyStateSnapshot — Read-only picture of the entire colony at one point in time.
//
// Built at the start of each planning tick by ColonyAIManager.Build().
// Passed to AIPlanner so every job-generation method sees a consistent board
// without needing to query CardManager repeatedly.
//
// NOTE: The snapshot is a shallow copy of references — it does not clone cards or
//       stacks.  Avoid mutating any referenced object from inside AIPlanner.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class ColonyStateSnapshot
    {
        // ── Aggregate stats (economy / card-cap / feeding) ────────────────────
        public StatsSnapshot Stats;

        // ── Full board contents ───────────────────────────────────────────────
        public List<CardInstance> AllCards  = new();
        public List<CardStack>    AllStacks = new();

        // ── Cards by category ─────────────────────────────────────────────────
        public List<CardInstance> Villagers      = new();  // Characters (locked + unlocked)
        public List<CardInstance> FoodCards      = new();  // Consumable
        public List<CardInstance> GoldCards      = new();  // Currency
        public List<CardInstance> MaterialCards  = new();  // Material
        public List<CardInstance> StructureCards = new();  // Structure
        public List<CardInstance> WeaponCards    = new();  // Equipment
        public List<CardInstance> PackCards      = new();  // PackInstance

        // ── Trade zones ───────────────────────────────────────────────────────
        public CardBuyer         SellZone    = null;   // The single CardBuyer zone
        public List<PackVendor>  PackVendors = new();  // Currently active PackVendor zones

        // ── Stacks by crafting state ───────────────────────────────────────────
        public List<CardStack> CraftingStacks = new();
        public List<CardStack> IdleStacks     = new();

        // ── Derived needs ─────────────────────────────────────────────────────
        // FoodRatio  = TotalNutrition / NutritionNeed,  clamped 0–1 (1 = plenty of food)
        public float FoodRatio;
        // CardCapRatio = CardsOwned / CardLimit,  clamped 0–1 (1 = exactly at cap)
        public float CardCapRatio;
        public bool  IsFoodCritical;      // FoodRatio < settings.FoodCriticalThreshold
        public bool  IsCardCapPressure;   // CardCapRatio >= settings.CardCapWarningThreshold

        // ── Time / phase ──────────────────────────────────────────────────────
        public GamePhase CurrentPhase;
        public int       CurrentDay;
        public float     DayProgress;         // 0–1 fraction through the current day
        public bool      IsNightApproaching;  // DayProgress >= NightPrepTriggerProgress
        // True during the end-of-day modal pipeline (feeding → selling → night combat).
        // AI should not act while this is running.
        public bool      IsDayCycleRunning;

        // ── Recipes ───────────────────────────────────────────────────────────
        public List<RecipeDefinition> AllRecipes         = new();
        public HashSet<string>        DiscoveredRecipeIds = new();

        // ── Available workers ─────────────────────────────────────────────────
        // CardAI instances that are free right now:
        //   • not manually locked
        //   • not being dragged by the player
        //   • not in combat
        //   • their current stack is not actively crafting
        public List<CardAI> AvailableWorkers = new();

        // ── Factory ───────────────────────────────────────────────────────────

        // Builds a fresh snapshot from global manager singletons.
        public static ColonyStateSnapshot Build(VillagerAISettings settings)
        {
            var snap = new ColonyStateSnapshot();

            if (CardManager.Instance == null) return snap;

            // ── Stats ──────────────────────────────────────────────────────────
            snap.Stats     = CardManager.Instance.GetStatsSnapshot();
            snap.AllCards  = CardManager.Instance.AllCards
                                .Where(c => c != null && c.Definition != null)
                                .ToList();
            snap.AllStacks = CardManager.Instance.AllStacks.ToList();

            // ── Categorise cards ───────────────────────────────────────────────
            foreach (var card in snap.AllCards)
            {
                switch (card.Definition.Category)
                {
                    case CardCategory.Character:  snap.Villagers.Add(card);       break;
                    case CardCategory.Consumable: snap.FoodCards.Add(card);       break;
                    case CardCategory.Currency:   snap.GoldCards.Add(card);       break;
                    case CardCategory.Material:   snap.MaterialCards.Add(card);   break;
                    case CardCategory.Structure:  snap.StructureCards.Add(card);  break;
                    case CardCategory.Equipment:  snap.WeaponCards.Add(card);     break;
                }
                if (card is PackInstance)
                    snap.PackCards.Add(card);
            }

            // ── Classify stacks ────────────────────────────────────────────────
            foreach (var stack in snap.AllStacks)
            {
                if (stack.IsCrafting) snap.CraftingStacks.Add(stack);
                else                  snap.IdleStacks.Add(stack);
            }

            // ── Derived needs ──────────────────────────────────────────────────
            float foodThresh = settings != null ? settings.FoodCriticalThreshold  : 0.5f;
            float capThresh  = settings != null ? settings.CardCapWarningThreshold : 0.85f;

            snap.FoodRatio    = snap.Stats.NutritionNeed > 0
                ? Mathf.Clamp01((float)snap.Stats.TotalNutrition / snap.Stats.NutritionNeed)
                : 1f;
            snap.CardCapRatio = snap.Stats.CardLimit > 0
                ? Mathf.Clamp01((float)snap.Stats.CardsOwned / snap.Stats.CardLimit)
                : 0f;
            snap.IsFoodCritical    = snap.FoodRatio    < foodThresh;
            snap.IsCardCapPressure = snap.CardCapRatio >= capThresh;

            // ── Time / phase ───────────────────────────────────────────────────
            if (TimeManager.Instance != null)
            {
                snap.CurrentDay  = TimeManager.Instance.CurrentDay;
                snap.DayProgress = TimeManager.Instance.NormalizedTime;
            }

            float nightTrigger = settings != null ? settings.NightPrepTriggerProgress : 0.75f;
            snap.IsNightApproaching = snap.DayProgress >= nightTrigger;
            snap.IsDayCycleRunning  = DayCycleManager.Instance != null
                                   && DayCycleManager.Instance.IsEndingCycle;

            if (RunStateManager.Instance != null)
                snap.CurrentPhase = RunStateManager.Instance.State.CurrentPhase;

            // ── Trade zones ────────────────────────────────────────────────────
            if (TradeManager.Instance != null)
            {
                snap.SellZone    = TradeManager.Instance.Buyer;
                snap.PackVendors = TradeManager.Instance.ActiveVendors
                                   .Where(v => v != null && v.RemainingCost < int.MaxValue)
                                   .ToList();
            }

            // ── Recipes ────────────────────────────────────────────────────────
            if (CraftingManager.Instance != null)
            {
                snap.AllRecipes = CraftingManager.Instance.AllRecipes
                                  ?? new List<RecipeDefinition>();
                snap.DiscoveredRecipeIds = new HashSet<string>(
                    CraftingManager.Instance.DiscoveredRecipes
                    ?? new HashSet<string>());
            }

            // ── Available workers ──────────────────────────────────────────────
            foreach (var card in snap.Villagers)
            {
                var ai        = card.GetComponent<CardAI>();
                var combatant = card.GetComponent<CardCombatant>();

                if (ai == null || ai.IsLocked)                    continue;
                if (card.IsBeingDragged)                           continue;
                if (combatant != null && combatant.IsInCombat)     continue;
                if (card.Stack != null && card.Stack.IsCrafting)   continue;

                snap.AvailableWorkers.Add(ai);
            }

            return snap;
        }
    }
}
