using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class CardBuyer : TradeZone
    {
        [BoxGroup("UI Components")]
        [SerializeField, Tooltip("Displays the texture of the currency used by this buyer.")]
        private MeshRenderer iconRenderer;

        [BoxGroup("Visual Effects")]
        [SerializeField, Tooltip("Glitch-destroy effect prefab spawned on successful sale. Create a prefab with GlitchDestroyEffect + a ParticleSystem and assign it here.")]
        private GlitchDestroyEffect glitchDestroyEffect;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Scale multiplier for the idle breathing animation.")]
        private float idlePulseScale = 1.02f;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Duration of one idle breathing half-cycle (seconds).")]
        private float idlePulseDuration = 1.8f;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Scale multiplier applied when the pointer hovers over the zone.")]
        private float hoverScaleMultiplier = 1.06f;

        [BoxGroup("Animations")]
        [SerializeField, Tooltip("Duration of the hover scale in/out transition (seconds).")]
        private float hoverScaleDuration = 0.12f;

        private CardDefinition currencyCard;
        private Tween _idleTween;

        protected override void Start()
        {
            base.Start();
            StartIdleAnimation();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            _idleTween?.Kill();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            _idleTween?.Kill();
        }

        private void StartIdleAnimation()
        {
            _idleTween?.Kill();
            _idleTween = transform
                .DOScale(_baseLocalScale * idlePulseScale, idlePulseDuration)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }

        public override void SetHighlighted(bool value)
        {
            if (value)
                _idleTween?.Kill();

            base.SetHighlighted(value);

            if (!value)
                StartIdleAnimation();
        }

        protected override void OnZonePointerEnter()
        {
            _idleTween?.Kill();
            transform
                .DOScale(_baseLocalScale * hoverScaleMultiplier, hoverScaleDuration)
                .SetEase(Ease.OutBack)
                .SetLink(gameObject);
            AudioManager.Instance?.PlaySFX(AudioId.Pop, transform.position);
        }

        protected override void OnZonePointerExit()
        {
            transform
                .DOScale(_baseLocalScale, hoverScaleDuration)
                .SetEase(Ease.OutCubic)
                .OnComplete(StartIdleAnimation)
                .SetLink(gameObject);
        }

        public override void Initialize(CardDefinition currencyDef, Vector3 spawnOffset)
        {
            base.Initialize(currencyDef, spawnOffset);

            if (currencyDef.Category is CardCategory.Currency)
            {
                this.currencyCard = currencyDef;
                iconRenderer.material.SetTexture("_MainTex", currencyCard.ArtTexture);
            }
            else
            {
                Debug.LogError($"Invalid CardDefinition category: {currencyDef.Category}. Expected Currency.", this);
            }
        }

        public override bool CanTrade(CardStack droppedStack)
        {
            return droppedStack.Cards.All(card =>
            {
                if (!card.Definition.IsSellable)
                    return false;

                if (card.TryGetComponent<ChestLogic>(out var chestLogic))
                {
                    if (chestLogic.StoredCoins > 0)
                        return false;
                }

                return true;
            });
        }

        protected override void ProcessTransaction(CardStack droppedStack)
        {
            int totalSellValue = droppedStack.Cards.Sum(card => card.Definition.SellPrice);

            TradeManager.Instance?.NotifyCardsSold(droppedStack);
            AudioManager.Instance?.PlaySFX(AudioId.Coins, transform.position);
            AudioManager.Instance?.PlaySFX(AudioId.CashRegister, transform.position);
            droppedStack.DestroyAllCards();

            if (glitchDestroyEffect != null)
                Instantiate(glitchDestroyEffect, transform.position, Quaternion.identity);

            for (int i = 0; i < totalSellValue; i++)
                CardManager.Instance?.CreateCardInstance(currencyCard, spawnPosition);

            CardManager.Instance?.NotifyStatsChanged();
        }

        protected override void PlaySellSuccessAnimation()
        {
            _idleTween?.Kill();
            transform.localScale = _baseLocalScale;
            transform
                .DOPunchScale(Vector3.one * 0.18f, 0.35f, 7, 0.5f)
                .SetLink(gameObject)
                .OnComplete(StartIdleAnimation);
        }

        public override (string, string) GetInfo()
        {
            return (
                GameLocalization.Get("trade.buyerHeader"),
                GameLocalization.Get("trade.buyerBody")
            );
        }
    }
}
