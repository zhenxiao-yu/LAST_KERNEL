using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Prep-phase data model for one unit in the player's battle lineup.
    /// Created from a CardInstance or synthesised for shop-hired temporary fighters.
    /// Converted to CombatUnit when the player clicks "Start Battle".
    ///
    /// Why separate from CombatUnit?
    ///   CombatUnit is a pure simulation object (no CardInstance reference for enemies,
    ///   immutable stats after creation). NightFighter is mutable during prep — the player
    ///   can apply shop items that modify BonusAttack / BonusMaxHealth before the lane starts.
    /// </summary>
    public class NightFighter
    {
        /// <summary>Stable ID — GUID from source card or generated for temporaries.</summary>
        public readonly string Id;
        public string DisplayName { get; private set; }

        /// <summary>Null for shop-hired temporary fighters.</summary>
        public readonly CardInstance SourceCard;
        public readonly bool IsTemporary;

        // ── Base stats (copied from card at prep start, immutable) ────────────────
        public int BaseAttack     { get; private set; }
        public int BaseDefense    { get; private set; }
        public int BaseMaxHealth  { get; private set; }
        public int BaseHealth     { get; private set; }
        public int BaseAttackSpeed { get; private set; }
        public float BaseAccuracy { get; private set; }
        public float BaseDodge    { get; private set; }
        public float BaseCritChance     { get; private set; }
        public float BaseCritMultiplier { get; private set; }

        // ── Shop bonuses (additive, applied during prep) ──────────────────────────
        public int BonusAttack     { get; private set; }
        public int BonusMaxHealth  { get; private set; }
        public bool FullHealOnStart { get; private set; }

        // ── Computed final stats ──────────────────────────────────────────────────
        public int FinalAttack    => BaseAttack + BonusAttack;
        public int FinalMaxHealth => BaseMaxHealth + BonusMaxHealth;
        public int FinalHealth    => FullHealOnStart
            ? FinalMaxHealth
            : Mathf.Clamp(BaseHealth + BonusMaxHealth, 1, FinalMaxHealth);

        // ── Factories ─────────────────────────────────────────────────────────────

        /// <summary>Create from an existing colony card.</summary>
        public static NightFighter FromCard(CardInstance card)
        {
            var stats = card.Stats;
            return new NightFighter(
                id:            $"{card.Definition.Id}:{card.GetInstanceID()}",
                displayName:   card.Definition.DisplayName,
                sourceCard:    card,
                isTemporary:   false,
                attack:        stats.Attack.Value,
                defense:       stats.Defense.Value,
                maxHealth:     card.CurrentHealth,   // effective max = current HP at prep start
                health:        card.CurrentHealth,
                attackSpeed:   stats.AttackSpeed.Value,
                accuracy:      stats.Accuracy.Value,
                dodge:         stats.Dodge.Value,
                critChance:    stats.CriticalChance.Value,
                critMultiplier: stats.CriticalMultiplier.Value
            );
        }

        /// <summary>Create a shop-hired temporary fighter (no colony card backing).</summary>
        public static NightFighter Temporary(string displayName, int attack, int health,
                                             int defense = 0, int attackSpeed = 100)
        {
            return new NightFighter(
                id:            System.Guid.NewGuid().ToString(),
                displayName:   displayName,
                sourceCard:    null,
                isTemporary:   true,
                attack:        attack,
                defense:       defense,
                maxHealth:     health,
                health:        health,
                attackSpeed:   attackSpeed,
                accuracy:      80f,
                dodge:         10f,
                critChance:    5f,
                critMultiplier: 150f
            );
        }

        // ── Shop modification (called by NightBattleModalController) ──────────────

        public void AddAttackBonus(int amount)    => BonusAttack    += amount;
        public void AddMaxHealthBonus(int amount) => BonusMaxHealth += amount;
        public void RequestFullHeal()             => FullHealOnStart = true;

        // ── Simulation conversion ─────────────────────────────────────────────────

        /// <summary>Snapshot this fighter's final stats into a CombatUnit for the lane simulation.</summary>
        public CombatUnit ToCombatUnit()
        {
            return new CombatUnit(
                side:             CombatUnitSide.Defender,
                displayName:      DisplayName,
                maxHP:            FinalMaxHealth,
                attack:           FinalAttack,
                defense:          BaseDefense,
                attackSpeedPercent: BaseAttackSpeed,
                accuracy:         BaseAccuracy,
                dodge:            BaseDodge,
                critChance:       BaseCritChance,
                critMultiplier:   BaseCritMultiplier,
                sourceCard:       SourceCard
            );
        }

        // ── Private constructor ───────────────────────────────────────────────────

        private NightFighter(
            string id, string displayName, CardInstance sourceCard, bool isTemporary,
            int attack, int defense, int maxHealth, int health, int attackSpeed,
            float accuracy, float dodge, float critChance, float critMultiplier)
        {
            Id              = id;
            DisplayName     = displayName;
            SourceCard      = sourceCard;
            IsTemporary     = isTemporary;
            BaseAttack      = Mathf.Max(0, attack);
            BaseDefense     = Mathf.Max(0, defense);
            BaseMaxHealth   = Mathf.Max(1, maxHealth);
            BaseHealth      = Mathf.Clamp(health, 1, BaseMaxHealth);
            BaseAttackSpeed = Mathf.Max(1, attackSpeed);
            BaseAccuracy    = Mathf.Clamp(accuracy, 0f, 100f);
            BaseDodge       = Mathf.Clamp(dodge, 0f, 100f);
            BaseCritChance  = Mathf.Clamp(critChance, 0f, 100f);
            BaseCritMultiplier = Mathf.Max(100f, critMultiplier);
        }
    }
}
