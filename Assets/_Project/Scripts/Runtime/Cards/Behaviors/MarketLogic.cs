using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CardInstance))]
    public class MarketLogic : MonoBehaviour, IOnStackable, IClickable
    {
        private CardInstance card;
        private CardDefinition currency;
        private int currentIndex;

        private MarketDefinition MarketDef => card?.Definition as MarketDefinition;

        private MarketDefinition.MarketListing CurrentListing
        {
            get
            {
                var def = MarketDef;
                if (def == null || def.Listings == null || def.Listings.Count == 0) return null;
                return def.Listings[currentIndex];
            }
        }

        public void Initialize(CardInstance cardInstance)
        {
            card = cardInstance;
            currency = TradeManager.Instance?.CurrencyCard;
            RefreshDisplay();
        }

        public bool OnStack(CardStack droppedStack)
        {
            var listing = CurrentListing;
            if (listing?.card == null || currency == null) return false;
            if (CountPayment(droppedStack) < listing.price) return false;

            card.PlayPuffParticle();
            ProcessPurchase(droppedStack, listing);
            return true;
        }

        public bool OnClick(Vector3 clickPosition)
        {
            var def = MarketDef;
            if (def == null || def.Listings.Count <= 1) return false;

            currentIndex = (currentIndex + 1) % def.Listings.Count;
            RefreshDisplay();
            AudioManager.Instance?.PlaySFX(AudioId.CardSwipe);
            return true;
        }

        private int CountPayment(CardStack stack)
        {
            int total = 0;
            foreach (var c in stack.Cards)
            {
                if (c.Definition.Category == CardCategory.Currency)
                    total++;
                else if (c.TryGetComponent<ChestLogic>(out var chest))
                    total += chest.StoredCoins;
            }
            return total;
        }

        private void ProcessPurchase(CardStack droppedStack, MarketDefinition.MarketListing listing)
        {
            int remaining = listing.price;
            var cards = droppedStack.Cards.ToList();
            bool statsChanged = false;

            foreach (var c in cards)
            {
                if (remaining <= 0) break;

                if (c.Definition.Category == CardCategory.Currency)
                {
                    droppedStack.DestroyCard(c);
                    remaining--;
                    statsChanged = true;
                }
                else if (c.TryGetComponent<ChestLogic>(out var chest))
                {
                    while (remaining > 0 && chest.TryWithdrawCoin(false))
                    {
                        remaining--;
                        statsChanged = true;
                    }
                }
            }

            if (remaining <= 0)
            {
                CardManager.Instance?.CreateCardInstance(
                    listing.card,
                    card.Stack.TargetPosition.Flatten()
                );

                AudioManager.Instance?.PlaySFX(AudioId.CashRegister);

                var def = MarketDef;
                if (def != null && def.Listings.Count > 1)
                    currentIndex = (currentIndex + 1) % def.Listings.Count;

                RefreshDisplay();
            }

            if (statsChanged)
                CardManager.Instance?.NotifyStatsChanged();
        }

        private void RefreshDisplay()
        {
            if (card == null) return;

            var listing = CurrentListing;
            if (listing == null)
            {
                card.UpdatePriceText("-");
                return;
            }

            card.UpdatePriceText(listing.price.ToString());
        }
    }
}
