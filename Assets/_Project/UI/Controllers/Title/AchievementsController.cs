using Markyu.LastKernel.Achievements;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the Achievements sub-panel (#panel-achievements) on the Title screen.
    /// Renders a flex-wrap card grid — one card per achievement, with icon placeholder,
    /// name, status label, and an optional thin progress bar.
    /// </summary>
    public sealed class AchievementsController : UIToolkitComponentController
    {
        private Label         _titleLabel;
        private Label         _emptyLabel;
        private VisualElement _grid;
        private Label         _progressLabel;
        private VisualElement _progressFill;
        private Button        _closeButton;

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel    = Root.Q<Label>        ("lbl-ach-title");
            _emptyLabel    = Root.Q<Label>         ("lbl-ach-empty");
            _grid          = Root.Q<VisualElement> ("ach-list");
            _progressLabel = Root.Q<Label>         ("lbl-ach-progress");
            _progressFill  = Root.Q<VisualElement> ("fill-ach-progress");
            _closeButton   = Root.Q<Button>        ("btn-ach-close");

            _closeButton.clicked += Hide;
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Show()
        {
            Root.RemoveFromClassList("lk-hidden");
            Rebuild();
            OnLocalizationRefresh();
        }

        public void Hide() => Root.AddToClassList("lk-hidden");

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel  != null) _titleLabel.text  = GameLocalization.GetOptional("title.achievements", "Achievements");
            if (_closeButton != null) _closeButton.text = GameLocalization.GetOptional("common.closeButton", "Close");
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Rebuild()
        {
            _grid.Clear();

            var svc = AchievementService.Instance;
            bool hasData = svc != null && svc.Database != null && svc.Database.All.Count > 0;

            _emptyLabel?.EnableInClassList("lk-hidden", hasData);
            if (!hasData) return;

            int total    = 0;
            int unlocked = 0;

            foreach (var def in svc.Database.All)
            {
                if (def == null) continue;
                total++;
                bool isUnlocked = svc.IsUnlocked(def);
                if (isUnlocked) unlocked++;
                _grid.Add(BuildCard(def, svc, isUnlocked));
            }

            UpdateFooter(unlocked, total);
        }

        private static VisualElement BuildCard(AchievementDefinition def, AchievementService svc, bool isUnlocked)
        {
            bool secret      = def.IsSecret && !isUnlocked;
            bool hasProgress = !isUnlocked && !secret && def.TargetCount > 1;

            // ── Card shell ───────────────────────────────────────────────
            var card = new VisualElement();
            card.AddToClassList("lk-ach-card");
            if (isUnlocked) card.AddToClassList("lk-ach-card--unlocked");

            // ── Top row: icon + text ─────────────────────────────────────
            var topRow = new VisualElement();
            topRow.style.flexDirection = FlexDirection.Row;
            topRow.style.alignItems    = Align.FlexStart;
            card.Add(topRow);

            // Icon placeholder (filled with sprite if available)
            var icon = new VisualElement();
            icon.AddToClassList("lk-ach-card__icon");
            if (isUnlocked) icon.AddToClassList("lk-ach-card__icon--unlocked");
            if (!secret && def.Icon != null)
                icon.style.backgroundImage = Background.FromSprite(def.Icon);
            topRow.Add(icon);

            // Text column
            var textCol = new VisualElement();
            textCol.style.flexDirection = FlexDirection.Column;
            textCol.style.flexGrow      = 1;
            topRow.Add(textCol);

            // Name
            string displayName = secret
                ? "???"
                : GameLocalization.GetOptional(def.LocalizationKey + ".name", def.Id);
            var nameLabel = new Label(displayName);
            nameLabel.AddToClassList("lk-ach-card__name");
            if (!isUnlocked) nameLabel.AddToClassList("lk-ach-card__name--dim");
            textCol.Add(nameLabel);

            // Description (hidden for secrets)
            if (!secret)
            {
                string desc = GameLocalization.GetOptional(def.LocalizationKey + ".description", "");
                if (!string.IsNullOrEmpty(desc))
                {
                    var descLabel = new Label(desc);
                    descLabel.AddToClassList("lk-ach-card__desc");
                    textCol.Add(descLabel);
                }
            }

            // Status line
            var status = BuildStatusLabel(def, svc, isUnlocked, secret);
            textCol.Add(status);

            // ── Progress bar (spans full card width) ─────────────────────
            if (hasProgress)
            {
                var barOuter = new VisualElement();
                barOuter.AddToClassList("lk-ach-card__progress-bar");

                var barFill = new VisualElement();
                barFill.AddToClassList("lk-ach-card__progress-fill");
                float norm = svc.GetProgressNormalized(def);
                barFill.style.width = new StyleLength(new Length(norm * 100f, LengthUnit.Percent));
                barOuter.Add(barFill);
                card.Add(barOuter);
            }

            return card;
        }

        private static Label BuildStatusLabel(AchievementDefinition def, AchievementService svc,
                                              bool isUnlocked, bool secret)
        {
            string text;
            string cssColor;

            if (isUnlocked)
            {
                text     = "✓  UNLOCKED";
                cssColor = "lk-label--accent";
            }
            else if (secret)
            {
                text     = "◈  SECRET";
                cssColor = "lk-label--dim";
            }
            else if (def.TargetCount > 1)
            {
                int count = svc.GetProgressCount(def);
                text      = $"{count} / {def.TargetCount}";
                cssColor  = "lk-label--dim";
            }
            else
            {
                text     = "○  LOCKED";
                cssColor = "lk-label--dim";
            }

            var lbl = new Label(text);
            lbl.AddToClassList(cssColor);
            lbl.AddToClassList("lk-ach-card__status");
            return lbl;
        }

        private void UpdateFooter(int unlocked, int total)
        {
            if (_progressLabel != null)
                _progressLabel.text = $"{unlocked} / {total}";

            if (_progressFill != null && total > 0)
                _progressFill.style.width = new StyleLength(
                    new Length((float)unlocked / total * 100f, LengthUnit.Percent));
        }
    }
}
