using Michsky.LSS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    // Boot → MainMenu transition via LSS loading screen.
    // Replaces the old synchronous LoadScene call that blocked the main thread for 6+ seconds.
    public static class BootSceneLoader
    {
        private const string BootSceneName  = "Boot";
        private const string MainMenuScene  = "MainMenu";
        private const string LssPreset      = "LastKernel";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadMainMenuFromBoot()
        {
            if (SceneManager.GetActiveScene().name != BootSceneName)
                return;

            LSS_LoadingScreen.LoadScene(MainMenuScene, LssPreset);
        }
    }
}
