// RewardData — ScriptableObject that describes what the player earns after a wave victory.

using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "Last Kernel/Defense/Reward Data", fileName = "Reward_")]
    public class RewardData : ScriptableObject
    {
        [BoxGroup("Currency")]
        [SerializeField, Min(0)] private int scrapAmount = 10;

        [BoxGroup("Card Pack")]
        [SerializeField, Tooltip("Leave null to skip the pack reward.")]
        private PackDefinition rewardPack;

        [BoxGroup("Presentation")]
        [SerializeField] private string rewardTitle = "Victory!";

        [BoxGroup("Presentation")]
        [SerializeField, TextArea(2, 4)] private string rewardDescription = "The attack has been repelled.";

        public int            ScrapAmount       => scrapAmount;
        public PackDefinition RewardPack        => rewardPack;
        public string         RewardTitle       => rewardTitle;
        public string         RewardDescription => rewardDescription;
    }
}
