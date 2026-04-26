using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Deterministic combat math. CombatTask owns timing and animation; this class owns rules.
    /// </summary>
    public static class CombatRules
    {
        private const float MinHitChance = 0.05f;
        private const float MaxHitChance = 0.95f;

        public static HitResult ResolveAttack(
            CombatStats attackerStats,
            CombatType attackerType,
            CombatStats defenderStats,
            CombatType defenderType,
            float advantageMultiplier,
            float disadvantageMultiplier,
            float hitRoll,
            float criticalRoll)
        {
            if (attackerStats == null || defenderStats == null)
            {
                return new HitResult(HitType.Miss, 0, CombatTypeAdvantage.None);
            }

            float hitChance = GetHitChance(attackerStats.Accuracy.Value, defenderStats.Dodge.Value);
            if (hitRoll > hitChance)
            {
                return new HitResult(HitType.Miss, 0, CombatTypeAdvantage.None);
            }

            bool isCritical = criticalRoll <= Mathf.Clamp01(attackerStats.CriticalChance.Value / 100f);
            CombatTypeAdvantage advantage = GetAdvantage(attackerType, defenderType);

            int damage = CalculateDamage(
                attackerStats.Attack.Value,
                defenderStats.Defense.Value,
                advantage,
                advantageMultiplier,
                disadvantageMultiplier,
                isCritical,
                attackerStats.CriticalMultiplier.Value);

            return new HitResult(isCritical ? HitType.Critical : HitType.Normal, damage, advantage);
        }

        public static float GetHitChance(int accuracy, int dodge)
        {
            return Mathf.Clamp((accuracy - dodge) / 100f, MinHitChance, MaxHitChance);
        }

        public static int CalculateDamage(
            int attack,
            int defense,
            CombatTypeAdvantage advantage,
            float advantageMultiplier,
            float disadvantageMultiplier,
            bool isCritical,
            int criticalMultiplier)
        {
            int damage = Mathf.Max(1, attack - defense);

            if (advantage == CombatTypeAdvantage.Advantage)
            {
                damage = Mathf.RoundToInt(damage * advantageMultiplier);
            }
            else if (advantage == CombatTypeAdvantage.Disadvantage)
            {
                damage = Mathf.RoundToInt(damage * disadvantageMultiplier);
            }

            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * criticalMultiplier / 100f);
            }

            return Mathf.Max(1, damage);
        }

        public static CombatTypeAdvantage GetAdvantage(CombatType attackerType, CombatType defenderType)
        {
            if (attackerType == CombatType.None || defenderType == CombatType.None || attackerType == defenderType)
            {
                return CombatTypeAdvantage.None;
            }

            bool hasAdvantage =
                attackerType == CombatType.Melee && defenderType == CombatType.Ranged ||
                attackerType == CombatType.Ranged && defenderType == CombatType.Magic ||
                attackerType == CombatType.Magic && defenderType == CombatType.Melee;

            return hasAdvantage ? CombatTypeAdvantage.Advantage : CombatTypeAdvantage.Disadvantage;
        }
    }
}
