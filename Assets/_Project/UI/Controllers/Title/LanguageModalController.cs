using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the language-picker modal (#panel-language).
    /// Selecting a button changes the language immediately so the user can read the
    /// UI in the target language. Apply closes and keeps the new language; Cancel
    /// reverts to the language that was active when the modal opened.
    /// Owned by the screen-level controller (Title or Pause) and shared between
    /// the nav trigger and the options panel trigger.
    /// </summary>
    public sealed class LanguageModalController : UIToolkitComponentController
    {
        private static readonly GameLanguage[] LangOrder =
        {
            GameLanguage.SimplifiedChinese, GameLanguage.English,
            GameLanguage.TraditionalChinese, GameLanguage.Japanese,
            GameLanguage.Korean, GameLanguage.French,
            GameLanguage.German, GameLanguage.Spanish,
        };

        private static readonly string[] LangButtonNames =
            { "btn-lang-zh", "btn-lang-en", "btn-lang-zht", "btn-lang-ja",
              "btn-lang-ko", "btn-lang-fr", "btn-lang-de",  "btn-lang-es" };

        private static readonly string[] LangLabelKeys =
            { "language.chinese", "language.english", "language.traditionalChinese", "language.japanese",
              "language.korean",  "language.french",  "language.german",             "language.spanish" };

        private Label    _titleLabel;
        private Label    _subtitleLabel;
        private Button   _cancelButton;
        private Button   _applyButton;
        private readonly Button[] _langButtons = new Button[8];

        private GameLanguage _originalLanguage;

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel    = Root.Q<Label> ("lbl-lang-title");
            _subtitleLabel = Root.Q<Label> ("lbl-lang-subtitle");
            _cancelButton  = Root.Q<Button>("btn-lang-cancel");
            _applyButton  = Root.Q<Button>("btn-lang-apply");

            for (int i = 0; i < LangButtonNames.Length; i++)
            {
                var btn = Root.Q<Button>(LangButtonNames[i]);
                _langButtons[i] = btn;
                if (btn == null) continue;
                int idx = i;
                btn.clicked += () => GameLocalization.SetLanguage(LangOrder[idx]);
            }

            if (_cancelButton != null) _cancelButton.clicked += Cancel;
            if (_applyButton  != null) _applyButton.clicked  += Apply;
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Show()
        {
            _originalLanguage = GameLocalization.CurrentLanguage;
            OnLocalizationRefresh();
            bool wasHidden = Root.ClassListContains("lk-hidden");
            Root.RemoveFromClassList("lk-hidden");
            if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
        }

        public void Hide()
        {
            bool wasVisible = !Root.ClassListContains("lk-hidden");
            Root.AddToClassList("lk-hidden");
            if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel    != null) _titleLabel.text    = GameLocalization.Get("options.language.title");
            if (_subtitleLabel != null) _subtitleLabel.text = GameLocalization.GetOptional("options.language.subtitle", "Interface Language");
            if (_cancelButton  != null) _cancelButton.text  = GameLocalization.Get("common.cancelButton");
            if (_applyButton  != null) _applyButton.text  = GameLocalization.Get("common.applyButton");
            RefreshButtons();
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void Cancel()
        {
            GameLocalization.SetLanguage(_originalLanguage);
            Hide();
        }

        private void Apply() => Hide();

        private void RefreshButtons()
        {
            GameLanguage current = GameLocalization.CurrentLanguage;
            for (int i = 0; i < _langButtons.Length; i++)
            {
                if (_langButtons[i] == null) continue;
                _langButtons[i].text = GameLocalization.Get(LangLabelKeys[i]);
                _langButtons[i].EnableInClassList("lk-button--active", LangOrder[i] == current);
            }
        }
    }
}
