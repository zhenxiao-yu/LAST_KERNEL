using System.Collections.Generic;
using System.Linq;

namespace Markyu.LastKernel
{
    /// <summary>
    /// The player's ordered battle lineup (up to 5 slots).
    /// Index 0 = FRONT (attacks first, takes first damage).
    /// Managed by NightBattleModalController during prep; converted to CombatUnits when battle starts.
    /// </summary>
    public class NightTeam
    {
        public const int MaxSlots = 5;

        private readonly NightFighter[] _slots = new NightFighter[MaxSlots];

        // ── Read access ───────────────────────────────────────────────────────────

        public NightFighter GetSlot(int index) =>
            index >= 0 && index < MaxSlots ? _slots[index] : null;

        public bool IsSlotEmpty(int index) => GetSlot(index) == null;

        public int FilledSlotCount => _slots.Count(f => f != null);

        public bool Contains(NightFighter fighter) =>
            fighter != null && _slots.Any(f => f != null && f.Id == fighter.Id);

        public int SlotOf(NightFighter fighter)
        {
            for (int i = 0; i < MaxSlots; i++)
                if (_slots[i] != null && _slots[i].Id == fighter.Id) return i;
            return -1;
        }

        // ── Mutation ──────────────────────────────────────────────────────────────

        /// <summary>
        /// Assign a fighter to a slot. If the slot is occupied the existing fighter is
        /// evicted (returned to available pool — the controller handles the UI side).
        /// If the fighter is already in another slot that slot is cleared first.
        /// Returns the evicted fighter (or null if the slot was empty).
        /// </summary>
        public NightFighter Assign(int slotIndex, NightFighter fighter)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots || fighter == null) return null;

            // Remove the fighter from its current slot, if any.
            int existingSlot = SlotOf(fighter);
            if (existingSlot >= 0) _slots[existingSlot] = null;

            var evicted = _slots[slotIndex];
            _slots[slotIndex] = fighter;
            return evicted;
        }

        /// <summary>Clear a slot and return the fighter that was there (or null).</summary>
        public NightFighter Clear(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= MaxSlots) return null;
            var removed = _slots[slotIndex];
            _slots[slotIndex] = null;
            return removed;
        }

        public void ClearAll()
        {
            for (int i = 0; i < MaxSlots; i++) _slots[i] = null;
        }

        // ── First available slot ──────────────────────────────────────────────────

        /// <summary>Index of the first empty slot, or -1 if all slots are full.</summary>
        public int FirstEmptySlot()
        {
            for (int i = 0; i < MaxSlots; i++)
                if (_slots[i] == null) return i;
            return -1;
        }

        // ── Simulation build ──────────────────────────────────────────────────────

        /// <summary>
        /// Converts filled slots into CombatUnits. Slot 0 → first unit in list (front of lane).
        /// Empty slots are skipped so the lane compresses naturally.
        /// </summary>
        public List<CombatUnit> BuildCombatUnits()
        {
            var units = new List<CombatUnit>(MaxSlots);
            foreach (var fighter in _slots)
            {
                if (fighter != null)
                    units.Add(fighter.ToCombatUnit());
            }
            return units;
        }
    }
}
