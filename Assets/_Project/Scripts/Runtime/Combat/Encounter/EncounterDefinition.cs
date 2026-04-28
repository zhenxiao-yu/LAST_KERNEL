using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    public enum EncounterType
    {
        SpecificDay,    // Happens exactly on Day X
        Recurring,      // Happens every X days (e.g. every 7 days)
        Range,          // Happens between Day X and Y
        MinimumDay      // Happens any day after Day X
    }

    [CreateAssetMenu(fileName = "Encounter_", menuName = "Last Kernel/Encounter")]
    public class EncounterDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [ReadOnly, SerializeField, Tooltip("Unique identifier — auto-generated, used in save files for one-time tracking.")]
        private string id;

        [BoxGroup("Identity")]
        [SerializeField, TextArea, Tooltip("Message shown to the player when this encounter triggers. Leave empty for a silent spawn.")]
        private string notificationMessage;

        [BoxGroup("Spawn")]
        [Required, SerializeField, Tooltip("Card to instantiate on the board.")]
        private CardDefinition cardToSpawn;

        [BoxGroup("Spawn")]
        [SerializeField, Min(1), Tooltip("How many instances to spawn (at random positions).")]
        private int count = 1;

        [BoxGroup("Spawn")]
        [SerializeField, Tooltip("If true, happens only once per save file.")]
        private bool oneTimeOnly = false;

        [BoxGroup("Conditions")]
        [SerializeField, Tooltip("Day-based trigger rule.")]
        private EncounterType type;

        [BoxGroup("Conditions")]
        [SerializeField, Tooltip("SpecificDay = exact day; Recurring = every N days; MinimumDay = from day N onward; Range = from day N.")]
        private int dayValue;

        [BoxGroup("Conditions")]
        [ShowIf("@type == EncounterType.Range")]
        [SerializeField, Tooltip("Last day (inclusive) for Range encounters.")]
        private int maxDayValue = 999;

        [BoxGroup("Priority & Chance")]
        [SerializeField, Tooltip("Higher values are evaluated first.")]
        private int priority = 0;

        [BoxGroup("Priority & Chance")]
        [SerializeField, Range(0f, 1f), Tooltip("1.0 = 100% chance.")]
        private float chance = 1.0f;

        [BoxGroup("Priority & Chance")]
        [SerializeField, Tooltip("Don't spawn if this many cards are already on the board.")]
        private int maxCardsOnBoardLimit = 100;

        public string Id => id;
        public string NotificationMessage => GameLocalization.GetOptional(
            LocalizationKeyBuilder.ForAsset(this, "encounter", "notification"),
            notificationMessage);

        public CardDefinition CardToSpawn => cardToSpawn;
        public int Count => count;
        public bool OneTimeOnly => oneTimeOnly;

        public EncounterType Type => type;

        public int Priority => priority;

        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");
        }

        public bool IsValidForDay(int currentDay, HashSet<string> completedEncounters, int totalCardsOnBoard, bool isFriendlyMode)
        {
            if (totalCardsOnBoard >= maxCardsOnBoardLimit) return false;
            if (oneTimeOnly && completedEncounters.Contains(id)) return false;
            if (cardToSpawn.IsAggressive && isFriendlyMode) return false;

            bool dayConditionMet = false;
            switch (type)
            {
                case EncounterType.SpecificDay:
                    dayConditionMet = (currentDay == dayValue);
                    break;
                case EncounterType.Recurring:
                    dayConditionMet = (currentDay > 0 && currentDay % dayValue == 0);
                    break;
                case EncounterType.MinimumDay:
                    dayConditionMet = (currentDay >= dayValue);
                    break;
                case EncounterType.Range:
                    dayConditionMet = (currentDay >= dayValue && currentDay <= maxDayValue);
                    break;
            }

            if (!dayConditionMet) return false;

            return Random.value <= chance;
        }
    }
}
