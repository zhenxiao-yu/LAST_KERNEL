using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Pack", fileName = "Pack_")]
    public class PackDefinition : CardDefinition
    {
        [BoxGroup("Pack")]
        [SerializeField, Min(0)]
        [ValidateInput("@buyPrice > 0", "Pack buyPrice should be greater than 0 unless intentionally free.")]
        private int buyPrice = 3;

        [BoxGroup("Pack")]
        [SerializeField, Min(0), Tooltip("Minimum quests required to unlock this pack.")]
        private int minQuests = 3;

        [BoxGroup("Pack")]
        [SerializeField, ListDrawerSettings(ShowIndexLabels = true, ListElementLabelName = "SlotLabel")]
        [ValidateInput("@slots != null && slots.Count > 0", "Pack has no slots — it will drop nothing.")]
        private List<PackSlot> slots;

        public int BuyPrice => buyPrice;
        public int MinQuests => minQuests;
        public List<PackSlot> Slots => slots;
    }
}
