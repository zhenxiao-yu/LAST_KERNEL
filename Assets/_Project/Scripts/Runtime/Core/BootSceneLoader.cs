using UnityEngine;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    // Boot → MainMenu transition. No loading screen here: Boot is a 1-frame
    // initialisation scene with no content, so the flash is imperceptible.
    public static class BootSceneLoader
    {
        private const string BootSceneName  = "Boot";
        private const string MainMenuScene  = "MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadMainMenuFromBoot()
        {
            if (SceneManager.GetActiveScene().name != BootSceneName)
                return;

            SceneManager.LoadScene(MainMenuScene);
        }
    }
}
