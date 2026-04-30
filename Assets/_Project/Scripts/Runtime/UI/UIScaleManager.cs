using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    public enum UIScale { Small = 0, Medium = 1, Large = 2, XLarge = 3 }

    /// <summary>
    /// Applies a CSS class to the root VisualElement of registered (game-HUD) UIDocuments.
    /// Descendant selectors in theme.uss then scale every child element inside that root.
    /// Unregistered documents (title, pause, menus) are never touched.
    /// </summary>
    public static class UIScaleManager
    {
        private const string PrefKey = "UIScale";

        // CSS class names applied to the root VisualElement of each registered document.
        // Medium uses no class — the :root defaults in theme.uss are the baseline.
        private static readonly string[] ScaleClasses =
        {
            "lk-ui-scale--small",   // 0 Small
            "",                      // 1 Medium — no class, use :root defaults
            "lk-ui-scale--large",   // 2 Large
            "lk-ui-scale--xlarge",  // 3 Super Large
        };

        private static readonly List<(UIDocument doc, VisualElement root)> Registrations = new();

        public static UIScale CurrentScale { get; private set; } = UIScale.Medium;

        public static event Action<UIScale> ScaleChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Init()
        {
            CurrentScale = (UIScale)Mathf.Clamp(
                PlayerPrefs.GetInt(PrefKey, (int)UIScale.Medium),
                (int)UIScale.Small, (int)UIScale.XLarge);
            Registrations.Clear();
        }

        // ── Registration ───────────────────────────────────────────────────────

        public static void Register(UIDocument doc, VisualElement root)
        {
            if (doc == null || root == null) return;
            foreach (var (d, _) in Registrations)
                if (d == doc) return;

            Registrations.Add((doc, root));
            ApplyClassTo(root, CurrentScale);
        }

        public static void Unregister(UIDocument doc)
        {
            for (int i = Registrations.Count - 1; i >= 0; i--)
            {
                if (Registrations[i].doc == doc)
                {
                    RemoveAllScaleClasses(Registrations[i].root);
                    Registrations.RemoveAt(i);
                    return;
                }
            }
        }

        // ── Scale control ──────────────────────────────────────────────────────

        public static void SetScale(UIScale scale)
        {
            CurrentScale = scale;
            PlayerPrefs.SetInt(PrefKey, (int)scale);
            PlayerPrefs.Save();
            ApplyToAll();
            ScaleChanged?.Invoke(scale);
        }

        public static void CycleScale()
        {
            SetScale((UIScale)(((int)CurrentScale + 1) % 4));
        }

        public static string GetScaleLabelKey(UIScale scale) => scale switch
        {
            UIScale.Small  => "options.uiScale.small",
            UIScale.Medium => "options.uiScale.medium",
            UIScale.Large  => "options.uiScale.large",
            UIScale.XLarge => "options.uiScale.xlarge",
            _              => "options.uiScale.medium",
        };

        // ── Internal ───────────────────────────────────────────────────────────

        private static void ApplyToAll()
        {
            for (int i = Registrations.Count - 1; i >= 0; i--)
            {
                var (doc, root) = Registrations[i];
                if (doc == null || root == null) { Registrations.RemoveAt(i); continue; }
                ApplyClassTo(root, CurrentScale);
            }
        }

        private static void ApplyClassTo(VisualElement root, UIScale scale)
        {
            RemoveAllScaleClasses(root);
            string cls = ScaleClasses[(int)scale];
            if (!string.IsNullOrEmpty(cls))
                root.AddToClassList(cls);
        }

        private static void RemoveAllScaleClasses(VisualElement root)
        {
            if (root == null) return;
            foreach (var cls in ScaleClasses)
                if (!string.IsNullOrEmpty(cls)) root.RemoveFromClassList(cls);
        }
    }
}
