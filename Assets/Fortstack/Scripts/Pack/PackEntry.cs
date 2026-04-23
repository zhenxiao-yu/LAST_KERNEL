using UnityEngine;

namespace Markyu.FortStack
{
    [System.Serializable]
    public class PackEntry
    {
        public CardDefinition Card;
        [Tooltip("Higher = More Likely")]
        public int Weight = 1;
    }
}

