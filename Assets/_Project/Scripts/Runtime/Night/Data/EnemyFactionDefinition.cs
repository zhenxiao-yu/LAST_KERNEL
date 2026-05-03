using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Identifies an enemy faction and carries display metadata + player-facing tactics hint.
    /// Assigned to EnemyDefinition assets — each enemy belongs to exactly one faction.
    ///
    /// Five shipped factions:
    ///   Scavengers (red)  — GangUp + Poison, swarm aggression
    ///   Machines   (blue) — Armor + Repair, durable attrition
    ///   Plagued    (green)— Poison + Infect + Resilient, DoT spread
    ///   Specters   (purple)— Ethereal, hard to pin down
    ///   Iron Cult  (gold) — Rally + Resilient, elite shock force
    /// </summary>
    [CreateAssetMenu(menuName = "LastKernel/Night/Enemy Faction", fileName = "Faction_")]
    public class EnemyFactionDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [SerializeField] private string factionName = "Unknown Faction";

        [BoxGroup("Identity")]
        [SerializeField] private Color factionColor = Color.white;

        [BoxGroup("Identity")]
        [SerializeField] private Sprite factionBadge;

        [BoxGroup("Lore")]
        [SerializeField, TextArea(2, 4)]
        private string factionDescription;

        [BoxGroup("Lore")]
        [Tooltip("One-line hint shown to the player about effective counter-builds.")]
        [SerializeField, TextArea(1, 3)]
        private string playerTacticHint;

        public string FactionName      => factionName;
        public Color  FactionColor     => factionColor;
        public Sprite FactionBadge     => factionBadge;
        public string FactionDescription => factionDescription;
        public string PlayerTacticHint => playerTacticHint;
    }
}
