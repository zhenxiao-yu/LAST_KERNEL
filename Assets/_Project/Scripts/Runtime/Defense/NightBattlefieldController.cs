// NightBattlefieldController — Coordinates the spatial night-defense auto-battler.
//
// Architecture
// ────────────
// This controller is the single owner of:
//   • EnemyUnit instances (spawned from wave data, destroyed on death/arrival)
//   • DefenderUnit instances (spawned from loadout, positioned at defenderSlots)
//   • BaseCoreController reference (reports when base HP hits zero)
//
// It does NOT own any card-board logic — use DefenseLoadoutController to bridge
// prepared cards into defender slots before calling StartWave().
//
// Scene setup (see also NightDefenseSetup editor tool):
//   1. Add this component to a scene GameObject named "NightBattlefield".
//   2. Assign baseCoreController, defenderSlots[], baseMarker, spawnMarker.
//   3. Assign enemyPrefab and defenderPrefab (simple quads with SpriteRenderer).
//   4. Assign currentWave (NightWaveDefinition asset).
//   5. Wire "Start Night" button → DefensePhaseController.Instance.StartNight().
//   6. Subscribe to OnWaveCleared / OnWaveFailed for result UI.

using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Spawns enemies, places defenders, and resolves the night-phase auto-battle.
    /// Driven by <see cref="DefensePhaseController.StartNight"/>.
    /// </summary>
    public class NightBattlefieldController : MonoBehaviour
    {
        public static NightBattlefieldController Instance { get; private set; }

        // ── Inspector wiring ──────────────────────────────────────────────────

        [BoxGroup("Scene References")]
        [SerializeField] private BaseCoreController baseCoreController;

        [BoxGroup("Scene References")]
        [SerializeField, Tooltip("World positions where defenders can be placed. Index 0 = frontline.")]
        private Transform[] defenderSlots;

        [BoxGroup("Scene References")]
        [SerializeField, Tooltip("Enemies spawn at this world position each wave.")]
        private Transform spawnMarker;

        [BoxGroup("Scene References")]
        [SerializeField, Tooltip("Enemies move toward this marker. Also used for base-reach detection.")]
        private Transform baseMarker;

        [BoxGroup("Prefabs")]
        [SerializeField] private GameObject enemyPrefab;

        [BoxGroup("Prefabs")]
        [SerializeField] private GameObject defenderPrefab;

        [BoxGroup("Wave")]
        [SerializeField] private NightWaveDefinition currentWave;

        [BoxGroup("Wave")]
        [SerializeField, Min(0.5f), Tooltip("Delay in seconds between each enemy spawn.")]
        private float spawnInterval = 1.2f;

        [BoxGroup("Loadout")]
        [SerializeField, Tooltip("Assign defender data per slot. Leave null to leave that slot empty.")]
        private DefenderData[] defaultLoadout;

        // ── Runtime state ─────────────────────────────────────────────────────

        // Shared list — DefenderUnit reads from this each frame without Overlap calls
        private readonly List<EnemyUnit>    _activeEnemies   = new();
        private readonly List<DefenderUnit> _activeDefenders = new();
        private int _enemiesRemainingToSpawn;
        private int _waveTotalEnemies;
        private bool _waveActive;

        // ── Events ────────────────────────────────────────────────────────────

        public event System.Action           OnWaveCleared;
        public event System.Action           OnWaveFailed;
        public event System.Action<int, int> OnEnemyCountChanged; // (alive, total)

        // ── Phase integration ─────────────────────────────────────────────────

        private void Awake()
        {
            Instance = this;
        }

        private void OnEnable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged += HandlePhaseChanged;

            if (baseCoreController != null)
                baseCoreController.OnBaseDestroyed += HandleBaseDestroyed;
        }

        private void OnDisable()
        {
            if (DefensePhaseController.Instance != null)
                DefensePhaseController.Instance.OnPhaseChanged -= HandlePhaseChanged;

            if (baseCoreController != null)
                baseCoreController.OnBaseDestroyed -= HandleBaseDestroyed;
        }

        private void HandlePhaseChanged(DefensePhase phase)
        {
            if (phase == DefensePhase.NightCombat)
                StartWave(currentWave);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Begin a wave. Called automatically when phase transitions to NightCombat,
        /// or manually for testing. Pass null to use the serialized currentWave.
        /// </summary>
        public void StartWave(NightWaveDefinition wave)
        {
            if (wave == null)
            {
                Debug.LogWarning("[NightBattlefield] No wave assigned — cannot start.", this);
                return;
            }

            currentWave = wave;
            _waveActive = true;

            baseCoreController?.ResetHP();
            ClearPreviousWave();
            SpawnDefenders();

            List<EnemyDefinition> enemies = wave.BuildEnemyList();
            _enemiesRemainingToSpawn = enemies.Count;
            _waveTotalEnemies        = enemies.Count;
            OnEnemyCountChanged?.Invoke(0, _waveTotalEnemies);

            StartCoroutine(SpawnEnemiesRoutine(enemies));
        }

        // ── Spawning ──────────────────────────────────────────────────────────

        private void SpawnDefenders()
        {
            if (defenderSlots == null) return;

            // Use the DefenseLoadoutController if present (card integration);
            // otherwise fall back to the serialized defaultLoadout.
            var loadout = GetComponent<DefenseLoadoutController>();
            DefenderData[] resolvedLoadout = loadout != null
                ? loadout.ResolveLoadout(defenderSlots.Length)
                : defaultLoadout;

            for (int i = 0; i < defenderSlots.Length; i++)
            {
                if (defenderSlots[i] == null) continue;

                DefenderData data = (resolvedLoadout != null && i < resolvedLoadout.Length)
                    ? resolvedLoadout[i]
                    : null;

                if (data == null) continue;

                GameObject go = defenderPrefab != null
                    ? Instantiate(defenderPrefab, defenderSlots[i].position, Quaternion.identity, transform)
                    : CreatePlaceholderGO("Defender_" + data.DisplayName, defenderSlots[i].position, Color.cyan);

                var unit = go.GetComponent<DefenderUnit>() ?? go.AddComponent<DefenderUnit>();
                unit.Initialize(data, _activeEnemies);
                _activeDefenders.Add(unit);
            }
        }

        private IEnumerator SpawnEnemiesRoutine(List<EnemyDefinition> enemies)
        {
            foreach (EnemyDefinition def in enemies)
            {
                if (!_waveActive) yield break;

                SpawnEnemy(def);
                _enemiesRemainingToSpawn--;
                yield return new WaitForSeconds(spawnInterval);
            }
            // All enemies queued — victory check runs from HandleEnemyDied
        }

        private void SpawnEnemy(EnemyDefinition def)
        {
            if (spawnMarker == null || baseMarker == null)
            {
                Debug.LogWarning("[NightBattlefield] spawnMarker or baseMarker not assigned.", this);
                return;
            }

            GameObject go = enemyPrefab != null
                ? Instantiate(enemyPrefab, spawnMarker.position, Quaternion.identity, transform)
                : CreatePlaceholderGO("Enemy_" + def.DisplayName, spawnMarker.position, Color.red);

            var unit = go.GetComponent<EnemyUnit>() ?? go.AddComponent<EnemyUnit>();
            unit.Initialize(def, baseMarker);
            unit.OnDied        += HandleEnemyDied;
            unit.OnReachedBase += HandleEnemyReachedBase;

            _activeEnemies.Add(unit);
            OnEnemyCountChanged?.Invoke(_activeEnemies.Count, _waveTotalEnemies);
        }

        // ── Event handlers ────────────────────────────────────────────────────

        private void HandleEnemyDied(EnemyUnit unit)
        {
            _activeEnemies.Remove(unit);
            OnEnemyCountChanged?.Invoke(_activeEnemies.Count, _waveTotalEnemies);
            CheckVictoryCondition();
        }

        private void HandleEnemyReachedBase(EnemyUnit unit)
        {
            _activeEnemies.Remove(unit);
            baseCoreController?.TakeDamage(unit.Data.DamageToBase);
            // Base destruction is handled by BaseCoreController → DefensePhaseController.DeclareDefeat()
        }

        private void HandleBaseDestroyed()
        {
            _waveActive = false;
            OnWaveFailed?.Invoke();
        }

        private void CheckVictoryCondition()
        {
            // Victory: no enemies currently alive AND spawn queue is fully exhausted
            if (_activeEnemies.Count == 0 && _enemiesRemainingToSpawn <= 0 && _waveActive)
            {
                _waveActive = false;
                OnWaveCleared?.Invoke();
                DefensePhaseController.Instance?.DeclareVictory();
            }
        }

        // ── Cleanup ───────────────────────────────────────────────────────────

        private void ClearPreviousWave()
        {
            foreach (var e in _activeEnemies)
                if (e != null) Destroy(e.gameObject);
            _activeEnemies.Clear();

            foreach (var d in _activeDefenders)
                if (d != null) Destroy(d.gameObject);
            _activeDefenders.Clear();
        }

        // ── Placeholder visuals (used when no prefab assigned) ────────────────

        /// <summary>
        /// Creates a simple coloured quad so the battlefield is visible during
        /// playtesting even without real art assets.
        /// </summary>
        private static GameObject CreatePlaceholderGO(string name, Vector3 pos, Color color)
        {
            var go       = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name      = name;
            go.transform.position   = pos;
            go.transform.localScale = Vector3.one * 0.6f;

            var renderer = go.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material       = new Material(Shader.Find("Sprites/Default"));
                renderer.material.color = color;
            }

            // Remove the collider — we use transform-distance checks, not physics
            Destroy(go.GetComponent<Collider>());
            return go;
        }
    }
}
