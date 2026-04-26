// NightDeploymentController — orchestrates the player-controlled dusk deployment phase.
//
// Responsibilities:
//   - Receive the list of eligible defenders from DayCycleManager
//   - Open NightDeploymentView and wait for player input
//   - Expose ConfirmedPlan once the player confirms or skips
//   - Fall back to BuildAutomatic if the view is not wired up
//
// Explicitly does NOT:
//   - Run night combat (that stays in NightPhaseManager)
//   - Mutate the board or run-state
//   - Own any phase transitions

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class NightDeploymentController : MonoBehaviour
    {
        public static NightDeploymentController Instance { get; private set; }

        [Header("View")]
        [SerializeField, Tooltip("The NightDeploymentView in the scene. Wire this in the Inspector. " +
                                 "Combat runs without it (auto-deploy fallback).")]
        private NightDeploymentView deploymentView;

        /// <summary>
        /// The deployment plan produced by the player.
        /// Available after RunDeploymentPhase() coroutine completes.
        /// Never null after the coroutine finishes — falls back to BuildAutomatic if needed.
        /// </summary>
        public NightDeploymentPlan ConfirmedPlan { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        // ── Public entry point ────────────────────────────────────────────────────

        /// <summary>
        /// Opens the deployment UI and yields until the player confirms a plan or skips.
        /// DayCycleManager yields on this before calling NightPhaseManager.RunNight().
        /// </summary>
        /// <param name="eligibleDefenders">
        /// Pre-filtered list of living Character cards the player may commit.
        /// Produced by DayCycleManager; empty list is valid (goes undefended).
        /// </param>
        public IEnumerator RunDeploymentPhase(List<CardInstance> eligibleDefenders)
        {
            ConfirmedPlan = null;

            if (deploymentView == null)
            {
                Debug.LogWarning("NightDeploymentController: No NightDeploymentView assigned. " +
                                 "Auto-deploying all eligible defenders.");
                ConfirmedPlan = NightDeploymentPlan.BuildAutomatic(eligibleDefenders);
                yield break;
            }

            bool done = false;

            deploymentView.Open(
                eligibleDefenders,

                // Player pressed CONFIRM — use their ordered selection
                onConfirm: (selectedInOrder) =>
                {
                    ConfirmedPlan = new NightDeploymentPlan(selectedInOrder);
                    done = true;
                },

                // Player pressed SKIP — fall back to automatic plan
                onCancel: () =>
                {
                    ConfirmedPlan = NightDeploymentPlan.BuildAutomatic(eligibleDefenders);
                    done = true;
                }
            );

            yield return new WaitUntil(() => done);

            // Close happens after the plan is captured so the view's cleanup
            // (card highlight removal) runs before NightPhaseManager starts.
            deploymentView.Close();
        }
    }
}
