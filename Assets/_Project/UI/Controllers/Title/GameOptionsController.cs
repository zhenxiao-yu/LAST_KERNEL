using System;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    public sealed class GameOptionsController : UIToolkitComponentController
    {
        private enum OptionsTab { Settings, Video, Language, Controls, Accessibility }

        private const string KeyScreenShake = "accessibility.screenshake";
        private const string KeyFlash       = "accessibility.flash";

        private readonly Action<string, string, Action> _showConfirm;
        private readonly Action _showLangModal;

        // ── Settings tab ───────────────────────────────────────────────────────
        private Label  _titleLabel;
        private Label  _subtitleLabel;
        private Label  _uiLabel;
        private Button _uiScaleButton;
        private Label  _audioLabel;
        private Label  _sfxLabel;
        private Slider _sfxSlider;
        private Label  _bgmLabel;
        private Slider _bgmSlider;

        // ── Video tab ──────────────────────────────────────────────────────────
        private Label  _graphicsLabel;
        private Button _resolutionButton;
        private Button _fullscreenButton;
        private Button _vSyncButton;
        private Button _fpsButton;
        private Button _shadowButton;

        // ── Language tab ───────────────────────────────────────────────────────
        private Button _languageButton;

        // ── Accessibility tab ──────────────────────────────────────────────────
        private Label  _accessibilityLabel;
        private Label  _accessibilityHintLabel;
        private Button _screenShakeButton;
        private Button _flashEffectsButton;

        // ── Language tab ───────────────────────────────────────────────────────
        private Label  _languageHintLabel;

        // ── Tab bar ────────────────────────────────────────────────────────────
        private Button      _tabSettings;
        private Button      _tabVideo;
        private Button      _tabLanguage;
        private Button      _tabControls;
        private Button      _tabAccessibility;
        private ScrollView  _settingsScroll;
        private ScrollView  _videoScroll;
        private ScrollView  _languageScroll;
        private ScrollView  _controlsScroll;
        private ScrollView  _accessibilityScroll;
        private VisualElement _keybindList;
        private OptionsTab  _currentTab;

        // ── Footer ─────────────────────────────────────────────────────────────
        private Button _resetButton;
        private Button _closeButton;

#if ENABLE_INPUT_SYSTEM
        private InputActionRebindingExtensions.RebindingOperation _rebindOp;
#endif

        public GameOptionsController(Action<string, string, Action> showConfirm, Action showLangModal)
        {
            _showConfirm   = showConfirm;
            _showLangModal = showLangModal;
        }

        protected override void OnBind()
        {
            // Settings
            _titleLabel       = Root.Q<Label>      ("lbl-opts-title");
            _subtitleLabel    = Root.Q<Label>      ("lbl-opts-subtitle");
            _uiLabel          = Root.Q<Label>      ("lbl-opts-ui");
            _uiScaleButton    = Root.Q<Button>     ("btn-opt-ui-scale");
            _audioLabel       = Root.Q<Label>      ("lbl-opts-audio");
            _sfxLabel         = Root.Q<Label>      ("lbl-sfx");
            _sfxSlider        = Root.Q<Slider>     ("slider-sfx");
            _bgmLabel         = Root.Q<Label>      ("lbl-bgm");
            _bgmSlider        = Root.Q<Slider>     ("slider-bgm");

            // Video
            _graphicsLabel    = Root.Q<Label>      ("lbl-opts-graphics");
            _resolutionButton = Root.Q<Button>     ("btn-opt-resolution");
            _fullscreenButton = Root.Q<Button>     ("btn-opt-fullscreen");
            _vSyncButton      = Root.Q<Button>     ("btn-opt-vsync");
            _fpsButton        = Root.Q<Button>     ("btn-opt-fps");
            _shadowButton     = Root.Q<Button>     ("btn-opt-shadows");

            // Language
            _languageButton   = Root.Q<Button>     ("btn-opt-language");

            // Accessibility
            _accessibilityLabel      = Root.Q<Label> ("lbl-opts-accessibility");
            _accessibilityHintLabel  = Root.Q<Label> ("lbl-accessibility-hint");
            _screenShakeButton       = Root.Q<Button>("btn-opt-screenshake");
            _flashEffectsButton      = Root.Q<Button>("btn-opt-flash");

            // Language hint
            _languageHintLabel = Root.Q<Label>("lbl-language-hint");

            // Tabs
            _tabSettings      = Root.Q<Button>     ("btn-tab-settings");
            _tabVideo         = Root.Q<Button>     ("btn-tab-video");
            _tabLanguage      = Root.Q<Button>     ("btn-tab-language");
            _tabControls      = Root.Q<Button>     ("btn-tab-controls");
            _tabAccessibility = Root.Q<Button>     ("btn-tab-accessibility");
            _settingsScroll      = Root.Q<ScrollView>("opts-scroll");
            _videoScroll         = Root.Q<ScrollView>("video-scroll");
            _languageScroll      = Root.Q<ScrollView>("language-scroll");
            _controlsScroll      = Root.Q<ScrollView>("controls-scroll");
            _accessibilityScroll = Root.Q<ScrollView>("accessibility-scroll");
            _keybindList         = Root.Q<VisualElement>("keybind-list");

            // Footer
            _resetButton = Root.Q<Button>("btn-opt-reset");
            _closeButton = Root.Q<Button>("btn-opt-close");

            // Video
            _resolutionButton.clicked += () => { GraphicsManager.Instance?.CycleScreenResolution(); RefreshGraphicsLabels(); };
            _fullscreenButton.clicked += () => { GraphicsManager.Instance?.CycleFullscreenMode();   RefreshGraphicsLabels(); };
            _vSyncButton.clicked      += () => { GraphicsManager.Instance?.CycleVSync();            RefreshGraphicsLabels(); };
            _fpsButton.clicked        += () => { GraphicsManager.Instance?.CycleFrameRateCap();     RefreshGraphicsLabels(); };
            _shadowButton.clicked     += () => { GraphicsManager.Instance?.CycleShadowPreset();     RefreshGraphicsLabels(); };

            // Settings
            if (_uiScaleButton   != null) _uiScaleButton.clicked   += () => { UIScaleManager.CycleScale(); UpdateUIScaleButton(); };

            // Language
            if (_languageButton  != null) _languageButton.clicked  += () => _showLangModal?.Invoke();

            // Accessibility
            if (_screenShakeButton  != null) _screenShakeButton.clicked  += OnScreenShakeClicked;
            if (_flashEffectsButton != null) _flashEffectsButton.clicked += OnFlashClicked;

            // Tabs
            if (_tabSettings      != null) _tabSettings.clicked      += () => SwitchTab(OptionsTab.Settings);
            if (_tabVideo         != null) _tabVideo.clicked         += () => SwitchTab(OptionsTab.Video);
            if (_tabLanguage      != null) _tabLanguage.clicked      += () => { SwitchTab(OptionsTab.Language); RefreshLanguageHint(); };
            if (_tabControls      != null) _tabControls.clicked      += () => SwitchTab(OptionsTab.Controls);
            if (_tabAccessibility != null) _tabAccessibility.clicked += () => SwitchTab(OptionsTab.Accessibility);

            _resetButton.clicked += OnResetClicked;
            _closeButton.clicked += Hide;

            _sfxSlider.RegisterValueChangedCallback(evt =>
            {
                AudioManager.Instance?.SetSFXVolume(evt.newValue);
                UpdateSfxLabel(evt.newValue);
            });
            _bgmSlider.RegisterValueChangedCallback(evt =>
            {
                AudioManager.Instance?.SetBGMVolume(evt.newValue);
                UpdateBgmLabel(evt.newValue);
            });
        }

        public void Show()
        {
            bool wasHidden = Root.ClassListContains("lk-hidden");
            Root.RemoveFromClassList("lk-hidden");
            SwitchTab(OptionsTab.Settings);
            RefreshFromManagers();
            OnLocalizationRefresh();
            if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
        }

        public void Hide()
        {
#if ENABLE_INPUT_SYSTEM
            _rebindOp?.Cancel();
            _rebindOp?.Dispose();
            _rebindOp = null;
#endif
            bool wasVisible = !Root.ClassListContains("lk-hidden");
            PlayerPrefs.Save();
            Root.AddToClassList("lk-hidden");
            if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
        }

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel         != null) _titleLabel.text         = GameLocalization.Get("options.header");
            if (_subtitleLabel      != null) _subtitleLabel.text      = GameLocalization.GetOptional("options.subtitle", "System Configuration");
            if (_graphicsLabel      != null) _graphicsLabel.text      = GameLocalization.Get("ui.video");
            if (_uiLabel            != null) _uiLabel.text            = GameLocalization.Get("options.uiScale");
            if (_audioLabel         != null) _audioLabel.text         = GameLocalization.Get("ui.audio");
            if (_accessibilityLabel     != null) _accessibilityLabel.text     = GameLocalization.GetOptional("options.accessibility", "Accessibility");
            if (_accessibilityHintLabel != null) _accessibilityHintLabel.text = GameLocalization.GetOptional("options.accessibility.hint", "Reduce visual intensity for comfort and epilepsy safety.");
            if (_languageButton         != null) _languageButton.text         = GameLocalization.GetLanguageButtonLabel();
            if (_languageHintLabel      != null) _languageHintLabel.text      = GameLocalization.GetOptional("options.language.title", "Select Language");
            if (_tabSettings        != null) _tabSettings.text        = GameLocalization.GetOptional("options.tab.settings",     "Settings");
            if (_tabVideo           != null) _tabVideo.text           = GameLocalization.GetOptional("options.tab.video",        "Video");
            if (_tabLanguage        != null) _tabLanguage.text        = GameLocalization.GetOptional("options.tab.language",     "Language");
            if (_tabControls        != null) _tabControls.text        = GameLocalization.GetOptional("options.tab.controls",     "Controls");
            if (_tabAccessibility   != null) _tabAccessibility.text   = GameLocalization.GetOptional("options.tab.accessibility","Access.");
            if (_closeButton        != null) _closeButton.text        = GameLocalization.Get("common.closeButton");

            UpdateResetButtonLabel();
            RefreshGraphicsLabels();
            UpdateUIScaleButton();
            RefreshAccessibilityButtons();
            if (_sfxSlider != null) UpdateSfxLabel(_sfxSlider.value);
            if (_bgmSlider != null) UpdateBgmLabel(_bgmSlider.value);
        }

        // ── Tab switching ──────────────────────────────────────────────────────

        private void SwitchTab(OptionsTab tab)
        {
            _currentTab = tab;

            _settingsScroll?.EnableInClassList     ("lk-hidden", tab != OptionsTab.Settings);
            _videoScroll?.EnableInClassList        ("lk-hidden", tab != OptionsTab.Video);
            _languageScroll?.EnableInClassList     ("lk-hidden", tab != OptionsTab.Language);
            _controlsScroll?.EnableInClassList     ("lk-hidden", tab != OptionsTab.Controls);
            _accessibilityScroll?.EnableInClassList("lk-hidden", tab != OptionsTab.Accessibility);

            _tabSettings?.EnableInClassList     ("lk-tab--active", tab == OptionsTab.Settings);
            _tabVideo?.EnableInClassList        ("lk-tab--active", tab == OptionsTab.Video);
            _tabLanguage?.EnableInClassList     ("lk-tab--active", tab == OptionsTab.Language);
            _tabControls?.EnableInClassList     ("lk-tab--active", tab == OptionsTab.Controls);
            _tabAccessibility?.EnableInClassList("lk-tab--active", tab == OptionsTab.Accessibility);

            if (tab == OptionsTab.Controls) BuildKeybindRows();
            UpdateResetButtonLabel();
        }

        private void UpdateResetButtonLabel()
        {
            if (_resetButton == null) return;
            _resetButton.EnableInClassList("lk-hidden", _currentTab == OptionsTab.Language);
            _resetButton.text = _currentTab == OptionsTab.Controls
                ? GameLocalization.GetOptional("options.controls.resetAll", "Reset All")
                : GameLocalization.Get("common.resetButton");
        }

        private void OnResetClicked()
        {
            switch (_currentTab)
            {
                case OptionsTab.Controls:
                    ResetAllKeybinds();
                    break;
                case OptionsTab.Accessibility:
                    ResetAccessibilitySettings();
                    break;
                default:
                    ShowResetConfirmation();
                    break;
            }
        }

        // ── Keybind rows ───────────────────────────────────────────────────────

        private void BuildKeybindRows()
        {
            if (_keybindList == null) return;
            _keybindList.Clear();

#if ENABLE_INPUT_SYSTEM
            var handler = GameInputHandler.Instance;
            if (handler == null)
            {
                var msg = new Label { text = GameLocalization.GetOptional("options.controls.unavailable", "Controls are only available during gameplay.") };
                msg.AddToClassList("lk-keybind-unavailable");
                _keybindList.Add(msg);
                LKUIInteractionPolisher.Refresh(Root);
                return;
            }

            foreach (var entry in handler.AllActions)
                _keybindList.Add(CreateKeybindRow(entry));
            LKUIInteractionPolisher.Refresh(Root);
#else
            _keybindList.Add(new Label { text = "New Input System required for rebinding." });
            LKUIInteractionPolisher.Refresh(Root);
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private VisualElement CreateKeybindRow(ActionEntry entry)
        {
            var row = new VisualElement();
            row.AddToClassList("lk-keybind-row");

            var nameLabel = new Label(entry.DisplayName);
            nameLabel.AddToClassList("lk-keybind-row__name");

            var bindingLabel = new Label(GetBindingDisplay(entry.Action));
            bindingLabel.AddToClassList("lk-keybind-badge");

            var rebindBtn = new Button { text = "✎" };
            rebindBtn.AddToClassList("lk-button");
            rebindBtn.AddToClassList("lk-button--compact");
            rebindBtn.AddToClassList("lk-button--utility");
            rebindBtn.AddToClassList("lk-keybind-icon-button");
            rebindBtn.AddToClassList("lk-keybind-icon-button--spaced");
            rebindBtn.clicked += () => StartRebind(entry, bindingLabel, rebindBtn);

            var resetBtn = new Button { text = "↩" };
            resetBtn.AddToClassList("lk-button");
            resetBtn.AddToClassList("lk-button--compact");
            resetBtn.AddToClassList("lk-button--quiet");
            resetBtn.AddToClassList("lk-keybind-icon-button");
            resetBtn.clicked += () => ResetSingleKeybind(entry, bindingLabel);

            row.Add(nameLabel);
            row.Add(bindingLabel);
            row.Add(rebindBtn);
            row.Add(resetBtn);
            return row;
        }

        private void StartRebind(ActionEntry entry, Label bindingLabel, Button rebindBtn)
        {
            _rebindOp?.Cancel();
            _rebindOp?.Dispose();

            entry.Action.Disable();
            rebindBtn.SetEnabled(false);
            bindingLabel.AddToClassList("lk-keybind-badge--conflict");
            bindingLabel.text = "...";

            _rebindOp = entry.Action
                .PerformInteractiveRebinding()
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Mouse>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op =>
                {
                    op.Dispose();
                    _rebindOp = null;

                    if (HasBindingConflict(entry.Action, out string conflictName))
                    {
                        InputActionRebindingExtensions.RemoveAllBindingOverrides(entry.Action);
                        entry.Action.Enable();
                        string original = GetBindingDisplay(entry.Action);
                        bindingLabel.AddToClassList("lk-keybind-badge--conflict");
                        bindingLabel.text = $"⚠ {conflictName}";
                        bindingLabel.schedule.Execute(() =>
                        {
                            bindingLabel.RemoveFromClassList("lk-keybind-badge--conflict");
                            bindingLabel.text = original;
                            rebindBtn.SetEnabled(true);
                        }).StartingIn(1500);
                        return;
                    }

                    entry.Action.Enable();
                    GameInputHandler.Instance?.SaveBindings();
                    BuildKeybindRows();
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    _rebindOp = null;
                    entry.Action.Enable();
                    BuildKeybindRows();
                })
                .Start();
        }

        private void ResetSingleKeybind(ActionEntry entry, Label bindingLabel)
        {
            _rebindOp?.Cancel();
            GameInputHandler.Instance?.ResetBinding(entry.Action);
            bindingLabel.text = GetBindingDisplay(entry.Action);
        }

        private void ResetAllKeybinds()
        {
            _rebindOp?.Cancel();
            GameInputHandler.Instance?.ResetAllBindings();
            BuildKeybindRows();
        }

        private static string GetBindingDisplay(InputAction action)
        {
            if (action == null || action.bindings.Count == 0) return "—";
            return action.GetBindingDisplayString(0);
        }

        private static bool HasBindingConflict(InputAction rebound, out string conflictName)
        {
            conflictName = null;
            var handler = GameInputHandler.Instance;
            if (handler == null) return false;

            string newPath = rebound.bindings.Count > 0 ? rebound.bindings[0].effectivePath : null;
            if (string.IsNullOrEmpty(newPath)) return false;

            foreach (var e in handler.AllActions)
            {
                if (e.Action == rebound) continue;
                if (e.Action.bindings.Count > 0 && e.Action.bindings[0].effectivePath == newPath)
                {
                    conflictName = e.DisplayName;
                    return true;
                }
            }
            return false;
        }
#else
        private void ResetAllKeybinds() { }
#endif

        // ── Language hint ──────────────────────────────────────────────────────

        private void RefreshLanguageHint()
        {
            if (_languageHintLabel != null)
                _languageHintLabel.text = GameLocalization.GetOptional("options.language.title", "Select Language");
        }

        // ── Accessibility ──────────────────────────────────────────────────────

        private void OnScreenShakeClicked()
        {
            bool current = PlayerPrefs.GetInt(KeyScreenShake, 1) != 0;
            PlayerPrefs.SetInt(KeyScreenShake, current ? 0 : 1);
            UpdateScreenShakeButton();
        }

        private void OnFlashClicked()
        {
            bool current = PlayerPrefs.GetInt(KeyFlash, 1) != 0;
            PlayerPrefs.SetInt(KeyFlash, current ? 0 : 1);
            UpdateFlashButton();
        }

        private void RefreshAccessibilityButtons()
        {
            UpdateScreenShakeButton();
            UpdateFlashButton();
        }

        private void UpdateScreenShakeButton()
        {
            if (_screenShakeButton == null) return;
            bool on = PlayerPrefs.GetInt(KeyScreenShake, 1) != 0;
            string state = GameLocalization.GetOptional(on ? "common.on" : "common.off", on ? "On" : "Off");
            _screenShakeButton.text = $"{GameLocalization.GetOptional("options.screenshake", "Screen Shake")}: {state}";
        }

        private void UpdateFlashButton()
        {
            if (_flashEffectsButton == null) return;
            bool on = PlayerPrefs.GetInt(KeyFlash, 1) != 0;
            string state = GameLocalization.GetOptional(on ? "common.on" : "common.off", on ? "On" : "Off");
            _flashEffectsButton.text = $"{GameLocalization.GetOptional("options.flash", "Flash Effects")}: {state}";
        }

        private void ResetAccessibilitySettings()
        {
            PlayerPrefs.DeleteKey(KeyScreenShake);
            PlayerPrefs.DeleteKey(KeyFlash);
            RefreshAccessibilityButtons();
        }

        // ── Settings helpers ───────────────────────────────────────────────────

        private void RefreshFromManagers()
        {
            RefreshGraphicsLabels();
            UpdateUIScaleButton();
            RefreshVolumeSliders();
            RefreshAccessibilityButtons();
        }

        private void RefreshGraphicsLabels()
        {
            GraphicsManager gm = GraphicsManager.Instance;
            if (gm == null) return;

            if (_resolutionButton != null) _resolutionButton.text = gm.GetResolutionLabel();
            if (_fullscreenButton != null) _fullscreenButton.text = gm.GetFullscreenLabel();
            if (_vSyncButton      != null) _vSyncButton.text      = gm.FormatVSyncLabel();
            if (_fpsButton        != null) _fpsButton.text        = gm.FormatFpsLabel();
            if (_shadowButton     != null) _shadowButton.text     = gm.FormatShadowLabel();
        }

        private void RefreshVolumeSliders()
        {
            AudioManager am = AudioManager.Instance;
            if (am == null) return;

            float sfx = am.GetSavedSFXVolumeSlider();
            float bgm = am.GetSavedBGMVolumeSlider();

            _sfxSlider?.SetValueWithoutNotify(sfx);
            _bgmSlider?.SetValueWithoutNotify(bgm);
            UpdateSfxLabel(sfx);
            UpdateBgmLabel(bgm);
        }

        private void UpdateSfxLabel(float value)
        {
            if (_sfxLabel != null)
                _sfxLabel.text = GameLocalization.Format("options.sfx", Mathf.RoundToInt(value * 100f));
        }

        private void UpdateBgmLabel(float value)
        {
            if (_bgmLabel != null)
                _bgmLabel.text = GameLocalization.Format("options.bgm", Mathf.RoundToInt(value * 100f));
        }

        private void UpdateUIScaleButton()
        {
            if (_uiScaleButton == null) return;
            string sizeLabel = GameLocalization.Get(UIScaleManager.GetScaleLabelKey(UIScaleManager.CurrentScale));
            _uiScaleButton.text = GameLocalization.Format("options.uiScale.label", sizeLabel);
        }

        private void ShowResetConfirmation()
        {
            if (_showConfirm != null)
                _showConfirm(
                    GameLocalization.Get("options.resetTitle"),
                    GameLocalization.Get("options.resetBody"),
                    ResetAllSettings);
        }

        private void ResetAllSettings()
        {
            string localeCode = UnityLocalizationBridge.CurrentLocaleCode;
            PlayerPrefs.DeleteAll();
            GameLocalization.SetLanguageByCode(localeCode, force: true);
            GraphicsManager.Instance?.InitGraphicsSettings();
            AudioManager.Instance?.InitAudioMixerVolumes();
            UIScaleManager.SetScale(UIScale.Medium);
            RefreshFromManagers();
            OnLocalizationRefresh();
        }
    }
}
