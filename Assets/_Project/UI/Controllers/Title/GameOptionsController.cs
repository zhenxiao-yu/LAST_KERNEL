using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the Options sub-panel (#panel-options): graphics cycles, audio
    /// sliders, language toggle, and reset.  Mirrors GameOptionsUI logic but
    /// routes intent through UIEventBus where applicable.
    /// </summary>
    public sealed class GameOptionsController : UIToolkitComponentController
    {
        private readonly Action<string, string, Action> _showConfirm;

        private Label  _titleLabel;
        private Label  _graphicsLabel;
        private Button _resolutionButton;
        private Button _fullscreenButton;
        private Button _vSyncButton;
        private Button _fpsButton;
        private Button _shadowButton;
        private Label  _audioLabel;
        private Label  _sfxLabel;
        private Slider _sfxSlider;
        private Label  _bgmLabel;
        private Slider _bgmSlider;
        private Button _languageButton;
        private Button _resetButton;
        private Button _closeButton;

        public GameOptionsController(Action<string, string, Action> showConfirm)
        {
            _showConfirm = showConfirm;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel       = Root.Q<Label>  ("lbl-opts-title");
            _graphicsLabel    = Root.Q<Label>  ("lbl-opts-graphics");
            _resolutionButton = Root.Q<Button> ("btn-opt-resolution");
            _fullscreenButton = Root.Q<Button> ("btn-opt-fullscreen");
            _vSyncButton      = Root.Q<Button> ("btn-opt-vsync");
            _fpsButton        = Root.Q<Button> ("btn-opt-fps");
            _shadowButton     = Root.Q<Button> ("btn-opt-shadows");
            _audioLabel       = Root.Q<Label>  ("lbl-opts-audio");
            _sfxLabel         = Root.Q<Label>  ("lbl-sfx");
            _sfxSlider        = Root.Q<Slider> ("slider-sfx");
            _bgmLabel         = Root.Q<Label>  ("lbl-bgm");
            _bgmSlider        = Root.Q<Slider> ("slider-bgm");
            _languageButton   = Root.Q<Button> ("btn-opt-language");
            _resetButton      = Root.Q<Button> ("btn-opt-reset");
            _closeButton      = Root.Q<Button> ("btn-opt-close");

            _resolutionButton.clicked += () => { GraphicsManager.Instance?.CycleScreenResolution(); RefreshGraphicsLabels(); };
            _fullscreenButton.clicked += () => { GraphicsManager.Instance?.CycleFullscreenMode();   RefreshGraphicsLabels(); };
            _vSyncButton.clicked      += () => { GraphicsManager.Instance?.CycleVSync();            RefreshGraphicsLabels(); };
            _fpsButton.clicked        += () => { GraphicsManager.Instance?.CycleFrameRateCap();     RefreshGraphicsLabels(); };
            _shadowButton.clicked     += () => { GraphicsManager.Instance?.CycleShadowPreset();     RefreshGraphicsLabels(); };
            _languageButton.clicked   += UIEventBus.RaiseLanguageCycleRequested;
            _resetButton.clicked      += ShowResetConfirmation;
            _closeButton.clicked      += Hide;

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

        // ── API ────────────────────────────────────────────────────────────────

        public void Show()
        {
            Root.RemoveFromClassList("lk-hidden");
            RefreshFromManagers();
            OnLocalizationRefresh();
        }

        public void Hide()
        {
            PlayerPrefs.Save();
            Root.AddToClassList("lk-hidden");
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel     != null) _titleLabel.text     = GameLocalization.Get("options.header");
            if (_graphicsLabel  != null) _graphicsLabel.text  = GameLocalization.Get("ui.video");
            if (_audioLabel     != null) _audioLabel.text     = GameLocalization.Get("ui.audio");
            if (_resetButton    != null) _resetButton.text    = GameLocalization.Get("common.resetButton");
            if (_closeButton    != null) _closeButton.text    = GameLocalization.Get("common.closeButton");
            if (_languageButton != null) _languageButton.text = GameLocalization.GetLanguageButtonLabel();

            RefreshGraphicsLabels();
            if (_sfxSlider != null) UpdateSfxLabel(_sfxSlider.value);
            if (_bgmSlider != null) UpdateBgmLabel(_bgmSlider.value);
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void RefreshFromManagers()
        {
            RefreshGraphicsLabels();
            RefreshVolumeSliders();
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
            RefreshFromManagers();
            OnLocalizationRefresh();
        }
    }
}
