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

        private static readonly string[] TipKeys =
        {
            "loading.tip.01", "loading.tip.02", "loading.tip.03", "loading.tip.04",
            "loading.tip.05", "loading.tip.06", "loading.tip.07", "loading.tip.08",
            "loading.tip.09", "loading.tip.10", "loading.tip.11", "loading.tip.12",
            "loading.tip.13", "loading.tip.14", "loading.tip.15", "loading.tip.16",
        };

        private LSS_LoadingScreen _lss;
        private string            _chosenTip;
        private bool              _typewriterDone;

        private void Awake()
        {
            _lss = GetComponent<LSS_LoadingScreen>();
            if (_lss == null) return;

            // Title / status
            _lss.titleObjText    = GameLocalization.Get("menu.title");
            _lss.titleObjDescText = GameLocalization.Get("loading.status");

            // Pick one tip; clear LSS's own list so it doesn't override hintsText
            var tips = BuildLocalizedTips();
            _chosenTip          = tips[Random.Range(0, tips.Count)];
            _lss.hintList       = new List<string>();   // prevent LSS from writing to hintsText
            _lss.changeHintWithTimer = false;
            SetPrivateBool(_lss, "enableRandomHints", false);

            // enablePressAnyKey must be true so LSS blocks auto-activation at 0.9f progress.
            // We hide the PAK overlay by zeroing its CanvasGroup; the inline hint we append
            // to the tip text takes its place on the same page.
            _lss.waitForPlayerInput = true;
            _lss.useCountdown       = false;
            SetPrivateBool(_lss, "enablePressAnyKey", true);
            HidePakPanel(_lss);
        }

        private void Start()
        {
            if (_lss == null || _lss.hintsText == null) return;

            _lss.hintsText.text = string.Empty;
            StartCoroutine(TypewriterRoutine());
        }

        private void Update()
        {
            if (_lss == null || _lss.loadingProcess == null) return;
            if (_lss.loadingProcess.allowSceneActivation) return; // already activating
            if (_lss.loadingProcess.progress < 0.9f) return;       // still loading

            bool pressed = WasAnyInputPressed();
            if (!pressed) return;

            // First press while typewriter is still running → snap text, require a second press
            if (!_typewriterDone)
            {
                StopAllCoroutines();
                if (_lss.hintsText != null) _lss.hintsText.text = _chosenTip;
                _typewriterDone = true;
                AppendContinueHint();
                return;
            }

            // Second press (or first if typewriter already finished) → activate scene
            _lss.loadingProcess.allowSceneActivation = true;
            _lss.canvasGroup.interactable   = false;
            _lss.canvasGroup.blocksRaycasts = false;
            _lss.StopCoroutine("FadeOutBackgroundScreen");
            _lss.StartCoroutine("FadeOutBackgroundScreen");
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
            AppendContinueHint();
        }

        private void AppendContinueHint()
        {
            if (_lss == null || _lss.hintsText == null) return;
            string hint = GameLocalization.Get("loading.continue");
            if (!string.IsNullOrEmpty(hint))
                _lss.hintsText.text += "\n\n" + hint;
        }

        private static bool WasAnyInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            bool pointer  = UnityEngine.InputSystem.Pointer.current?.press.wasPressedThisFrame ?? false;
            bool keyboard = UnityEngine.InputSystem.Keyboard.current?.anyKey.wasPressedThisFrame ?? false;
            return pointer || keyboard;
#else
            return Input.anyKeyDown;
#endif
        }

        private static void HidePakPanel(LSS_LoadingScreen lss)
        {
            var field = typeof(LSS_LoadingScreen).GetField("pakCanvasGroup", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field?.GetValue(lss) is CanvasGroup cg)
            {
                cg.alpha          = 0f;
                cg.interactable   = false;
                cg.blocksRaycasts = false;
            }
        }

        private static void SetPrivateBool(LSS_LoadingScreen lss, string fieldName, bool value)
        {
            var field = typeof(LSS_LoadingScreen).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(lss, value);
        }

        private static List<string> BuildLocalizedTips()
        {
            var list = new List<string>(TipKeys.Length);
            foreach (string key in TipKeys)
            {
                string text = GameLocalization.Get(key);
                if (!string.IsNullOrEmpty(text))
                    list.Add(text);
            }
            return list.Count > 0 ? list : new List<string> { "Hold the bunker." };
        }
    }
}
