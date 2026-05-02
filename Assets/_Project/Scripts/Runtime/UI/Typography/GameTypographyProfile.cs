using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(fileName = "GameTypographyProfile", menuName = "LastKernel/Typography/Profile")]
    public sealed class GameTypographyProfile : ScriptableObject
    {
        [Header("Assign after dropping TTF files and creating TMP Font Assets")]
        [Tooltip("MiSans Global Regular — primary readable UI font for all languages")]
        public TMP_FontAsset uiFont;

        [Tooltip("Sarasa Mono SC Regular — machine/system voice, data readouts, terminal displays")]
        public TMP_FontAsset terminalFont;

        [Tooltip("Oxanium Regular — EN-only cyberpunk accent: phase labels, speed buttons, HUD badges")]
        public TMP_FontAsset accentFont;

        [Tooltip("Paid display font slot — logo, main title, brand moments. Leave null until purchased.")]
        public TMP_FontAsset displayFont;

        [Tooltip("Noto Sans SC — emergency fallback only. Not for primary use.")]
        public TMP_FontAsset fallbackFont;

        public TMP_FontAsset GetFont(GameTextRole role)
        {
            TMP_FontAsset resolved = role switch
            {
                GameTextRole.Terminal => terminalFont,
                GameTextRole.Accent   => accentFont,
                GameTextRole.Display  => displayFont,
                _                     => uiFont,
            };

            // Fall through to uiFont if the specific slot isn't wired yet
            if (resolved != null) return resolved;
            if (uiFont != null) return uiFont;
            return fallbackFont;
        }
    }
}
