using System;
using Sirenix.OdinInspector;

namespace Markyu.LastKernel
{
    [Serializable]
    public struct CategoryEntry
    {
        [TableColumnWidth(120)]
        public CardCategory category;
        [TableColumnWidth(200)]
        public CardInstance prefab;
    }
}
