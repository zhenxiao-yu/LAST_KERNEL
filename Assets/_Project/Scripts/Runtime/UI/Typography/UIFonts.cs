using UnityEngine;
using UnityEngine.TextCore.Text;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Applies designer-grade font choices to UIToolkit VisualElements.
    /// Fonts are loaded from Resources/Typography/ at first use and cached.
    /// All Apply* methods are null-safe — missing assets are a no-op.
    /// </summary>
    public static class UIFonts
    {
        private static FontAsset _oxaniumSemibold;
        private static FontAsset _oxaniumBold;
        private static FontAsset _misansHeavy;

        private static bool _loaded;

        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _oxaniumSemibold = Resources.Load<FontAsset>("Typography/FA_Oxanium_SemiBold");
            _oxaniumBold     = Resources.Load<FontAsset>("Typography/FA_Oxanium_Bold");
            _misansHeavy     = Resources.Load<FontAsset>("Typography/FA_MiSans_Heavy");
        }

        // Oxanium SemiBold — HUD phase badge (DAY), wave counter, resource accent labels
        public static void AccentSemibold(VisualElement el) { EnsureLoaded(); Apply(el, _oxaniumSemibold); }

        // Oxanium Bold — HUD night phase badge (NIGHT), speed toggle (2×), wave header
        public static void AccentBold(VisualElement el) { EnsureLoaded(); Apply(el, _oxaniumBold); }

        // MiSans Heavy — logo, main title, Victory/Defeat screen titles
        public static void DisplayHeavy(VisualElement el) { EnsureLoaded(); Apply(el, _misansHeavy); }

        private static void Apply(VisualElement el, FontAsset fa)
        {
            if (el == null || fa == null) return;
            el.style.unityFontDefinition = FontDefinition.FromSDFFont(fa);
        }
    }
}
