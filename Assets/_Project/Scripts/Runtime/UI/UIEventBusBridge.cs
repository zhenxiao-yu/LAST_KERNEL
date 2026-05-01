using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Subscribes to UIEventBus and routes intent events to the appropriate
    /// game systems (GameDirector, GameLocalization, etc.).
    ///
    /// Add one instance to the MainMenu scene and one to the Game scene.
    /// This is the only component allowed to call game-system singletons in
    /// response to UI events — keeps UIToolkit controllers free of runtime deps.
    /// </summary>
    public sealed class UIEventBusBridge : MonoBehaviour
    {
        private void OnEnable()
        {
            UIEventBus.OnStartNewGame           += HandleStartNewGame;
            UIEventBus.OnLoadGame               += HandleLoadGame;
            UIEventBus.OnDeleteGame             += HandleDeleteGame;
            UIEventBus.OnClearAllSaves          += HandleClearAllSaves;
            UIEventBus.OnLanguageCycleRequested  += HandleLanguageCycle;
            UIEventBus.OnLanguageSelectRequested  += HandleLanguageSelect;
            UIEventBus.OnSettingsResetRequested   += HandleSettingsReset;
            UIEventBus.OnStartNightRequested    += HandleStartNight;
            UIEventBus.OnSaveRequested          += HandleSave;
            UIEventBus.OnSaveToSlotRequested    += HandleSaveToSlot;
            UIEventBus.OnOptionsRequested       += HandleOptions;
            UIEventBus.OnBackToTitleRequested   += HandleBackToTitle;
            UIEventBus.OnContinueToDayRequested += HandleContinueToDay;
            UIEventBus.OnRetryRequested         += HandleRetry;
        }

        private void OnDisable()
        {
            UIEventBus.OnStartNewGame           -= HandleStartNewGame;
            UIEventBus.OnLoadGame               -= HandleLoadGame;
            UIEventBus.OnDeleteGame             -= HandleDeleteGame;
            UIEventBus.OnClearAllSaves          -= HandleClearAllSaves;
            UIEventBus.OnLanguageCycleRequested  -= HandleLanguageCycle;
            UIEventBus.OnLanguageSelectRequested  -= HandleLanguageSelect;
            UIEventBus.OnSettingsResetRequested   -= HandleSettingsReset;
            UIEventBus.OnStartNightRequested    -= HandleStartNight;
            UIEventBus.OnSaveRequested          -= HandleSave;
            UIEventBus.OnSaveToSlotRequested    -= HandleSaveToSlot;
            UIEventBus.OnOptionsRequested       -= HandleOptions;
            UIEventBus.OnBackToTitleRequested   -= HandleBackToTitle;
            UIEventBus.OnContinueToDayRequested -= HandleContinueToDay;
            UIEventBus.OnRetryRequested         -= HandleRetry;
        }

        private static void HandleStartNewGame(GameplayPrefs prefs) => GameDirector.Instance?.NewGame(prefs);
        private static void HandleLoadGame(GameData data)           => GameDirector.Instance?.LoadGame(data);
        private static void HandleDeleteGame(GameData data)         => GameDirector.Instance?.DeleteGame(data);
        private static void HandleStartNight()                      => DefensePhaseController.Instance?.StartNight();
        private static void HandleSave()                            => GameDirector.Instance?.SaveGame();
        private static void HandleSaveToSlot(int slot)              => GameDirector.Instance?.SaveToSlot(slot);
        private static void HandleOptions()                         => GameOptionsUI.Instance?.Open();
        private static void HandleBackToTitle()                     => GameDirector.Instance?.BackToTitle();
        private static void HandleContinueToDay()                   => DefensePhaseController.Instance?.ReturnToDay();

        private static void HandleRetry()
        {
            Time.timeScale = 1f;
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private static void HandleClearAllSaves()
        {
            GameDirector director = GameDirector.Instance;
            if (director == null) return;
            var snapshot = new List<GameData>(director.SavedGames.Values);
            foreach (GameData save in snapshot)
                director.DeleteGame(save);
        }

        private static void HandleLanguageCycle()                    => GameLocalization.CycleLanguage();
        private static void HandleLanguageSelect(GameLanguage lang)   => GameLocalization.SetLanguage(lang);

        private static void HandleSettingsReset()
        {
            string localeCode = UnityLocalizationBridge.CurrentLocaleCode;
            PlayerPrefs.DeleteAll();
            GameLocalization.SetLanguageByCode(localeCode, force: true);
            GraphicsManager.Instance?.InitGraphicsSettings();
            AudioManager.Instance?.InitAudioMixerVolumes();
        }
    }
}
