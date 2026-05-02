using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static partial class GameLocalization
    {
        private const GameLanguage DefaultLanguage = GameLanguage.English;

        private static bool isInitialized;
        private static bool isSubscribedToUnityLocalization;

        private static readonly GameLanguage[] LanguageCycle =
        {
            GameLanguage.SimplifiedChinese,
            GameLanguage.English,
            GameLanguage.TraditionalChinese,
            GameLanguage.Japanese,
            GameLanguage.Korean,
            GameLanguage.French,
            GameLanguage.German,
            GameLanguage.Spanish
        };

        private static readonly Dictionary<string, GameLanguage> LanguageByCode = new(StringComparer.OrdinalIgnoreCase)
        {
            ["zh"] = GameLanguage.SimplifiedChinese,
            ["zh-Hans"] = GameLanguage.SimplifiedChinese,
            ["zh-CN"] = GameLanguage.SimplifiedChinese,
            ["zh-SG"] = GameLanguage.SimplifiedChinese,
            ["zh-Hant"] = GameLanguage.TraditionalChinese,
            ["zh-TW"] = GameLanguage.TraditionalChinese,
            ["zh-HK"] = GameLanguage.TraditionalChinese,
            ["en"] = GameLanguage.English,
            ["en-US"] = GameLanguage.English,
            ["en-CA"] = GameLanguage.English,
            ["en-GB"] = GameLanguage.English,
            ["ja"] = GameLanguage.Japanese,
            ["ja-JP"] = GameLanguage.Japanese,
            ["ko"] = GameLanguage.Korean,
            ["ko-KR"] = GameLanguage.Korean,
            ["fr"] = GameLanguage.French,
            ["fr-FR"] = GameLanguage.French,
            ["fr-CA"] = GameLanguage.French,
            ["de"] = GameLanguage.German,
            ["de-DE"] = GameLanguage.German,
            ["es"] = GameLanguage.Spanish,
            ["es-ES"] = GameLanguage.Spanish,
            ["es-MX"] = GameLanguage.Spanish
        };

        public static event Action<GameLanguage> LanguageChanged;

        public static IReadOnlyList<GameLanguage> AvailableLanguages => LanguageCycle;

        public static IReadOnlyCollection<string> LegacyKeys => TextEntries.Keys;

        public static GameLanguage CurrentLanguage { get; private set; } = DefaultLanguage;

        public static CultureInfo CurrentCulture
        {
            get
            {
                if (UnityLocalizationBridge.Initialize())
                {
                    return UnityLocalizationBridge.CurrentCulture;
                }

                return CurrentLanguage switch
                {
                    GameLanguage.English => CultureInfo.GetCultureInfo("en-US"),
                    GameLanguage.SimplifiedChinese => CultureInfo.GetCultureInfo("zh-CN"),
                    GameLanguage.TraditionalChinese => CultureInfo.GetCultureInfo("zh-TW"),
                    GameLanguage.Japanese => CultureInfo.GetCultureInfo("ja-JP"),
                    GameLanguage.Korean => CultureInfo.GetCultureInfo("ko-KR"),
                    GameLanguage.French => CultureInfo.GetCultureInfo("fr-FR"),
                    GameLanguage.German => CultureInfo.GetCultureInfo("de-DE"),
                    GameLanguage.Spanish => CultureInfo.GetCultureInfo("es-ES"),
                    _ => CultureInfo.GetCultureInfo("en-US")
                };
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            Initialize();
        }

        public static void Initialize()
        {
            if (isInitialized)
                return;

            isInitialized = true;
            SubscribeToUnityLocalization();

            if (UnityLocalizationBridge.Initialize())
            {
                if (UnityLocalizationBridge.IsInitializationComplete)
                {
                    SyncCurrentLanguageFromUnity(savePreference: true, notify: false);
                }
                else
                {
                    CurrentLanguage = LoadPreferredLanguage();
                }

                return;
            }

            CurrentLanguage = LoadPreferredLanguage();
        }

        public static void SetLanguage(GameLanguage language, bool force = false)
        {
            Initialize();

            if (!force && CurrentLanguage == language)
                return;

            if (UnityLocalizationBridge.SetLocaleByCode(GetLocaleCode(language), force))
            {
                if (CurrentLanguage != language)
                {
                    CurrentLanguage = language;
                    SaveLanguagePreference(language);
                    LanguageChanged?.Invoke(language);
                }

                return;
            }

            CurrentLanguage = language;
            SaveLanguagePreference(language);
            LanguageChanged?.Invoke(language);
        }

        public static void CycleLanguage()
        {
            Initialize();
            int currentIndex = Array.IndexOf(LanguageCycle, CurrentLanguage);
            int nextIndex = currentIndex < 0 ? 0 : (currentIndex + 1) % LanguageCycle.Length;
            SetLanguage(LanguageCycle[nextIndex]);
        }

        public static bool SetLanguageByCode(string localeCode, bool force = false)
        {
            if (!TryGetLanguageFromCode(localeCode, out GameLanguage language))
            {
                Debug.LogWarning($"GameLocalization: Unsupported locale code '{localeCode}'.");
                return false;
            }

            SetLanguage(language, force);
            return true;
        }

        public static bool TryGetLanguageFromCode(string localeCode, out GameLanguage language)
        {
            language = DefaultLanguage;

            if (string.IsNullOrWhiteSpace(localeCode))
            {
                return false;
            }

            string normalizedCode = localeCode.Trim().Replace('_', '-');
            if (LanguageByCode.TryGetValue(normalizedCode, out language))
            {
                return true;
            }

            int regionSeparatorIndex = normalizedCode.IndexOf('-');
            if (regionSeparatorIndex > 0 &&
                LanguageByCode.TryGetValue(normalizedCode.Substring(0, regionSeparatorIndex), out language))
            {
                return true;
            }

            return false;
        }

        public static string GetLocaleCode(GameLanguage language)
        {
            return language switch
            {
                GameLanguage.English => "en",
                GameLanguage.SimplifiedChinese => "zh-Hans",
                GameLanguage.TraditionalChinese => "zh-Hant",
                GameLanguage.Japanese => "ja",
                GameLanguage.Korean => "ko",
                GameLanguage.French => "fr",
                GameLanguage.German => "de",
                GameLanguage.Spanish => "es",
                _ => "en"
            };
        }

        public static string Get(string key)
        {
            Initialize();

            if (UnityLocalizationBridge.TryGetString(key, out string localized))
            {
                return localized;
            }

            if (TextEntries.TryGetValue(key, out LocalizedTextEntry entry))
            {
                return entry.GetText(CurrentLanguage);
            }

            Debug.LogWarning($"GameLocalization: Missing key '{key}'.");
            return key;
        }

        public static string GetOptional(string key, string fallback)
        {
            Initialize();

            if (UnityLocalizationBridge.TryGetString(key, out string localized))
            {
                return localized;
            }

            if (TextEntries.TryGetValue(key, out LocalizedTextEntry entry))
            {
                return entry.GetText(CurrentLanguage);
            }

            return fallback;
        }

        public static bool TryGetLegacyText(string key, GameLanguage language, out string text)
        {
            if (TextEntries.TryGetValue(key, out LocalizedTextEntry entry))
            {
                text = entry.GetText(language);
                return true;
            }

            text = null;
            return false;
        }

        public static string Format(string key, params object[] args)
        {
            return string.Format(CurrentCulture, Get(key), args);
        }

        public static string GetCurrentLanguageDisplayName()
        {
            return CurrentLanguage switch
            {
                GameLanguage.English => Get("language.english"),
                GameLanguage.SimplifiedChinese => Get("language.chinese"),
                GameLanguage.TraditionalChinese => Get("language.traditionalChinese"),
                GameLanguage.Japanese => Get("language.japanese"),
                GameLanguage.Korean => Get("language.korean"),
                GameLanguage.French => Get("language.french"),
                GameLanguage.German => Get("language.german"),
                GameLanguage.Spanish => Get("language.spanish"),
                _ => Get("language.english")
            };
        }

        public static string GetLanguageButtonLabel()
        {
            return Format("language.label", GetCurrentLanguageDisplayName());
        }

        public static string GetRecipeCategoryLabel(RecipeCategory category)
        {
            return category switch
            {
                RecipeCategory.Misc => Get("recipe.category.misc"),
                RecipeCategory.Gathering => Get("recipe.category.gathering"),
                RecipeCategory.Construction => Get("recipe.category.construction"),
                RecipeCategory.Cooking => Get("recipe.category.cooking"),
                RecipeCategory.Forging => Get("recipe.category.forging"),
                RecipeCategory.Refining => Get("recipe.category.refining"),
                RecipeCategory.Husbandry => Get("recipe.category.husbandry"),
                _ => category.ToString()
            };
        }

        public static bool TryTranslateInspectorText(string source, out string translated)
        {
            Initialize();

            translated = null;

            if (string.IsNullOrEmpty(source))
                return false;

            if (InspectorTextKeys.TryGetValue(source, out string key))
            {
                translated = Get(key);
                return true;
            }

            return false;
        }

        private static void SubscribeToUnityLocalization()
        {
            if (isSubscribedToUnityLocalization)
                return;

            UnityLocalizationBridge.LocaleChanged += HandleUnityLocaleChanged;
            isSubscribedToUnityLocalization = true;
        }

        private static void HandleUnityLocaleChanged()
        {
            SyncCurrentLanguageFromUnity(savePreference: true, notify: true);
        }

        private static void SyncCurrentLanguageFromUnity(bool savePreference, bool notify)
        {
            string localeCode = UnityLocalizationBridge.CurrentLocaleCode;
            if (!TryGetLanguageFromCode(localeCode, out GameLanguage language))
            {
                language = DefaultLanguage;
            }

            bool changed = CurrentLanguage != language;
            CurrentLanguage = language;

            if (savePreference)
            {
                SaveLanguagePreference(language);
            }

            if (notify && changed)
            {
                LanguageChanged?.Invoke(language);
            }
            else if (notify && !changed)
            {
                LanguageChanged?.Invoke(language);
            }
        }

        private static void SaveLanguagePreference(GameLanguage language)
        {
            PlayerPrefs.SetString(GameIdentity.LocaleCodePlayerPrefsKey, GetLocaleCode(language));
            PlayerPrefs.SetInt(GameIdentity.LanguagePlayerPrefsKey, (int)language);
            PlayerPrefs.Save();
        }

        private static GameLanguage LoadPreferredLanguage()
        {
            if (PlayerPrefs.HasKey(GameIdentity.LocaleCodePlayerPrefsKey))
            {
                string savedCode = PlayerPrefs.GetString(GameIdentity.LocaleCodePlayerPrefsKey, string.Empty);
                if (TryGetLanguageFromCode(savedCode, out GameLanguage language))
                {
                    return language;
                }
            }

            int savedValue = LoadLanguagePreference();
            return Enum.IsDefined(typeof(GameLanguage), savedValue)
                ? (GameLanguage)savedValue
                : DefaultLanguage;
        }

        private static int LoadLanguagePreference()
        {
            if (PlayerPrefs.HasKey(GameIdentity.LanguagePlayerPrefsKey))
            {
                return PlayerPrefs.GetInt(GameIdentity.LanguagePlayerPrefsKey, (int)DefaultLanguage);
            }

            if (PlayerPrefs.HasKey(GameIdentity.LegacyLanguagePlayerPrefsKey))
            {
                int legacyValue = PlayerPrefs.GetInt(GameIdentity.LegacyLanguagePlayerPrefsKey, (int)DefaultLanguage);
                PlayerPrefs.SetInt(GameIdentity.LanguagePlayerPrefsKey, legacyValue);
                PlayerPrefs.Save();
                return legacyValue;
            }

            return (int)DefaultLanguage;
        }
    }
}
