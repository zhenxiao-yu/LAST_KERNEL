using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Michsky.LSS;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(LSS_LoadingScreen))]
    public sealed class LoadingScreenBridge : MonoBehaviour
    {
        // Seconds between each character in the typewriter reveal
        private const float TypewriterCharDelay = 0.022f;
        // Brief pause before typing starts after the screen fades in
        private const float TypewriterStartDelay = 0.35f;
        private const string DefaultTip = "Hold the bunker.";

        private static readonly string[] TipKeys =
        {
            "loading.tip.01", "loading.tip.02", "loading.tip.03", "loading.tip.04",
            "loading.tip.05", "loading.tip.06", "loading.tip.07", "loading.tip.08",
            "loading.tip.09", "loading.tip.10", "loading.tip.11", "loading.tip.12",
            "loading.tip.13", "loading.tip.14", "loading.tip.15", "loading.tip.16",
        };

        private LSS_LoadingScreen _lss;
        private string            _chosenTip;
        private string            _chosenTipKey;
        private bool              _typewriterDone;
        private bool              _continueHintShown;

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += HandleLanguageChanged;
        }

        private void OnDisable()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void Awake()
        {
            _lss = GetComponent<LSS_LoadingScreen>();
            if (_lss == null) return;

            // Clear LSS's own list so it doesn't override the localized typewriter text.
            _lss.hintList       = new List<string>();
            _lss.changeHintWithTimer = false;
            SetPrivateBool(_lss, "enableRandomHints", false);
            ApplyLoadingText(GameIdentity.DisplayName, string.Empty, string.Empty);
            if (_lss.hintsText != null) _lss.hintsText.text = string.Empty;

            // LSS blocks auto-activation at 0.9f, while the bridge keeps the prompt
            // inline with the tip/image page instead of using LSS's separate PAK page.
            _lss.waitForPlayerInput = true;
            _lss.useCountdown       = false;
            _lss.keepPressAnyKeyOnContentPage = true;
            SetPrivateBool(_lss, "enablePressAnyKey", true);
            HidePakPanel(_lss);
        }

        private IEnumerator Start()
        {
            if (_lss == null || _lss.hintsText == null) yield break;

            yield return WaitForLocalizationReady();
            ApplyLocalizedText(chooseNewTip: true);
            _lss.hintsText.text = string.Empty;
            StartCoroutine(TypewriterRoutine());
        }

        private static IEnumerator WaitForLocalizationReady()
        {
            bool unityLocalizationAvailable = UnityLocalizationBridge.Initialize();
            GameLocalization.Initialize();

            if (!unityLocalizationAvailable)
                yield break;

            while (!UnityLocalizationBridge.IsInitializationComplete)
                yield return null;
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            if (_lss == null)
                return;

            bool hadContinueHint = _continueHintShown;
            ApplyLocalizedText(chooseNewTip: false);

            if (_lss.hintsText == null || !_typewriterDone)
                return;

            _continueHintShown = false;
            _lss.hintsText.text = _chosenTip;
            if (hadContinueHint || IsReadyToContinue())
                AppendContinueHint();
        }

        private void ApplyLocalizedText(bool chooseNewTip)
        {
            if (_lss == null)
                return;

            ApplyLoadingText(
                GameLocalization.Get("menu.title"),
                GameLocalization.Get("loading.status"),
                GameLocalization.Get("loading.continue"));

            if (chooseNewTip || string.IsNullOrEmpty(_chosenTip))
                ChooseLocalizedTip();
            else
                RefreshChosenTipText();
        }

        private void ApplyLoadingText(string title, string status, string continueText)
        {
            if (_lss == null)
                return;

            _lss.titleObjText = title;
            _lss.titleObjDescText = status;
            _lss.pakText = continueText;

            if (_lss.titleObj != null) _lss.titleObj.text = title;
            if (_lss.descriptionObj != null) _lss.descriptionObj.text = status;
            if (_lss.pakTextObj != null) _lss.pakTextObj.text = continueText;
        }

        private void Update()
        {
            if (_lss == null || _lss.loadingProcess == null) return;
            if (_lss.loadingProcess.allowSceneActivation) return; // already activating

            bool pressed = WasAnyInputPressed();
            bool readyToContinue = IsReadyToContinue();

            bool pakPromptVisible =
                _lss.isPAKFadeInRunning ||
                _lss.isPAKFadeInCompleted ||
                (_lss.pakCanvasGroup != null && _lss.pakCanvasGroup.alpha > 0.01f);

            bool contentStillVisible =
                _lss.contentCanvasGroup != null &&
                _lss.contentCanvasGroup.alpha > 0.1f;

            // First press while the tip is still typing snaps the text into place.
            // The inline continue prompt requires a later press after it is visible.
            if (pressed && !_typewriterDone && !pakPromptVisible && contentStillVisible)
            {
                CompleteTypewriter(readyToContinue);
                return;
            }

            bool continueWasVisible = _continueHintShown;
            if (readyToContinue && _typewriterDone)
                AppendContinueHint();

            if (!pressed || !continueWasVisible)
                return;

            // Second press, or the first press on the PAK page, activates and dismisses.
            ActivateSceneAndDismissLoadingScreen();
        }

        private void ActivateSceneAndDismissLoadingScreen()
        {
            _lss.loadingProcess.allowSceneActivation = true;

            if (_lss.canvasGroup != null)
            {
                _lss.canvasGroup.interactable   = false;
                _lss.canvasGroup.blocksRaycasts = false;
            }

            bool pakPromptVisible =
                _lss.isPAKFadeInRunning ||
                _lss.isPAKFadeInCompleted ||
                (_lss.pakCanvasGroup != null && _lss.pakCanvasGroup.alpha > 0.01f);

            _lss.StopCoroutine("FadeOutBackgroundScreen");
            _lss.StartCoroutine("FadeOutBackgroundScreen");

            if (pakPromptVisible)
            {
                _lss.StopCoroutine("FadeInPAKScreen");
                _lss.StopCoroutine("FadeOutPAKScreen");
                _lss.StartCoroutine("FadeOutPAKScreen", true);
            }
            else
            {
                _lss.StopCoroutine("FadeInContentScreen");
                _lss.StopCoroutine("FadeOutContentScreen");
                _lss.StartCoroutine("FadeOutContentScreen", true);
            }
        }

        private IEnumerator TypewriterRoutine()
        {
            yield return new WaitForSecondsRealtime(TypewriterStartDelay);

            foreach (char c in _chosenTip)
            {
                if (_lss == null || _lss.hintsText == null) yield break;
                _lss.hintsText.text += c;
                yield return new WaitForSecondsRealtime(TypewriterCharDelay);
            }

            _typewriterDone = true;
            if (IsReadyToContinue())
                AppendContinueHint();
        }

        private void CompleteTypewriter(bool showContinueHint)
        {
            StopAllCoroutines();
            if (_lss.hintsText != null) _lss.hintsText.text = _chosenTip;
            _typewriterDone = true;

            if (showContinueHint)
                AppendContinueHint();
        }

        private void AppendContinueHint()
        {
            if (_lss == null || _lss.hintsText == null) return;
            if (_continueHintShown) return;

            string hint = GameLocalization.GetOptional("loading.continue", "Tap to continue");
            if (!string.IsNullOrEmpty(hint))
            {
                _lss.hintsText.text += "\n\n" + hint;
                _continueHintShown = true;
            }
        }

        private bool IsReadyToContinue()
        {
            return _lss != null &&
                   _lss.loadingProcess != null &&
                   _lss.loadingProcess.progress >= 0.9f;
        }

        private static bool WasAnyInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool pointer  = UnityEngine.InputSystem.Pointer.current?.press.wasPressedThisFrame ?? false;
            bool mouse    = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            bool touch    = Touchscreen.current?.primaryTouch.press.wasPressedThisFrame ?? false;
            bool pen      = Pen.current?.tip.wasPressedThisFrame ?? false;
            bool keyboard = Keyboard.current?.anyKey.wasPressedThisFrame ?? false;
            bool gamepad  =
                (Gamepad.current?.buttonSouth.wasPressedThisFrame ?? false) ||
                (Gamepad.current?.startButton.wasPressedThisFrame ?? false);

            return pointer || mouse || touch || pen || keyboard || gamepad;
#else
            return Input.anyKeyDown;
#endif
        }

        private static void HidePakPanel(LSS_LoadingScreen lss)
        {
            CanvasGroup cg = lss.pakCanvasGroup;
            if (cg == null)
            {
                var field = typeof(LSS_LoadingScreen).GetField(
                    "pakCanvasGroup",
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                cg = field?.GetValue(lss) as CanvasGroup;
            }

            if (cg == null)
                return;

            cg.alpha          = 0f;
            cg.interactable   = false;
            cg.blocksRaycasts = false;
        }

        private static void SetPrivateBool(LSS_LoadingScreen lss, string fieldName, bool value)
        {
            var field = typeof(LSS_LoadingScreen).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(lss, value);
        }

        private void ChooseLocalizedTip()
        {
            List<string> keys = BuildAvailableTipKeys();
            if (keys.Count == 0)
            {
                _chosenTipKey = null;
                _chosenTip = DefaultTip;
                return;
            }

            _chosenTipKey = keys[UnityEngine.Random.Range(0, keys.Count)];
            RefreshChosenTipText();
        }

        private void RefreshChosenTipText()
        {
            if (!string.IsNullOrEmpty(_chosenTipKey))
                _chosenTip = GameLocalization.GetOptional(_chosenTipKey, DefaultTip);

            if (string.IsNullOrEmpty(_chosenTip))
                _chosenTip = DefaultTip;
        }

        private static List<string> BuildAvailableTipKeys()
        {
            var list = new List<string>(TipKeys.Length);
            foreach (string key in TipKeys)
            {
                string text = GameLocalization.GetOptional(key, string.Empty);
                if (!string.IsNullOrEmpty(text) && !string.Equals(text, key, StringComparison.Ordinal))
                    list.Add(key);
            }

            return list;
        }
    }
}
