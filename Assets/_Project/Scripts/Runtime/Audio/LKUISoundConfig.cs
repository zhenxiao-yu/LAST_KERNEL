using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(fileName = "LKUISoundConfig", menuName = "LAST KERNEL/UI/Sound Config")]
    public class LKUISoundConfig : ScriptableObject
    {
        [BoxGroup("Button")]
        [Tooltip("Played when any Button is clicked.")]
        public AudioId ButtonClick = AudioId.Click;

        [BoxGroup("Panel")]
        [Tooltip("Played when a panel or modal opens. Call PlayPanelOpen() from screen controllers.")]
        public AudioId PanelOpen = AudioId.Pop;

        [BoxGroup("Panel")]
        [Tooltip("Played when a panel or modal closes. Call PlayPanelClose() from screen controllers.")]
        public AudioId PanelClose = AudioId.Pop;

        [BoxGroup("Hover")]
        [Tooltip("Play a sound when the pointer enters a Button. Disabled by default — can be spammy.")]
        public bool PlayHoverSounds = false;

        [BoxGroup("Hover"), ShowIf(nameof(PlayHoverSounds))]
        public AudioId ButtonHover = AudioId.Pop;
    }
}
