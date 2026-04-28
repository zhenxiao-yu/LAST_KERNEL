// NightHUD — Heads-up display shown during the NightCombat phase.
//
// Displays:
//   • Current wave number
//   • Base HP (bar + text)
//   • Active enemy count
//   • Optional speed multiplier toggle (1× / 2×)
//
// All data comes from events; this script never polls game state.
// Wire references in the Inspector on the NightHUD Canvas GameObject.

using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI controller for the night-phase heads-up display.
    /// Subscribes to battlefield and base events; never owns simulation logic.
    /// </summary>
    public class NightHUD : MonoBehaviour
    {
        [BoxGroup("Phase Labels")]
        [SerializeField] private TextMeshProUGUI phaseLabel;

        [BoxGroup("Phase Labels")]
        [SerializeField] private TextMeshProUGUI waveLabel;

        [BoxGroup("Base HP")]
        [SerializeField] private Slider baseHPBar;

        [BoxGroup("Base HP")]
        [SerializeField] private TextMeshProUGUI baseHPText;

        [BoxGroup("Enemy Count")]
        [SerializeField] private TextMeshProUGUI enemyCountLabel;

        [BoxGroup("Speed Control")]
        [SerializeField] private Button speedToggleButton;

        [BoxGroup("Speed Control")]
        [SerializeField] private TextMeshProUGUI speedLabel;
        private bool _isDoubleSpeed;

        [BoxGroup("References")]
        [SerializeField] private BaseCoreController baseCoreController;

        [BoxGroup("References")]
        [SerializeField] private NightBattlefieldController battlefield;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (baseCoreController != null)
                baseCoreController.OnDamaged += UpdateBaseHP;

            if (battlefield != null)
                battlefield.OnEnemyCountChanged += UpdateEnemyCount;

            if (speedToggleButton != null)
                speedToggleButton.onClick.AddListener(ToggleSpeed);
        }

        private void OnDisable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            if (baseCoreController != null)
                baseCoreController.OnDamaged -= UpdateBaseHP;

            if (battlefield != null)
                battlefield.OnEnemyCountChanged -= UpdateEnemyCount;

            if (speedToggleButton != null)
                speedToggleButton.onClick.RemoveListener(ToggleSpeed);
        }

        private void Start()
        {
            // Initialise with current state so HUD is correct even if phase started before OnEnable
            if (baseCoreController != null)
                UpdateBaseHP(baseCoreController.CurrentHP, baseCoreController.MaxHP);

            SetSpeedLabel();
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandlePhaseChanged(DefensePhase phase)
        {
            if (phaseLabel != null)
                phaseLabel.text = phase == DefensePhase.NightCombat ? "NIGHT" : phase.ToString().ToUpper();
        }

        private void UpdateBaseHP(int current, int max)
        {
            if (baseHPBar  != null) baseHPBar.value = max > 0 ? (float)current / max : 0f;
            if (baseHPText != null) baseHPText.text = $"{current} / {max}";
        }

        private void UpdateEnemyCount(int alive, int total)
        {
            if (enemyCountLabel != null)
                enemyCountLabel.text = $"Enemies: {alive} / {total}";
        }

        // ── Speed control ─────────────────────────────────────────────────────

        private void ToggleSpeed()
        {
            _isDoubleSpeed   = !_isDoubleSpeed;
            Time.timeScale   = _isDoubleSpeed ? 2f : 1f;
            SetSpeedLabel();
        }

        private void SetSpeedLabel()
        {
            if (speedLabel != null)
                speedLabel.text = _isDoubleSpeed ? "2×" : "1×";
        }

        private void OnDestroy()
        {
            // Always restore time scale when the HUD is torn down
            Time.timeScale = 1f;
        }

        // ── Public helpers (called by NightDefenseSetup or debug tools) ───────

        /// <summary>Set the wave number displayed in the header.</summary>
        public void SetWaveNumber(int wave)
        {
            if (waveLabel != null)
                waveLabel.text = $"Night {wave}";
        }
    }
}
