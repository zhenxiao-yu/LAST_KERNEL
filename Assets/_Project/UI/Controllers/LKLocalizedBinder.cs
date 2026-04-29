using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Manages Label → localization-key bindings for a UIToolkitScreenController.
    ///
    /// Usage in a screen controller:
    ///   1. In OnBind():   Localizer.Bind(root.Q&lt;Label&gt;("title"), "menu.title");
    ///   2. In OnBind():   Localizer.BindFormat(waveLabel, "night.waveLabel", () => new object[] { _waveNumber });
    ///   3. OnLocalizationRefresh() in the base class calls Localizer.RefreshAll() automatically.
    ///
    /// For labels whose text changes during gameplay (resource counters, progress values),
    /// set label.text directly in the event handler — do not use BindFormat for those.
    /// </summary>
    public sealed class LKLocalizedBinder
    {
        private readonly List<Entry> _entries = new();

        // ── Binding API ────────────────────────────────────────────────────────

        /// <summary>
        /// Registers a label for automatic refresh on every language change.
        /// Use for static text: button labels, headers, panel titles.
        /// </summary>
        public void Bind(Label label, string key)
        {
            if (label == null || string.IsNullOrEmpty(key)) return;
            _entries.Add(new Entry(label, key, null));
        }

        /// <summary>
        /// Registers a format-string label. <paramref name="argsProvider"/> is invoked
        /// every refresh so the label always shows current values in the current language.
        /// Use for text like "Day {0}" or "Night {0}" where the number changes mid-session.
        /// </summary>
        public void BindFormat(Label label, string key, Func<object[]> argsProvider)
        {
            if (label == null || string.IsNullOrEmpty(key) || argsProvider == null) return;
            _entries.Add(new Entry(label, key, argsProvider));
        }

        // ── Refresh ────────────────────────────────────────────────────────────

        /// <summary>
        /// Updates all registered labels with the current language.
        /// Called automatically from UIToolkitScreenController.OnLocalizationRefresh().
        /// </summary>
        public void RefreshAll()
        {
            foreach (var entry in _entries)
            {
                if (entry.Label == null) continue;

                entry.Label.text = entry.ArgsProvider != null
                    ? GameLocalization.Format(entry.Key, entry.ArgsProvider())
                    : Resolve(entry.Key);
            }
        }

        /// <summary>Clears all bindings. Call if the controller is rebound to a new UXML root.</summary>
        public void Clear() => _entries.Clear();

        // ── Internal ───────────────────────────────────────────────────────────

        private static string Resolve(string key)
        {
            string result = GameLocalization.GetOptional(key, null);

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (result == null)
            {
                Debug.LogWarning($"[LKLocalizedBinder] Missing localization key: \"{key}\"");
                return $"[{key}]";
            }
#endif
            return result ?? key;
        }

        private readonly struct Entry
        {
            public readonly Label Label;
            public readonly string Key;
            public readonly Func<object[]> ArgsProvider;

            public Entry(Label label, string key, Func<object[]> argsProvider)
            {
                Label       = label;
                Key         = key;
                ArgsProvider = argsProvider;
            }
        }
    }
}
