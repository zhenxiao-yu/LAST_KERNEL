using System.Collections.Generic;

namespace Markyu.LastKernel
{
    public class QuestsView : MenuView
    {
        private const string SYMBOL_COMPLETED = "\u221A";

        #region Data Structures
        // Maps Quest (ScriptableObject) to its group
        private readonly Dictionary<Quest, QuestGroup> questToGroupMap = new();

        // Maps QuestGroup to its UI header button
        private readonly Dictionary<QuestGroup, TextButton> groupHeaderButtons = new();

        // Maps QuestInstance (runtime) to its UI button (for updates)
        private readonly Dictionary<QuestInstance, TextButton> allQuestButtons = new();

        // Tracks the expanded/collapsed state of each group
        private readonly Dictionary<QuestGroup, bool> groupToggleState = new();
        #endregion

        #region Unity & Event Methods
        private void Start()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestActivated += HandleQuestActivated;
                QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            }

            BuildGroupMap();
            PopulateView();
        }

        private void OnDestroy()
        {
            if (QuestManager.Instance != null)
            {
                QuestManager.Instance.OnQuestActivated -= HandleQuestActivated;
                QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            }

            ClearView();
        }
        #endregion

        #region View Population
        /// <summary>
        /// Builds the fast-lookup map for finding a quest's group.
        /// </summary>
        private void BuildGroupMap()
        {
            questToGroupMap.Clear();
            foreach (var group in QuestManager.Instance.QuestGroups)
            {
                foreach (var quest in group.Quests)
                {
                    if (!questToGroupMap.ContainsKey(quest))
                    {
                        questToGroupMap.Add(quest, group);
                    }
                }
            }
        }

        /// <summary>
        /// Clears and rebuilds the entire quest list UI.
        /// </summary>
        private void PopulateView()
        {
            ClearView();

            // 1. Create Group Headers
            foreach (var group in QuestManager.Instance.QuestGroups)
            {
                // Ensure toggle state exists
                groupToggleState.TryAdd(group, true); // Default to expanded

                TextButton groupBtn = CreateItemButton(
                    GetGroupHeaderLabel(group, true),
                    group,
                    35f
                );

                groupBtn.SetOnClick(() => ToggleGroup(group));
                groupBtn.SetColor(headerColor);
                groupBtn.gameObject.SetActive(false);
                groupHeaderButtons.Add(group, groupBtn);
            }

            // 2. Add all quests (active and completed)
            foreach (var quest in QuestManager.Instance.AllQuests)
            {
                CreateQuestButton(quest);
            }
        }

        /// <summary>
        /// Destroys all created UI buttons and clears tracking dictionaries.
        /// </summary>
        private void ClearView()
        {
            foreach (var btn in groupHeaderButtons.Values)
            {
                Destroy(btn.gameObject);
            }
            foreach (var btn in allQuestButtons.Values)
            {
                Destroy(btn.gameObject);
            }

            groupHeaderButtons.Clear();
            allQuestButtons.Clear();
        }
        #endregion

        #region Event Handlers
        private void HandleQuestActivated(QuestInstance quest)
        {
            if (gameObject.activeInHierarchy)
            {
                CreateQuestButton(quest);
            }
        }

        private void HandleQuestCompleted(QuestInstance quest)
        {
            if (allQuestButtons.TryGetValue(quest, out var questBtn))
            {
                questBtn.SetText(PreserveNewIndicator(questBtn.GetText(), GetQuestButtonLabel(quest)));
            }
        }
        #endregion

        #region Button & Group Logic
        private TextButton CreateQuestButton(QuestInstance quest)
        {
            if (quest == null || allQuestButtons.ContainsKey(quest))
            {
                return null;
            }

            TextButton questBtn = CreateItemButton(GetQuestButtonLabel(quest), quest, 30f);
            allQuestButtons.Add(quest, questBtn);

            // Find its group and place it under the header
            if (questToGroupMap.TryGetValue(quest.QuestData, out var group))
            {
                if (groupHeaderButtons.TryGetValue(group, out var groupBtn))
                {
                    // Ensure the header is visible
                    groupBtn.gameObject.SetActive(true);

                    // Set as child of the same parent (the layout group)
                    questBtn.transform.SetParent(groupBtn.transform.parent, false);
                    // Place it right after the group header
                    questBtn.transform.SetSiblingIndex(groupBtn.transform.GetSiblingIndex() + 1);
                    // Set visibility based on group's toggle state
                    questBtn.gameObject.SetActive(groupToggleState[group]);
                }
            }

            return questBtn;
        }

        private void ToggleGroup(QuestGroup group)
        {
            // Flip the state
            bool newState = !groupToggleState[group];
            groupToggleState[group] = newState;

            // Update header text (e.g., add indicator)
            groupHeaderButtons[group].SetText(GetGroupHeaderLabel(group, newState));

            // Toggle visibility of all member quests
            foreach (var (questInstance, questBtn) in allQuestButtons)
            {
                // Check if this quest belongs to the toggled group
                if (questToGroupMap.TryGetValue(questInstance.QuestData, out var questGroup) && questGroup == group)
                {
                    questBtn.gameObject.SetActive(newState);
                }
            }
        }

        /// <summary>
        /// Provides the formatted info panel text for a given quest or group.
        /// </summary>
        protected override (string header, string body) GetItemInfo(object item)
        {
            // Handle QuestInstance (active quests)
            if (item is QuestInstance questInstance)
            {
                string header = $"{questInstance.QuestData.Title}";
                string body = questInstance.QuestData.Description;
                if (!questInstance.IsComplete())
                {
                    body += $"\n\n{GameLocalization.Format("quest.progress", questInstance.CurrentAmount, questInstance.QuestData.TargetAmount)}";
                }
                return (header, body);
            }

            return ("", "");
        }

        protected override string GetItemId(object item)
        {
            if (item is QuestInstance questInstance)
                return questInstance.QuestData.Id;

            return null;
        }

        protected override void RefreshLocalizedText()
        {
            foreach (var kvp in groupHeaderButtons)
            {
                kvp.Value.SetText(GetGroupHeaderLabel(kvp.Key, groupToggleState[kvp.Key]));
            }

            foreach (var kvp in allQuestButtons)
            {
                kvp.Value.SetText(PreserveNewIndicator(kvp.Value.GetText(), GetQuestButtonLabel(kvp.Key)));
            }
        }

        private static string GetQuestButtonLabel(QuestInstance quest)
        {
            string suffix = quest.IsComplete() ? $" {SYMBOL_COMPLETED}" : "";
            return $"{SYMBOL_BULLET} {quest.QuestData.Title}{suffix}";
        }

        private static string GetGroupHeaderLabel(QuestGroup group, bool isExpanded)
        {
            string groupName = group != null ? group.GroupName : string.Empty;
            string key = $"quest.group.{LocalizationKeyBuilder.ToKeySegment(groupName)}";
            return $"{GameLocalization.GetOptional(key, groupName)} {(isExpanded ? SYMBOL_EXPANDED : SYMBOL_COLLAPSED)}";
        }
        #endregion
    }
}

