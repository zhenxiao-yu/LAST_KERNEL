using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Per-weight font family. Each slot falls back up the weight chain if null,
    /// so assigning just Regular + Bold is sufficient for minimal setups.
    /// </summary>
    [System.Serializable]
    public struct GameFontFamily
    {
        [Tooltip("Body, labels, secondary — MiSans Regular / Sarasa Regular / Oxanium Regular")]
        public TMP_FontAsset regular;

        [Tooltip("Buttons, interactive text — MiSans Medium / Oxanium Medium")]
        public TMP_FontAsset medium;

        [Tooltip("Sub-headers, tabs, resource values — MiSans Semibold / Sarasa SemiBold / Oxanium SemiBold")]
        public TMP_FontAsset semibold;

        [Tooltip("Panel/modal titles, section headers — MiSans Bold / Oxanium Bold")]
        public TMP_FontAsset bold;

        [Tooltip("Screen titles, logo, max emphasis — MiSans Heavy / Oxanium ExtraBold")]
        public TMP_FontAsset heavy;

        public TMP_FontAsset Resolve(GameFontWeight weight)
        {
            TMP_FontAsset resolved = weight switch
            {
                GameFontWeight.Medium   => medium   != null ? medium   : regular,
                GameFontWeight.Semibold => semibold != null ? semibold : medium   != null ? medium   : regular,
                GameFontWeight.Bold     => bold     != null ? bold     : semibold != null ? semibold : regular,
                GameFontWeight.Heavy    => heavy    != null ? heavy    : bold     != null ? bold     : regular,
                _                       => regular,
            };
            return resolved;
        }
    }

    [CreateAssetMenu(fileName = "GameTypographyProfile", menuName = "LastKernel/Typography/Profile")]
    public sealed class GameTypographyProfile : ScriptableObject
    {
        [Header("UI — MiSans Global (all roles, full CJK)")]
        public GameFontFamily ui;

        [Header("Terminal — Sarasa Gothic SC (system feed, data readouts)")]
        public GameFontFamily terminal;

        [Header("Accent — Oxanium (EN-only: phase labels, speed buttons, HUD badges)")]
        public GameFontFamily accent;

        [Header("Display — Paid font slot (logo, brand moments — leave null until licensed)")]
        public GameFontFamily display;

        [Header("Emergency CJK Fallback — Noto Sans SC (do not use as primary)")]
        public TMP_FontAsset fallbackFont;

        public TMP_FontAsset GetFont(GameTextRole role, GameFontWeight weight = GameFontWeight.Regular)
        {
            GameFontFamily family = role switch
            {
                GameTextRole.Terminal => terminal,
                GameTextRole.Accent   => accent,
                GameTextRole.Display  => display,
                _                     => ui,
            };

            // If the target family has no regular set, fall back to the ui family
            TMP_FontAsset resolved = family.Resolve(weight);
            if (resolved != null) return resolved;

            resolved = ui.Resolve(weight);
            if (resolved != null) return resolved;

            return fallbackFont;
        }
    }
}
