using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Markyu.LastKernel
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CardInstance))]
    public class CardFeelPresenter : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        private static readonly int FlashAmountId    = Shader.PropertyToID("_FlashAmount");
        private static readonly int OverlayOffsetId  = Shader.PropertyToID("_OverlayOffset");
        private static readonly int BrightnessId     = Shader.PropertyToID("_Brightness");
        private static readonly int SaturationId     = Shader.PropertyToID("_Saturation");
        private static readonly int HueShiftId       = Shader.PropertyToID("_HueShift");
        private static readonly int EmissionColorId  = Shader.PropertyToID("_EmissionColor");

        [BoxGroup("References")]
        [Required, SerializeField] private Transform visualRoot;

        private CardInstance              _card;
        private CardFeelProfile           _profile;
        private CardRenderOrderController _renderOrder;
        private MeshRenderer              _renderer;
        private MaterialPropertyBlock     _propertyBlock;

        private Tween _scaleTween;
        private Tween _flashTween;

        private float _phaseOffset;
        private float _flashAmount;
        private float _currentGlow;

        private Vector4 _baseOverlayOffset;
        private Color   _baseEmissionColor;
        private float   _baseBrightness;
        private float   _baseSaturation;
        private float   _baseHueShift;

        private bool _hasFlashAmount;
        private bool _hasOverlayOffset;
        private bool _hasBrightness;
        private bool _hasSaturation;
        private bool _hasHueShift;
        private bool _hasEmissionColor;

        private bool _isHovered;
        private bool _initialized;
        // Fix C-5: track ownership so only the instance that set IsDraggingAny clears it.
        // Without this, any card being destroyed/disabled (e.g. mid-drag loot drops)
        // would unconditionally clear the flag, breaking hover suppression on other cards.
        private bool _ownsIsDraggingAny;

        public static bool IsDraggingAny { get; private set; }
        public bool IsInitialized => _initialized;

        private void ClearDraggingOwnership()
        {
            if (_ownsIsDraggingAny)
            {
                IsDraggingAny      = false;
                _ownsIsDraggingAny = false;
            }
        }

        public static CardFeelPresenter EnsureOn(GameObject owner)
        {
            if (owner == null) return null;

            return owner.TryGetComponent(out CardFeelPresenter presenter)
                ? presenter
                : owner.AddComponent<CardFeelPresenter>();
        }

        private void Awake()
        {
            _card        = GetComponent<CardInstance>();
            _renderOrder = GetComponent<CardRenderOrderController>();
            _propertyBlock = new MaterialPropertyBlock();
            _phaseOffset = Random.value * Mathf.PI * 2f;

            ResolveVisualRoot();
            ResolveRenderer();
        }

        public void Initialize(CardFeelProfile profile)
        {
            if (profile == null)
            {
                Debug.LogWarning($"CardFeelPresenter: '{name}' has no CardFeelProfile assigned.", this);
                _initialized = false;
                return;
            }

            _profile     = profile;
            _initialized = true;

            ResolveVisualRoot();
            ResolveRenderer();
            CacheMaterialDefaults();

            visualRoot.localScale    = Vector3.one * _profile.SpawnStartScale;
            visualRoot.localRotation = Quaternion.identity;

            _flashAmount = 0f;
            _currentGlow = 0f;

            ApplyMaterialFeedback();
            PlaySpawnPop();
        }

        private void Update()
        {
            if (!_initialized || _card == null || _profile == null) return;

            UpdateMaterialFeedback(Time.unscaledDeltaTime);
        }

        private void OnDisable()
        {
            ClearDraggingOwnership();
            _isHovered    = false;

            _renderOrder?.SetHovered(false);
            _renderOrder?.SetMerging(false);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale    = Vector3.one;
            }

            ResetMaterialFeedback();
        }

        public void OnPointerEnter(PointerEventData _)
        {
            if (!_initialized || _profile == null || _card.IsBeingDragged || IsDraggingAny) return;

            _isHovered = true;
            _renderOrder?.SetHovered(true);

            ScaleTo(_profile.HoverScale, _profile.HoverScaleDuration, _profile.HoverScaleEase);
            PulseFlash(_profile.HoverFlashAmount, _profile.FlashReturnDuration);
        }

        public void OnPointerExit(PointerEventData _)
        {
            if (!_initialized || _profile == null || _card.IsBeingDragged) return;

            _isHovered = false;
            _renderOrder?.SetHovered(false);

            ScaleTo(1f, _profile.HoverScaleDuration, _profile.HoverScaleEase);
        }

        public void OnPickup()
        {
            if (!_initialized || _profile == null) return;

            _isHovered         = false;
            IsDraggingAny      = true;
            _ownsIsDraggingAny = true;

            KillScaleTween();
            PulseFlash(_profile.PickupFlashAmount, _profile.FlashReturnDuration);

            float peakScale  = 1f + _profile.PickupPunchAmount;
            float holdScale  = _profile.DragHoldScale;
            float halfDur    = _profile.PickupPunchDuration * 0.45f;

            _scaleTween = DOTween.Sequence()
                .Append(visualRoot.DOScale(Vector3.one * peakScale, halfDur).SetEase(Ease.OutQuad))
                .Append(visualRoot.DOScale(Vector3.one * holdScale,  halfDur).SetEase(Ease.InOutQuad))
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        public void OnRelease()
        {
            if (!_initialized || _profile == null) return;

            ClearDraggingOwnership();

            float restScale = _isHovered ? _profile.HoverScale : 1f;

            KillScaleTween();

            _scaleTween = DOTween.Sequence()
                .Append(visualRoot.DOScale(Vector3.one * _profile.DropSquishScale, _profile.DropSquishDuration).SetEase(Ease.OutQuad))
                .Append(visualRoot.DOScale(Vector3.one * restScale, _profile.DropSettleDuration)
                    .SetEase(_profile.DropSettleEase, _profile.DropSettleOvershoot))
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => visualRoot.localScale = Vector3.one * restScale);
        }

        public void OnMergeReceived()
        {
            if (!_initialized || _profile == null) return;

            _renderOrder?.SetMerging(true);
            KillScaleTween();
            PulseFlash(_profile.MergeFlashAmount, _profile.FlashReturnDuration);

            _scaleTween = visualRoot
                .DOPunchScale(
                    Vector3.one * _profile.MergePunchAmount,
                    _profile.MergePunchDuration,
                    _profile.MergePunchVibrato,
                    elasticity: 0.5f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => _renderOrder?.SetMerging(false));
        }

        public bool OnDamageTaken()
        {
            if (!_initialized || _profile == null) return false;

            PulseFlash(_profile.DamageFlashAmount, _profile.FlashReturnDuration);

            KillScaleTween();
            _scaleTween = visualRoot
                .DOPunchScale(
                    Vector3.one * _profile.DamageShakeAmount,
                    _profile.DamageShakeDuration,
                    vibrato: 6,
                    elasticity: 0.3f)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => visualRoot.localScale = Vector3.one);

            return true;
        }

        private void UpdateMaterialFeedback(float deltaTime)
        {
            float targetGlow = 0f;

            if (_card.IsBeingDragged)
                targetGlow = _profile.DragGlowIntensity;
            else if (_isHovered)
                targetGlow = _profile.HoverGlowIntensity;

            _currentGlow = Mathf.Lerp(_currentGlow, targetGlow, 1f - Mathf.Exp(-14f * deltaTime));

            ApplyMaterialFeedback();
        }

        private void PlaySpawnPop()
        {
            KillScaleTween();
            PulseFlash(_profile.HoverFlashAmount, _profile.FlashReturnDuration);

            _scaleTween = visualRoot
                .DOScale(Vector3.one, _profile.SpawnDuration)
                .From(Vector3.one * _profile.SpawnStartScale)
                .SetEase(_profile.SpawnEase, _profile.SpawnOvershoot)
                .SetUpdate(true)
                .SetLink(gameObject)
                .OnComplete(() => visualRoot.localScale = Vector3.one);
        }

        private void PulseFlash(float amount, float returnDuration)
        {
            if (amount <= 0f) return;

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
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void ScaleTo(float targetUniform, float duration, Ease ease)
        {
            KillScaleTween();

            if (visualRoot == null) return;

            _scaleTween = visualRoot
                .DOScale(Vector3.one * targetUniform, duration)
                .SetEase(ease)
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private void ResolveVisualRoot()
        {
            if (visualRoot != null) return;

            Transform existing = transform.Find("Visual");
            if (existing != null)
            {
                visualRoot = existing;
                return;
            }

            visualRoot = transform;
            Debug.LogWarning(
                $"CardFeelPresenter: '{name}' has no child named 'Visual'. " +
                "Using root as fallback — create a Visual child for best results.",
                this);
        }

        private void ResolveRenderer()
        {
            if (visualRoot != null)
                _renderer = visualRoot.GetComponentInChildren<MeshRenderer>(true);

            if (_renderer == null)
                _renderer = GetComponentInChildren<MeshRenderer>(true);
        }

        private void CacheMaterialDefaults()
        {
            ResolveRenderer();

            Material material = _renderer != null ? _renderer.sharedMaterial : null;

            if (material == null)
            {
                ResetMaterialFlags();
                return;
            }

            _hasFlashAmount  = material.HasProperty(FlashAmountId);
            _hasOverlayOffset = material.HasProperty(OverlayOffsetId);
            _hasBrightness   = material.HasProperty(BrightnessId);
            _hasSaturation   = material.HasProperty(SaturationId);
            _hasHueShift     = material.HasProperty(HueShiftId);
            _hasEmissionColor = material.HasProperty(EmissionColorId);

            _baseOverlayOffset = _hasOverlayOffset  ? material.GetVector(OverlayOffsetId) : Vector4.zero;
            _baseBrightness    = _hasBrightness      ? material.GetFloat(BrightnessId)     : 1f;
            _baseSaturation    = _hasSaturation      ? material.GetFloat(SaturationId)     : 1f;
            _baseHueShift      = _hasHueShift        ? material.GetFloat(HueShiftId)       : 0f;
            _baseEmissionColor = _hasEmissionColor   ? material.GetColor(EmissionColorId)  : Color.black;
        }

        private void ApplyMaterialFeedback()
        {
            if (_renderer == null || _propertyBlock == null || _profile == null) return;

            _renderer.GetPropertyBlock(_propertyBlock);

            if (_hasFlashAmount)
                _propertyBlock.SetFloat(FlashAmountId, Mathf.Clamp01(_flashAmount + _currentGlow * 0.35f));

            if (_hasOverlayOffset)
                _propertyBlock.SetVector(OverlayOffsetId, _baseOverlayOffset);

            float hover01 = _isHovered ? 1f : 0f;
            float drag01  = _card != null && _card.IsBeingDragged ? 1f : 0f;

            if (_hasBrightness)
            {
                float brightness = _baseBrightness
                    + hover01 * _profile.HoverBrightnessBoost
                    + drag01  * _profile.DragBrightnessBoost
                    + _flashAmount * 0.12f;

                _propertyBlock.SetFloat(BrightnessId, brightness);
            }

            if (_hasSaturation)
                _propertyBlock.SetFloat(SaturationId, _baseSaturation + hover01 * _profile.HoverSaturationBoost);

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
            if (_renderer == null || _propertyBlock == null) return;

            _propertyBlock.Clear();
            _renderer.SetPropertyBlock(_propertyBlock);
        }

        private void ResetMaterialFlags()
        {
            _hasFlashAmount   = false;
            _hasOverlayOffset = false;
            _hasBrightness    = false;
            _hasSaturation    = false;
            _hasHueShift      = false;
            _hasEmissionColor = false;
        }

        private void KillScaleTween()
        {
            _scaleTween?.Kill();
            _scaleTween = null;
        }

        public void RefreshMaterialState()
        {
            if (!_initialized || _profile == null) return;

            CacheMaterialDefaults();
            ApplyMaterialFeedback();
        }

        public void KillFeelTweens()
        {
            KillScaleTween();

            _flashTween?.Kill();
            _flashTween = null;

            _flashAmount  = 0f;
            _currentGlow  = 0f;
            ClearDraggingOwnership();

            _renderOrder?.SetHovered(false);
            _renderOrder?.SetMerging(false);

            if (visualRoot != null)
            {
                visualRoot.localRotation = Quaternion.identity;
                visualRoot.localScale    = Vector3.one;
            }

            ApplyMaterialFeedback();
        }
    }
}
