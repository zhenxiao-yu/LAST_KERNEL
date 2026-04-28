using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Base class for all Last Kernel data ScriptableObjects.
    /// Provides a consistent identity (auto-generated GUID id) and Odin inspector foundation.
    /// Subclass with your own [BoxGroup] and [FoldoutGroup] fields.
    /// Do NOT put gameplay logic here — SOs are data/config only.
    /// </summary>
    public abstract class LastKernelDataAsset : ScriptableObject
    {
        [BoxGroup("Asset")]
        [SerializeField, ReadOnly, Tooltip("Auto-generated GUID. Never set manually.")]
        protected string id;

        public string Id => id;

        protected virtual void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(id))
                id = System.Guid.NewGuid().ToString("N");
        }
    }
}
