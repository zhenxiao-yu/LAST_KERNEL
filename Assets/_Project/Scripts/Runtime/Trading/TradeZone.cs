using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(MeshRenderer), typeof(BoxCollider))]
    public abstract class TradeZone : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [BoxGroup("Visual Effects")]
        [SerializeField, Tooltip("Particle effect instantiated when a successful transaction occurs.")]
        private PuffParticle puffParticle;

        [BoxGroup("Visual Effects")]
        [SerializeField, Tooltip("Material used by the highlight system to draw the zone outline.")]
        private Material outlineMaterial;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Scale multiplier applied when the zone pulses during drag-hover highlight.")]
        private float highlightPulseScale = 1.05f;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Duration of one highlight pulse half-cycle (seconds).")]
        private float highlightPulseDuration = 0.45f;

        protected Vector3 _baseLocalScale = Vector3.one;

        private Tween _highlightTween;
        private Tween _rejectTween;

        private Vector3 spawnOffset;

        protected Vector3 spawnPosition => transform.position + spawnOffset;

        private Highlight highlight;

        protected virtual void Start()
        {
            _baseLocalScale = transform.localScale;
        }

        public virtual void Initialize(CardDefinition definition, Vector3 spawnOffset)
        {
            this.spawnOffset = spawnOffset;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            InfoPanel.Instance?.RegisterHover(GetInfo());
            OnZonePointerEnter();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            InfoPanel.Instance?.UnregisterHover();
            OnZonePointerExit();
        }

        protected virtual void OnZonePointerEnter() { }
        protected virtual void OnZonePointerExit() { }

        protected virtual void OnDisable()
        {
            InfoPanel.Instance?.UnregisterHover();
            _highlightTween?.Kill();
            _rejectTween?.Kill();
        }

        protected virtual void OnDestroy()
        {
            _highlightTween?.Kill();
            _rejectTween?.Kill();
        }

        public bool TryTradeAndConsumeStack(CardStack droppedStack)
        {
            if (CanTrade(droppedStack))
            {
                PlayPuffParticle();
                ProcessTransaction(droppedStack);
                PlaySellSuccessAnimation();
                return droppedStack.Cards.Count == 0;
            }

            PlayRejectAnimation();
            return false;
        }

        public abstract bool CanTrade(CardStack droppedStack);
        protected abstract void ProcessTransaction(CardStack droppedStack);

        protected virtual void PlaySellSuccessAnimation()
        {
            _highlightTween?.Kill();
            transform.localScale = _baseLocalScale;
            transform
                .DOPunchScale(Vector3.one * 0.12f, 0.3f, 6, 0.4f)
                .SetLink(gameObject);
        }

        protected virtual void PlayRejectAnimation()
        {
            _rejectTween?.Kill();
            _rejectTween = transform
                .DOShakePosition(0.25f, new Vector3(0.07f, 0f, 0.07f), 12, 0f)
                .SetLink(gameObject);
            AudioManager.Instance?.PlaySFX(AudioId.Pop, transform.position);
        }

        public void CopyVisualEffectsFrom(TradeZone source)
        {
            if (source == null) return;
            puffParticle = source.puffParticle;
            outlineMaterial = source.outlineMaterial;
        }

        public virtual void SetHighlighted(bool value)
        {
            if (outlineMaterial == null) return;

            if (highlight == null)
            {
                var filter = GetComponent<MeshFilter>();
                if (filter == null || filter.mesh == null) return;
                highlight = new Highlight(transform, filter.mesh, outlineMaterial);
            }

            highlight.SetActive(value);

            _highlightTween?.Kill();

            if (value)
            {
                AudioManager.Instance?.PlaySFX(AudioId.Click, transform.position);
                transform.localScale = _baseLocalScale;
                _highlightTween = transform
                    .DOScale(_baseLocalScale * highlightPulseScale, highlightPulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetLink(gameObject);
            }
            else
            {
                _highlightTween = transform
                    .DOScale(_baseLocalScale, 0.15f)
                    .SetEase(Ease.OutCubic)
                    .SetLink(gameObject);
            }
        }

        public void PlayPuffParticle()
        {
            if (puffParticle == null) return;
            Instantiate(puffParticle, transform.position, Quaternion.identity);
        }

        public abstract (string, string) GetInfo();
    }
}
