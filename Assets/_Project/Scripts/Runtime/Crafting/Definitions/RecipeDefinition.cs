using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Recipe", fileName = "Recipe_")]
    public class RecipeDefinition : ScriptableObject
    {
        [System.Serializable]
        public struct Ingredient
        {
            [TableColumnWidth(180)]
            public CardDefinition card;
            [TableColumnWidth(50)]
            public int count;
            [TableColumnWidth(100)]
            public IngredientConsumption consumptionMode;
        }

        // ── Identity ──────────────────────────────────────────────────────────

        [BoxGroup("Identity")]
        [SerializeField, ReadOnly, Tooltip("Auto-generated unique ID.")]
        protected string id;

        [BoxGroup("Identity")]
        [SerializeField, Required]
        protected string displayName;

        [BoxGroup("Identity")]
        [SerializeField]
        protected RecipeCategory category;

        // ── Ingredients & Output ──────────────────────────────────────────────

        [BoxGroup("Recipe")]
        [SerializeField, TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        [ValidateInput("@requiredIngredients != null && requiredIngredients.Count > 0", "Recipe needs at least one ingredient.")]
        protected List<Ingredient> requiredIngredients;

        [BoxGroup("Recipe")]
        [SerializeField, InlineEditor(InlineEditorObjectFieldModes.Foldout)]
        protected CardDefinition resultingCard;

        // ── Behaviour ─────────────────────────────────────────────────────────

        [BoxGroup("Behaviour")]
        [SerializeField, Tooltip("Runs automatically and continuously (e.g. Recycler + Recruit).")]
        protected bool isContinuous = false;

        [BoxGroup("Behaviour")]
        [SerializeField, Tooltip("Matches even if the stack has more items than required.")]
        protected bool allowExcessIngredients = false;

        [BoxGroup("Behaviour")]
        [SerializeField, Min(0.1f)]
        protected float craftingDuration = 5f;

        [BoxGroup("Behaviour")]
        [SerializeField, Min(0f), Tooltip("Relative weight when multiple recipes match. Higher = more likely.")]
        protected float randomWeight = 1.0f;

        // ── Properties ────────────────────────────────────────────────────────

        public string Id => id;
        public RecipeCategory Category => category;
        public string DisplayName => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "recipe", "name"),
            displayName);
        public List<Ingredient> RequiredIngredients => requiredIngredients;
        public CardDefinition ResultingCard => resultingCard;
        public virtual bool RequiresResultingCard => true;
        public bool IsContinuous => isContinuous;
        public bool AllowExcessIngredients => allowExcessIngredients;
        public float CraftingDuration => craftingDuration;
        public float RandomWeight => randomWeight < 0 ? 0 : randomWeight;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");

            if (requiredIngredients == null)
                requiredIngredients = new List<Ingredient>();

            for (int i = 0; i < requiredIngredients.Count; i++)
            {
                var ingredient = requiredIngredients[i];
                ingredient.count = Mathf.Max(1, ingredient.count);
                requiredIngredients[i] = ingredient;
            }

            craftingDuration = Mathf.Max(0f, craftingDuration);
        }

        /// <summary>
        /// Core execution logic. Override in subclasses for specialized behavior.
        /// Base: Consume inputs → Spawn output.
        /// </summary>
        public virtual void Execute(CardStack stack)
        {
            if (stack == null || stack.TopCard == null)
            {
                Debug.LogWarning($"RecipeDefinition: Cannot execute '{DisplayName}' without a valid stack.", this);
                return;
            }

            var rules = GetIngredientRules();

            stack.TopCard.PlayPuffParticle();
            AudioManager.Instance?.PlaySFX(AudioId.Pop);

            ConsumeIngredients(stack, rules);

            if (resultingCard != null)
            {
                CardManager.Instance?.CreateCardInstance(
                    resultingCard,
                    stack.TargetPosition.Flatten(),
                    stack
                );

                CraftingManager.Instance?.NotifyCraftingFinished(resultingCard);
            }
        }

        public bool HasConsumableIngredients()
        {
            return requiredIngredients.Any(i =>
                i.consumptionMode == IngredientConsumption.Consume ||
                i.consumptionMode == IngredientConsumption.Destroy);
        }

        #region Subclass Helpers
        protected Dictionary<CardDefinition, Ingredient> GetIngredientRules()
        {
            return requiredIngredients
                .Where(i => i.card != null)
                .GroupBy(i => i.card)
                .ToDictionary(g => g.Key, g => g.First());
        }

        protected void ConsumeIngredients(CardStack stack, Dictionary<CardDefinition, Ingredient> rules)
        {
            var cardsToCheck = stack.Cards.ToList();

            var remainingNeeds = requiredIngredients
                .Where(i => i.card != null)
                .GroupBy(i => i.card)
                .ToDictionary(g => g.Key, g => g.Sum(i => i.count));

            foreach (var card in cardsToCheck)
            {
                if (rules.TryGetValue(card.BaseDefinition, out var rule))
                {
                    if (remainingNeeds[card.BaseDefinition] > 0)
                    {
                        ApplyConsumptionRule(card, rule.consumptionMode, stack);
                        remainingNeeds[card.BaseDefinition]--;
                    }
                }
            }
        }

        protected void ApplyConsumptionRule(CardInstance card, IngredientConsumption mode, CardStack stack)
        {
            switch (mode)
            {
                case IngredientConsumption.Keep:
                    break;
                case IngredientConsumption.Consume:
                    card.Use();
                    if (card.UsesLeft <= 0) stack.DestroyCard(card);
                    break;
                case IngredientConsumption.Destroy:
                    stack.DestroyCard(card);
                    break;
            }
        }
        #endregion
    }

    public enum IngredientConsumption
    {
        /// <summary>Default. Reduces uses count or durability. Destroys if empty.</summary>
        Consume = 0,

        /// <summary>Required but not modified (e.g., Nutrient Bed, Recruit).</summary>
        Keep = 1,

        /// <summary>Destroys the card instance immediately, ignoring durability/uses.</summary>
        Destroy = 2
    }

    public enum RecipeCategory
    {
        Misc,
        Gathering,
        Construction,
        Cooking,
        Forging,
        Refining,
        Husbandry
    }
}
