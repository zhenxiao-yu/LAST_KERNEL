using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Routes UI Toolkit button events to AudioManager so all sounds go through
    /// the project's AudioMixer (respecting the SFX volume slider).
    ///
    /// Add to every UIDocument GameObject alongside UIToolkitScreenController.
    /// Assign a LKUISoundConfig asset in the inspector.
    ///
    /// For panel open/close sounds, call PlayPanelOpen() / PlayPanelClose()
    /// from the relevant UIToolkitScreenController.OnShow() / OnHide().
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class LKUISoundBridge : MonoBehaviour
    {
        [SerializeField] private LKUISoundConfig config;

        private UIDocument _document;

        private void Start()
        {
            _document = GetComponent<UIDocument>();
            var root = _document?.rootVisualElement;
            if (root == null) return;

            root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

            if (config != null && config.PlayHoverSounds)
                root.RegisterCallback<PointerEnterEvent>(OnHover, TrickleDown.TrickleDown);
        }

        private void OnDestroy()
        {
            var root = _document?.rootVisualElement;
            if (root == null) return;

            root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);

            if (config != null && config.PlayHoverSounds)
                root.UnregisterCallback<PointerEnterEvent>(OnHover, TrickleDown.TrickleDown);
        }

        private void OnClick(ClickEvent evt)
        {
            if (evt.target is not Button) return;
            if (_document?.rootVisualElement?.ClassListContains("lk-premium-root") == true) return;
            Play(config != null ? config.ButtonClick : AudioId.Click);
        }

        private void OnHover(PointerEnterEvent evt)
        {
            if (evt.target is not Button) return;
            Play(config != null ? config.ButtonHover : AudioId.Pop);
        }

        public void PlayPanelOpen()  => Play(config != null ? config.PanelOpen  : AudioId.Pop);
        public void PlayPanelClose() => Play(config != null ? config.PanelClose : AudioId.Pop);

        private void Play(AudioId id)
        {
            AudioManager.Instance?.PlaySFX(id);
        }
    }
}
