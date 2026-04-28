// DefenseLoadoutController — Bridge between the card system and the defense loadout.
//
// Full card integration is a future milestone. This controller provides:
//   a) A clean seam that NightBattlefieldController calls (via ResolveLoadout)
//   b) A fall-through to defenderOverrides for playtesting without card setup
//
// Future integration: read CardInstances assigned to defense slots during the day phase,
// look up DefenderData via CardDefinition.DefenderDataOverride or a mapping table,
// and return the resolved array from ResolveLoadout().

using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Resolves which DefenderData fills each defender slot for the upcoming night.
    /// Attach to the same GameObject as <see cref="NightBattlefieldController"/>.
    /// </summary>
    public class DefenseLoadoutController : MonoBehaviour
    {
        [BoxGroup("Fallback Loadout")]
        [InfoBox("Used when card integration is not yet active.")]
        [SerializeField, Tooltip("One entry per defender slot. Null = empty slot.")]
        private DefenderData[] defenderOverrides;

        /// <summary>
        /// Returns an array of DefenderData sized to <paramref name="slotCount"/>.
        /// Each index maps to the same-indexed defender slot.
        /// Returns null entries for empty slots.
        /// </summary>
        public DefenderData[] ResolveLoadout(int slotCount)
        {
            // Card integration not yet active — fall through to manual overrides.
            var result = new DefenderData[slotCount];

            if (defenderOverrides == null) return result;

            for (int i = 0; i < slotCount && i < defenderOverrides.Length; i++)
                result[i] = defenderOverrides[i];

            return result;
        }
    }
}
