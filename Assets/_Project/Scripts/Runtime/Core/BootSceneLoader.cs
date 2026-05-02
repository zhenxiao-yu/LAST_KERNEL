using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    // Boot → MainMenu: async load with a simple progress overlay.
    // Uses plain LoadSceneAsync (not LSS) so there is no "press any key" gate
    // that could conflict with LSS used later in TravelSequence.
    public static class BootSceneLoader
    {
        private const string BootSceneName = "Boot";
        private const string MainMenuScene  = "MainMenu";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void LoadMainMenuFromBoot()
        {
            if (SceneManager.GetActiveScene().name != BootSceneName)
                return;

            var runner = new GameObject("[BootLoader]");
            Object.DontDestroyOnLoad(runner);
            runner.AddComponent<BootLoaderRunner>().Load(MainMenuScene);
        }

        private sealed class BootLoaderRunner : MonoBehaviour
        {
            public void Load(string sceneName) => StartCoroutine(LoadAsync(sceneName));

            private IEnumerator LoadAsync(string sceneName)
            {
                // Minimal full-screen overlay so the user sees progress instead of pure black.
                var canvasGo = new GameObject("[BootCanvas]");
                canvasGo.transform.SetParent(transform, false);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                scaler.matchWidthOrHeight = 0.5f;
                canvasGo.AddComponent<GraphicRaycaster>();

                var backgroundGo = new GameObject("Background");
                backgroundGo.transform.SetParent(canvasGo.transform, false);
                var background = backgroundGo.AddComponent<Image>();
                background.color = Color.black;
                background.raycastTarget = true;
                var backgroundRect = background.GetComponent<RectTransform>();
                backgroundRect.anchorMin = Vector2.zero;
                backgroundRect.anchorMax = Vector2.one;
                backgroundRect.offsetMin = Vector2.zero;
                backgroundRect.offsetMax = Vector2.zero;

                var textGo  = new GameObject("LoadingLabel");
                textGo.transform.SetParent(canvasGo.transform, false);
                var tmp     = textGo.AddComponent<TextMeshProUGUI>();
                tmp.text    = "LOADING";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize  = 28;
                tmp.color     = new Color(0.45f, 0.9f, 1f);
                var rect = tmp.GetComponent<RectTransform>();
                rect.anchorMin    = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
                rect.sizeDelta    = new Vector2(500, 60);
                rect.anchoredPosition = new Vector2(0, -100);

                AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
                while (!op.isDone)
                {
                    int progress = Mathf.Clamp(Mathf.RoundToInt(op.progress / 0.9f * 100f), 0, 100);
                    tmp.text = $"LOADING  {progress}%";
                    yield return null;
                }

                Destroy(canvasGo);
                Destroy(gameObject);
            }
        }
    }
}
