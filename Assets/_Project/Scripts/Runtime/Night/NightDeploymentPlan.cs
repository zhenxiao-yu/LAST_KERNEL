using System.Collections.Generic;
using System.Linq;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Describes which colony defenders are committed to the night lane and in what order.
    /// Produced at dusk; consumed by NightPhaseManager to build CombatUnits.
    /// Does not mutate the board.
    /// </summary>
    public class NightDeploymentPlan
    {
        /// <summary>Defenders in lane order. Index 0 is the frontline unit.</summary>
        public IReadOnlyList<CardInstance> Defenders { get; }

        public NightDeploymentPlan(IEnumerable<CardInstance> defenders)
        {
            Defenders = new List<CardInstance>(defenders);
        }

        public bool IsEmpty => Defenders.Count == 0;

        /// <summary>
        /// Auto-selects all living Character cards from the provided list, sorted
        /// deterministically by instance ID so order is stable across frames.
        /// </summary>
        public static NightDeploymentPlan BuildAutomatic(IEnumerable<CardInstance> colonyCards)
        {
            var defenders = colonyCards
                .Where(c => c != null
                         && c.Definition != null
                         && c.Definition.Category == CardCategory.Character
                         && c.CurrentHealth > 0)
                .OrderBy(c => c.GetInstanceID())
                .ToList();

            return new NightDeploymentPlan(defenders);
        }
    }
}
