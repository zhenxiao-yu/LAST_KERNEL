using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Adds a consistent premium interaction layer to every UI Toolkit document:
    /// hover/focus/press classes, throttled hover audio, and arrow/WASD focus movement.
    /// Styling lives in USS; this class only coordinates state.
    /// </summary>
    internal static class LKUIInteractionPolisher
    {
        private const float HoverSoundCooldown = 0.075f;

        private static readonly HashSet<int> BoundRoots = new();
        private static float _lastHoverSoundTime;

        public static void Bind(VisualElement root)
        {
            if (root == null) return;

            int id = root.GetHashCode();
            if (!BoundRoots.Add(id))
                return;

            root.AddToClassList("lk-premium-root");
            Refresh(root);

            root.RegisterCallback<PointerEnterEvent>(OnPointerEnter, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerLeaveEvent>(OnPointerLeave, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerDownEvent>(OnPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusInEvent>(OnFocusIn, TrickleDown.TrickleDown);
            root.RegisterCallback<FocusOutEvent>(OnFocusOut, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown, TrickleDown.TrickleDown);
        }

        public static void Refresh(VisualElement root)
        {
            if (root == null) return;

            root.Query<Button>().ForEach(button =>
            {
                button.focusable = true;
                button.AddToClassList("lk-interactive");
            });

            root.Query<Slider>().ForEach(slider =>
            {
                slider.focusable = true;
                slider.AddToClassList("lk-interactive");
                slider.AddToClassList("lk-slider--premium");
            });
        }

        private static void OnPointerEnter(PointerEnterEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.AddToClassList("lk-ui-hovered");
            PlayHoverSound();
        }

        private static void OnPointerLeave(PointerLeaveEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.RemoveFromClassList("lk-ui-hovered");
            el.RemoveFromClassList("lk-ui-pressed");
        }

        private static void OnPointerDown(PointerDownEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.AddToClassList("lk-ui-pressed");
            el.Focus();
        }

        private static void OnPointerUp(PointerUpEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.RemoveFromClassList("lk-ui-pressed");
        }

        private static void OnFocusIn(FocusInEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.AddToClassList("lk-ui-focused");
            PlayHoverSound();
        }

        private static void OnFocusOut(FocusOutEvent evt)
        {
            if (FindInteractive(evt.target as VisualElement) is not { } el) return;
            el.RemoveFromClassList("lk-ui-focused");
            el.RemoveFromClassList("lk-ui-pressed");
        }

        private static void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.currentTarget is not VisualElement root) return;

            int delta = evt.keyCode switch
            {
                KeyCode.DownArrow or KeyCode.RightArrow or KeyCode.S or KeyCode.D => 1,
                KeyCode.UpArrow or KeyCode.LeftArrow or KeyCode.W or KeyCode.A    => -1,
                _                                                                 => 0
            };

            if (delta == 0) return;

            Refresh(root);
            var buttons = GetFocusableButtons(root);
            if (buttons.Count == 0) return;

            var current = root.panel?.focusController?.focusedElement as Button;
            int index = current != null ? buttons.IndexOf(current) : -1;
            int next = index < 0 ? 0 : (index + delta + buttons.Count) % buttons.Count;

            buttons[next].Focus();
            evt.StopPropagation();
        }

        private static List<Button> GetFocusableButtons(VisualElement root)
        {
            var buttons = new List<Button>();
            root.Query<Button>().ForEach(button =>
            {
                if (IsFocusable(button))
                    buttons.Add(button);
            });
            return buttons;
        }

        private static bool IsFocusable(Button button)
        {
            if (button == null || !button.enabledSelf || !button.focusable)
                return false;
            if (button.resolvedStyle.display == DisplayStyle.None)
                return false;
            if (button.resolvedStyle.visibility == Visibility.Hidden)
                return false;

            for (VisualElement p = button.parent; p != null; p = p.parent)
            {
                if (p.resolvedStyle.display == DisplayStyle.None ||
                    p.resolvedStyle.visibility == Visibility.Hidden)
                    return false;
            }

            return true;
        }

        private static VisualElement FindInteractive(VisualElement start)
        {
            for (VisualElement el = start; el != null; el = el.parent)
            {
                if (el is Button || el is Slider || el.ClassListContains("lk-interactive"))
                    return el;
            }
            return null;
        }

        private static void PlayHoverSound()
        {
            if (AudioManager.Instance == null)
                return;

            float now = Time.unscaledTime;
            if (now - _lastHoverSoundTime < HoverSoundCooldown)
                return;

            _lastHoverSoundTime = now;
            AudioManager.Instance.PlaySFX(AudioId.Pop);
        }
    }
}
