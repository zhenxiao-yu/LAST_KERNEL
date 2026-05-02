#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    [InitializeOnLoad]
    public static class PlayModeStartScene
    {
        private const string PlayModeScenePath = "Assets/_Project/Scenes/Boot.unity";
        private const string PreviousSceneKey = "PlayModeStartScene.PreviousScene";

        static PlayModeStartScene()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            switch (state)
            {
                case PlayModeStateChange.ExitingEditMode:
                    HandleEnteringPlayMode();
                    break;

                case PlayModeStateChange.EnteredEditMode:
                    HandleExitingPlayMode();
                    break;
            }
        }

        private static void HandleEnteringPlayMode()
        {
            if (IsUnityTestRunActive())
                return;

            Scene activeScene = SceneManager.GetActiveScene();

            if (activeScene.path == PlayModeScenePath)
                return;

            SessionState.SetString(PreviousSceneKey, activeScene.path);

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorApplication.isPlaying = false;
                return;
            }

            EditorSceneManager.OpenScene(PlayModeScenePath);
        }

        private static void HandleExitingPlayMode()
        {
            string previousScenePath = SessionState.GetString(PreviousSceneKey, string.Empty);

            if (string.IsNullOrEmpty(previousScenePath))
                return;

            if (!System.IO.File.Exists(previousScenePath))
                return;

            EditorSceneManager.OpenScene(previousScenePath);
            SessionState.EraseString(PreviousSceneKey);
        }

        private static bool IsUnityTestRunActive()
        {
            // Test Runner enters PlayMode through a generated scene. Forcing Boot here
            // prevents the runner from receiving its RunStarted callback.
            const BindingFlags Flags = BindingFlags.Static | BindingFlags.NonPublic;
            System.Type apiType = System.Type.GetType(
                "UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
            MethodInfo method = apiType?.GetMethod("IsRunActive", Flags);

            return method?.Invoke(null, null) is true;
        }
    }
}
#endif

