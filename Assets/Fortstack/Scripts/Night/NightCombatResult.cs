using System.Collections.Generic;

namespace Markyu.FortStack
{
    /// <summary>
    /// Immutable output of a completed night battle.
    /// Produced by CombatLane; consumed by NightPhaseManager for aftermath and DayCycleManager for run-state.
    /// Does not apply itself to any system.
    /// </summary>
    public class NightCombatResult
    {
        public bool PlayerWon { get; }

        public IReadOnlyList<CardInstance> DeadDefenders { get; }
        public IReadOnlyList<CardInstance> SurvivorDefenders { get; }

        public int TotalDefenders { get; }
        public int EnemiesKilled { get; }
        public int TotalEnemies { get; }

        public int MoraleDelta { get; }
        public int FatigueDelta { get; }
        public int SalvageDelta { get; }

        public NightCombatResult(
            bool playerWon,
            IReadOnlyList<CardInstance> deadDefenders,
            IReadOnlyList<CardInstance> survivorDefenders,
            int totalDefenders,
            int enemiesKilled,
            int totalEnemies,
            int moraleDelta,
            int fatigueDelta,
            int salvageDelta)
        {
            PlayerWon = playerWon;
            DeadDefenders = deadDefenders;
            SurvivorDefenders = survivorDefenders;
            TotalDefenders = totalDefenders;
            EnemiesKilled = enemiesKilled;
            TotalEnemies = totalEnemies;
            MoraleDelta = moraleDelta;
            FatigueDelta = fatigueDelta;
            SalvageDelta = salvageDelta;
        }

        public string GetSummaryText()
        {
            if (PlayerWon)
            {
                return $"Wave repelled. {EnemiesKilled}/{TotalEnemies} enemies destroyed.\n"
                     + (DeadDefenders.Count > 0
                         ? $"{DeadDefenders.Count} defender(s) lost."
                         : "No casualties.");
            }
            else
            {
                return $"Defenses breached. {DeadDefenders.Count}/{TotalDefenders} defender(s) lost.\n"
                     + $"{EnemiesKilled}/{TotalEnemies} enemies destroyed before breach.";
            }
        }
    }
}
