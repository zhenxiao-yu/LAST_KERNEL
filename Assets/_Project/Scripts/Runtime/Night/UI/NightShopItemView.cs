using System;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// VisualElement helper for one item in the Night Shop panel.
    /// The controller creates one per active shop slot and adds it to nb-shop-items-row.
    ///
    /// States:
    ///   Available  — player can afford it and click to purchase
    ///   Selected   — item is pending a target click (requiresTarget == true)
    ///   Disabled   — player can't afford it, or battle has already started
    ///   Purchased  — bought; shown as greyed out (optionally hidden)
    /// </summary>
    public class NightShopItemView
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when this item is clicked and the player can afford it.</summary>
        public event Action<NightShopItemDefinition> OnClicked;

        // ── State ─────────────────────────────────────────────────────────────────
        public NightShopItemDefinition Definition { get; }
        public VisualElement           Root       { get; }
        public bool                    Purchased  { get; private set; }

        private bool _selected;

        // ── Child elements ────────────────────────────────────────────────────────
        private readonly Label _nameLabel;
        private readonly Label _descLabel;
        private readonly Label _costLabel;

        // ── Constructor ───────────────────────────────────────────────────────────

        public NightShopItemView(NightShopItemDefinition def)
        {
            Definition = def;

            Root = new VisualElement();
            Root.AddToClassList("nb-shop-item");

            // Use Definition.DisplayName if set, else fall back to SO.name for runtime items.
            string itemName = !string.IsNullOrEmpty(def.DisplayName) ? def.DisplayName : def.name;

            _nameLabel = new Label(itemName.ToUpper());
            _nameLabel.AddToClassList("nb-shop-item__name");

            string desc = !string.IsNullOrEmpty(def.Description) ? def.Description : EffectDescription(def);
            _descLabel = new Label(desc);
            _descLabel.AddToClassList("nb-shop-item__desc");

            _costLabel = new Label($"◈ {def.goldCost}");
            _costLabel.AddToClassList("nb-shop-item__cost");

            Root.Add(_nameLabel);
            Root.Add(_descLabel);
            Root.Add(_costLabel);

            Root.RegisterCallback<ClickEvent>(_ =>
            {
                if (!Purchased) OnClicked?.Invoke(Definition);
            });
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void SetSelected(bool selected)
        {
            _selected = selected;
            if (selected) Root.AddToClassList("nb-shop-item--selected");
            else          Root.RemoveFromClassList("nb-shop-item--selected");
        }

        public void SetAffordable(bool affordable)
        {
            if (Purchased) return;
            if (affordable) Root.RemoveFromClassList("nb-shop-item--disabled");
            else            Root.AddToClassList("nb-shop-item--disabled");
        }

        public void MarkPurchased()
        {
            Purchased = true;
            Root.AddToClassList("nb-shop-item--purchased");
            Root.RemoveFromClassList("nb-shop-item--selected");
            Root.RemoveFromClassList("nb-shop-item--disabled");
        }

        public void SetLocked(bool locked)
        {
            if (locked) Root.AddToClassList("nb-shop-item--disabled");
            else        SetAffordable(true);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string EffectDescription(NightShopItemDefinition d) =>
            d.effect switch
            {
                NightShopEffect.AddAttack    => $"+{d.effectValue} ATK",
                NightShopEffect.AddMaxHealth => $"+{d.effectValue} Max HP",
                NightShopEffect.FullHeal     => "Restore full HP",
                NightShopEffect.HireGuard    => $"Temp fighter {d.hireAttack}ATK/{d.hireHealth}HP",
                _                            => "—"
            };
    }
}
