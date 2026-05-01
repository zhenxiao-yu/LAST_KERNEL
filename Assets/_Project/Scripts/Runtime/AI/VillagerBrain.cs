// VillagerBrain — Per-villager job executor.
//
// One instance lives inside every non-aggressive CardAI.
// It drives a four-state execution machine that carries out an AIJob step by step:
//
//   JoiningStack   — move to a stack and join it (villager is an ingredient).
//   MovingToFetch  — move to a source stack to pick up a card.
//   Delivering     — carry the picked-up card to the destination stack.
//   Idle           — wander near current position when no better job exists.
//
// Job lifecycle:
//   ColonyAIManager.AssignJob()   →  sets CurrentJob and enters the right state
//   Tick()                        →  called each AutoMove interval by CardAI
//   CompleteCurrentJob()          →  job succeeded, releases reservations
//   CancelCurrentJob()            →  job failed / timed-out, drops carried cards
//
// Managed vs standalone mode:
//   When ColonyAIManager is in the scene AND EnableColonyAutopilot is true,
//   the brain simply idles when it has no job (the manager will push one next tick).
//   When there is no manager, the brain calls AIPlanner directly to self-assign jobs.
//   This keeps the AI working even without the global manager in the scene.

using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class VillagerBrain
    {
        // ── References ────────────────────────────────────────────────────────
        private readonly CardAI       _owner;
        private readonly CardInstance _card;

        // ── Current job ───────────────────────────────────────────────────────
        public AIJob CurrentJob { get; private set; }

        private float _jobStartTime;
        private float _lastJobEndTime = -999f;

        // ── Execution state ───────────────────────────────────────────────────
        private enum Exec { Idle, JoiningStack, MovingToFetch, Delivering }
        private Exec _exec;

        // True while the villager is physically carrying a picked-up card.
        // CardAI reads this to skip the "detach from stack" step.
        public bool IsCarrying => _exec == Exec.Delivering;

        // ── Debug ─────────────────────────────────────────────────────────────
        public string DebugDescription =>
            CurrentJob != null ? $"{CurrentJob.Type}: {CurrentJob.Description}" : "Idle";

        // ── Construction ──────────────────────────────────────────────────────
        public VillagerBrain(CardAI owner, CardInstance card)
        {
            _owner = owner;
            _card  = card;
        }

        // ── Public API ────────────────────────────────────────────────────────

        // Assign a new job (usually called by ColonyAIManager).
        public void AssignJob(AIJob job)
        {
            if (job == null) return;

            // Release reservations from any previous job.
            AIReservationSystem.Instance.ReleaseAll(_owner);

            CurrentJob     = job;
            _jobStartTime  = Time.time;

            // Enter the appropriate starting state for this job type.
            _exec = job.Type switch
            {
                AIJobType.JoinRecipeStack   => Exec.JoiningStack,
                AIJobType.FetchIngredient
                    or AIJobType.OrganizeFood
                    or AIJobType.OrganizeGold
                    or AIJobType.OrganizeMaterials
                    or AIJobType.SellExcessCard
                    or AIJobType.BuyPack
                    or AIJobType.ExposeCard      => Exec.MovingToFetch,
                _                               => Exec.Idle
            };

            // Claim reservations so other villagers skip these targets.
            if (job.SourceCard       != null) AIReservationSystem.Instance.TryReserveCard(job.SourceCard, _owner);
            if (job.DestinationStack != null) AIReservationSystem.Instance.TryReserveStack(job.DestinationStack, _owner);
        }

        // Cancel the current job, drop any carried cards, and release all reservations.
        // Call this when the villager is locked, enters combat, or is dragged by the player.
        public void CancelCurrentJob()
        {
            if (CurrentJob != null) CurrentJob.IsCanceled = true;
            AIReservationSystem.Instance.ReleaseAll(_owner);
            // Only drop cargo when actually mid-delivery — calling this unconditionally
            // would scatter any card co-located with the villager (e.g. just-joined recipe
            // stacks, or cards placed there by the player), causing unwanted board movement.
            if (_exec == Exec.Delivering) DropCarriedCards();
            CurrentJob      = null;
            _exec           = Exec.Idle;
            _lastJobEndTime = Time.time;
        }

        // ── Main tick ─────────────────────────────────────────────────────────
        // Called by CardAI.AutoMove once per MoveInterval.
        public void Tick()
        {
            // Check for timeout or external cancellation.
            if (CurrentJob != null)
            {
                float maxDuration = ColonyAIManager.Instance?.Settings != null
                    ? ColonyAIManager.Instance.Settings.MaxJobDuration
                    : 45f;

                bool timedOut   = (Time.time - _jobStartTime) > maxDuration;
                bool invalidated = !IsJobStillValid();

                if (timedOut || CurrentJob.IsCanceled || invalidated)
                {
                    CancelCurrentJob();
                    return;
                }
            }

            // Execute or request a job.
            if (CurrentJob == null || CurrentJob.Type == AIJobType.Idle)
                TryRequestOrRunStandalone();
            else
                ExecuteCurrentJob();
        }

        // ── Job request ───────────────────────────────────────────────────────

        // Ask ColonyAIManager for a job, or self-generate one if no manager is present.
        private void TryRequestOrRunStandalone()
        {
            var manager = ColonyAIManager.Instance;

            if (manager != null)
            {
                // ColonyAIManager is in the scene — it owns job assignment.
                // When EnableColonyAutopilot is true it pushes jobs on its planning tick.
                // When false it stays silent and the brain just wanders (vanilla behavior).
                RunIdle();
                return;
            }

            // No manager in scene — standalone self-assign with fallback settings.
            float cooldown = FallbackSettings().JobCooldown;
            if ((Time.time - _lastJobEndTime) < cooldown)
            {
                RunIdle();
                return;
            }

            var snapshot = ColonyStateSnapshot.Build(null);
            var jobs     = AIPlanner.GenerateJobsForVillager(_owner, snapshot, FallbackSettings());
            var best     = jobs.Count > 0 ? jobs[0] : null;

            if (best != null && best.Type != AIJobType.Idle)
                AssignJob(best);
            else
                RunIdle();
        }

        // ── Execution machine ─────────────────────────────────────────────────

        private void ExecuteCurrentJob()
        {
            switch (_exec)
            {
                case Exec.JoiningStack:  HandleJoiningStack();  break;
                case Exec.MovingToFetch: HandleMovingToFetch(); break;
                case Exec.Delivering:    HandleDelivering();    break;
                default:                 RunIdle();             break;
            }
        }

        // Walk to DestinationStack and join it directly (villager is an ingredient).
        private void HandleJoiningStack()
        {
            var job = CurrentJob;
            if (!IsDestinationValid(job)) { CancelCurrentJob(); return; }

            float arrivalDist = ArrivalThreshold();
            float dist = Vector3.Distance(_card.transform.position, job.DestinationStack.TargetPosition);
            if (dist <= arrivalDist)
            {
                JoinSolo(job.DestinationStack);
                CompleteCurrentJob();
            }
            else
            {
                MoveTowards(job.DestinationStack.TargetPosition);
            }
        }

        // Walk to SourceStack to pick up SourceCard.
        private void HandleMovingToFetch()
        {
            var job = CurrentJob;

            // Validate the source is still reachable and untouched by the player.
            if (job.SourceStack == null || job.SourceStack.Cards.Count == 0
                || job.SourceCard == null || !job.SourceStack.Cards.Contains(job.SourceCard)
                || job.SourceStack.IsCrafting)
            { CancelCurrentJob(); return; }

            if (job.SourceCard.IsBeingDragged) return; // Wait for the player to release it.

            float arrivalDist = ArrivalThreshold();
            float dist = Vector3.Distance(_card.transform.position, job.SourceStack.TargetPosition);
            if (dist <= arrivalDist)
            {
                // If the target card is buried under other cards, split the stack first so
                // it becomes the top card of a new sub-stack before we pick it up.
                // Example: stack [A, B, TARGET, D] → after SplitAt(TARGET): original=[A,B], new=[TARGET,D]
                // We then take TARGET from the new stack; D is left on the board.
                ExposeCardBySplitting(job);

                // For ExposeCard jobs the split is the entire goal — no pickup or delivery.
                if (job.Type == AIJobType.ExposeCard)
                { CompleteCurrentJob(); return; }

                // At this point job.SourceStack references the stack that holds job.SourceCard at index 0.
                if (job.SourceStack == null || !job.SourceStack.Cards.Contains(job.SourceCard))
                { CancelCurrentJob(); return; }

                PickupCard(job.SourceCard, job.SourceStack);
                AIReservationSystem.Instance.ReleaseCard(job.SourceCard, _owner);
                _exec = Exec.Delivering;
            }
            else
            {
                MoveTowards(job.SourceStack.TargetPosition);
            }
        }

        // If SourceCard is buried in a multi-card stack, split the stack so SourceCard
        // rises to index 0 (top) of a freshly-registered sub-stack.
        // Updates job.SourceStack to the sub-stack so subsequent steps use the right reference.
        private void ExposeCardBySplitting(AIJob job)
        {
            var src   = job.SourceStack;
            int index = src.Cards.IndexOf(job.SourceCard);

            if (index <= 0) return; // Already the top card — no split needed.

            // SplitAt(card) creates a new stack: [card, cards_below_card...]
            // and removes those cards from the original stack.
            var subStack = src.SplitAt(job.SourceCard);
            if (subStack == null) return; // Shouldn't happen if index > 0, but guard anyway.

            // Register the sub-stack so the board and physics solver know about it.
            CardManager.Instance?.RegisterStack(subStack);

            // Let the original stack re-evaluate itself — it may now match a different recipe
            // or need to stop a crafting task that depended on the removed cards.
            if (src.Cards.Count > 0)
            {
                if (src.IsCrafting)
                    CraftingManager.Instance?.StopCraftingTask(src);
                else
                    CraftingManager.Instance?.CheckForRecipe(src);
            }

            CardManager.Instance?.ResolveOverlaps();

            // Point the job at the sub-stack so PickupCard uses the right reference.
            job.SourceStack = subStack;
        }

        // Carry the picked-up card to a TradeZone (sell or buy-pack jobs).
        private void HandleTradeDelivery(AIJob job)
        {
            float dist = Vector3.Distance(_card.transform.position, job.DestinationZone.transform.position);
            if (dist <= ArrivalThreshold())
            {
                // Extract the cargo card from the villager's stack into a solo temp stack
                // so TryTradeAndConsumeStack receives only the intended card.
                var cargo = _card.Stack.Cards.FirstOrDefault(c => c != _card);
                if (cargo == null) { CancelCurrentJob(); return; }

                _card.Stack.RemoveCard(cargo);

                var tempStack = new CardStack(cargo, cargo.transform.position);
                CardManager.Instance?.RegisterStack(tempStack);

                job.DestinationZone.TryTradeAndConsumeStack(tempStack);

                // If the trade consumed the card the temp stack is now empty — unregister it.
                // If CanTrade failed the cargo sits on the board as a solo stack (no re-register needed).
                if (tempStack.Cards.Count == 0)
                    CardManager.Instance?.UnregisterStack(tempStack);

                CardManager.Instance?.ResolveOverlaps();
                CompleteCurrentJob();
            }
            else
            {
                MoveTowards(job.DestinationZone.transform.position);
            }
        }

        // Carry the picked-up card to DestinationStack.
        private void HandleDelivering()
        {
            var job = CurrentJob;

            // If we somehow lost the carried card (e.g. player grabbed it), abort.
            if (_card.Stack == null || _card.Stack.Cards.Count < 2)
            { CancelCurrentJob(); return; }

            // Trade-zone delivery (sell / buy-pack) — no DestinationStack involved.
            if (job.DestinationZone != null) { HandleTradeDelivery(job); return; }

            if (!IsDestinationValid(job)) { DropCarriedCards(); CancelCurrentJob(); return; }

            float arrivalDist = ArrivalThreshold();
            float dist = Vector3.Distance(_card.transform.position, job.DestinationStack.TargetPosition);
            if (dist <= arrivalDist)
            {
                if (job.NeedsVillagerToJoin)
                    JoinWithAllCarried(job.DestinationStack);
                else
                    DeliverCarriedOnly(job.DestinationStack);

                CompleteCurrentJob();
            }
            else
            {
                MoveTowards(job.DestinationStack.TargetPosition);
            }
        }

        // Wander when idle.
        private void RunIdle()
        {
            MoveRandomly();
        }

        // ── Completion ────────────────────────────────────────────────────────

        private void CompleteCurrentJob()
        {
            if (CurrentJob != null) CurrentJob.IsComplete = true;
            AIReservationSystem.Instance.ReleaseAll(_owner);
            CurrentJob      = null;
            _exec           = Exec.Idle;
            _lastJobEndTime = Time.time;
        }

        // ── Validation ────────────────────────────────────────────────────────

        private bool IsJobStillValid()
        {
            if (CurrentJob == null) return false;
            switch (CurrentJob.Type)
            {
                case AIJobType.JoinRecipeStack:
                    return CurrentJob.DestinationStack != null
                        && CurrentJob.DestinationStack.Cards.Count > 0
                        && !CurrentJob.DestinationStack.IsCrafting;

                case AIJobType.FetchIngredient:
                case AIJobType.OrganizeFood:
                case AIJobType.OrganizeGold:
                case AIJobType.OrganizeMaterials:
                    // If we are already delivering (carrying the card), only the destination matters.
                    if (_exec == Exec.Delivering)
                        return CurrentJob.DestinationStack != null
                            && !CurrentJob.DestinationStack.IsCrafting;
                    // Otherwise the source must still hold the card.
                    return CurrentJob.SourceCard  != null
                        && CurrentJob.SourceStack != null
                        && CurrentJob.SourceStack.Cards.Contains(CurrentJob.SourceCard)
                        && !CurrentJob.SourceStack.IsCrafting
                        && CurrentJob.DestinationStack != null;

                case AIJobType.SellExcessCard:
                case AIJobType.BuyPack:
                    if (_exec == Exec.Delivering)
                        return CurrentJob.DestinationZone != null;
                    return CurrentJob.SourceCard  != null
                        && CurrentJob.SourceStack != null
                        && CurrentJob.SourceStack.Cards.Contains(CurrentJob.SourceCard)
                        && !CurrentJob.SourceStack.IsCrafting
                        && CurrentJob.DestinationZone != null;

                case AIJobType.ExposeCard:
                    return CurrentJob.SourceCard  != null
                        && CurrentJob.SourceStack != null
                        && CurrentJob.SourceStack.Cards.Contains(CurrentJob.SourceCard)
                        && !CurrentJob.SourceStack.IsCrafting
                        // Cancel if player already freed the card manually.
                        && CurrentJob.SourceStack.Cards.IndexOf(CurrentJob.SourceCard) > 0;

                case AIJobType.Idle:
                    return true;

                default:
                    return true;
            }
        }

        private bool IsDestinationValid(AIJob job) =>
            job?.DestinationStack != null && !job.DestinationStack.IsCrafting;

        // ── Stack operations ─────────────────────────────────────────────────
        // Mirrors the private helpers from the original CardAI.

        // Move only this villager into `target`.
        private void JoinSolo(CardStack target)
        {
            var myStack = _card.Stack;
            myStack.RemoveCard(_card);
            if (myStack.Cards.Count == 0)
                CardManager.Instance.UnregisterStack(myStack);

            target.AddCard(_card);
            target.SetTargetPosition(target.TargetPosition);
            CraftingManager.Instance?.CheckForRecipe(target);
            CardManager.Instance?.ResolveOverlaps();
        }

        // Move this villager AND all cards it is carrying into `target`.
        private void JoinWithAllCarried(CardStack target)
        {
            var myStack = _card.Stack;
            var toMove  = myStack.Cards.ToList();
            foreach (var c in toMove)
            {
                myStack.RemoveCard(c);
                target.AddCard(c);
            }
            if (myStack.Cards.Count == 0)
                CardManager.Instance.UnregisterStack(myStack);

            target.SetTargetPosition(target.TargetPosition);
            CraftingManager.Instance?.CheckForRecipe(target);
            CardManager.Instance?.ResolveOverlaps();
        }

        // Drop only the carried card into `target`; this villager stays solo.
        private void DeliverCarriedOnly(CardStack target)
        {
            var carried = _card.Stack.Cards.FirstOrDefault(c => c != _card);
            if (carried == null) return;

            _card.Stack.RemoveCard(carried);
            target.AddCard(carried);
            target.SetTargetPosition(target.TargetPosition);
            CraftingManager.Instance?.CheckForRecipe(target);
            CardManager.Instance?.ResolveOverlaps();
        }

        // Lift `card` out of `source` and place it in this villager's stack.
        private void PickupCard(CardInstance card, CardStack source)
        {
            source.RemoveCard(card);
            if (source.Cards.Count == 0)
                CardManager.Instance.UnregisterStack(source);
            else
                CraftingManager.Instance?.CheckForRecipe(source);

            _card.Stack.AddCard(card);
            _card.Stack.SetTargetPosition(_card.Stack.TargetPosition);
            CardManager.Instance?.ResolveOverlaps();
        }

        // Return any cards the villager is carrying to solo stacks on the board.
        private void DropCarriedCards()
        {
            if (_card?.Stack == null) return;
            var extras = _card.Stack.Cards.Where(c => c != _card).ToList();
            foreach (var extra in extras)
            {
                _card.Stack.RemoveCard(extra);
                CardManager.Instance?.RegisterStack(new CardStack(extra, extra.transform.position));
            }
            if (extras.Count > 0)
                CardManager.Instance?.ResolveOverlaps();
        }

        // ── Movement ─────────────────────────────────────────────────────────
        // Uses CardSettings for step size (same values as the original CardAI).

        private void MoveTowards(Vector3 targetPos)
        {
            if (_card?.Stack == null) return;

            Vector3 dir  = (targetPos - _card.transform.position).normalized;
            Vector3 next = _card.transform.position + dir * _card.Settings.MoveRadius;

            if (Board.Instance != null && Board.Instance.IsPointValid(next, _card.Stack))
            {
                _card.Stack.SetTargetPosition(next);
                CardManager.Instance?.ResolveOverlaps();
            }
        }

        private void MoveRandomly()
        {
            if (_card?.Stack == null) return;

            Vector3 basePos = _card.Stack.TargetPosition;
            for (int i = 0; i < _card.Settings.MaxAttemptsPerMove; i++)
            {
                Vector2 dir       = Random.insideUnitCircle.normalized;
                Vector3 candidate = basePos + new Vector3(dir.x, 0f, dir.y) * _card.Settings.MoveRadius;

                if (Board.Instance != null && Board.Instance.IsPointValid(candidate, _card.Stack))
                {
                    _card.Stack.SetTargetPosition(candidate);
                    CardManager.Instance?.ResolveOverlaps();
                    return;
                }
            }
        }

        // ── Utility ───────────────────────────────────────────────────────────

        // Distance within which the villager considers itself "arrived" at a target.
        private float ArrivalThreshold() => _card.Settings.MoveRadius * 1.5f;

        // Minimal settings used in standalone mode when no ColonyAIManager is present.
        private static VillagerAISettings _fallback;
        private static VillagerAISettings FallbackSettings()
        {
            if (_fallback != null) return _fallback;
            // ScriptableObject.CreateInstance works at runtime; never persisted to disk.
            _fallback = ScriptableObject.CreateInstance<VillagerAISettings>();
            return _fallback;
        }
    }
}
