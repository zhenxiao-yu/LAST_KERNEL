using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Special Cards/Chest Card", fileName = "Card_Chest_")]
    public class ChestDefinition : CardDefinition
    {
        [BoxGroup("Chest")]
        [SerializeField, Min(1), Tooltip("How many currency units this chest can hold (e.g., Coins, Corals).")]
        private int capacity = 50;

        public int Capacity => capacity;
    }
}
