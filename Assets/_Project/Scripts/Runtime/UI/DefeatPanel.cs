// DefeatPanel — Shown when the base core is destroyed.
//
// Presents a game-over state with options to retry (restart scene) or quit to main menu.

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Game-over overlay shown when <see cref="BaseCoreController.OnBaseDestroyed"/> fires.
    /// </summary>
    public class DefeatPanel : MonoBehaviour
    {
        [BoxGroup("UI Elements")]
        [SerializeField] private TextMeshProUGUI messageLabel;

        [BoxGroup("UI Elements")]
        [SerializeField] private Button retryButton;

        [BoxGroup("UI Elements")]
        [SerializeField] private Button quitToMenuButton;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (retryButton     != null) retryButton.onClick.AddListener(OnRetry);
            if (quitToMenuButton != null) quitToMenuButton.onClick.AddListener(OnQuitToMenu);
        }

        private void OnDisable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            if (retryButton     != null) retryButton.onClick.RemoveListener(OnRetry);
            if (quitToMenuButton != null) quitToMenuButton.onClick.RemoveListener(OnQuitToMenu);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            if (phase == DefensePhase.Defeat)
            {
                Time.timeScale = 1f;
                gameObject.SetActive(true);
                if (messageLabel != null)
                    messageLabel.text = "The base has fallen.";
            }
            else
            {
                gameObject.SetActive(false);
            }
        }

        private void OnRetry()
        {
            // Restart the current scene — quickest path for playtesting
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
        }

        private void OnQuitToMenu()
        {
            Time.timeScale = 1f;
            GameDirector.Instance?.BackToTitle();
        }
    }
}
