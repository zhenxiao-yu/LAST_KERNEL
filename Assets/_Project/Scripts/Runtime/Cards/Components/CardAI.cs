using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CardInstance))]
    public class CardAI : MonoBehaviour
    {
        private CardInstance _card;
        private CardCombatant _combatant;
        private Coroutine _autoMoveCoroutine;
        private Coroutine _produceCoroutine;

        private enum VillagerState { Idle, JoiningStack, FetchingCard, DeliveringCard }
        private VillagerState _villagerState;
        private CardStack _workTarget;
        private CardInstance _fetchCard;
        private CardStack _fetchStack;
        private bool _joinAfterDelivery;

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
            _combatant = GetComponent<CardCombatant>();
        }

        private void Start()
        {
            StartAI();
        }

        public void StartAI()
        {
            StopAI();
            _autoMoveCoroutine = StartCoroutine(AutoMove());
            if (!_card.Definition.IsAggressive && _card.Definition.ProduceCard != null)
                _produceCoroutine = StartCoroutine(ProduceLoop());
        }

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
            DropCarriedCards();
            ResetVillagerTask();
        }

        #region Auto Move Logic
        private IEnumerator AutoMove()
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));

            while (true)
            {
                yield return new WaitForSeconds(_card.Settings.MoveInterval);

                if (!CanMove()) continue;
                if (ShouldStayInEnclosure()) continue;

                // Skip detach when intentionally carrying a card for delivery
                if (_villagerState != VillagerState.DeliveringCard)
                    EnsureDetachedFromStack();

                if (_card.Definition.IsAggressive)
                    ExecuteAggressiveBehavior();
                else
                    ExecuteVillagerBehavior();
            }
        }

        public bool IsLocked { get; private set; }

        public void SetLocked(bool value)
        {
            IsLocked = value;
            if (IsLocked)
                StopAI();
            else
                StartAI();
        }

        private bool CanMove()
        {
            return !IsLocked
                && !_card.IsBeingDragged
                && !(_card.Stack != null && _card.Stack.IsCrafting)
                && !_combatant.IsInCombat;
        }

        private void EnsureDetachedFromStack()
        {
            if (_card.Stack.Cards.Count > 1)
                DetachFromStack();
        }

        private bool ShouldStayInEnclosure()
        {
            if (_card.Definition.IsAggressive || _card.Stack == null)
                return false;

            var enclosureCard = _card.Stack.Cards.FirstOrDefault(
                c => c.GetComponent<EnclosureLogic>() != null);

            if (enclosureCard == null) return false;

            int capacity = enclosureCard.GetComponent<EnclosureLogic>().Capacity;
            int enclosureIndex = _card.Stack.Cards.IndexOf(enclosureCard);
            int myIndex = _card.Stack.Cards.IndexOf(_card);
            int distanceAboveEnclosure = myIndex - enclosureIndex;
            return distanceAboveEnclosure > 0 && distanceAboveEnclosure <= capacity;
        }

        private void ExecuteAggressiveBehavior()
        {
            if (TryJoinExistingCombat()) return;
            if (TryHuntPlayer()) return;
            MoveRandomly();
        }

        private bool TryJoinExistingCombat()
        {
            CombatTask closestCombat = FindClosestRelevantCombat(_card.Definition.AggroRadius);
            if (closestCombat == null || closestCombat.Rect == null) return false;

            float distanceToCombat = Vector3.Distance(transform.position, closestCombat.Rect.transform.position);
            if (distanceToCombat <= _card.Definition.AttackRadius * 1.5f)
                closestCombat.AddCombatants(_card.Stack.Cards.ToList());
            else
                MoveTowards(closestCombat.Rect.transform.position);

            return true;
        }

        private bool TryHuntPlayer()
        {
            CardInstance target = FindClosestPlayerCard(_card.Definition.AggroRadius);
            if (target == null) return false;

            float distanceToTarget = Vector3.Distance(transform.position, target.transform.position);
            if (distanceToTarget <= _card.Definition.AttackRadius)
            {
                var attackerCards = _card.Stack.Cards.Where(_card.IsCombatant).ToList();
                var defenderCards = target.Stack.Cards.Where(_card.IsCombatant).ToList();
                if (attackerCards.Any() && defenderCards.Any())
                    CombatManager.Instance.StartCombat(attackerCards, defenderCards, false);
            }
            else
            {
                MoveTowards(target.transform.position);
            }
            return true;
        }
        #endregion

        #region Villager Behavior
        private void ExecuteVillagerBehavior()
        {
            switch (_villagerState)
            {
                case VillagerState.JoiningStack:   HandleJoiningStack();   break;
                case VillagerState.FetchingCard:   HandleFetchingCard();   break;
                case VillagerState.DeliveringCard: HandleDeliveringCard(); break;
                default:                           HandleIdleSearch();     break;
            }
        }

        // Search for the highest-priority task available right now
        private void HandleIdleSearch()
        {
            // Priority 1: walk to a stack and join it to complete a recipe
            _workTarget = FindDirectJoinTarget();
            if (_workTarget != null)
            {
                _villagerState = VillagerState.JoiningStack;
                return;
            }

            // Priority 2: fetch a card from another stack and deliver it to enable a recipe
            if (FindDeliveryTask(out var fc, out var fs, out var ds, out bool join))
            {
                _fetchCard = fc;
                _fetchStack = fs;
                _workTarget = ds;
                _joinAfterDelivery = join;
                _villagerState = VillagerState.FetchingCard;
                return;
            }

            // Priority 3: nothing useful to do, roam
            MoveRandomly();
        }

        private void HandleJoiningStack()
        {
            if (!IsWorkTargetValid())
            {
                ResetVillagerTask();
                return;
            }

            float dist = Vector3.Distance(transform.position, _workTarget.TargetPosition);
            if (dist <= _card.Settings.MoveRadius * 1.5f)
            {
                JoinSolo(_workTarget);
                ResetVillagerTask();
            }
            else
            {
                MoveTowards(_workTarget.TargetPosition);
            }
        }

        private void HandleFetchingCard()
        {
            if (_fetchCard == null
                || _fetchStack == null || _fetchStack.Cards.Count == 0 || _fetchStack.IsCrafting
                || !_fetchStack.Cards.Contains(_fetchCard)
                || !IsWorkTargetValid())
            {
                ResetVillagerTask();
                return;
            }

            float dist = Vector3.Distance(transform.position, _fetchStack.TargetPosition);
            if (dist <= _card.Settings.MoveRadius * 1.5f)
            {
                PickupCard(_fetchCard, _fetchStack);
                _fetchCard = null;
                _fetchStack = null;
                _villagerState = VillagerState.DeliveringCard;
            }
            else
            {
                MoveTowards(_fetchStack.TargetPosition);
            }
        }

        private void HandleDeliveringCard()
        {
            if (_card.Stack.Cards.Count < 2 || !IsWorkTargetValid())
            {
                DropCarriedCards();
                ResetVillagerTask();
                return;
            }

            float dist = Vector3.Distance(transform.position, _workTarget.TargetPosition);
            if (dist <= _card.Settings.MoveRadius * 1.5f)
            {
                if (_joinAfterDelivery)
                    JoinWithAllCarried(_workTarget);
                else
                    DeliverCarriedOnly(_workTarget);
                ResetVillagerTask();
            }
            else
            {
                MoveTowards(_workTarget.TargetPosition);
            }
        }

        private bool IsWorkTargetValid() =>
            _workTarget != null && _workTarget.Cards.Count > 0 && !_workTarget.IsCrafting;

        private void ResetVillagerTask()
        {
            _villagerState = VillagerState.Idle;
            _workTarget = null;
            _fetchCard = null;
            _fetchStack = null;
        }

        // Move only this villager into the target stack
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

        // Move this villager AND all carried cards into the target stack
        private void JoinWithAllCarried(CardStack target)
        {
            var myStack = _card.Stack;
            var toMove = myStack.Cards.ToList();
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

        // Place only the carried card into the target stack; villager stays solo
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

        private void DropCarriedCards()
        {
            if (_card.Stack == null) return;
            var extras = _card.Stack.Cards.Where(c => c != _card).ToList();
            foreach (var extra in extras)
            {
                _card.Stack.RemoveCard(extra);
                CardManager.Instance?.RegisterStack(new CardStack(extra, extra.transform.position));
            }
            if (extras.Count > 0)
                CardManager.Instance?.ResolveOverlaps();
        }

        // Find the nearest non-crafting stack that would match a recipe if this villager joined it
        private CardStack FindDirectJoinTarget()
        {
            if (CraftingManager.Instance == null || CardManager.Instance == null) return null;
            var allRecipes = CraftingManager.Instance.AllRecipes;
            if (allRecipes == null || allRecipes.Count == 0) return null;

            var myDef = _card.Definition;
            CardStack best = null;
            float bestDistSq = float.MaxValue;
            Vector3 myPos = transform.position;

            foreach (var stack in CardManager.Instance.AllStacks.ToList())
            {
                if (stack == _card.Stack || stack.IsCrafting || stack.Cards.Count == 0) continue;

                var hypo = stack.Cards.Select(c => c.Definition).Append(myDef);
                if (RecipeMatcher.FindMatchingRecipes(hypo, allRecipes).Count > 0)
                {
                    float dSq = (stack.TargetPosition - myPos).sqrMagnitude;
                    if (dSq < bestDistSq) { bestDistSq = dSq; best = stack; }
                }
            }
            return best;
        }

        // Find the best single-card delivery: fetch one card from one stack, bring to another to enable a recipe.
        // Sets joinAfter=true when the villager itself must also join the destination (it's a recipe ingredient).
        private bool FindDeliveryTask(
            out CardInstance fetchCard, out CardStack fetchStack,
            out CardStack destStack, out bool joinAfter)
        {
            fetchCard = null; fetchStack = null; destStack = null; joinAfter = false;
            if (CraftingManager.Instance == null || CardManager.Instance == null) return false;

            var allRecipes = CraftingManager.Instance.AllRecipes;
            if (allRecipes == null || allRecipes.Count == 0) return false;

            var allStacks = CardManager.Instance.AllStacks.ToList();
            var myDef = _card.Definition;
            float bestScore = float.MaxValue;
            bool found = false;
            Vector3 myPos = transform.position;

            foreach (var recipe in allRecipes)
            {
                var ingredients = recipe.RequiredIngredients;
                if (ingredients == null || ingredients.Count == 0) continue;

                bool villagerRequired = ingredients.Any(ing => ing.card == myDef);

                foreach (var dest in allStacks)
                {
                    if (dest == _card.Stack || dest.IsCrafting || dest.Cards.Count == 0) continue;

                    // Hypothetical composition at destination, including this villager if it's an ingredient
                    IEnumerable<CardDefinition> baseHypo = villagerRequired
                        ? dest.Cards.Select(c => c.Definition).Append(myDef)
                        : dest.Cards.Select(c => c.Definition);

                    // Only proceed if exactly one card (of count 1) is still missing
                    var missing = GetMissingIngredients(baseHypo, recipe);
                    if (missing == null || missing.Count != 1 || missing[0].Item2 != 1) continue;

                    var neededDef = missing[0].Item1;

                    foreach (var src in allStacks)
                    {
                        if (src == dest || src == _card.Stack || src.IsCrafting) continue;
                        var candidate = src.Cards.FirstOrDefault(c => c.Definition == neededDef);
                        if (candidate == null) continue;

                        // Score by total travel distance: villager→source + source→destination
                        float score = Vector3.Distance(myPos, src.TargetPosition)
                                    + Vector3.Distance(src.TargetPosition, dest.TargetPosition);
                        if (score < bestScore)
                        {
                            bestScore = score;
                            fetchCard = candidate;
                            fetchStack = src;
                            destStack = dest;
                            joinAfter = villagerRequired;
                            found = true;
                        }
                    }
                }
            }
            return found;
        }

        // Returns which ingredients are still missing from stackDefs to satisfy recipe.
        // Returns null if the stack is incompatible (has foreign cards in a strict recipe).
        private static List<(CardDefinition, int)> GetMissingIngredients(
            IEnumerable<CardDefinition> stackDefs, RecipeDefinition recipe)
        {
            if (recipe?.RequiredIngredients == null) return null;

            var counts = stackDefs
                .Where(d => d != null)
                .GroupBy(d => d)
                .ToDictionary(g => g.Key, g => g.Count());

            if (!recipe.AllowExcessIngredients)
            {
                foreach (var def in counts.Keys)
                    if (!recipe.RequiredIngredients.Any(ing => ing.card == def))
                        return null;
            }

            var missing = new List<(CardDefinition, int)>();
            foreach (var ing in recipe.RequiredIngredients)
            {
                if (ing.card == null) continue;
                int have = counts.TryGetValue(ing.card, out int c) ? c : 0;
                int need = ing.count - have;
                if (need > 0) missing.Add((ing.card, need));
            }
            return missing;
        }
        #endregion

        #region Movement & Stack Helpers
        private CombatTask FindClosestRelevantCombat(float radius)
        {
            if (CombatManager.Instance == null) return null;

            CombatTask bestTask = null;
            float bestDistSq = radius * radius;
            Vector3 myPos = transform.position;

            foreach (var task in CombatManager.Instance.ActiveCombats)
            {
                if (!task.IsOngoing || task.Rect == null) continue;

                bool hasPlayerInvolvement = task.Attackers.Any(c => c.Definition.Faction == CardFaction.Player)
                                         || task.Defenders.Any(c => c.Definition.Faction == CardFaction.Player);
                if (!hasPlayerInvolvement) continue;

                float distSq = (task.Rect.transform.position - myPos).sqrMagnitude;
                if (distSq < bestDistSq) { bestDistSq = distSq; bestTask = task; }
            }
            return bestTask;
        }

        private CardInstance FindClosestPlayerCard(float radius)
        {
            return CardManager.Instance.AllCards
                .Where(c => c != null &&
                            c.Definition.Faction == CardFaction.Player &&
                            !c.Combatant.IsInCombat)
                .OrderBy(c => Vector3.Distance(transform.position, c.transform.position))
                .FirstOrDefault();
        }

        private void MoveTowards(Vector3 targetPos)
        {
            Vector3 direction = (targetPos - transform.position).normalized;
            Vector3 movePosition = transform.position + direction * _card.Settings.MoveRadius;

            if (Board.Instance.IsPointValid(movePosition, _card.Stack))
            {
                _card.Stack.SetTargetPosition(movePosition);
                CardManager.Instance?.ResolveOverlaps();
            }
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
                if (oldStack.IsCrafting)
                    CraftingManager.Instance.StopCraftingTask(oldStack);
                else
                    CraftingManager.Instance.CheckForRecipe(oldStack);
            }
        }

        private void MoveRandomly()
        {
            Vector3 basePosition = _card.Stack.TargetPosition;
            Vector3 targetPosition = basePosition;

            for (int i = 0; i < _card.Settings.MaxAttemptsPerMove; i++)
            {
                Vector2 direction = Random.insideUnitCircle.normalized;
                Vector3 candidate = basePosition + new Vector3(direction.x, 0f, direction.y) * _card.Settings.MoveRadius;

                if (Board.Instance.IsPointValid(candidate, _card.Stack))
                {
                    targetPosition = candidate;
                    break;
                }
            }

            _card.Stack.SetTargetPosition(targetPosition);
            CardManager.Instance?.ResolveOverlaps();
        }
        #endregion

        #region Produce Spawning
        private IEnumerator ProduceLoop()
        {
            yield return new WaitForSeconds(Random.Range(0.5f, 1.0f));

            while (true)
            {
                yield return new WaitForSeconds(_card.Definition.ProduceInterval);

                if (_card.Definition.IsAggressive || !CanMove())
                    continue;

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
        #endregion
    }
}
