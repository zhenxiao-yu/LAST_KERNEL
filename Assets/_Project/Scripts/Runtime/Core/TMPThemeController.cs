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

            // Load typography profile from Resources (placed at Resources/Typography/GameTypographyProfile)
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
            // Resolve the default (UI role) font: profile.uiFont → TMP_Settings default
            TMP_FontAsset defaultFont = typographyProfile != null ? typographyProfile.uiFont : null;
            defaultFont ??= TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
                return;

            ConfigureFontFallbacks(defaultFont);

            TMP_Text[] allText = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
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

        // Returns the correct font for a text component based on its GameTypographyApplier role.
        // Components without a GameTypographyApplier always get the default (UI / MiSans) font.
        private TMP_FontAsset ResolveFont(TMP_Text text, TMP_FontAsset defaultFont)
        {
            if (typographyProfile == null)
                return defaultFont;

            if (!text.TryGetComponent<GameTypographyApplier>(out var applier))
                return defaultFont;

            TMP_FontAsset roleFont = typographyProfile.GetFont(applier.role);
            return roleFont != null ? roleFont : defaultFont;
        }

        private void ConfigureFontFallbacks(TMP_FontAsset defaultFont)
        {
            defaultFont.isMultiAtlasTexturesEnabled = true;

            List<TMP_FontAsset> fallbackFonts = TMP_Settings.fallbackFontAssets ?? new List<TMP_FontAsset>();

            // Ensure Noto emergency fallback is in the global fallback list
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
            if (text.font != font)
                text.font = font;

            Material defaultMaterial = font.material;
            if (defaultMaterial != null)
            {
                Material currentMaterial = text.fontSharedMaterial;
                if (currentMaterial == null || currentMaterial.mainTexture != defaultMaterial.mainTexture)
                    text.fontSharedMaterial = defaultMaterial;
            }

            text.havePropertiesChanged = true;
            text.UpdateMeshPadding();
            text.ForceMeshUpdate(ignoreActiveState: true, forceTextReparsing: true);
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
