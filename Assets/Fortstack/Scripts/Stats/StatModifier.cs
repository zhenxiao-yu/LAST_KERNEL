using UnityEngine;

namespace Markyu.LastKernel
{
    [System.Serializable]
    public struct StatModifier : IStatModifier
    {
        public StatType Stat;

        [SerializeField]
        private float value;

        public float Value => value;
    }
}

