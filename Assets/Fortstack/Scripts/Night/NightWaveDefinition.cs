using System.Collections.Generic;
using UnityEngine;

namespace Markyu.FortStack
{
    [CreateAssetMenu(menuName = "LastKernel/Night Wave", fileName = "Wave_Night_")]
    public class NightWaveDefinition : ScriptableObject
    {
        [SerializeField] private string waveName = "Incursion Wave";

        [SerializeField, TextArea]
        private string flavorText;

        [SerializeField] private List<EnemyEntry> enemies = new();

        [Header("Run-State Consequences")]
        [SerializeField, Tooltip("Morale change on player victory.")]
        private int victoryMoraleDelta = 5;

        [SerializeField, Tooltip("Morale change on player defeat (use negative value).")]
        private int defeatMoraleDelta = -10;

        [SerializeField, Tooltip("Fatigue added per defender committed to the lane.")]
        private int fatigueCostPerDefender = 2;

        [SerializeField, Tooltip("Salvage earned per enemy killed.")]
        private int salvagePerKill = 1;

        public string WaveName => waveName;
        public string FlavorText => flavorText;
        public IReadOnlyList<EnemyEntry> Enemies => enemies;
        public int VictoryMoraleDelta => victoryMoraleDelta;
        public int DefeatMoraleDelta => defeatMoraleDelta;
        public int FatigueCostPerDefender => fatigueCostPerDefender;
        public int SalvagePerKill => salvagePerKill;

        public List<EnemyDefinition> BuildEnemyList()
        {
            var result = new List<EnemyDefinition>();
            foreach (var entry in enemies)
            {
                if (entry.Enemy == null) continue;
                for (int i = 0; i < entry.Count; i++)
                    result.Add(entry.Enemy);
            }
            return result;
        }
    }

    [System.Serializable]
    public class EnemyEntry
    {
        public EnemyDefinition Enemy;
        [Min(1)] public int Count = 1;
    }
}
