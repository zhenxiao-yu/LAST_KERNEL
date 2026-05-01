using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel.Achievements
{
    public enum AchievementTrigger
    {
        Custom          = 0,
        CardCreated     = 10,
        CardKilled      = 11,
        CardDiscovered  = 12,
        CardEquipped    = 13,
        RecipeDiscovered = 20,
        RecipeCrafted   = 21,
        QuestCompleted  = 30,
        NightSurvived   = 40,
        DayReached      = 41,
        GameWon         = 50,
        GameLost        = 51,
    }

    // Cumulative: progress accumulates (craft 10 things, kill 50 enemies).
    // Threshold:  progress is SET to the incoming value and checked against target (reach day 30).
    public enum AchievementCountMode { Cumulative, Threshold }

    [CreateAssetMenu(menuName = "Last Kernel/Achievement Definition", fileName = "ACH_New")]
    public class AchievementDefinition : ScriptableObject
    {
        [BoxGroup("Identity"), Required]
        [Tooltip("Must match the achievement API key on Steam/Epic exactly.")]
        public string Id;

        [BoxGroup("Identity")]
        [Tooltip("Localization table key for title + description.")]
        public string LocalizationKey;

        [BoxGroup("Identity"), PreviewField(55)]
        public Sprite Icon;

        [BoxGroup("Trigger")]
        public AchievementTrigger Trigger;

        [BoxGroup("Trigger")]
        [Tooltip("Optional: only match events with this ID (CardDefinition.Id, QuestId, etc.). Leave empty to match any.")]
        public string FilterId;

        [BoxGroup("Progress")]
        public AchievementCountMode CountMode = AchievementCountMode.Cumulative;

        [BoxGroup("Progress"), MinValue(1)]
        public int TargetCount = 1;

        [BoxGroup("Progress"), EnableIf("@TargetCount > 1")]
        public bool ShowProgressBar;

        [BoxGroup("Flags")]
        public bool IsSecret;

#if UNITY_EDITOR
        [BoxGroup("Identity"), ShowInInspector, ReadOnly]
        private string ValidationStatus => string.IsNullOrEmpty(Id) ? "⚠ Id is required" : "✓";
#endif
    }
}
