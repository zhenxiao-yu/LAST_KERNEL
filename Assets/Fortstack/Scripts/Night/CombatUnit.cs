using UnityEngine;

namespace Markyu.FortStack
{
    public enum CombatUnitSide { Defender, Enemy }

    /// <summary>
    /// Runtime combatant for the night lane. Plain C# — no MonoBehaviour.
    /// Owns combat stats copied at spawn time; holds a SourceCard reference only for defenders.
    /// </summary>
    public class CombatUnit
    {
        public CombatUnitSide Side { get; }
        public string DisplayName { get; }

        public int MaxHP { get; }
        public int CurrentHP { get; private set; }
        public int Attack { get; }
        public int Defense { get; }

        // AttackCooldown in seconds: derived from attackSpeed% (100 = 1 attack/sec).
        public float AttackCooldown { get; }
        public float AttackTimer { get; set; }

        public float AccuracyPercent { get; }
        public float DodgePercent { get; }
        public float CritChancePercent { get; }
        public float CritMultiplier { get; }

        public bool IsAlive => CurrentHP > 0;

        /// <summary>Only set for defender-side units. Null for enemies.</summary>
        public CardInstance SourceCard { get; }

        public CombatUnit(
            CombatUnitSide side,
            string displayName,
            int maxHP,
            int attack,
            int defense,
            int attackSpeedPercent,
            float accuracy,
            float dodge,
            float critChance,
            float critMultiplier,
            CardInstance sourceCard = null)
        {
            Side = side;
            DisplayName = displayName;
            MaxHP = Mathf.Max(1, maxHP);
            CurrentHP = MaxHP;
            Attack = Mathf.Max(0, attack);
            Defense = Mathf.Max(0, defense);
            AttackCooldown = attackSpeedPercent > 0 ? 100f / attackSpeedPercent : 2f;
            AttackTimer = 0f;
            AccuracyPercent = Mathf.Clamp(accuracy, 0f, 100f);
            DodgePercent = Mathf.Clamp(dodge, 0f, 100f);
            CritChancePercent = Mathf.Clamp(critChance, 0f, 100f);
            CritMultiplier = Mathf.Max(1f, critMultiplier);
            SourceCard = sourceCard;
        }

        public void TakeDamage(int amount)
        {
            CurrentHP = Mathf.Max(0, CurrentHP - Mathf.Max(0, amount));
        }

        public float HPFraction => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;

        public static CombatUnit FromCardInstance(CardInstance card)
        {
            var stats = card.Stats;
            return new CombatUnit(
                CombatUnitSide.Defender,
                card.Definition.DisplayName,
                card.CurrentHealth,
                stats.Attack.Value,
                stats.Defense.Value,
                stats.AttackSpeed.Value,
                stats.Accuracy.Value,
                stats.Dodge.Value,
                stats.CriticalChance.Value,
                stats.CriticalMultiplier.Value,
                card
            );
        }

        public static CombatUnit FromEnemyDefinition(EnemyDefinition def)
        {
            return new CombatUnit(
                CombatUnitSide.Enemy,
                def.DisplayName,
                def.MaxHP,
                def.Attack,
                def.Defense,
                def.AttackSpeed,
                def.Accuracy,
                def.Dodge,
                def.CritChance,
                def.CritMultiplier
            );
        }
    }
}
