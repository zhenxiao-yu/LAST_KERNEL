using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Tags a TMP_Text component with a semantic font role.
    /// TMPThemeController reads this at runtime to assign the correct font from GameTypographyProfile.
    /// Default (no component) = GameTextRole.UI → MiSans.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class GameTypographyApplier : MonoBehaviour
    {
        [Tooltip("UI = MiSans (default), Terminal = Sarasa Mono, Accent = Oxanium, Display = brand font")]
        public GameTextRole role = GameTextRole.UI;
    }
}
