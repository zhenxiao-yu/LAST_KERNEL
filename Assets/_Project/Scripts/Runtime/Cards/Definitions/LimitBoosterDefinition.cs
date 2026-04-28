using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Special Cards/Limit Booster Card", fileName = "Card_Booster_")]
    public class LimitBoosterDefinition : CardDefinition
    {
        [BoxGroup("Limit Booster")]
        [SerializeField, Min(1), Tooltip("How much extra card capacity this card provides.")]
        private int boostAmount = 4;

        public int BoostAmount => boostAmount;
    }
}
