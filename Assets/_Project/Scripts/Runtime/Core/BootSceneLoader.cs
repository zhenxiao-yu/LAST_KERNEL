using UnityEngine;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    public static class BootSceneLoader
    {
        private const string BootSceneName = "Boot";
        private const string MainMenuSceneName = "MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadMainMenuFromBoot()
        {
            if (SceneManager.GetActiveScene().name != BootSceneName)
                return;

            SceneManager.LoadScene(MainMenuSceneName);
        }
    }
}
