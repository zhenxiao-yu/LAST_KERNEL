using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    public sealed class TMPThemeController : MonoBehaviour
    {
        private static TMPThemeController instance;

        private readonly Dictionary<TMP_Text, string> originalTextByInstance = new();

        private GameTypographyProfile typographyProfile;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (instance != null)
                return;

            var controllerObject = new GameObject(nameof(TMPThemeController));
            DontDestroyOnLoad(controllerObject);
            instance = controllerObject.AddComponent<TMPThemeController>();
        }

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }

            instance = this;

            typographyProfile = Resources.Load<GameTypographyProfile>("Typography/GameTypographyProfile");

            UnityLocalizationBridge.Initialize();
            GameLocalization.Initialize();
            SceneManager.sceneLoaded += HandleSceneLoaded;
            GameLocalization.LanguageChanged += HandleLanguageChanged;

            RefreshAllText();
        }

        private void OnDestroy()
        {
            if (instance != this)
                return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
            instance = null;
        }

        private void HandleSceneLoaded(Scene _, LoadSceneMode __) => StartCoroutine(RefreshNextFrame());
        private void HandleLanguageChanged(GameLanguage _) => RefreshAllText();

        private IEnumerator RefreshNextFrame()
        {
            yield return null;
            RefreshAllText();
        }

        private void RefreshAllText()
        {
            // Default font: profile.ui.regular → TMP_Settings.defaultFontAsset
            TMP_FontAsset defaultFont = typographyProfile?.ui.regular ?? TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
                return;

            ConfigureFontFallbacks(defaultFont);

            TMP_Text[] allText = FindObjectsByType<TMP_Text>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (TMP_Text text in allText)
            {
                if (text == null)
                    continue;

                if (!originalTextByInstance.ContainsKey(text))
                    originalTextByInstance[text] = text.text;

                TMP_FontAsset font = ResolveFont(text, defaultFont);
                ApplyFont(text, font);
                ApplyInspectorTranslation(text, originalTextByInstance[text]);
            }
        }

        private TMP_FontAsset ResolveFont(TMP_Text text, TMP_FontAsset defaultFont)
        {
            if (typographyProfile == null)
                return defaultFont;

            if (!text.TryGetComponent<GameTypographyApplier>(out var applier))
                return defaultFont;

            TMP_FontAsset roleFont = typographyProfile.GetFont(applier.role, applier.weight);
            return roleFont != null ? roleFont : defaultFont;
        }

        private void ConfigureFontFallbacks(TMP_FontAsset defaultFont)
        {
            defaultFont.isMultiAtlasTexturesEnabled = true;

            List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();

            // Noto SC emergency fallback — last resort for any missing CJK glyphs
            TMP_FontAsset notoFallback = typographyProfile?.fallbackFont;
            if (notoFallback != null && !fallbackFonts.Contains(notoFallback))
                fallbackFonts.Add(notoFallback);

            TMP_Settings.fallbackFontAssets = fallbackFonts;

            foreach (TMP_FontAsset fallbackFont in fallbackFonts)
            {
                if (fallbackFont == null)
                    continue;

                fallbackFont.isMultiAtlasTexturesEnabled = true;

                if (!defaultFont.fallbackFontAssetTable.Contains(fallbackFont))
                    defaultFont.fallbackFontAssetTable.Add(fallbackFont);
            }
        }

        private static void ApplyFont(TMP_Text text, TMP_FontAsset font)
        {
            bool changed = false;

            if (text.font != font)
            {
                text.font = font;
                changed = true;
            }

            Material defaultMaterial = font.material;
            if (defaultMaterial != null)
            {
                Material currentMaterial = text.fontSharedMaterial;
                if (currentMaterial == null || currentMaterial.mainTexture != defaultMaterial.mainTexture)
                {
                    text.fontSharedMaterial = defaultMaterial;
                    changed = true;
                }
            }

            if (changed)
            {
                text.havePropertiesChanged = true;
                text.UpdateMeshPadding();
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            }
        }

        private static void ApplyInspectorTranslation(TMP_Text text, string source)
        {
            if (!GameLocalization.TryTranslateInspectorText(source, out string translated))
                return;

            if (text.text != translated)
            {
                text.text = translated;
                text.havePropertiesChanged = true;
                text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
            }
        }
    }
}
