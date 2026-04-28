using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class CardBuyer : TradeZone
    {
        [BoxGroup("UI Components")]
        [SerializeField, Tooltip("Displays the texture of the currency used by this buyer.")]
        private MeshRenderer iconRenderer;

        private CardDefinition currencyCard;

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
                // Basic sellable check
                if (!card.Definition.IsSellable)
                {
                    return false;
                }

                // If it's a chest, it must be empty to be sold
                if (card.TryGetComponent<ChestLogic>(out var chestLogic))
                {
                    // Return false if the chest is NOT empty
                    if (chestLogic.StoredCoins > 0)
                    {
                        return false;
                    }
                }

                // If it's not a chest, or it's an empty chest, it's fine
                return true;
            });
        }

        protected override void ProcessTransaction(CardStack droppedStack)
        {
            int totalSellValue = droppedStack.Cards.Sum(card => card.Definition.SellPrice);

            TradeManager.Instance?.NotifyCardsSold(droppedStack);
            AudioManager.Instance?.PlaySFX(AudioId.Coins);
            droppedStack.DestroyAllCards();

            for (int i = 0; i < totalSellValue; i++)
            {
                CardManager.Instance?.CreateCardInstance(currencyCard, spawnPosition);
            }

            CardManager.Instance?.NotifyStatsChanged();
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

