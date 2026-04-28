using UnityEngine;
using UnityEngine.EventSystems;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Detects a long press (held without significant movement) on a card and
    /// shows the card's info in the InfoPanel. This replaces hover-based inspect
    /// for touch input. Works alongside CardController — both can receive the same
    /// pointer-down event without conflict.
    ///
    /// Added automatically by CardInstance.Initialize() via EnsureOn().
    /// </summary>
    [RequireComponent(typeof(CardInstance))]
    public class CardLongPressHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
    {
        private CardInstance _card;

        private float   _pressStartTime     = -1f;
        private Vector2 _pressStartScreen;
        private bool    _triggered;

        private const float LongPressSeconds    = 0.5f;
        private const float MaxMovePxBeforeCancel = 20f;

        public static CardLongPressHandler EnsureOn(GameObject owner)
        {
            if (owner == null) return null;
            return owner.TryGetComponent(out CardLongPressHandler h)
                ? h
                : owner.AddComponent<CardLongPressHandler>();
        }

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressStartTime   = Time.unscaledTime;
            _pressStartScreen = eventData.position;
            _triggered        = false;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            // Hide inspect info when the finger lifts (unless a higher-priority
            // panel is showing — InfoPanel priority handles that gracefully).
            if (_triggered)
                _card.HideInspectInfo();

            _pressStartTime = -1f;
        }

        private void Update()
        {
            if (_pressStartTime < 0f || _triggered) return;

            // Cancel if the card started being dragged.
            if (_card.IsBeingDragged)
            {
                _pressStartTime = -1f;
                return;
            }

            // Cancel if the finger drifted too far (accidental hold while moving).
            Vector2 currentPos = InputManager.Instance != null
                ? InputManager.Instance.GetPointerScreenPosition()
                : Vector2.zero;

            if (Vector2.Distance(currentPos, _pressStartScreen) > MaxMovePxBeforeCancel)
            {
                _pressStartTime = -1f;
                return;
            }

            if (Time.unscaledTime - _pressStartTime >= LongPressSeconds)
            {
                _triggered      = true;
                _pressStartTime = -1f;
                _card.ShowInspectInfo();
            }
        }
    }
}
