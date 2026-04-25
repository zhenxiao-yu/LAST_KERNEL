using UnityEngine;
using UnityEngine.EventSystems;
using DG.Tweening;

namespace Markyu.FortStack
{
    /// <summary>
    /// Owns all card interaction feel: hover, pickup, drag tilt, release settle, spawn pop, merge pop.
    ///
    /// Architecture contract:
    ///   - Purely presentational. Zero game-rule logic.
    ///   - Called by CardController at interaction events.
    ///   - Reads tunables from CardFeelProfile (set via CardSettings).
    ///   - Safe to be absent from a card prefab — all callers null-check.
    ///
    /// Last Kernel feel intent:
    ///   Sharp. Controlled. Short. Mechanical settle. Nothing bubbly.
    ///
    /// Tilt system has three modes (priority order):
    ///   1. Dragging  — velocity-based bank/pitch from movement speed.
    ///   2. Hovered   — mouse-cursor lean (card tilts toward cursor, like Balatro's manualTiltAmount).
    ///   3. Idle      — sine/cosine breathing with per-card phase offset (no synchronized bobbing).
    /// Hover yaw punch drives a separate float so it composes cleanly with the continuous tilt
    /// without fighting transform.rotation writes.
    /// </summary>
    [RequireComponent(typeof(CardInstance))]
    public class CardFeelPresenter : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private CardInstance _card;
        private CardFeelProfile _profile;
        private Camera _mainCam;

        // Scale tween — holds DOScale tweens and Sequences. Killed before each new scale transition.
        private Tween _scaleTween;

        // Rotation punch — drives _punchYaw as a float so it composes with continuous tilt
        // without conflicting on transform.rotation.
        private Tween _rotationTween;
        private float _punchYaw;

        // Continuous tilt state
        private Vector3 _lastPosition;
        private Vector2 _currentTilt; // x = pitch (X-axis rotation), y = bank (Z-axis rotation)

        // Per-card random phase so idle oscillation is never in sync across the board.
        private float _phaseOffset;

        private bool _isHovered;
        private bool _initialized;

        // ── Lifecycle ─────────────────────────────────────────────────────────────

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
            _mainCam = Camera.main;
            _phaseOffset = Random.value * Mathf.PI * 2f;
        }

        /// <summary>
        /// Called by CardInstance.Initialize() after the card is fully set up.
        /// Must be called before any feel reactions fire.
        /// </summary>
        public void Initialize(CardFeelProfile profile)
        {
            _profile = profile;
            _initialized = true;

            transform.localScale = Vector3.zero;
            PlaySpawnPop();
        }

        private void Update()
        {
            if (!_initialized || _card == null || _profile == null) return;

            UpdateTilt();
        }

        // ── Hover ─────────────────────────────────────────────────────────────────

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_initialized || _profile == null) return;
            if (_card.IsBeingDragged) return;

            _isHovered = true;
            ScaleTo(_profile.HoverScale, _profile.HoverScaleDuration, _profile.HoverScaleEase);
            PlayHoverPunch();
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_initialized || _profile == null) return;
            if (_card.IsBeingDragged) return;

            _isHovered = false;
            ScaleTo(1f, _profile.HoverScaleDuration, _profile.HoverScaleEase);
        }

        // ── Pickup ────────────────────────────────────────────────────────────────

        /// <summary>Called by CardController immediately after drag begins.</summary>
        public void OnPickup()
        {
            if (!_initialized || _profile == null) return;

            _lastPosition = transform.position;

            // Kill rotation punch — drag tilt takes over immediately.
            _rotationTween?.Kill();
            _punchYaw = 0f;

            KillScaleTween();

            // Phase 1: spike up (pickup punch).
            // Phase 2: settle to held drag scale.
            float peakScale = 1f + _profile.PickupPunchAmount;
            float holdScale = _profile.DragHoldScale;
            float halfDur   = _profile.PickupPunchDuration * 0.45f;

            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one * peakScale, halfDur).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(Vector3.one * holdScale, halfDur).SetEase(Ease.InOutQuad))
                .SetUpdate(true);
        }

        // ── Release / settle ──────────────────────────────────────────────────────

        /// <summary>Called by CardController immediately after drag ends.</summary>
        public void OnRelease()
        {
            if (!_initialized || _profile == null) return;

            float restScale = _isHovered ? _profile.HoverScale : 1f;

            KillScaleTween();

            // Phase 1: quick squish (weight impact).
            // Phase 2: settle back to rest with slight overshoot (mechanical lock-in).
            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one * _profile.DropSquishScale, _profile.DropSquishDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(Vector3.one * restScale, _profile.DropSettleDuration)
                    .SetEase(_profile.DropSettleEase, _profile.DropSettleOvershoot))
                .SetUpdate(true)
                .OnComplete(() => transform.localScale = Vector3.one * restScale);
        }

        // ── Merge / stack accept ──────────────────────────────────────────────────

        /// <summary>
        /// Called on the receiving stack's top card when another card merges into this stack.
        /// Communicates "system accepted — state changed."
        /// </summary>
        public void OnMergeReceived()
        {
            if (!_initialized || _profile == null) return;

            KillScaleTween();
            _scaleTween = transform
                .DOPunchScale(
                    Vector3.one * _profile.MergePunchAmount,
                    _profile.MergePunchDuration,
                    _profile.MergePunchVibrato,
                    elasticity: 0.5f)
                .SetUpdate(true);
        }

        // ── Tilt system ───────────────────────────────────────────────────────────

        private void UpdateTilt()
        {
            Vector2 targetTilt;
            float smoothing;

            if (_card.IsBeingDragged)
            {
                // Velocity-based bank/pitch — stronger and faster than hover tilt.
                Vector3 vel = (transform.position - _lastPosition) / Mathf.Max(Time.deltaTime, 0.001f);
                float bank  = Mathf.Clamp( vel.x * _profile.DragTiltStrength, -_profile.DragTiltMax, _profile.DragTiltMax);
                float pitch = Mathf.Clamp(-vel.z * _profile.DragTiltStrength, -_profile.DragTiltMax, _profile.DragTiltMax);
                targetTilt = new Vector2(pitch, bank);
                smoothing  = _profile.DragTiltSmoothing;
            }
            else if (_isHovered && _profile.MouseTiltEnabled)
            {
                // Mouse cursor lean: card tilts toward where the cursor is relative to its center.
                // offset.z → pitch (X-axis tilt, forward/back lean)
                // offset.x → bank (Z-axis tilt, left/right lean, inverted so card leans INTO cursor)
                Vector3 offset = transform.position - GetMouseWorldPos();
                float pitch = Mathf.Clamp( offset.z * _profile.MouseTiltAmount, -_profile.DragTiltMax, _profile.DragTiltMax);
                float bank  = Mathf.Clamp(-offset.x * _profile.MouseTiltAmount, -_profile.DragTiltMax, _profile.DragTiltMax);
                targetTilt = new Vector2(pitch, bank);
                smoothing  = _profile.MouseTiltSmoothing;
            }
            else if (_profile.AutoTiltEnabled)
            {
                // Idle breathing: per-card sine/cosine oscillation so the board looks alive.
                float phase = Time.time * _profile.AutoTiltFrequency + _phaseOffset;
                targetTilt = new Vector2(
                    Mathf.Sin(phase) * _profile.AutoTiltAmount,
                    Mathf.Cos(phase) * _profile.AutoTiltAmount);
                smoothing = _profile.MouseTiltSmoothing;
            }
            else
            {
                targetTilt = Vector2.zero;
                smoothing  = _profile.DragTiltSmoothing;
            }

            _lastPosition = transform.position;

            float t = smoothing * Time.deltaTime;
            _currentTilt.x = Mathf.Lerp(_currentTilt.x, targetTilt.x, t);
            _currentTilt.y = Mathf.Lerp(_currentTilt.y, targetTilt.y, t);

            // Compose continuous tilt + hover punch yaw into final rotation.
            if (_currentTilt.sqrMagnitude > 0.0001f || Mathf.Abs(_punchYaw) > 0.01f)
                transform.rotation = Quaternion.Euler(_currentTilt.x, _punchYaw, _currentTilt.y);
            else
            {
                transform.rotation = Quaternion.identity;
                _currentTilt = Vector2.zero;
                _punchYaw    = 0f;
            }
        }

        // ── Hover punch ───────────────────────────────────────────────────────────

        private void PlayHoverPunch()
        {
            if (_profile.HoverPunchAngle <= 0f) return;

            _rotationTween?.Kill();
            _punchYaw = 0f;

            float angle = _profile.HoverPunchAngle;
            float dur   = _profile.HoverPunchDuration;

            // Three-phase damped oscillation driving _punchYaw directly so UpdateTilt can compose it.
            _rotationTween = DOTween.Sequence()
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v,  angle,          dur * 0.25f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v, -angle * 0.35f,  dur * 0.35f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v,  0f,             dur * 0.40f).SetEase(Ease.OutSine))
                .SetUpdate(true)
                .OnComplete(() => _punchYaw = 0f);
        }

        // ── Spawn pop ─────────────────────────────────────────────────────────────

        private void PlaySpawnPop()
        {
            if (!_initialized || _profile == null) return;

            KillScaleTween();
            _scaleTween = transform
                .DOScale(Vector3.one, _profile.SpawnDuration)
                .From(Vector3.one * _profile.SpawnStartScale)
                .SetEase(_profile.SpawnEase, _profile.SpawnOvershoot)
                .SetUpdate(true)
                .OnComplete(() => transform.localScale = Vector3.one);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private Vector3 GetMouseWorldPos()
        {
            if (_mainCam == null) return transform.position;
            Vector2 screenPos = InputManager.Instance != null
                ? InputManager.Instance.GetPointerScreenPosition()
                : (Vector2)Input.mousePosition;
            Ray ray = _mainCam.ScreenPointToRay(screenPos);
            var ground = new Plane(Vector3.up, Vector3.zero);
            return ground.Raycast(ray, out float dist) ? ray.GetPoint(dist) : transform.position;
        }

        private void ScaleTo(float targetUniform, float duration, Ease ease)
        {
            KillScaleTween();
            _scaleTween = transform
                .DOScale(Vector3.one * targetUniform, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        /// <summary>Kills all active feel tweens. Called from CardInstance.KillTweens().</summary>
        public void KillFeelTweens()
        {
            KillScaleTween();
            _rotationTween?.Kill();
            _rotationTween = null;
            _punchYaw    = 0f;
            _currentTilt = Vector2.zero;
            transform.rotation = Quaternion.identity;
        }
    }
}
