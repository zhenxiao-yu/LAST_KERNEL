using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Owns card presentation feedback: hover, pickup, release, merge, hit flash, tilt, and shader detail.
    /// Game rules should call this component, but this component should never decide gameplay.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardInstance), typeof(MeshRenderer))]
    public class CardFeelPresenter : MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int OverlayOffsetId = Shader.PropertyToID("_OverlayOffset");
        private static readonly int BrightnessId = Shader.PropertyToID("_Brightness");
        private static readonly int SaturationId = Shader.PropertyToID("_Saturation");
        private static readonly int HueShiftId = Shader.PropertyToID("_HueShift");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        private CardInstance _card;
        private CardFeelProfile _profile;
        private Camera _mainCam;
        private MeshRenderer _renderer;
        private MaterialPropertyBlock _propertyBlock;

        private Tween _scaleTween;
        private Tween _rotationTween;
        private Tween _flashTween;

        private Vector3 _lastPosition;
        private Vector2 _currentTilt; // x = pitch, y = bank
        private float _punchYaw;
        private float _phaseOffset;
        private float _flashAmount;
        private float _currentGlow;

        // Hover lift — raises the card in Y while hovered so tilt can't dip card edges
        // into neighboring flat cards. Magnitude is auto-computed from DragTiltMax each
        // frame, so it is correct even if HoverLiftAmount in the profile is 0.
        // Applied in LateUpdate using the stack's authoritative TargetPosition as the
        // base Y so the value is always correct regardless of in-flight DOTween animations.
        private float _currentHoverLift;

        // Per-renderer sort-order management. On hover, all card renderers (body + TMP text
        // + panel overlays) are elevated so the hovered card draws over neighboring cards'
        // transparent layers (TMP, alpha-tested borders) without depending on 3D depth order.
        private Renderer[] _allRenderers;
        private int[] _savedSortingOrders;

        private Vector4 _baseOverlayOffset;
        private Color _baseEmissionColor;
        private float _baseBrightness;
        private float _baseSaturation;
        private float _baseHueShift;

        private bool _hasFlashAmount;
        private bool _hasOverlayOffset;
        private bool _hasBrightness;
        private bool _hasSaturation;
        private bool _hasHueShift;
        private bool _hasEmissionColor;

        private bool _isHovered;
        private bool _initialized;

        public bool IsInitialized => _initialized;

        /// <summary>
        /// Runtime guard for old prefabs. Several card prefabs predate this component, so callers
        /// ensure the presenter exists before caching or initializing it.
        /// </summary>
        public static CardFeelPresenter EnsureOn(GameObject owner)
        {
            if (owner == null)
            {
                return null;
            }

            return owner.TryGetComponent(out CardFeelPresenter presenter)
                ? presenter
                : owner.AddComponent<CardFeelPresenter>();
        }

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
            _renderer = GetComponent<MeshRenderer>();
            _propertyBlock = new MaterialPropertyBlock();
            _mainCam = Camera.main;
            _lastPosition = transform.position;
            _phaseOffset = Random.value * Mathf.PI * 2f;
        }

        public void Initialize(CardFeelProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning($"CardFeelPresenter: '{name}' has no CardFeelProfile assigned; card feel is disabled.", this);
                _initialized = false;
                return;
            }

            _profile = profile;
            _initialized = true;
            _lastPosition = transform.position;
            _mainCam = Camera.main;

            CacheMaterialDefaults();
            CacheRenderers();

            transform.localScale = Vector3.one * _profile.SpawnStartScale;
            _flashAmount = 0f;
            _currentGlow = 0f;
            _currentHoverLift = 0f;
            ApplyMaterialFeedback();
            PlaySpawnPop();
        }

        public void RefreshMaterialState()
        {
            if (!_initialized || _profile == null)
            {
                return;
            }

            CacheMaterialDefaults();
            ApplyMaterialFeedback();
        }

        private void Update()
        {
            if (!_initialized || _card == null || _profile == null)
            {
                return;
            }

            float deltaTime = Time.unscaledDeltaTime;
            UpdateTilt(deltaTime);
            UpdateHoverLift(deltaTime);
            UpdateMaterialFeedback(deltaTime);
        }

        private void LateUpdate()
        {
            if (!_initialized || _card == null || _card.IsBeingDragged) return;
            if (_currentHoverLift < 0.0001f) return;

            // Resolve the Y the stack intends for this card, then add the hover lift on top.
            // Reading from CardStack.TargetPosition is authoritative and immune to DOTween
            // desync: DOTween updates run in Update; this LateUpdate always wins afterward.
            float baseY = ComputeStackBaseY();
            var pos = transform.position;
            pos.y = baseY + _currentHoverLift;
            transform.position = pos;
        }

        private void OnDisable()
        {
            RestoreSortOrder();

            if (_currentHoverLift > 0.0001f)
            {
                var pos = transform.position;
                pos.y = ComputeStackBaseY();
                transform.position = pos;
            }
            _currentHoverLift = 0f;
            ResetMaterialFeedback();
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_initialized || _profile == null || _card.IsBeingDragged)
            {
                return;
            }

            _isHovered = true;
            ElevateSortOrder();
            ScaleTo(_profile.HoverScale, _profile.HoverScaleDuration, _profile.HoverScaleEase);
            PulseFlash(_profile.HoverFlashAmount, _profile.FlashReturnDuration);
            PlayYawPunch(_profile.HoverPunchAngle, _profile.HoverPunchDuration);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_initialized || _profile == null || _card.IsBeingDragged)
            {
                return;
            }

            _isHovered = false;
            RestoreSortOrder();
            ScaleTo(1f, _profile.HoverScaleDuration, _profile.HoverScaleEase);
        }

        public void OnPickup()
        {
            if (!_initialized || _profile == null)
            {
                return;
            }

            _lastPosition = transform.position;
            _isHovered = false;
            RestoreSortOrder();
            _rotationTween?.Kill();
            _punchYaw = 0f;

            KillScaleTween();
            PulseFlash(_profile.PickupFlashAmount, _profile.FlashReturnDuration);

            float peakScale = 1f + _profile.PickupPunchAmount;
            float holdScale = _profile.DragHoldScale;
            float halfDuration = _profile.PickupPunchDuration * 0.45f;

            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one * peakScale, halfDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(Vector3.one * holdScale, halfDuration).SetEase(Ease.InOutQuad))
                .SetUpdate(true);
        }

        public void OnRelease()
        {
            if (!_initialized || _profile == null)
            {
                return;
            }

            float restScale = _isHovered ? _profile.HoverScale : 1f;

            KillScaleTween();

            _scaleTween = DOTween.Sequence()
                .Append(transform.DOScale(Vector3.one * _profile.DropSquishScale, _profile.DropSquishDuration).SetEase(Ease.OutQuad))
                .Append(transform.DOScale(Vector3.one * restScale, _profile.DropSettleDuration)
                    .SetEase(_profile.DropSettleEase, _profile.DropSettleOvershoot))
                .SetUpdate(true)
                .OnComplete(() => transform.localScale = Vector3.one * restScale);
        }

        public void OnMergeReceived()
        {
            if (!_initialized || _profile == null)
            {
                return;
            }

            KillScaleTween();
            PulseFlash(_profile.MergeFlashAmount, _profile.FlashReturnDuration);

            _scaleTween = transform
                .DOPunchScale(
                    Vector3.one * _profile.MergePunchAmount,
                    _profile.MergePunchDuration,
                    _profile.MergePunchVibrato,
                    elasticity: 0.5f)
                .SetUpdate(true);
        }

        public bool OnDamageTaken()
        {
            if (!_initialized || _profile == null)
            {
                return false;
            }

            PulseFlash(_profile.DamageFlashAmount, _profile.FlashReturnDuration);
            PlayYawPunch(_profile.DamagePunchAngle, _profile.DamagePunchDuration);
            return true;
        }

        private void UpdateTilt(float deltaTime)
        {
            Vector2 targetTilt;
            float smoothing;

            if (_card.IsBeingDragged)
            {
                Vector3 velocity = (transform.position - _lastPosition) / Mathf.Max(deltaTime, 0.001f);
                float bank = Mathf.Clamp(velocity.x * _profile.DragTiltStrength, -_profile.DragTiltMax, _profile.DragTiltMax);
                float pitch = Mathf.Clamp(-velocity.z * _profile.DragTiltStrength, -_profile.DragTiltMax, _profile.DragTiltMax);
                targetTilt = new Vector2(pitch, bank);
                smoothing = _profile.DragTiltSmoothing;
            }
            else if (_isHovered && _profile.MouseTiltEnabled)
            {
                Vector3 offset = transform.position - GetMouseWorldPosition();
                float pitch = Mathf.Clamp(offset.z * _profile.MouseTiltAmount, -_profile.DragTiltMax, _profile.DragTiltMax);
                float bank = Mathf.Clamp(-offset.x * _profile.MouseTiltAmount, -_profile.DragTiltMax, _profile.DragTiltMax);
                targetTilt = new Vector2(pitch, bank);
                smoothing = _profile.MouseTiltSmoothing;
            }
            else if (_profile.AutoTiltEnabled)
            {
                float phase = Time.unscaledTime * _profile.AutoTiltFrequency + _phaseOffset;
                targetTilt = new Vector2(
                    Mathf.Sin(phase) * _profile.AutoTiltAmount,
                    Mathf.Cos(phase) * _profile.AutoTiltAmount);
                smoothing = _profile.MouseTiltSmoothing;
            }
            else
            {
                targetTilt = Vector2.zero;
                smoothing = _profile.DragTiltSmoothing;
            }

            _lastPosition = transform.position;

            float t = 1f - Mathf.Exp(-smoothing * deltaTime);
            _currentTilt.x = Mathf.Lerp(_currentTilt.x, targetTilt.x, t);
            _currentTilt.y = Mathf.Lerp(_currentTilt.y, targetTilt.y, t);

            if (_currentTilt.sqrMagnitude > 0.0001f || Mathf.Abs(_punchYaw) > 0.01f)
            {
                transform.rotation = Quaternion.Euler(_currentTilt.x, _punchYaw, _currentTilt.y);
            }
            else
            {
                transform.rotation = Quaternion.identity;
                _currentTilt = Vector2.zero;
                _punchYaw = 0f;
            }
        }

        private void UpdateMaterialFeedback(float deltaTime)
        {
            float targetGlow = 0f;

            if (_card.IsBeingDragged)
            {
                targetGlow = _profile.DragGlowIntensity;
            }
            else if (_isHovered)
            {
                targetGlow = _profile.HoverGlowIntensity;
            }

            float t = 1f - Mathf.Exp(-14f * deltaTime);
            _currentGlow = Mathf.Lerp(_currentGlow, targetGlow, t);
            ApplyMaterialFeedback();
        }

        private void UpdateHoverLift(float deltaTime)
        {
            float target = 0f;
            if (_isHovered && !_card.IsBeingDragged)
            {
                // Compute the minimum lift needed to keep all tilted card edges above Y=0.
                // Standard cards are 0.8×1.0 units; half-diagonal = 0.64. Use 0.65 to cover
                // all prefab sizes. At DragTiltMax=7°: 0.65×sin(7°)+0.02 ≈ 0.094 units.
                // This is computed every frame from DragTiltMax so it is always correct
                // even when HoverLiftAmount in the profile asset is still at its default 0.
                float maxTiltRad = Mathf.Max(_profile.DragTiltMax, 1f) * Mathf.Deg2Rad;
                float computedLift = 0.65f * Mathf.Sin(maxTiltRad) + 0.02f;
                target = Mathf.Max(_profile.HoverLiftAmount, computedLift);
            }
            _currentHoverLift = Mathf.Lerp(_currentHoverLift, target, 1f - Mathf.Exp(-12f * deltaTime));
        }

        private void PlaySpawnPop()
        {
            KillScaleTween();
            PulseFlash(_profile.HoverFlashAmount, _profile.FlashReturnDuration);

            _scaleTween = transform
                .DOScale(Vector3.one, _profile.SpawnDuration)
                .From(Vector3.one * _profile.SpawnStartScale)
                .SetEase(_profile.SpawnEase, _profile.SpawnOvershoot)
                .SetUpdate(true)
                .OnComplete(() => transform.localScale = Vector3.one);
        }

        private void PlayYawPunch(float angle, float duration)
        {
            if (angle <= 0f || duration <= 0f)
            {
                return;
            }

            _rotationTween?.Kill();
            _punchYaw = 0f;

            _rotationTween = DOTween.Sequence()
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v, angle, duration * 0.25f).SetEase(Ease.OutQuad))
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v, -angle * 0.35f, duration * 0.35f).SetEase(Ease.InOutSine))
                .Append(DOTween.To(() => _punchYaw, v => _punchYaw = v, 0f, duration * 0.40f).SetEase(Ease.OutSine))
                .SetUpdate(true)
                .OnComplete(() => _punchYaw = 0f);
        }

        private void PulseFlash(float amount, float returnDuration)
        {
            if (amount <= 0f)
            {
                return;
            }

            _flashTween?.Kill();
            _flashAmount = Mathf.Clamp01(Mathf.Max(_flashAmount, amount));
            ApplyMaterialFeedback();

            _flashTween = DOTween
                .To(() => _flashAmount, value =>
                {
                    _flashAmount = value;
                    ApplyMaterialFeedback();
                }, 0f, Mathf.Max(0.03f, returnDuration))
                .SetEase(Ease.OutQuad)
                .SetUpdate(true);
        }

        private void ScaleTo(float targetUniform, float duration, Ease ease)
        {
            KillScaleTween();
            _scaleTween = transform
                .DOScale(Vector3.one * targetUniform, duration)
                .SetEase(ease)
                .SetUpdate(true);
        }

        private Vector3 GetMouseWorldPosition()
        {
            if (_mainCam == null)
            {
                _mainCam = Camera.main;
            }

            if (_mainCam == null)
            {
                return transform.position;
            }

            Vector2 screenPosition = InputManager.Instance != null
                ? InputManager.Instance.GetPointerScreenPosition()
                : (Vector2)Input.mousePosition;

            Ray ray = _mainCam.ScreenPointToRay(screenPosition);
            var plane = new Plane(Vector3.up, new Vector3(0f, transform.position.y, 0f));
            return plane.Raycast(ray, out float distance)
                ? ray.GetPoint(distance)
                : transform.position;
        }

        private void CacheMaterialDefaults()
        {
            _renderer = GetComponent<MeshRenderer>();

            Material material = _renderer != null ? _renderer.sharedMaterial : null;
            if (material == null)
            {
                ResetMaterialFlags();
                return;
            }

            _hasFlashAmount = material.HasProperty(FlashAmountId);
            _hasOverlayOffset = material.HasProperty(OverlayOffsetId);
            _hasBrightness = material.HasProperty(BrightnessId);
            _hasSaturation = material.HasProperty(SaturationId);
            _hasHueShift = material.HasProperty(HueShiftId);
            _hasEmissionColor = material.HasProperty(EmissionColorId);

            _baseOverlayOffset = _hasOverlayOffset ? material.GetVector(OverlayOffsetId) : Vector4.zero;
            _baseBrightness = _hasBrightness ? material.GetFloat(BrightnessId) : 1f;
            _baseSaturation = _hasSaturation ? material.GetFloat(SaturationId) : 1f;
            _baseHueShift = _hasHueShift ? material.GetFloat(HueShiftId) : 0f;
            _baseEmissionColor = _hasEmissionColor ? material.GetColor(EmissionColorId) : Color.black;
        }

        private void ApplyMaterialFeedback()
        {
            if (_renderer == null || _propertyBlock == null || _profile == null)
            {
                return;
            }

            _renderer.GetPropertyBlock(_propertyBlock);

            float maxTilt = Mathf.Max(_profile != null ? _profile.DragTiltMax : 1f, 0.001f);
            Vector2 tilt01 = new Vector2(
                Mathf.Clamp(_currentTilt.y / maxTilt, -1f, 1f),
                Mathf.Clamp(_currentTilt.x / maxTilt, -1f, 1f));

            if (_hasFlashAmount)
            {
                _propertyBlock.SetFloat(FlashAmountId, Mathf.Clamp01(_flashAmount + _currentGlow * 0.35f));
            }

            if (_hasOverlayOffset)
            {
                // UV-space parallax and idle drift are disabled for pixel-art cards.
                //
                // Shifting _OverlayOffset while the base card art stays fixed at UV 0,0→1,1
                // creates a hard seam wherever the overlay has sharp-edged content (card frames,
                // borders, icons). With Point (nearest-neighbor) texture filtering the offset
                // snaps to integer pixel boundaries, making the split sharply visible as a
                // diagonal tear when both X and Y shift simultaneously.
                //
                // The tilt feel is already delivered by transform.rotation in UpdateTilt —
                // the whole card rotates as a rigid mesh, which is safe for pixel art.
                // To re-enable UV parallax: switch overlay textures to Bilinear/Trilinear
                // filtering AND ensure the overlay has no hard edges that would produce a seam.
                _propertyBlock.SetVector(OverlayOffsetId, _baseOverlayOffset);
            }

            float hover01 = _isHovered ? 1f : 0f;
            float drag01 = _card != null && _card.IsBeingDragged ? 1f : 0f;

            if (_hasBrightness)
            {
                float brightness = _baseBrightness
                    + hover01 * _profile.HoverBrightnessBoost
                    + drag01 * _profile.DragBrightnessBoost
                    + _flashAmount * 0.12f;
                _propertyBlock.SetFloat(BrightnessId, brightness);
            }

            if (_hasSaturation)
            {
                _propertyBlock.SetFloat(SaturationId, _baseSaturation + hover01 * _profile.HoverSaturationBoost);
            }

            if (_hasHueShift)
            {
                float huePhase = Time.unscaledTime * _profile.IdleHueShiftFrequency + _phaseOffset;
                _propertyBlock.SetFloat(HueShiftId, _baseHueShift + Mathf.Sin(huePhase) * _profile.IdleHueShiftAmount);
            }

            if (_hasEmissionColor)
            {
                Color glow = _profile.GlowColor * Mathf.Clamp01(_currentGlow + _flashAmount * 0.35f);
                _propertyBlock.SetColor(EmissionColorId, _baseEmissionColor + glow);
            }

            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResetMaterialFeedback()
        {
            if (_renderer == null || _propertyBlock == null)
            {
                return;
            }

            _propertyBlock.Clear();
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResetMaterialFlags()
        {
            _hasFlashAmount = false;
            _hasOverlayOffset = false;
            _hasBrightness = false;
            _hasSaturation = false;
            _hasHueShift = false;
            _hasEmissionColor = false;
        }

        // ── Hover-lift helpers ────────────────────────────────────────────────────

        private float ComputeStackBaseY()
        {
            if (_card == null) return 0f;
            var stack = _card.Stack;
            if (stack == null || _card.Settings == null) return 0f;
            int idx = stack.Cards.IndexOf(_card);
            return idx < 0 ? 0f : stack.TargetPosition.y + _card.Settings.StackStep.y * idx;
        }

        // ── Sort-order helpers ────────────────────────────────────────────────────

        private void CacheRenderers()
        {
            _allRenderers = GetComponentsInChildren<Renderer>(true);
            _savedSortingOrders = new int[_allRenderers.Length];
            for (int i = 0; i < _allRenderers.Length; i++)
                _savedSortingOrders[i] = _allRenderers[i].sortingOrder;
        }

        // Elevates every renderer on this card by +100 sort order so the hovered card's
        // transparent layers (TMP text, alpha-tested borders) draw over those on all
        // neighbors regardless of camera-space depth.
        private void ElevateSortOrder()
        {
            if (_allRenderers == null) return;
            for (int i = 0; i < _allRenderers.Length; i++)
                _allRenderers[i].sortingOrder = _savedSortingOrders[i] + 100;
        }

        private void RestoreSortOrder()
        {
            if (_allRenderers == null) return;
            for (int i = 0; i < _allRenderers.Length; i++)
                _allRenderers[i].sortingOrder = _savedSortingOrders[i];
        }

        // ── Scale-tween helpers ───────────────────────────────────────────────────

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        public void KillFeelTweens()
        {
            KillScaleTween();

            _rotationTween?.Kill();
            _rotationTween = null;

            _flashTween?.Kill();
            _flashTween = null;

            _punchYaw = 0f;
            _flashAmount = 0f;
            _currentGlow = 0f;
            _currentHoverLift = 0f;
            _currentTilt = Vector2.zero;
            RestoreSortOrder();
            transform.rotation = Quaternion.identity;
            ApplyMaterialFeedback();
        }
    }
}
