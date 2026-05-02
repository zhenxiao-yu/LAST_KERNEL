using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// VisualElement helper for one player battle slot in the Night Prep modal.
    /// The controller creates MaxSlots=5 of these and adds them to the player-team-row container.
    ///
    /// States:
    ///   Empty     — shows slot label ("FRONT" / "#2"…), dim styling
    ///   Filled    — shows fighter name, ATK, HP stats, HP bar
    ///   Targeted  — highlighted when a fighter or shop item is pending assignment to this slot
    ///   Locked    — battle in progress; click does nothing
    /// </summary>
    public class NightPrepSlotView
    {
        // ── Events ────────────────────────────────────────────────────────────────
        /// <summary>Fired when this slot is clicked. Controller handles assignment logic.</summary>
        public event Action<int> OnClicked;

        // ── State ─────────────────────────────────────────────────────────────────
        public int          SlotIndex       { get; }
        public bool         IsFront         { get; }
        public NightFighter AssignedFighter { get; private set; }
        public VisualElement Root           { get; }

        private bool _locked;

        // ── Child elements ────────────────────────────────────────────────────────
        private readonly Label         _slotLabel;   // "FRONT" / "#2" when empty
        private readonly VisualElement _icon;        // card art thumbnail
        private readonly Label         _nameLabel;
        private readonly Label         _statsLabel;
        private readonly VisualElement _hpBar;
        private readonly VisualElement _hpFill;
        private readonly Label         _hpText;

        // ── Constructor ───────────────────────────────────────────────────────────

        public NightPrepSlotView(int slotIndex)
        {
            SlotIndex = slotIndex;
            IsFront   = slotIndex == 0;

            // Root card
            Root = new VisualElement();
            Root.AddToClassList("nb-prep-slot");
            if (IsFront) Root.AddToClassList("nb-prep-slot--front");

            Root.RegisterCallback<ClickEvent>(_ => { if (!_locked) OnClicked?.Invoke(SlotIndex); });

            // Slot position label (shown when empty)
            string slotText = IsFront
                ? GameLocalization.Get("night.modal.slot.front")
                : GameLocalization.Format("night.modal.slot.n", slotIndex + 1);
            _slotLabel = new Label(slotText);
            _slotLabel.AddToClassList("nb-prep-slot__label");
            Root.Add(_slotLabel);

            // Card art icon (hidden when empty, shown when a fighter is assigned)
            _icon = new VisualElement();
            _icon.AddToClassList("nb-prep-slot__icon");
            _icon.AddToClassList("lk-hidden");
            Root.Add(_icon);

            // Fighter content (hidden when empty)
            _nameLabel = new Label();
            _nameLabel.AddToClassList("nb-prep-slot__name");
            Root.Add(_nameLabel);

            _statsLabel = new Label();
            _statsLabel.AddToClassList("nb-prep-slot__stats");
            Root.Add(_statsLabel);

            _hpBar = new VisualElement();
            _hpBar.AddToClassList("nb-hp-bar");
            _hpBar.style.overflow = Overflow.Hidden;

            _hpFill = new VisualElement();
            _hpFill.AddToClassList("nb-hp-fill");
            _hpBar.Add(_hpFill);
            Root.Add(_hpBar);

            _hpText = new Label();
            _hpText.AddToClassList("nb-fighter-hp-text");
            Root.Add(_hpText);

            ShowEmpty();
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Assign(NightFighter fighter)
        {
            AssignedFighter = fighter;

            _slotLabel.AddToClassList("lk-hidden");
            _nameLabel.RemoveFromClassList("lk-hidden");
            _statsLabel.RemoveFromClassList("lk-hidden");
            _hpBar.RemoveFromClassList("lk-hidden");
            _hpText.RemoveFromClassList("lk-hidden");

            var tex = fighter.SourceCard?.Definition?.ArtTexture;
            if (tex != null)
            {
                _icon.style.backgroundImage = new StyleBackground(Background.FromTexture2D(tex));
                _icon.RemoveFromClassList("lk-hidden");
            }
            else
            {
                _icon.AddToClassList("lk-hidden");
            }

            Root.RemoveFromClassList("nb-prep-slot--empty");
            Root.AddToClassList("nb-prep-slot--filled");

            RefreshDisplay(fighter);
        }

        public void Clear()
        {
            AssignedFighter = null;
            ShowEmpty();
        }

        /// <summary>Refresh stats labels after a shop item modifies the fighter.</summary>
        public void RefreshDisplay(NightFighter fighter = null)
        {
            var f = fighter ?? AssignedFighter;
            if (f == null) return;

            _nameLabel.text  = f.DisplayName;
            _statsLabel.text = $"ATK {f.FinalAttack}  |  HP {f.FinalMaxHealth}";
            _hpFill.style.width = Length.Percent(100f); // full at prep time
            _hpText.text = $"{f.FinalHealth}/{f.FinalMaxHealth}";
        }

        /// <summary>Update HP bar during battle from a live CombatUnit.</summary>
        public void RefreshBattle(CombatUnit unit)
        {
            if (unit == null) return;

            if (!unit.IsAlive)
            {
                _nameLabel.text = $"✗ {unit.DisplayName}";
                Root.AddToClassList("nb-prep-slot--dead");
                _hpFill.style.width = Length.Percent(0f);
                _hpText.text = "—";
                return;
            }

            float pct = unit.HPFraction * 100f;
            _hpFill.style.width = Length.Percent(pct);
            _hpText.text = $"{unit.CurrentHP}/{unit.MaxHP}";

            if (pct < 35f) _hpFill.AddToClassList("nb-hp-fill--low");
            else            _hpFill.RemoveFromClassList("nb-hp-fill--low");
        }

        public void SetHighlighted(bool on)
        {
            if (on) Root.AddToClassList("nb-prep-slot--targeted");
            else    Root.RemoveFromClassList("nb-prep-slot--targeted");
        }

        public void SetLocked(bool locked)
        {
            _locked = locked;
            if (locked) Root.AddToClassList("nb-prep-slot--locked");
            else        Root.RemoveFromClassList("nb-prep-slot--locked");
        }

        // ── Private ───────────────────────────────────────────────────────────────

        private void ShowEmpty()
        {
            _slotLabel.RemoveFromClassList("lk-hidden");
            _icon.AddToClassList("lk-hidden");
            _nameLabel.AddToClassList("lk-hidden");
            _statsLabel.AddToClassList("lk-hidden");
            _hpBar.AddToClassList("lk-hidden");
            _hpText.AddToClassList("lk-hidden");

            Root.RemoveFromClassList("nb-prep-slot--filled");
            Root.RemoveFromClassList("nb-prep-slot--dead");
            Root.AddToClassList("nb-prep-slot--empty");
        }
    }
}
