// DayHUD — Minimal heads-up display for the day phase.
//
// Currently manages:
//   • Phase label (DAY / NIGHT)
//   • "End Day / Start Night" button
//   • Hides itself during NightCombat so the NightHUD takes over
//
// Extend this as you add more day-phase UI elements (resource counters, quest progress, etc.).

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Day-phase HUD root. Hides during NightCombat and shows the Start Night button.
    /// </summary>
    public class DayHUD : MonoBehaviour
    {
        [BoxGroup("UI Elements")]
        [SerializeField] private TextMeshProUGUI phaseLabel;

        [BoxGroup("UI Elements")]
        [SerializeField] private Button startNightButton;

        [BoxGroup("UI Elements")]
        [SerializeField, Tooltip("Root GameObject of the day-phase HUD — hidden at night.")]
        private GameObject dayHUDRoot;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (startNightButton != null)
                startNightButton.onClick.AddListener(OnStartNightClicked);
        }

        private void OnDisable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            if (startNightButton != null)
                startNightButton.onClick.RemoveListener(OnStartNightClicked);
        }

        private void Start()
        {
            // Initialise display to current phase (handles scene hot-reload cases)
            if (DefensePhaseController.Instance != null)
                HandlePhaseChanged(DefensePhaseController.Instance.CurrentPhase);
            else
                SetDayVisible(true);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            bool isDay = phase == DefensePhase.Day || phase == DefensePhase.NightPrep;

            SetDayVisible(isDay);

            if (phaseLabel != null)
                phaseLabel.text = isDay ? "DAY" : "NIGHT";
        }

        private void OnStartNightClicked()
        {
            DefensePhaseController.Instance?.StartNight();
        }

        private void SetDayVisible(bool visible)
        {
            if (dayHUDRoot != null)
                dayHUDRoot.SetActive(visible);
        }
    }
}
