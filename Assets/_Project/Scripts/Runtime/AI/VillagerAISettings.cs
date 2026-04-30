// VillagerAISettings — Inspector-tunable ScriptableObject for the colony AI.
//
// Attach one instance to the ColonyAIManager.  All planning parameters,
// urgency thresholds, priority weights, and autopilot feature flags live here
// so you can tune behaviour without touching code.
//
// Create via: Assets → Create → Last Kernel → AI → Villager AI Settings

using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/AI/Villager AI Settings")]
    public class VillagerAISettings : ScriptableObject
    {
        // ── Autopilot feature flags ───────────────────────────────────────────

        [BoxGroup("Autopilot")]
        [Tooltip("Master switch. When off, villagers wander like vanilla Stacklands — no autonomous jobs.")]
        public bool EnableColonyAutopilot = false;

        [BoxGroup("Autopilot")]
        [Tooltip("Allow the AI to stack a pack card onto a villager to open it automatically.")]
        public bool AllowAIToOpenPacks = false;

        [BoxGroup("Autopilot")]
        [Tooltip("Allow the AI to carry sellable cards to the trade zone.")]
        public bool AllowAIToSellCards = true;

        [BoxGroup("Autopilot")]
        [Tooltip("Allow the AI to carry gold to PackVendors to buy new packs.")]
        public bool AllowAIToBuyPacks = true;

        [BoxGroup("Autopilot")]
        [Tooltip("Allow the AI to attempt recipes the player hasn't discovered yet. " +
                 "On by default so villagers are useful from day 1. Turn off late-game if you want " +
                 "to guard rare ingredients from being consumed accidentally.")]
        public bool AllowAIToCraftUnknownRecipes = true;

        [BoxGroup("Autopilot"), Min(1)]
        [Tooltip("Cap on how many new job assignments are made per planning tick.  Prevents every villager being re-routed at once.")]
        public int MaxActionsPerPlanningTick = 5;

        // ── Timing ────────────────────────────────────────────────────────────

        [BoxGroup("Timing"), Min(0.1f)]
        [Tooltip("Seconds between each villager's move step.  Lower = faster physical movement.  " +
                 "Overrides CardSettings.MoveInterval for non-aggressive cards only.")]
        public float VillagerMoveInterval = 1.5f;

        [BoxGroup("Timing"), Min(0.5f)]
        [Tooltip("Seconds between global planning evaluations.  Lower = more responsive but higher CPU cost.")]
        public float PlanningInterval = 1.5f;

        [BoxGroup("Timing"), Min(0f)]
        [Tooltip("Minimum seconds a villager must wait before accepting a new job after finishing one.  Prevents jitter.")]
        public float JobCooldown = 0.5f;

        [BoxGroup("Timing"), Min(5f)]
        [Tooltip("A job is automatically cancelled if it has not completed within this many seconds.")]
        public float MaxJobDuration = 45f;

        // ── Survival thresholds ───────────────────────────────────────────────

        [BoxGroup("Survival"), Range(0f, 1f)]
        [Tooltip("(TotalNutrition / NutritionNeed) below this value → food jobs become urgent.")]
        public float FoodCriticalThreshold = 0.5f;

        [BoxGroup("Survival"), Range(0f, 1f)]
        [Tooltip("(CardsOwned / CardLimit) above this value → sell/discard pressure kicks in.")]
        public float CardCapWarningThreshold = 0.85f;

        [BoxGroup("Survival"), Range(0f, 1f)]
        [Tooltip("Day normalised time (0–1) above which night-prep jobs are generated.")]
        public float NightPrepTriggerProgress = 0.75f;

        // ── Priority weights ─────────────────────────────────────────────────
        // Jobs are scored:  baseWeight × urgencyMultiplier − distance × penalty
        // Higher weight = higher priority in the job list.

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for a known/discovered recipe crafting job.")]
        public float DiscoveredRecipeWeight = 60f;

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for an unknown recipe crafting job (must also enable AllowAIToCraftUnknownRecipes).")]
        public float UnknownRecipeWeight = 30f;

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for card organisation jobs (consolidating food, gold, materials).")]
        public float OrganizationWeight = 18f;

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for selling a card.  Multiplied by CardCapUrgencyMultiplier when at card-cap pressure.")]
        public float SellWeight = 40f;

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for carrying gold to a PackVendor.  Set below SellWeight so selling is preferred when at cap.")]
        public float BuyPackWeight = 35f;

        [BoxGroup("Priority Weights"), Min(0f)]
        [Tooltip("Base score for the idle fallback job.  Should be the lowest weight of all.")]
        public float IdleWeight = 1f;

        // ── Scoring ───────────────────────────────────────────────────────────

        [BoxGroup("Scoring"), Min(0f)]
        [Tooltip("Score is reduced by (distance * this value).  Higher = stronger preference for nearby tasks.")]
        public float DistancePenaltyPerUnit = 0.3f;

        [BoxGroup("Scoring"), Min(0f)]
        [Tooltip("Urgency bonus multiplier applied to food jobs when food is critically low.  Stacks on top of base weight.")]
        public float FoodUrgencyMultiplier = 2.5f;

        [BoxGroup("Scoring"), Min(0f)]
        [Tooltip("Urgency bonus multiplier applied to sell jobs when card cap is at or above threshold.")]
        public float CardCapUrgencyMultiplier = 2f;

        // ── Idle movement ─────────────────────────────────────────────────────

        [BoxGroup("Movement"), Min(0f)]
        [Tooltip("Radius used when a villager is performing idle wandering.  Should be >= CardSettings.MoveRadius.")]
        public float IdleWanderRadius = 1.5f;
    }
}
