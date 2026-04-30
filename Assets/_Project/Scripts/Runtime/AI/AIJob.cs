// AIJob — A concrete task that a VillagerBrain can execute.
//
// Created by AIPlanner, assigned by ColonyAIManager (or self-assigned in standalone mode).
// VillagerBrain drives the multi-step state machine that actually carries out the job.
//
// Lifecycle:
//   AIPlanner.GenerateJobsForVillager()  →  list of scored candidates
//   ColonyAIManager picks the best       →  VillagerBrain.AssignJob(job)
//   VillagerBrain.Tick()                 →  executes steps until IsComplete or IsCanceled

using UnityEngine;

namespace Markyu.LastKernel
{
    // Every job the AI can assign to a villager.
    public enum AIJobType
    {
        // Lowest priority — wander near colony centre and wait.
        Idle,

        // Walk toward a stack and join it; the villager itself is a recipe ingredient.
        JoinRecipeStack,

        // Fetch one card from a source stack, carry it to a destination stack to
        // complete (or start) a recipe.  Also used for card organisation.
        FetchIngredient,

        // Consolidation variants: same execution as FetchIngredient but different label
        // so the debug overlay can display a meaningful goal.
        OrganizeFood,
        OrganizeGold,
        OrganizeMaterials,

        // Carry an excess card to a nearby trade zone for selling.
        SellExcessCard,

        // Move a pack card near a villager to open it.
        OpenPack,

        // Move weapons / armour near villager cards before night.
        PrepareForNight,

        // Stay near a specific workstation while waiting for ingredients to arrive.
        WaitNearWorkstation,

        // Split a stack to free a buried valuable card (food/gold/material).
        // No delivery — exposing the card to the board IS the whole job.
        ExposeCard,

        // Carry a gold coin to a PackVendor trade zone to pay toward a new pack.
        BuyPack,
    }

    // A single concrete task with all the context needed to execute it.
    public class AIJob
    {
        // What kind of task this is — drives which state machine branch executes it.
        public AIJobType Type;

        // Absolute score used to rank competing jobs.  Higher = higher priority.
        // Computed as:  baseWeight × urgencyMultiplier − distancePenalty
        public float Score;

        // 0–1 urgency multiplier baked into Score at generation time.
        // Stored separately for debug display ("why is this job so high?").
        public float Urgency = 1f;

        // --- Target references ---

        // The specific card to pick up (for fetch / organise / sell jobs).
        public CardInstance SourceCard;

        // Stack that currently contains SourceCard (validated before each move step).
        public CardStack SourceStack;

        // Destination stack — where SourceCard should be delivered, or where the
        // villager should join for a JoinRecipeStack job.
        public CardStack DestinationStack;

        // Trade zone target (CardBuyer or PackVendor).  Set instead of DestinationStack
        // for SellExcessCard and BuyPack jobs — the brain calls TryTradeAndConsumeStack
        // on arrival rather than stacking the carried card.
        public TradeZone DestinationZone;

        // World position used for jobs that have no stack target (Idle, WaitNear, PrepareForNight).
        public Vector3 TargetPosition;

        // The recipe this job is working toward.  Used to re-validate whether the
        // job is still worth doing after the board changes.
        public RecipeDefinition Recipe;

        // True when the villager itself must also join the destination stack after
        // delivering the fetched card (the villager is one of the recipe ingredients).
        public bool NeedsVillagerToJoin;

        // Human-readable description used in debug overlays and inspector logs.
        public string Description;

        // --- Runtime state (mutated by VillagerBrain) ---

        // Set by VillagerBrain when the job cannot be completed (target gone, timeout, etc.).
        public bool IsCanceled;

        // Set by VillagerBrain when the final delivery or join action has executed.
        public bool IsComplete;

        // Time.time when this job was assigned, used to detect timeout.
        public float AssignedTime;
    }
}
