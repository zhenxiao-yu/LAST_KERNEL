namespace Markyu.LastKernel
{
    public class CombatStats
    {
        public Stat MaxHealth { get; private set; }
        public Stat Attack { get; private set; }
        public Stat Defense { get; private set; }
        public Stat AttackSpeed { get; private set; }
        public Stat Accuracy { get; private set; }
        public Stat Dodge { get; private set; }
        public Stat CriticalChance { get; private set; }
        public Stat CriticalMultiplier { get; private set; }

        public CombatStats(float maxHealth, float attack, float defense, float attackSpeed,
                           float accuracy, float dodge, float criticalChance, float criticalMultiplier)
        {
            MaxHealth = new Stat(maxHealth);
            Attack = new Stat(attack);
            Defense = new Stat(defense);
            AttackSpeed = new Stat(attackSpeed);
            Accuracy = new Stat(accuracy);
            Dodge = new Stat(dodge);
            CriticalChance = new Stat(criticalChance);
            CriticalMultiplier = new Stat(criticalMultiplier);
        }

        /// <summary>
        /// Generates a multiline string containing the current base value of every combat statistic,
        /// formatted for display in a UI tooltip or debug log.
        /// </summary>
        /// <returns>A string with each combat stat and its value on a new line.</returns>
        public string GetFormattedStats()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();

            sb.Append($"{GameLocalization.Get("stat.maxHealth")} ({MaxHealth.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.attack")} ({Attack.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.defense")} ({Defense.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.attackSpeed")} ({AttackSpeed.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.accuracy")} ({Accuracy.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.dodge")} ({Dodge.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.criticalChance")} ({CriticalChance.Value.ToString(GameLocalization.CurrentCulture)})\n");
            sb.Append($"{GameLocalization.Get("stat.criticalMultiplier")} ({CriticalMultiplier.Value.ToString(GameLocalization.CurrentCulture)})");

            return sb.ToString();
        }
    }
}

