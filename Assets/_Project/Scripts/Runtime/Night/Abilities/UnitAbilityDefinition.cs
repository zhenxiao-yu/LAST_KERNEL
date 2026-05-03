using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "LastKernel/Night/Ability Definition", fileName = "Ability_")]
    public class UnitAbilityDefinition : ScriptableObject
    {
        [BoxGroup("Identity")]
        [SerializeField] private UnitAbilityKeyword keyword;

        [BoxGroup("Identity")]
        [SerializeField, Min(0), Tooltip("Numeric parameter: damage stacks, heal amount, ATK bonus, % bonus, etc.")]
        private int value = 1;

        [BoxGroup("Display")]
        [SerializeField, TextArea(2, 4)]
        private string description;

        [BoxGroup("Display")]
        [SerializeField] private Sprite icon;

        public UnitAbilityKeyword Keyword    => keyword;
        public int                Value       => value;
        public string             Description => description;
        public Sprite             Icon        => icon;

        public static UnitAbilityDefinition CreateRuntime(UnitAbilityKeyword keyword, int value, string description = "")
        {
            var def = ScriptableObject.CreateInstance<UnitAbilityDefinition>();
            def.keyword     = keyword;
            def.value       = value;
            def.description = description;
            return def;
        }
    }
}
