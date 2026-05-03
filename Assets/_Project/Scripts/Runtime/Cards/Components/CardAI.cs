// CardAI — Autonomous behaviour component added to every Character and Mob card.
//
// Two distinct modes share this component:
//
//   Aggressive mobs (IsAggressive = true)
//     • TryJoinExistingCombat  — join the nearest ongoing player-vs-mob fight.
//     • TryHuntPlayer          — charge the nearest un-engaged player card.
//     • MoveRandomly           — roam when nothing to attack.
//     [Unchanged from original behaviour.]
//
//   Villagers (IsAggressive = false)
//     • All decision-making is delegated to VillagerBrain.
//     • VillagerBrain.Tick() is called each AutoMove interval.
//     • When ColonyAIManager is in the scene the brain receives jobs from it.
//     • When there is no manager the brain self-generates jobs via AIPlanner.
//
// Public API (consumed by CardCombatant, VillagerLockToggle, CardSpawnService):
//   IsLocked     — read by UI and save/load
//   SetLocked()  — toggles AI on/off; locks suppress the brain and stop the coroutine
//   StartAI()    — called by CardCombatant.LeaveCombat
//   StopAI()     — called by CardCombatant.EnterCombat
//   Brain        — VillagerBrain reference for ColonyAIManager

using System.Collections;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CardInstance))]
    public class CardAI : MonoBehaviour
    {
        // ── Component references ───────────────────────────────────────────────
        private CardInstance  _card;
        private CardCombatant _combatant;

        // ── Coroutine handles ─────────────────────────────────────────────────
        private Coroutine _autoMoveCoroutine;
        private Coroutine _produceCoroutine;

        // ── Villager brain (non-aggressive cards only) ────────────────────────
        // Null for aggressive mobs.
        public VillagerBrain Brain { get; private set; }

        // ── Unity lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            _card      = GetComponent<CardInstance>();
            _combatant = GetComponent<CardCombatant>();

            // Create the brain once for villager cards.  Aggressive mobs do not need it.
            if (_card != null && _card.Definition != null && !_card.Definition.IsAggressive)
                Brain = new VillagerBrain(this, _card);
        }

        private void Start()
        {
            StartAI();
        }

        // ── Public AI control ─────────────────────────────────────────────────

        // IsLocked is true when the player has manually pinned this villager via VillagerLockToggle.
        // Locked villagers do not move, do not accept jobs, and do not interfere with crafting.
        public bool IsLocked { get; private set; }

        // Toggle the lock.  Called by VillagerLockToggle.OnClick and restored from save data.
        public void SetLocked(bool value)
        {
            IsLocked = value;
            if (IsLocked)
            {
                StopAI();
            }
            else
            {
                // Detach synchronously before the coroutine starts so the card snaps
                // to its solo position immediately on unlock rather than drifting after
                // the next AutoMove tick fires.
                EnsureDetachedFromStack();
                StartAI();
            }
        }

        // Resume the AI coroutines.  Called when the card leaves combat.
        public void StartAI()
        {
            StopAI();
            _autoMoveCoroutine = StartCoroutine(AutoMove());

            if (_card.Definition != null && !_card.Definition.IsAggressive
                && _card.Definition.ProduceCard != null)
            {
                _produceCoroutine = StartCoroutine(ProduceLoop());
            }
        }

        // Stop all AI activity.  Called when the card enters combat or is locked.
        public void StopAI()
        {
            if (_autoMoveCoroutine != null)
            {
                StopCoroutine(_autoMoveCoroutine);
                _autoMoveCoroutine = null;
            }
            if (_produceCoroutine != null)
            {
                StopCoroutine(_produceCoroutine);
                _produceCoroutine = null;
            }

            // Cancel any in-progress job and drop carried cards.
            Brain?.CancelCurrentJob();
        }

        // ── AutoMove coroutine ────────────────────────────────────────────────
        // Fires once per MoveInterval.  Drives both aggressive and villager behaviour.

        // Returns the correct tick interval for this card.
        // Villagers use VillagerAISettings.VillagerMoveInterval so they can move faster than
        // aggressive mobs without changing the shared CardSettings value.
        private float AIInterval()
        {
            if (_card?.Definition == null || _card.Definition.IsAggressive)
                return _card.Settings.MoveInterval;
            var s = ColonyAIManager.Instance?.Settings;
            // Autopilot off or no manager → wander at the original card pace (Stacklands feel).
            if (s == null || !s.EnableColonyAutopilot)
                return _card.Settings.MoveInterval;
            return s.VillagerMoveInterval;
        }

        private IEnumerator AutoMove()
        {
            // Stagger start so all villagers don't tick simultaneously.
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));

            while (true)
            {
                yield return new WaitForSeconds(AIInterval());

                if (!CanMove()) continue;
                if (!_card.Definition.IsAggressive && ShouldStayInEnclosure()) continue;

                // Do NOT detach from the stack when the brain is currently carrying a card
                // for delivery — that would drop the cargo mid-transit.
                bool isCarrying = Brain != null && Brain.IsCarrying;
                if (!isCarrying)
                    EnsureDetachedFromStack();

                if (_card.Definition.IsAggressive)
                    ExecuteAggressiveBehavior();
                else
                    Brain?.Tick();
            }
        }

        // Returns false if the card must not move this tick.
        private bool CanMove()
        {
            return !IsLocked
                && !_card.IsBeingDragged
                && !(_card.Stack != null && _card.Stack.IsCrafting)
                && !_combatant.IsInCombat;
        }

        // ── Enclosure constraint ──────────────────────────────────────────────
        // Non-aggressive mobs inside an enclosure card stay put.

        private bool ShouldStayInEnclosure()
        {
            if (_card.Definition.IsAggressive || _card.Stack == null) return false;

            var enclosureCard = _card.Stack.Cards
                .FirstOrDefault(c => c.GetComponent<EnclosureLogic>() != null);
            if (enclosureCard == null) return false;

            int capacity             = enclosureCard.GetComponent<EnclosureLogic>().Capacity;
            int enclosureIndex       = _card.Stack.Cards.IndexOf(enclosureCard);
            int myIndex              = _card.Stack.Cards.IndexOf(_card);
            int distanceAboveEnclosure = myIndex - enclosureIndex;
            return distanceAboveEnclosure > 0 && distanceAboveEnclosure <= capacity;
        }

        // ── Stack detach ──────────────────────────────────────────────────────
        // If the villager is sitting in someone else's stack, split off before moving.

        private void EnsureDetachedFromStack()
        {
            if (_card.Stack != null && _card.Stack.Cards.Count > 1)
                DetachFromStack();
        }

        private void DetachFromStack()
        {
            var oldStack = _card.Stack;
            if (!oldStack.Cards.Remove(_card)) return;

            var newStack = new CardStack(_card, transform.position);
            CardManager.Instance.RegisterStack(newStack);

            if (oldStack.Cards.Count == 0)
            {
                CardManager.Instance?.UnregisterStack(oldStack);
            }
            else
            {
                oldStack.SetTargetPosition(oldStack.TargetPosition);
                // Re-evaluate the leftover stack — it may still form a recipe.
                if (oldStack.IsCrafting)
                    CraftingManager.Instance?.ValidateAndResumeTask(oldStack);
                else
                    CraftingManager.Instance?.CheckForRecipe(oldStack);
            }
        }

        // ── Aggressive mob behaviour (unchanged from original) ─────────────────

        private void ExecuteAggressiveBehavior()
        {
            if (TryJoinExistingCombat()) return;
            if (TryHuntPlayer())         return;
            MoveRandomly();
        }

        private bool TryJoinExistingCombat()
        {
            var closestCombat = FindClosestRelevantCombat(_card.Definition.AggroRadius);
            if (closestCombat == null || closestCombat.Rect == null) return false;

            float dist = Vector3.Distance(transform.position, closestCombat.Rect.transform.position);
            if (dist <= _card.Definition.AttackRadius * 1.5f)
                closestCombat.AddCombatants(_card.Stack.Cards.ToList());
            else
                MoveTowards(closestCombat.Rect.transform.position);

            return true;
        }

        private bool TryHuntPlayer()
        {
            var target = FindClosestPlayerCard(_card.Definition.AggroRadius);
            if (target == null) return false;

            float dist = Vector3.Distance(transform.position, target.transform.position);
            if (dist <= _card.Definition.AttackRadius)
            {
                var attackers = _card.Stack.Cards.Where(_card.IsCombatant).ToList();
                var defenders = target.Stack.Cards.Where(_card.IsCombatant).ToList();
                if (attackers.Any() && defenders.Any())
                    CombatManager.Instance.StartCombat(attackers, defenders, false);
            }
            else
            {
                MoveTowards(target.transform.position);
            }
            return true;
        }

        // ── Mob movement helpers (used by aggressive path only) ───────────────

        private void MoveTowards(Vector3 targetPos)
        {
            if (_card?.Stack == null) return;

            Vector3 dir  = (targetPos - transform.position).normalized;
            Vector3 next = transform.position + dir * _card.Settings.MoveRadius;

            if (Board.Instance.IsPointValid(next, _card.Stack))
            {
                // Tight stagger (0.02s) keeps the stack cohesive while hunting — feels urgent.
                _card.Stack.SetTargetPositionOrganic(next, duration: 0.18f, trailStagger: 0.02f);
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

                if (Board.Instance.IsPointValid(candidate, _card.Stack))
                {
                    // Loose stagger (0.06s) lets trailing cards sway behind — feels lazy and alive.
                    _card.Stack.SetTargetPositionOrganic(candidate, duration: 0.32f, trailStagger: 0.06f);
                    CardManager.Instance?.ResolveOverlaps();
                    return;
                }
            }
        }

        // ── Mob target finders (unchanged from original) ──────────────────────

        private CombatTask FindClosestRelevantCombat(float radius)
        {
            if (CombatManager.Instance == null) return null;

            CombatTask best      = null;
            float      bestDistSq = radius * radius;
            Vector3    myPos     = transform.position;

            foreach (var task in CombatManager.Instance.ActiveCombats)
            {
                if (!task.IsOngoing || task.Rect == null) continue;

                bool hasPlayer = task.Attackers.Any(c => c.Definition.Faction == CardFaction.Player)
                              || task.Defenders.Any(c => c.Definition.Faction == CardFaction.Player);
                if (!hasPlayer) continue;

                float distSq = (task.Rect.transform.position - myPos).sqrMagnitude;
                if (distSq < bestDistSq) { bestDistSq = distSq; best = task; }
            }
            return best;
        }

        private CardInstance FindClosestPlayerCard(float radius)
        {
            return CardManager.Instance.AllCards
                .Where(c => c != null
                         && c.Definition.Faction == CardFaction.Player
                         && !c.Combatant.IsInCombat)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position))
                .FirstOrDefault();
        }

        // ── Passive produce loop ───────────────────────────────────────────────
        // Non-aggressive cards with a ProduceCard spawn it periodically.
        // Unchanged from original.

        private IEnumerator ProduceLoop()
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));

            while (true)
            {
                yield return new WaitForSeconds(_card.Definition.ProduceInterval);

                if (_card.Definition.IsAggressive || !CanMove()) continue;
                SpawnProduce();
            }
        }

        private void SpawnProduce()
        {
            var produceCard = _card.Definition.ProduceCard;
            if (produceCard == null) return;
            CardManager.Instance.CreateCardInstance(produceCard, transform.position);
            _card.PlayPuffParticle();
        }
    }
}
