using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Pure static resolver for unit ability effects during night combat.
    /// Called by CombatLane at defined hook points. No state, no MonoBehaviour.
    ///
    /// Hook call order per tick:
    ///   1. ApplyBattleStartAbilities   — once, at CombatLane construction
    ///   2. ApplyTickEffects            — every Tick(), before attacks
    ///   3. GetTarget                   — per attacker, respects Taunt
    ///   4. CheckEtherealEvasion        — per attack, after hit-chance roll
    ///   5. ComputeBonusDamage          — per attack, before TakeDamage
    ///   6. ResolveOnHitAbilities       — per attack, after TakeDamage
    ///   7. CheckDeathSurvival          — when HP hits 0, before death is processed
    ///   8. ResolveOnKillAbilities      — after confirmed kill
    ///   9. ResolveOnDeathAbilities     — after confirmed kill (death effects)
    /// </summary>
    public static class AbilityResolver
    {
        // ── Battle start ──────────────────────────────────────────────────────────

        public static void ApplyBattleStartAbilities(
            IReadOnlyList<CombatUnit> defenders,
            IReadOnlyList<CombatUnit> enemies)
        {
            ApplyBattleStartToSide(defenders);
            ApplyBattleStartToSide(enemies);
            RefreshGangUpAuras(enemies);
        }

        private static void ApplyBattleStartToSide(IReadOnlyList<CombatUnit> units)
        {
            foreach (var u in units)
            {
                if (u.HasAbility(UnitAbilityKeyword.Shield))
                    u.SetShield(u.GetAbilityValue(UnitAbilityKeyword.Shield));
            }
        }

        // ── Per-tick effects ──────────────────────────────────────────────────────

        /// <summary>
        /// Apply DoT, Repair, Healer, and refresh GangUp auras.
        /// Called once per Tick before attack processing.
        /// </summary>
        public static void ApplyTickEffects(
            IReadOnlyList<CombatUnit> defenders,
            IReadOnlyList<CombatUnit> enemies)
        {
            RefreshGangUpAuras(enemies);
            ApplyTickToSide(defenders, defenders);
            ApplyTickToSide(enemies, enemies);
        }

        private static void ApplyTickToSide(
            IReadOnlyList<CombatUnit> side,
            IReadOnlyList<CombatUnit> allies)
        {
            foreach (var u in side)
            {
                if (!u.IsAlive) continue;

                if (u.PoisonStacks > 0)
                    u.TakeDamage(u.PoisonStacks);

                if (u.HasAbility(UnitAbilityKeyword.Repair) && u.HPFraction < 0.5f)
                    u.HealDirect(u.GetAbilityValue(UnitAbilityKeyword.Repair));

                if (u.HasAbility(UnitAbilityKeyword.Healer))
                    HealMostWounded(u.GetAbilityValue(UnitAbilityKeyword.Healer), allies);
            }
        }

        private static void HealMostWounded(int amount, IReadOnlyList<CombatUnit> allies)
        {
            CombatUnit target = null;
            float lowestFraction = 1f;
            foreach (var a in allies)
            {
                if (a.IsAlive && a.HPFraction < lowestFraction)
                {
                    lowestFraction = a.HPFraction;
                    target = a;
                }
            }
            target?.HealDirect(amount);
        }

        private static void RefreshGangUpAuras(IReadOnlyList<CombatUnit> enemies)
        {
            int gangUpUnits = 0;
            int gangUpValue = 0;
            foreach (var e in enemies)
            {
                if (e.IsAlive && e.HasAbility(UnitAbilityKeyword.GangUp))
                {
                    gangUpUnits++;
                    // Use the highest Value among all GangUp units present
                    gangUpValue = Mathf.Max(gangUpValue, e.GetAbilityValue(UnitAbilityKeyword.GangUp));
                }
            }

            int auraBonus = gangUpUnits * gangUpValue;
            foreach (var e in enemies)
                e.SetAuraBonus(e.HasAbility(UnitAbilityKeyword.GangUp) ? auraBonus : 0);
        }

        // ── Targeting ─────────────────────────────────────────────────────────────

        /// <summary>Returns the target to attack, honouring Taunt.</summary>
        public static CombatUnit GetTarget(IReadOnlyList<CombatUnit> targets)
        {
            foreach (var t in targets)
                if (t.IsAlive && t.HasAbility(UnitAbilityKeyword.Taunt)) return t;

            foreach (var t in targets)
                if (t.IsAlive) return t;

            return null;
        }

        // ── Per-attack hooks ──────────────────────────────────────────────────────

        /// <summary>Returns true if the attack is negated by Ethereal (one-time).</summary>
        public static bool CheckEtherealEvasion(CombatUnit target)
        {
            if (!target.HasAbility(UnitAbilityKeyword.Ethereal) || target.EtherealUsed) return false;
            target.SetEtherealUsed();
            return true;
        }

        /// <summary>Returns extra damage to add for Executioner vs. low-HP targets.</summary>
        public static int ComputeBonusDamage(CombatUnit attacker, CombatUnit target, int baseDamage)
        {
            if (!attacker.HasAbility(UnitAbilityKeyword.Executioner)) return 0;
            if (target.HPFraction >= 0.25f) return 0;
            return Mathf.RoundToInt(baseDamage * attacker.GetAbilityValue(UnitAbilityKeyword.Executioner) / 100f);
        }

        /// <summary>Called after damage is applied to the target.</summary>
        public static void ResolveOnHitAbilities(CombatUnit attacker, CombatUnit target, int actualDamage)
        {
            if (actualDamage <= 0) return;
            if (attacker.HasAbility(UnitAbilityKeyword.Poison))
                target.AddPoisonStacks(attacker.GetAbilityValue(UnitAbilityKeyword.Poison));
        }

        // ── Death hooks ───────────────────────────────────────────────────────────

        /// <summary>
        /// Returns true if the unit survives (Resilient proc). Sets HP to 1 and marks used.
        /// </summary>
        public static bool CheckDeathSurvival(CombatUnit unit)
        {
            if (!unit.HasAbility(UnitAbilityKeyword.Resilient) || unit.ResilientUsed) return false;
            unit.SetHP(1);
            unit.SetResilientUsed();
            return true;
        }

        /// <summary>Called immediately after a confirmed kill. Handles Veteran.</summary>
        public static void ResolveOnKillAbilities(CombatUnit killer, CombatUnit killed)
        {
            if (killer == null || killer.Side == killed.Side) return;
            if (killer.HasAbility(UnitAbilityKeyword.Veteran))
                killer.AddPermanentAttackBonus(killer.GetAbilityValue(UnitAbilityKeyword.Veteran));
        }

        /// <summary>
        /// Called after a unit officially dies.
        /// deadSideAllies: units on the same team as the dead unit.
        /// deadSideFoes: opposing team (targets for Infect).
        /// </summary>
        public static void ResolveOnDeathAbilities(
            CombatUnit dead,
            IReadOnlyList<CombatUnit> deadSideAllies,
            IReadOnlyList<CombatUnit> deadSideFoes)
        {
            // Infect: spread poison to front foe
            if (dead.HasAbility(UnitAbilityKeyword.Infect))
            {
                var front = GetFrontAlive(deadSideFoes);
                front?.AddPoisonStacks(dead.GetAbilityValue(UnitAbilityKeyword.Infect));
            }

            // Rally: each alive ally with Rally boosts all alive allies
            foreach (var ally in deadSideAllies)
            {
                if (!ally.IsAlive || !ally.HasAbility(UnitAbilityKeyword.Rally)) continue;
                int bonus = ally.GetAbilityValue(UnitAbilityKeyword.Rally);
                foreach (var beneficiary in deadSideAllies)
                {
                    if (beneficiary.IsAlive)
                        beneficiary.AddPermanentAttackBonus(bonus);
                }
            }
        }

        // ── Internal helpers ──────────────────────────────────────────────────────

        private static CombatUnit GetFrontAlive(IReadOnlyList<CombatUnit> units)
        {
            foreach (var u in units)
                if (u.IsAlive) return u;
            return null;
        }
    }
}
