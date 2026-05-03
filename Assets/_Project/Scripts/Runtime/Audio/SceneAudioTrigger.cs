using UnityEngine;

namespace Markyu.LastKernel
{
    // Drop into any scene root. Fires PlayBGM once on Start.
    // Useful for scenes that don't run through DayCycleManager (MainMenu, etc.).
    [AddComponentMenu("LastKernel/Audio/Scene Audio Trigger")]
    public class SceneAudioTrigger : MonoBehaviour
    {
        [SerializeField] private MusicContext context = MusicContext.MainMenu;
        [SerializeField, Min(0f)] private float fadeDuration = 1.5f;

        private void Start()
        {
            AudioManager.Instance?.PlayBGM(context, fadeDuration);
        }
    }
}
