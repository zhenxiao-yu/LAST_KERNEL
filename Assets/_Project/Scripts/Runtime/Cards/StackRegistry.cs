// Owns the active CardStack list and all stack-level operations:
// overlap resolution, stacking-rule checks, and drag highlights.
//
// Consumed exclusively by CardManager (the ICardService facade).
// External callers go through CardManager — they never touch this directly.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    internal sealed class StackRegistry
    {
        private readonly List<CardStack> stacks = new();
        private readonly List<CardInstance> highlightedCards = new();
        private readonly StackingRulesMatrix stackingMatrix;
        private readonly CardSettings cardSettings;

        public IReadOnlyList<CardStack> AllStacks => stacks;

        // Cards engaged in active combat are temporarily removed from their stacks
        // and owned by CombatManager. Including them here ensures every consumer of
        // AllCards — feeding, stats, quest checks — accounts for every living card.
        public IEnumerable<CardInstance> AllCards
        {
            get
            {
                var stackedCards = stacks.SelectMany(s => s.Cards);
                if (CombatManager.Instance == null) return stackedCards;

                var combatCards = CombatManager.Instance.ActiveCombats
                    .Where(c => c.IsOngoing)
                    .SelectMany(c => c.Attackers.Concat(c.Defenders));

                return stackedCards.Concat(combatCards);
            }
        }

        public StackRegistry(StackingRulesMatrix stackingMatrix, CardSettings cardSettings)
        {
            this.stackingMatrix = stackingMatrix;
            this.cardSettings = cardSettings;
        }

        public void Register(CardStack stack)
        {
            if (stack != null && !stacks.Contains(stack))
                stacks.Add(stack);
        }

        public void Unregister(CardStack stack)
        {
            if (stack != null) stacks.Remove(stack);
        }

        public bool CanStack(CardDefinition bottom, CardDefinition top)
        {
            if (bottom == null || top == null) return false;

            if (stackingMatrix == null)
            {
                Debug.LogWarning("StackRegistry: Stacking matrix is not assigned; refusing stack operation.");
                return false;
            }

            return stackingMatrix.GetRule(bottom.Category, top.Category) switch
            {
                StackingRule.None => false,
                StackingRule.CategoryWide => true,
                StackingRule.SameDefinition => bottom == top,
                _ => false
            };
        }

        public void HighlightStackableStacks(CardInstance liftedCard)
        {
            foreach (var stack in stacks)
            {
                if (stack.BottomCard == null) continue;

                bool canStack = CanStack(liftedCard.Definition, stack.BottomCard.Definition);
                bool sameCard = liftedCard == stack.BottomCard;
                bool sameStack = liftedCard.Stack == stack;

                if (canStack && !sameCard && !sameStack && !stack.IsCrafting)
                {
                    stack.BottomCard.SetHighlighted(true);
                    highlightedCards.Add(stack.BottomCard);
                }
            }
        }

        public void TurnOffHighlightedCards()
        {
            highlightedCards.ForEach(card => card.SetHighlighted(false));
            highlightedCards.Clear();
        }

        public void ResolveOverlaps()
        {
            int maxIterations = cardSettings != null ? cardSettings.MaxIterations : 10;
            var combatRects = CombatManager.Instance != null ? CombatManager.Instance.ActiveCombatRects : null;
            CardPhysicsSolver.ResolveOverlaps(stacks, combatRects, maxIterations);
        }

        public void ResolveOverlaps(CombatRect combatRect, CardStack stackToIgnore = null)
        {
            int maxIterations = cardSettings != null ? cardSettings.MaxIterations : 10;
            var combatRects = CombatManager.Instance != null ? CombatManager.Instance.ActiveCombatRects : null;
            CardPhysicsSolver.ResolveOverlaps(stacks, combatRects, maxIterations);
        }

        public void EnforceBoardLimits()
        {
            if (Board.Instance == null) return;

            bool anyMoved = false;
            foreach (var stack in stacks)
            {
                if (stack.IsLocked) continue;

                Vector3 currentPos = stack.TargetPosition;
                Vector3 validPos = Board.Instance.EnforcePlacementRules(currentPos, stack);

                if (Vector3.SqrMagnitude(currentPos - validPos) > 0.001f)
                {
                    stack.SetTargetPosition(validPos, instant: false);
                    anyMoved = true;
                }
            }

            if (anyMoved) ResolveOverlaps();
        }
    }
}
