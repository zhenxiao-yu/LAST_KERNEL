using System.Linq;
using TMPro;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class BoardExpansionVendor : TradeZone
    {
        private Board targetBoard;
        private int baseRowPrice;
        private int priceIncreasePerRow;
        private int paidAmount;

        private TextMeshPro titleText;
        private TextMeshPro priceText;
        private TextMeshPro trackerText;

        public int PaidAmount => paidAmount;

        private int CurrentRowPrice
        {
            get
            {
                int purchasedRows = targetBoard != null ? targetBoard.PurchasedExpansionRows : 0;
                return Mathf.Max(1, baseRowPrice + purchasedRows * priceIncreasePerRow);
            }
        }

        public override void Initialize(CardDefinition definition, Vector3 spawnOffset)
        {
            base.Initialize(definition, spawnOffset);
        }

        public void Initialize(Vector3 spawnOffset, int basePrice, int priceStep)
        {
            base.Initialize(null, spawnOffset);
            baseRowPrice = Mathf.Max(1, basePrice);
            priceIncreasePerRow = Mathf.Max(0, priceStep);

            CreateLabels();
            RefreshText();
        }

        public void Bind(Board board)
        {
            targetBoard = board;
            ClampPaidAmount();
            RefreshText();
        }

        public void RestoreState(int loadedPaidAmount)
        {
            paidAmount = Mathf.Max(0, loadedPaidAmount);
            ClampPaidAmount();
            RefreshText();
        }

        private void OnEnable()
        {
            GameLocalization.LanguageChanged += HandleLanguageChanged;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            GameLocalization.LanguageChanged -= HandleLanguageChanged;
        }

        public override bool CanTrade(CardStack droppedStack)
        {
            EnsureBoard();

            return IsExpansionAvailable() &&
                   droppedStack != null &&
                   droppedStack.Cards.Count > 0 &&
                   droppedStack.Cards.All(IsPaymentCard);
        }

        protected override void ProcessTransaction(CardStack droppedStack)
        {
            EnsureBoard();

            if (!IsExpansionAvailable() || droppedStack == null)
            {
                RefreshText();
                return;
            }

            bool statsChanged = false;
            int rowPrice = CurrentRowPrice;

            while (paidAmount < rowPrice && droppedStack.Cards.Count > 0)
            {
                var bottomCard = droppedStack.BottomCard;
                if (bottomCard == null)
                {
                    break;
                }

                if (bottomCard.Definition.Category == CardCategory.Currency)
                {
                    droppedStack.DestroyCard(bottomCard);
                    paidAmount++;
                    statsChanged = true;
                }
                else if (bottomCard.TryGetComponent<ChestLogic>(out var chestLogic))
                {
                    if (!chestLogic.TryWithdrawCoin(false))
                    {
                        break;
                    }

                    paidAmount++;
                    statsChanged = true;
                }
                else
                {
                    break;
                }
            }

            if (paidAmount >= rowPrice && targetBoard.TryPurchaseExpansionRow())
            {
                paidAmount = 0;
                AudioManager.Instance?.PlaySFX(AudioId.CashRegister);
            }

            if (statsChanged)
            {
                CardManager.Instance?.NotifyStatsChanged();
            }

            ClampPaidAmount();
            RefreshText();
        }

        public override (string, string) GetInfo()
        {
            EnsureBoard();

            if (!IsExpansionAvailable())
            {
                return (
                    GameLocalization.Get("trade.expansionHeader"),
                    GameLocalization.Get("trade.expansionCompleteBody")
                );
            }

            return (
                GameLocalization.Get("trade.expansionHeader"),
                GameLocalization.Format("trade.expansionBody", CurrentRowPrice - paidAmount)
            );
        }

        private bool IsExpansionAvailable()
        {
            return targetBoard != null && targetBoard.CanPurchaseExpansionRow;
        }

        private bool IsPaymentCard(CardInstance card)
        {
            if (card == null)
            {
                return false;
            }

            if (card.Definition.Category == CardCategory.Currency)
            {
                return true;
            }

            return card.TryGetComponent<ChestLogic>(out var chestLogic) && chestLogic.StoredCoins > 0;
        }

        private void EnsureBoard()
        {
            if (targetBoard == null)
            {
                targetBoard = Board.Instance;
            }
        }

        private void ClampPaidAmount()
        {
            if (!IsExpansionAvailable())
            {
                paidAmount = 0;
                return;
            }

            paidAmount = Mathf.Clamp(paidAmount, 0, CurrentRowPrice - 1);
        }

        private void CreateLabels()
        {
            if (titleText != null)
            {
                return;
            }

            titleText = CreateLabel("Title", new Vector3(0f, 0.02f, -0.32f), new Vector2(0.85f, 0.3f), 1.05f);
            priceText = CreateLabel("Price", new Vector3(0f, 0.02f, 0f), new Vector2(0.85f, 0.25f), 0.72f);
            trackerText = CreateLabel("Tracker", new Vector3(0f, 0.02f, 0.32f), new Vector2(0.85f, 0.3f), 0.58f);
        }

        private TextMeshPro CreateLabel(string labelName, Vector3 localPosition, Vector2 size, float fontSize)
        {
            var labelObject = new GameObject(labelName, typeof(RectTransform));
            labelObject.transform.SetParent(transform, false);
            labelObject.transform.localPosition = localPosition;
            labelObject.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

            var rect = (RectTransform)labelObject.transform;
            rect.sizeDelta = size;

            var label = labelObject.AddComponent<TextMeshPro>();
            label.alignment = TextAlignmentOptions.Center;
            label.color = Color.white;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.fontSize = fontSize;
            label.richText = true;
            return label;
        }

        private void RefreshText()
        {
            EnsureBoard();

            if (titleText != null)
            {
                titleText.text = GameLocalization.Get("trade.expansionTitle");
            }

            if (priceText != null)
            {
                priceText.text = IsExpansionAvailable()
                    ? GameLocalization.Format("trade.price", CurrentRowPrice - paidAmount)
                    : GameLocalization.Get("trade.expansionComplete");
            }

            if (trackerText != null)
            {
                int purchasedRows = targetBoard != null ? targetBoard.PurchasedExpansionRows : 0;
                int maxRows = targetBoard != null ? targetBoard.MaxPurchasedExpansionRows : 0;
                trackerText.text = GameLocalization.Format("trade.expansionProgress", purchasedRows, maxRows);
            }
        }

        private void HandleLanguageChanged(GameLanguage _)
        {
            RefreshText();
        }
    }
}
