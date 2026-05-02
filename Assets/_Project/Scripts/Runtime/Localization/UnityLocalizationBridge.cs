using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Markyu.LastKernel
{
    public static class UnityLocalizationBridge
    {
        public const string DefaultStringTable = "GameText";
        public const string EnglishLocaleCode = "en";

        private static readonly string[] SupportedLocaleCodes =
        {
            "en",
            "zh-Hans",
            "zh-Hant",
            "ja",
            "ko",
            "fr",
            "de",
            "es"
        };

        private static readonly Dictionary<string, string> LocaleAliases = new(StringComparer.OrdinalIgnoreCase)
        {
            ["en"] = "en",
            ["en-US"] = "en",
            ["en-CA"] = "en",
            ["en-GB"] = "en",
            ["zh"] = "zh-Hans",
            ["zh-Hans"] = "zh-Hans",
            ["zh-CN"] = "zh-Hans",
            ["zh-SG"] = "zh-Hans",
            ["zh-Hant"] = "zh-Hant",
            ["zh-TW"] = "zh-Hant",
            ["zh-HK"] = "zh-Hant",
            ["ja"] = "ja",
            ["ja-JP"] = "ja",
            ["ko"] = "ko",
            ["ko-KR"] = "ko",
            ["fr"] = "fr",
            ["fr-FR"] = "fr",
            ["fr-CA"] = "fr",
            ["de"] = "de",
            ["de-DE"] = "de",
            ["es"] = "es",
            ["es-ES"] = "es",
            ["es-MX"] = "es"
        };

        private static bool initialized;
        private static bool warningLogged;
        private static string cachedLocaleCode;
        private static Dictionary<string, string> cachedStrings;

        public static event Action LocaleChanged;

        public static string CurrentLocaleCode => NormalizeLocaleCode(LocalizationSettings.SelectedLocale?.Identifier.Code) ?? EnglishLocaleCode;

        public static bool IsInitializationComplete =>
            LocalizationSettings.HasSettings && LocalizationSettings.InitializationOperation.IsDone;

        public static CultureInfo CurrentCulture
        {
            get
            {
                Locale locale = LocalizationSettings.SelectedLocale;
                return locale?.Identifier.CultureInfo ?? CultureInfo.GetCultureInfo("en-US");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Initialize();
        }

        public static bool Initialize()
        {
            if (initialized)
                return true;

            if (!LocalizationSettings.HasSettings)
            {
                LogMissingSettingsWarning();
                return false;
            }

            initialized = true;
            LocalizationSettings.SelectedLocaleChanged += HandleSelectedLocaleChanged;

            var initOp = LocalizationSettings.InitializationOperation;
            if (initOp.IsDone)
                ApplyStartupLocale();
            else
                initOp.Completed += _ => ApplyStartupLocale();

            return true;
        }

        public static IReadOnlyList<string> GetAvailableLocaleCodes()
        {
            if (!Initialize())
                return SupportedLocaleCodes;

            var available = new List<string>();
            foreach (string supportedCode in SupportedLocaleCodes)
            {
                if (FindAvailableLocale(supportedCode) != null)
                {
                    available.Add(supportedCode);
                }
            }

            return available.Count > 0 ? available : SupportedLocaleCodes;
        }

        public static bool SetLocaleByCode(string localeCode, bool force = false)
        {
            if (!Initialize())
                return false;

            Locale locale = FindAvailableLocale(localeCode) ?? FindAvailableLocale(EnglishLocaleCode);
            if (locale == null)
                return false;

            string normalizedCode = NormalizeLocaleCode(locale.Identifier.Code) ?? EnglishLocaleCode;
            Locale selectedLocale = LocalizationSettings.SelectedLocale;
            bool isSameLocale = selectedLocale != null && selectedLocale.Identifier == locale.Identifier;

            SaveLocaleCode(normalizedCode);

            if (isSameLocale)
            {
                if (force)
                {
                    ClearStringCache();
                    LocalizationSettings.Instance.ForceRefresh();
                    LocaleChanged?.Invoke();
                }

                return true;
            }

            LocalizationSettings.SelectedLocale = locale;
            return true;
        }

        public static void CycleLocale()
        {
            IReadOnlyList<string> localeCodes = GetAvailableLocaleCodes();
            if (localeCodes.Count == 0)
                return;

            string currentCode = CurrentLocaleCode;
            int currentIndex = -1;
            for (int i = 0; i < localeCodes.Count; i++)
            {
                if (string.Equals(localeCodes[i], currentCode, StringComparison.OrdinalIgnoreCase))
                {
                    currentIndex = i;
                    break;
                }
            }

            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % localeCodes.Count;
            SetLocaleByCode(localeCodes[nextIndex]);
        }

        public static bool TryGetString(string key, out string localized)
        {
            localized = null;

            if (string.IsNullOrWhiteSpace(key) || !Initialize() || !EnsureStringCache())
                return false;

            return cachedStrings.TryGetValue(key, out localized) && !string.IsNullOrEmpty(localized);
        }

        public static bool TryGetString(string key, string fallback, out string localized)
        {
            if (TryGetString(key, out localized))
                return true;

            localized = fallback;
            return false;
        }

        public static string NormalizeLocaleCode(string localeCode)
        {
            if (string.IsNullOrWhiteSpace(localeCode))
                return null;

            string normalized = localeCode.Trim().Replace('_', '-');
            if (LocaleAliases.TryGetValue(normalized, out string canonical))
                return canonical;

            int regionSeparatorIndex = normalized.IndexOf('-');
            if (regionSeparatorIndex > 0 &&
                LocaleAliases.TryGetValue(normalized.Substring(0, regionSeparatorIndex), out canonical))
            {
                return canonical;
            }

            return normalized;
        }

        private static void ApplyStartupLocale()
        {
            string savedCode = LoadSavedLocaleCode();
            if (!SetLocaleByCode(savedCode))
            {
                SetLocaleByCode(EnglishLocaleCode, force: true);
            }
        }

        private static void HandleSelectedLocaleChanged(Locale locale)
        {
            string localeCode = NormalizeLocaleCode(locale?.Identifier.Code) ?? EnglishLocaleCode;
            SaveLocaleCode(localeCode);
            ClearStringCache();
            LocaleChanged?.Invoke();
        }

        private static bool EnsureStringCache()
        {
            string selectedCode = CurrentLocaleCode;
            if (cachedStrings != null && string.Equals(cachedLocaleCode, selectedCode, StringComparison.OrdinalIgnoreCase))
                return true;

            cachedStrings = null;
            cachedLocaleCode = null;

            StringTable table = LoadStringTableForSelectedLocale() ?? LoadStringTable(EnglishLocaleCode);
            if (table == null)
                return false;

            var strings = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (StringTableEntry entry in table.Values)
            {
                if (entry == null || entry.SharedEntry == null || string.IsNullOrWhiteSpace(entry.SharedEntry.Key))
                    continue;

                strings[entry.SharedEntry.Key] = entry.Value;
            }

            cachedLocaleCode = selectedCode;
            cachedStrings = strings;
            return true;
        }

        private static StringTable LoadStringTableForSelectedLocale()
        {
            Locale selectedLocale = LocalizationSettings.SelectedLocale;
            if (selectedLocale == null)
                return null;

            try
            {
                return LocalizationSettings.StringDatabase.GetTable(DefaultStringTable, selectedLocale);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"UnityLocalizationBridge: Unable to load '{DefaultStringTable}' for locale '{selectedLocale.Identifier.Code}'. {exception.Message}");
                return null;
            }
        }

        private static StringTable LoadStringTable(string localeCode)
        {
            Locale locale = FindAvailableLocale(localeCode);
            if (locale == null)
                return null;

            try
            {
                return LocalizationSettings.StringDatabase.GetTable(DefaultStringTable, locale);
            }
            catch (Exception exception)
            {
                Debug.LogWarning($"UnityLocalizationBridge: Unable to load '{DefaultStringTable}' for locale '{localeCode}'. {exception.Message}");
                return null;
            }
        }

        private static void ClearStringCache()
        {
            cachedLocaleCode = null;
            cachedStrings = null;
        }

        private static Locale FindAvailableLocale(string localeCode)
        {
            string normalizedCode = NormalizeLocaleCode(localeCode);
            if (string.IsNullOrWhiteSpace(normalizedCode) || LocalizationSettings.AvailableLocales == null)
                return null;

            if (!LocalizationSettings.InitializationOperation.IsDone)
                return null;

            foreach (Locale locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale == null)
                    continue;

                string candidateCode = NormalizeLocaleCode(locale.Identifier.Code);
                if (string.Equals(candidateCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
                {
                    return locale;
                }
            }

            return null;
        }

        private static string LoadSavedLocaleCode()
        {
            if (PlayerPrefs.HasKey(GameIdentity.LocaleCodePlayerPrefsKey))
            {
                string savedCode = PlayerPrefs.GetString(GameIdentity.LocaleCodePlayerPrefsKey, string.Empty);
                if (!string.IsNullOrWhiteSpace(savedCode))
                {
                    return savedCode;
                }
            }

            if (PlayerPrefs.HasKey(GameIdentity.LanguagePlayerPrefsKey))
            {
                int savedLanguage = PlayerPrefs.GetInt(GameIdentity.LanguagePlayerPrefsKey, (int)GameLanguage.English);
                if (Enum.IsDefined(typeof(GameLanguage), savedLanguage))
                {
                    return GameLocalization.GetLocaleCode((GameLanguage)savedLanguage);
                }
            }

            if (PlayerPrefs.HasKey(GameIdentity.LegacyLanguagePlayerPrefsKey))
            {
                int savedLanguage = PlayerPrefs.GetInt(GameIdentity.LegacyLanguagePlayerPrefsKey, (int)GameLanguage.English);
                if (Enum.IsDefined(typeof(GameLanguage), savedLanguage))
                {
                    return GameLocalization.GetLocaleCode((GameLanguage)savedLanguage);
                }
            }

            return EnglishLocaleCode;
        }

        private static void SaveLocaleCode(string localeCode)
        {
            string normalizedCode = NormalizeLocaleCode(localeCode) ?? EnglishLocaleCode;
            PlayerPrefs.SetString(GameIdentity.LocaleCodePlayerPrefsKey, normalizedCode);
            PlayerPrefs.Save();
        }

        private static void LogMissingSettingsWarning()
        {
            if (warningLogged)
                return;

            warningLogged = true;
            Debug.LogWarning("UnityLocalizationBridge: Unity Localization settings are not configured. Falling back to legacy GameLocalization entries.");
        }
    }
}
