using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Centralizes localization event subscription for UI components so
    /// each screen only needs to describe how its visible text is refreshed.
    /// </summary>
    public abstract class LocalizedUIBehaviour : MonoBehaviour
    {
        protected virtual void OnEnable()
        {
            UnityLocalizationBridge.Initialize();
            GameLocalization.Initialize();
            GameLocalization.LanguageChanged += HandleLanguageChanged;
            RefreshLocalizedText();
        }

        protected virtual void OnDisable()
        {
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshLocalizedText();
        }

        protected abstract void RefreshLocalizedText();
    }
}
