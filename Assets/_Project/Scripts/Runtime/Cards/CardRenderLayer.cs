using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Attach to any child renderer inside a card hierarchy to pin its local
    /// sorting-order offset relative to the card's computed base order.
    ///
    /// Suggested offsets (must stay under CardRenderOrderController.ChildSlots = 10):
    ///   Body / Background   = 0
    ///   Art / Icon          = 1
    ///   Frame / Border      = 2
    ///   Title / Stats / TMP = 3
    ///   Overlay / Highlight = 4
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class CardRenderLayer : MonoBehaviour
    {
        [Tooltip("Sorting-order offset added to the card's computed base order. " +
                 "Keep in 0–9 range (CardRenderOrderController.ChildSlots = 10).")]
        public int localOrderOffset;
    }
}
