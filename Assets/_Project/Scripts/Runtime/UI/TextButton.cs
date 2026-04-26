using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Markyu.LastKernel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField, Tooltip("Whether the label should underline while hovered.")]
        private bool underlineOnHover = true;

        [SerializeField, Tooltip("Whether a standard click SFX should play when the button action fires.")]
        private bool playClickSound = true;

        private TextMeshProUGUI cachedText;
        private Action onClick;
        private Action<bool> onHover;
        private FontStyles baseFontStyle;
        private bool hasCachedBaseStyle;
        private bool isHovering;

        private TextMeshProUGUI Text
        {
            get
            {
                if (cachedText == null)
                {
                    cachedText = GetComponent<TextMeshProUGUI>();
                }

                return cachedText;
            }
        }

        private void Awake()
        {
            CacheBaseStyle(force: true);
            ApplyVisualState();
        }

        public void Setup(string label, float fontSize, Action<bool> onHover = null, Action onClick = null)
        {
            gameObject.SetActive(true);
            Text.text = label;
            Text.fontSize = fontSize;
            this.onHover = onHover;
            this.onClick = onClick;
            CacheBaseStyle(force: true);
            SetHoverState(false, notifyListeners: false);
        }

        public void SetOnClick(Action onClick)
        {
            this.onClick = onClick;
        }

        public void SetOnHover(Action<bool> onHover)
        {
            this.onHover = onHover;
        }

        public string GetText()
        {
            return Text.text;
        }

        public void SetText(string text)
        {
            Text.text = text;
        }

        public void SetColor(Color color)
        {
            Text.color = color;
        }

        public void Deactivate()
        {
            SetHoverState(false, notifyListeners: false);
            Text.text = string.Empty;
            onClick = null;
            onHover = null;
            gameObject.SetActive(false);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (onClick == null)
            {
                return;
            }

            onClick.Invoke();

            if (playClickSound)
            {
                AudioManager.Instance?.PlaySFX(AudioId.Click);
            }
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            SetHoverState(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHoverState(false);
        }

        private void OnDisable()
        {
            SetHoverState(false, notifyListeners: false);
        }

        private void SetHoverState(bool hoverState, bool notifyListeners = true)
        {
            isHovering = hoverState;
            ApplyVisualState();

            if (notifyListeners)
            {
                onHover?.Invoke(hoverState);
            }
        }

        private void ApplyVisualState()
        {
            CacheBaseStyle();

            Text.fontStyle = isHovering && underlineOnHover
                ? baseFontStyle | FontStyles.Underline
                : baseFontStyle;
        }

        private void CacheBaseStyle(bool force = false)
        {
            if (hasCachedBaseStyle && !force)
            {
                return;
            }

            baseFontStyle = Text.fontStyle & ~FontStyles.Underline;
            hasCachedBaseStyle = true;
        }
    }
}

