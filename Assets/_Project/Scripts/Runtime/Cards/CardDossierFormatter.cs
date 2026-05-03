using System.Collections.Generic;
using System.Text;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Shared card dossier copy and stat formatting for reward cards, hover panels,
    /// and any future card inspection UI.
    /// </summary>
    public static class CardDossierFormatter
    {
        public static string CategoryLabel(CardCategory category) =>
            GameLocalization.GetOptional($"card.category.{category.ToString().ToLowerInvariant()}", category.ToString());

        public static string CombatTypeLabel(CombatType combatType) =>
            GameLocalization.GetOptional($"card.combatType.{combatType.ToString().ToLowerInvariant()}", combatType.ToString());

        public static string BuildLore(CardDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            string description = definition.Description;
            string fallback = GameLocalization.GetOptional(
                $"card.lore.{definition.Category.ToString().ToLowerInvariant()}",
                GameLocalization.GetOptional("card.lore.default", string.Empty));

            if (string.IsNullOrWhiteSpace(description))
                return fallback;

            if (string.IsNullOrWhiteSpace(fallback))
                return description;

            return description.TrimEnd() + "\n\n" + fallback;
        }

        public static string BuildVisibleStats(CardDefinition definition, CombatStats stats = null, int currentHP = -1)
        {
            if (definition == null)
                return string.Empty;

            stats ??= definition.CreateCombatStats();
            var lines = new List<string>();

            if (definition.CombatType != CombatType.None)
            {
                lines.Add(Line("stat.maxHealth", currentHP >= 0
                    ? $"{currentHP}/{stats.MaxHealth.Value.ToString(GameLocalization.CurrentCulture)}"
                    : stats.MaxHealth.Value.ToString(GameLocalization.CurrentCulture)));
                lines.Add(Line("stat.attack", stats.Attack.Value.ToString(GameLocalization.CurrentCulture)));
                lines.Add(Line("stat.defense", stats.Defense.Value.ToString(GameLocalization.CurrentCulture)));
            }

            if (definition.Nutrition > 0)
                lines.Add(GameLocalization.Format("card.nutritionValue", definition.Nutrition));

            if (definition.IsSellable && definition.SellPrice > 0)
                lines.Add(GameLocalization.Format("card.sell", definition.SellPrice));

            return string.Join("\n", lines);
        }

        public static string BuildHiddenStats(CardDefinition definition, CombatStats stats = null)
        {
            if (definition == null || definition.CombatType == CombatType.None)
                return string.Empty;

            stats ??= definition.CreateCombatStats();
            var sb = new StringBuilder();
            sb.AppendLine(GameLocalization.Format("card.hidden.combatType", CombatTypeLabel(definition.CombatType)));
            sb.AppendLine(Line("stat.attackSpeed", stats.AttackSpeed.Value.ToString(GameLocalization.CurrentCulture)));
            sb.AppendLine(Line("stat.accuracy", stats.Accuracy.Value.ToString(GameLocalization.CurrentCulture)));
            sb.AppendLine(Line("stat.dodge", stats.Dodge.Value.ToString(GameLocalization.CurrentCulture)));
            sb.AppendLine(Line("stat.criticalChance", stats.CriticalChance.Value.ToString(GameLocalization.CurrentCulture)));
            sb.Append(Line("stat.criticalMultiplier", stats.CriticalMultiplier.Value.ToString(GameLocalization.CurrentCulture)));
            return sb.ToString();
        }

        public static string BuildEconomy(CardDefinition definition, int currentNutrition = -1, int usesLeft = 0)
        {
            if (definition == null)
                return string.Empty;

            var lines = new List<string>();
            if (definition.IsSellable && definition.SellPrice > 0)
                lines.Add(GameLocalization.Format("card.sell", definition.SellPrice));

            int nutrition = currentNutrition >= 0 ? currentNutrition : definition.Nutrition;
            if (nutrition > 0)
                lines.Add(GameLocalization.Format("card.nutritionValue", nutrition));

            if (usesLeft > 0)
                lines.Add(GameLocalization.Format("card.usesLeft", usesLeft));

            return string.Join("\n", lines);
        }

        public static string BuildRewardPod(CardDefinition definition)
        {
            if (definition == null)
                return string.Empty;

            CombatStats stats = definition.CreateCombatStats();
            return definition.CombatType != CombatType.None
                ? GameLocalization.Format(
                    "night.modal.reward.stats.combat",
                    stats.Attack.Value,
                    stats.MaxHealth.Value,
                    stats.Defense.Value)
                : GameLocalization.Format(
                    "night.modal.reward.stats.utility",
                    CategoryLabel(definition.Category),
                    definition.SellPrice);
        }

        private static string Line(string labelKey, string value) =>
            GameLocalization.Format("card.statLine", GameLocalization.Get(labelKey), value);
    }
}
