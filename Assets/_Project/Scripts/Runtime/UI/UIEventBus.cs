using System;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Decoupled command bus: UI Toolkit controllers raise events here;
    /// game systems (GameDirector, DefensePhaseController, etc.) subscribe.
    /// Lives in _Project.Runtime so both sides can reference it without circular dependencies.
    /// </summary>
    public static class UIEventBus
    {
        // ── Title Screen ──────────────────────────────────────────────────────
        public static event Action OnNewGameRequested;
        public static event Action OnLoadGameRequested;
        public static event Action OnOptionsRequested;
        public static event Action OnQuitRequested;
        public static event Action<GameplayPrefs> OnStartNewGame;
        public static event Action<GameData> OnLoadGame;
        public static event Action<GameData> OnDeleteGame;
        public static event Action OnClearAllSaves;

        // ── In-game ───────────────────────────────────────────────────────────
        public static event Action OnSaveRequested;
        public static event Action<int> OnSaveToSlotRequested;
        public static event Action OnResumeRequested;
        public static event Action OnBackToTitleRequested;
        public static event Action OnStartNightRequested;
        public static event Action OnSpeedToggleRequested;
        public static event Action OnContinueToDayRequested;
        public static event Action OnRetryRequested;

        // ── Settings ──────────────────────────────────────────────────────────
        public static event Action OnLanguageCycleRequested;
        public static event Action<GameLanguage> OnLanguageSelectRequested;
        public static event Action OnSettingsResetRequested;

        // ── Raise helpers ─────────────────────────────────────────────────────
        public static void RaiseNewGameRequested()              => OnNewGameRequested?.Invoke();
        public static void RaiseLoadGameRequested()             => OnLoadGameRequested?.Invoke();
        public static void RaiseOptionsRequested()              => OnOptionsRequested?.Invoke();
        public static void RaiseQuitRequested()                 => OnQuitRequested?.Invoke();
        public static void RaiseStartNewGame(GameplayPrefs p)   => OnStartNewGame?.Invoke(p);
        public static void RaiseLoadGame(GameData d)            => OnLoadGame?.Invoke(d);
        public static void RaiseDeleteGame(GameData d)          => OnDeleteGame?.Invoke(d);
        public static void RaiseClearAllSaves()                 => OnClearAllSaves?.Invoke();
        public static void RaiseSaveRequested()                 => OnSaveRequested?.Invoke();
        public static void RaiseSaveToSlotRequested(int slot)   => OnSaveToSlotRequested?.Invoke(slot);
        public static void RaiseResumeRequested()               => OnResumeRequested?.Invoke();
        public static void RaiseBackToTitleRequested()          => OnBackToTitleRequested?.Invoke();
        public static void RaiseStartNightRequested()           => OnStartNightRequested?.Invoke();
        public static void RaiseSpeedToggleRequested()          => OnSpeedToggleRequested?.Invoke();
        public static void RaiseContinueToDayRequested()        => OnContinueToDayRequested?.Invoke();
        public static void RaiseRetryRequested()                => OnRetryRequested?.Invoke();
        public static void RaiseLanguageCycleRequested()              => OnLanguageCycleRequested?.Invoke();
        public static void RaiseLanguageSelectRequested(GameLanguage l) => OnLanguageSelectRequested?.Invoke(l);
        public static void RaiseSettingsResetRequested()               => OnSettingsResetRequested?.Invoke();
    }
}
