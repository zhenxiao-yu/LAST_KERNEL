using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Tags a TMP_Text with a semantic font role and weight.
    /// TMPThemeController reads this to pick the correct font from GameTypographyProfile.
    /// No component = GameTextRole.UI + GameFontWeight.Regular (MiSans Regular).
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class GameTypographyApplier : MonoBehaviour
    {
        [Tooltip("UI=MiSans, Terminal=Sarasa Gothic SC, Accent=Oxanium, Display=brand font")]
        public GameTextRole role = GameTextRole.UI;

        [Tooltip("Regular=body, Medium=buttons, Semibold=sub-headers, Bold=titles, Heavy=screen titles")]
        public GameFontWeight weight = GameFontWeight.Regular;
    }
}
