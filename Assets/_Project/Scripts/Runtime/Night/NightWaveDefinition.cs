using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "LastKernel/Night Wave", fileName = "Wave_Night_")]
    public class NightWaveDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [SerializeField] private string waveName = "Incursion Wave";

        [BoxGroup("Identity")]
        [SerializeField, TextArea]
        private string flavorText;

        [BoxGroup("Enemies")]
        [TableList(AlwaysExpanded = true)]
        [SerializeField] private List<EnemyEntry> enemies = new();

        [BoxGroup("Run-State Consequences")]
        [SerializeField, Tooltip("Morale change on player victory.")]
        private int victoryMoraleDelta = 7;

        [BoxGroup("Run-State Consequences")]
        [SerializeField, Tooltip("Morale change on player defeat (use negative value).")]
        private int defeatMoraleDelta = -7;

        [BoxGroup("Run-State Consequences")]
        [SerializeField, Tooltip("Fatigue added per defender committed to the lane.")]
        private int fatigueCostPerDefender = 2;

        [BoxGroup("Run-State Consequences")]
        [SerializeField, Tooltip("Salvage earned per enemy killed.")]
        private int salvagePerKill = 1;

        public string WaveName => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "night.wave", "name"),
            waveName);

        public string FlavorText => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "night.wave", "flavor"),
            flavorText);
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

        /// <summary>
        /// Builds the enemy list with stats scaled for the given day number.
        /// Day 1 returns base stats; higher days apply EnemyDefinition.ScaledForDay().
        /// </summary>
        public List<EnemyDefinition> BuildEnemyListScaled(int day)
        {
            var result = new List<EnemyDefinition>();
            foreach (var entry in enemies)
            {
                if (entry.Enemy == null) continue;
                var scaled = entry.Enemy.ScaledForDay(day);
                for (int i = 0; i < entry.Count; i++)
                    result.Add(scaled);
            }
            return result;
        }

        public static NightWaveDefinition CreateRuntime(
            string name, string flavor, List<EnemyEntry> entries,
            int victoryMorale = 7, int defeatMorale = -7,
            int fatigue = 1, int salvage = 1)
        {
            var wave = ScriptableObject.CreateInstance<NightWaveDefinition>();
            wave.waveName               = name;
            wave.flavorText             = flavor;
            wave.enemies                = entries;
            wave.victoryMoraleDelta     = victoryMorale;
            wave.defeatMoraleDelta      = defeatMorale;
            wave.fatigueCostPerDefender = fatigue;
            wave.salvagePerKill         = salvage;
            return wave;
        }
    }

    [System.Serializable]
    public class EnemyEntry
    {
        [TableColumnWidth(200)]
        public EnemyDefinition Enemy;
        [TableColumnWidth(60), Min(1)]
        public int Count = 1;
    }
}
