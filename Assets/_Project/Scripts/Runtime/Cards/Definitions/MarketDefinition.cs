using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Market Definition", fileName = "Card_Market_")]
    public class MarketDefinition : CardDefinition
    {
        [System.Serializable]
        public class MarketListing
        {
            [TableColumnWidth(180)]
            public CardDefinition card;

            [TableColumnWidth(50), Min(1)]
            public int price = 1;
        }

        [BoxGroup("Market")]
        [SerializeField, TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        [ValidateInput("@listings != null && listings.Count > 0", "Market needs at least one listing.")]
        private List<MarketListing> listings;

        public IReadOnlyList<MarketListing> Listings => listings;
    }
}
