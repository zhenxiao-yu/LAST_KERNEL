using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    public enum CombatUnitSide { Defender, Enemy }

    /// <summary>
    /// Runtime combatant for the night lane. Plain C# — no MonoBehaviour.
    /// Owns combat stats copied at spawn time; supports the ability system via
    /// AbilityResolver hooks in CombatLane.
    /// </summary>
    public class CombatUnit
    {
        public CombatUnitSide Side        { get; }
        public string         DisplayName { get; }

        public int MaxHP     { get; }
        public int CurrentHP { get; private set; }

        // Base attack is the stat at construction time.
        // Effective attack adds permanent bonuses (Veteran, Rally) and aura bonuses (GangUp).
        public int BaseAttack { get; }
        public int Attack     => BaseAttack + _permanentAttackBonus + _auraAttackBonus;

        public int Defense { get; }

        public float AttackCooldown { get; }
        public float AttackTimer    { get; set; }

        // While below 50% HP and Berserker is active, effective cooldown is shorter.
        public float EffectiveAttackCooldown
        {
            get
            {
                if (HPFraction >= 0.5f || !HasAbility(UnitAbilityKeyword.Berserker))
                    return AttackCooldown;
                float speedMult = 1f + GetAbilityValue(UnitAbilityKeyword.Berserker) / 100f;
                return AttackCooldown / speedMult;
            }
        }

        public float AccuracyPercent    { get; }
        public float DodgePercent       { get; }
        public float CritChancePercent  { get; }
        public float CritMultiplier     { get; }

        public bool IsAlive        => CurrentHP > 0;
        public float HPFraction    => MaxHP > 0 ? (float)CurrentHP / MaxHP : 0f;

        // One-time flags reset each battle (set at construction, never reset during a fight).
        public bool ResilientUsed  { get; private set; }
        public bool EtherealUsed   { get; private set; }

        // Set true once this unit's death has been fully processed, to prevent double-firing.
        public bool DeathProcessed { get; private set; }

        public int ShieldHP      => _shieldHP;
        public int PoisonStacks  => _poisonStacks;

        public IReadOnlyList<UnitAbilityDefinition> Abilities { get; }

        /// <summary>Only set for defender-side units. Null for enemies.</summary>
        public CardInstance SourceCard { get; }

        // ── Private state ─────────────────────────────────────────────────────────

        private int _permanentAttackBonus; // Veteran kills, Rally procs
        private int _auraAttackBonus;      // GangUp — overwritten each tick
        private int _shieldHP;             // absorbs damage before HP
        private int _poisonStacks;         // each stack = 1 damage per tick

        // ── Constructor ───────────────────────────────────────────────────────────

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
            CardInstance sourceCard = null,
            int startingHP = 0,
            IReadOnlyList<UnitAbilityDefinition> abilities = null)
        {
            Side          = side;
            DisplayName   = displayName;
            MaxHP         = Mathf.Max(1, maxHP);
            CurrentHP     = startingHP > 0 ? Mathf.Clamp(startingHP, 1, MaxHP) : MaxHP;
            BaseAttack    = Mathf.Max(0, attack);
            Defense       = Mathf.Max(0, defense);
            AttackCooldown = attackSpeedPercent > 0 ? 100f / attackSpeedPercent : 2f;
            AttackTimer   = AttackCooldown * 0.5f;
            AccuracyPercent   = Mathf.Clamp(accuracy, 0f, 100f);
            DodgePercent      = Mathf.Clamp(dodge, 0f, 100f);
            CritChancePercent = Mathf.Clamp(critChance, 0f, 100f);
            CritMultiplier    = Mathf.Max(1f, critMultiplier);
            SourceCard    = sourceCard;
            Abilities     = abilities ?? System.Array.Empty<UnitAbilityDefinition>();
        }

        // ── Ability helpers ───────────────────────────────────────────────────────

        public bool HasAbility(UnitAbilityKeyword kw)
        {
            foreach (var a in Abilities)
                if (a != null && a.Keyword == kw) return true;
            return false;
        }

        public int GetAbilityValue(UnitAbilityKeyword kw)
        {
            foreach (var a in Abilities)
                if (a != null && a.Keyword == kw) return a.Value;
            return 0;
        }

        // ── Stat mutators (called by AbilityResolver) ─────────────────────────────

        /// <summary>
        /// Apply raw damage. Armor reduces first, then shield absorbs, then HP drains.
        /// Returns actual HP damage dealt (after all reductions).
        /// </summary>
        public int TakeDamage(int rawAmount)
        {
            if (rawAmount <= 0) return 0;

            int armorReduction = HasAbility(UnitAbilityKeyword.Armor)
                ? GetAbilityValue(UnitAbilityKeyword.Armor) : 0;

            int afterArmor = Mathf.Max(0, rawAmount - armorReduction);
            if (afterArmor == 0) return 0;

            if (_shieldHP > 0)
            {
                int absorbed = Mathf.Min(_shieldHP, afterArmor);
                _shieldHP    -= absorbed;
                afterArmor   -= absorbed;
            }

            CurrentHP = Mathf.Max(0, CurrentHP - afterArmor);
            return afterArmor;
        }

        public void HealDirect(int amount)
        {
            if (amount > 0)
                CurrentHP = Mathf.Min(MaxHP, CurrentHP + amount);
        }

        public void SetHP(int value) =>
            CurrentHP = Mathf.Clamp(value, 0, MaxHP);

        public void SetShield(int amount) =>
            _shieldHP = Mathf.Max(0, amount);

        public void AddPoisonStacks(int stacks)
        {
            if (stacks > 0) _poisonStacks += stacks;
        }

        public void AddPermanentAttackBonus(int amount)
        {
            if (amount > 0) _permanentAttackBonus += amount;
        }

        public void SetAuraBonus(int bonus) =>
            _auraAttackBonus = Mathf.Max(0, bonus);

        public void SetResilientUsed() => ResilientUsed = true;
        public void SetEtherealUsed()  => EtherealUsed  = true;
        public void MarkDeathProcessed() => DeathProcessed = true;

        // ── Factory methods ───────────────────────────────────────────────────────

        public static CombatUnit FromCardInstance(CardInstance card)
        {
            var stats = card.Stats;
            return new CombatUnit(
                CombatUnitSide.Defender,
                card.Definition.DisplayName,
                stats.MaxHealth.Value,
                stats.Attack.Value,
                stats.Defense.Value,
                stats.AttackSpeed.Value,
                stats.Accuracy.Value,
                stats.Dodge.Value,
                stats.CriticalChance.Value,
                stats.CriticalMultiplier.Value,
                card,
                card.CurrentHealth
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
                def.CritMultiplier,
                abilities: def.Abilities
            );
        }
    }
}
