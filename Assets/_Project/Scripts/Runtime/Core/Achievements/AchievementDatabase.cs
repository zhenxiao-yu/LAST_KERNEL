using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel.Achievements
{
    [CreateAssetMenu(menuName = "Last Kernel/Achievement Database", fileName = "AchievementDatabase")]
    public class AchievementDatabase : ScriptableObject
    {
        [TableList(ShowIndexLabels = true)]
        public List<AchievementDefinition> Achievements = new();

        private Dictionary<string, AchievementDefinition> _lookup;

        public IReadOnlyList<AchievementDefinition> All => Achievements;

        public AchievementDefinition GetById(string id)
        {
            if (_lookup == null) BuildLookup();
            _lookup.TryGetValue(id, out var def);
            return def;
        }

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, AchievementDefinition>(Achievements.Count);
            foreach (var a in Achievements)
            {
                if (a != null && !string.IsNullOrEmpty(a.Id))
                    _lookup[a.Id] = a;
            }
        }

        private void OnValidate() => _lookup = null;
    }
}
