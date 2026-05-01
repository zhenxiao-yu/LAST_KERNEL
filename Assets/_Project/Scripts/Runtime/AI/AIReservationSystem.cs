// AIReservationSystem — Prevents two villagers from fighting over the same card or stack.
//
// When AIPlanner picks a card or stack as a job target it reserves it through this
// system.  Other villagers scanning for jobs see the reservation and skip those targets.
//
// Rules:
//   • A villager may only hold one reservation per card / stack.
//   • Re-reserving a card you already own just succeeds (idempotent).
//   • Reservations are released when a job completes, is cancelled, or the villager dies.
//   • ColonyAIManager.CleanupStaleReservations() sweeps locked / dead villagers each tick.
//
// This is a plain C# singleton — no MonoBehaviour, no scene dependency.
// Reset() is called by ColonyAIManager.OnDestroy to clear state between sessions.

using System.Collections.Generic;

namespace Markyu.LastKernel
{
    public class AIReservationSystem
    {
        // ── Singleton ─────────────────────────────────────────────────────────
        public static readonly AIReservationSystem Instance = new AIReservationSystem();
        private AIReservationSystem() { }

        // ── Storage ───────────────────────────────────────────────────────────
        // Maps each reserved card / stack to the CardAI that owns the reservation.
        private readonly Dictionary<CardInstance, CardAI> _cardOwners  = new();
        private readonly Dictionary<CardStack,    CardAI> _stackOwners = new();

        // ── Card reservations ─────────────────────────────────────────────────

        // Try to claim exclusive access to a card.
        // Returns true if the card is now reserved by `owner` (either newly or already was).
        public bool TryReserveCard(CardInstance card, CardAI owner)
        {
            if (card == null || owner == null) return false;
            if (_cardOwners.TryGetValue(card, out var existing))
                return existing == owner;   // already ours → still ok
            _cardOwners[card] = owner;
            return true;
        }

        public void ReleaseCard(CardInstance card, CardAI owner)
        {
            if (card == null) return;
            if (_cardOwners.TryGetValue(card, out var existing) && existing == owner)
                _cardOwners.Remove(card);
        }

        public bool IsCardReserved(CardInstance card) =>
            card != null && _cardOwners.ContainsKey(card);

        public bool IsCardReservedBy(CardInstance card, CardAI owner) =>
            card != null && _cardOwners.TryGetValue(card, out var existing) && existing == owner;

        // ── Stack reservations ────────────────────────────────────────────────

        // Reserves the destination stack so no other villager tries to join it simultaneously.
        public bool TryReserveStack(CardStack stack, CardAI owner)
        {
            if (stack == null || owner == null) return false;
            if (_stackOwners.TryGetValue(stack, out var existing))
                return existing == owner;
            _stackOwners[stack] = owner;
            return true;
        }

        public void ReleaseStack(CardStack stack, CardAI owner)
        {
            if (stack == null) return;
            if (_stackOwners.TryGetValue(stack, out var existing) && existing == owner)
                _stackOwners.Remove(stack);
        }

        public bool IsStackReserved(CardStack stack) =>
            stack != null && _stackOwners.ContainsKey(stack);

        // ── Bulk release ──────────────────────────────────────────────────────

        // Release every reservation held by a specific villager.
        // Call this when a job ends, is cancelled, or the villager is locked/dies.
        public void ReleaseAll(CardAI owner)
        {
            if (owner == null) return;

            // Collect keys first to avoid modifying the dictionary while iterating.
            var cardKeys = new List<CardInstance>();
            foreach (var kvp in _cardOwners)
                if (kvp.Value == owner) cardKeys.Add(kvp.Key);
            foreach (var k in cardKeys) _cardOwners.Remove(k);

            var stackKeys = new List<CardStack>();
            foreach (var kvp in _stackOwners)
                if (kvp.Value == owner) stackKeys.Add(kvp.Key);
            foreach (var k in stackKeys) _stackOwners.Remove(k);
        }

        // ── Debug ─────────────────────────────────────────────────────────────

        public int ReservedCardCount  => _cardOwners.Count;
        public int ReservedStackCount => _stackOwners.Count;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        // Clear all reservations.  Called by ColonyAIManager when the scene unloads.
        public void Reset()
        {
            _cardOwners.Clear();
            _stackOwners.Clear();
        }
    }
}
