using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public enum QuestType
    {
        // Cards
        Have,
        Obtain,
        Discover,
        Defeat,
        Craft,

        // Actions
        Sell,
        Buy,
        Equip,
        Explore,

        // Time
        Time,
        Day,

        // Stats
        Food,
        Coins,
        Capacity
    }

    [CreateAssetMenu(fileName = "New Quest", menuName = "Last Kernel/Quest")]
    public class Quest : ScriptableObject
    {
        [BoxGroup("Info")]
        [ReadOnly, SerializeField, Tooltip("Unique identifier — auto-generated on creation.")]
        private string id;

        [BoxGroup("Info")]
        [SerializeField, Tooltip("Short display name of the quest.")]
        private string title;

        [BoxGroup("Info")]
        [SerializeField, TextArea, Tooltip("Detailed description or guide for the quest.")]
        private string description;

        [BoxGroup("Requirements")]
        [SerializeField, Tooltip("The type of action or condition required to complete this quest.")]
        private QuestType type;

        [BoxGroup("Requirements")]
        [ShowIf("@type == QuestType.Have || type == QuestType.Obtain || type == QuestType.Defeat || type == QuestType.Craft || type == QuestType.Sell || type == QuestType.Buy || type == QuestType.Equip || type == QuestType.Explore")]
        [SerializeField, Tooltip("Card required by this quest.")]
        private CardDefinition targetCard;

        [BoxGroup("Requirements")]
        [ShowIf("@type == QuestType.Discover")]
        [SerializeField, Tooltip("Recipe required for Discover quests.")]
        private RecipeDefinition targetRecipe;

        [BoxGroup("Requirements")]
        [HideIf("@type == QuestType.Discover || type == QuestType.Equip || type == QuestType.Explore || type == QuestType.Time")]
        [SerializeField, Min(1), Tooltip("Quantity or duration required.")]
        private int targetAmount = 1;

        [BoxGroup("Requirements")]
        [ShowIf("@type == QuestType.Time")]
        [SerializeField, Tooltip("Time pace condition for Time quests.")]
        private TimePace targetPace = TimePace.Normal;

        [BoxGroup("Flow")]
        [SerializeField, Tooltip("Quests that must be completed before this one unlocks.")]
        private List<Quest> prerequisiteQuests;

        [BoxGroup("Flow")]
        [SerializeField, Tooltip("Quests unlocked upon completion.")]
        private List<Quest> questsToUnlock;

        public string Id => id;
        public string Title => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "quest", "title"),
            title);

        public string Description => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "quest", "description"),
            description);

        public QuestType Type => type;
        public CardDefinition TargetCard => targetCard;
        public RecipeDefinition TargetRecipe => targetRecipe;
        public int TargetAmount => targetAmount;
        public TimePace TargetPace => targetPace;

        public List<Quest> PrerequisiteQuests => prerequisiteQuests;
        public List<Quest> QuestsToUnlock => questsToUnlock;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");

            if (type == QuestType.Discover || type == QuestType.Equip ||
                type == QuestType.Explore || type == QuestType.Time)
                targetAmount = 1;
        }
    }
}
