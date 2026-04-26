// Push-notification contract for internal game objects that inform CardManager
// about state changes they own.
//
// Separated from ICardService so the public service interface does not expose
// mutation methods that only make sense for tightly-coupled internal callers.
//
// Intended callers:
//   CardInstance      — NotifyCardKilled  (on Kill())
//   CardEquipper      — NotifyCardEquipped (on Equip())
//   CraftingManager   — NotifyStatsChanged (after a craft completes)
//
// Do NOT use from UI, Editor tooling, or test code.

namespace Markyu.LastKernel
{
    internal interface ICardEventSink
    {
        void NotifyCardKilled(CardInstance card);
        void NotifyCardEquipped(CardDefinition card);
        void NotifyStatsChanged();
    }
}
