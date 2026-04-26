using System.Collections.Generic;

namespace Markyu.LastKernel
{
    public interface IQuestService
    {
        event System.Action<QuestInstance> OnQuestActivated;
        event System.Action<QuestInstance> OnQuestCompleted;

        IEnumerable<QuestGroup> QuestGroups { get; }
        IEnumerable<QuestInstance> AllQuests { get; }
        int CompletedQuestsCount { get; }
    }
}
