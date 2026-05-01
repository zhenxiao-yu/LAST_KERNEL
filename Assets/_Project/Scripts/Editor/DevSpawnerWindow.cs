#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Editor window for spawning cards and testing game systems during development.
    /// Uses CardManager.CreateCardInstance so spawned cards are fully registered
    /// (have a CardStack, are tracked by the manager, can be dragged immediately).
    /// Never active in release builds.
    /// </summary>
    public class DevSpawnerWindow : EditorWindow
    {
        // ─── State ───────────────────────────────────────────────────────────

        private Vector2 _cardListScroll;
        private string  _searchFilter  = string.Empty;

        private CardDefinition[] _allDefinitions;
        private CardDefinition   _selectedDefinition;

        private int     _spawnCount    = 1;
        private Vector3 _spawnPosition = Vector3.zero;

        private readonly List<CardInstance> _spawnedCards = new();

        private bool _showPresets;
        private bool _showCombat;
        private bool _showDefense;
        private bool _showWorld;
        private bool _showLanguage;

        // ─── Menu Item ───────────────────────────────────────────────────────

        [MenuItem("LAST KERNEL/Dev/Dev Spawner", false, 1)]
        public static void Open()
        {
            var window = GetWindow<DevSpawnerWindow>("Dev Spawner");
            window.minSize = new Vector2(320, 520);
            window.Show();
        }

        // ─── Lifecycle ───────────────────────────────────────────────────────

        private void OnEnable() => RefreshDefinitions();
        private void OnFocus()  => RefreshDefinitions();

        private void RefreshDefinitions()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { "Assets/_Project" });
            _allDefinitions = guids
                .Select(g => AssetDatabase.LoadAssetAtPath<CardDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(d => d != null && d is not PackDefinition && d.Category != CardCategory.None)
                .OrderBy(d => d.name)
                .ToArray();
        }

        // ─── GUI ─────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            DrawPlayModeWarning();
            DrawCardPicker();
            EditorGUILayout.Space(4);
            DrawSpawnControls();
            EditorGUILayout.Space(4);
            DrawPresets();
            EditorGUILayout.Space(4);
            DrawCombatTools();
            EditorGUILayout.Space(4);
            DrawDefenseTools();
            EditorGUILayout.Space(4);
            DrawWorldTools();
            EditorGUILayout.Space(4);
            DrawLanguageTools();
            EditorGUILayout.Space(4);
            DrawClearButton();
        }

        // ─── Section: header ─────────────────────────────────────────────────

        private static void DrawPlayModeWarning()
        {
            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use spawn actions. The card list is available in Edit Mode.",
                    MessageType.Info);
            }
            else if (CardManager.Instance == null)
            {
                EditorGUILayout.HelpBox(
                    "CardManager not found in scene. Cards cannot be spawned.",
                    MessageType.Warning);
            }
        }

        // ─── Section: card picker ─────────────────────────────────────────────

        private void DrawCardPicker()
        {
            EditorGUILayout.LabelField("Card Definitions", EditorStyles.boldLabel);

            _searchFilter = EditorGUILayout.TextField("Filter", _searchFilter);

            IEnumerable<CardDefinition> filtered = string.IsNullOrWhiteSpace(_searchFilter)
                ? _allDefinitions
                : _allDefinitions.Where(d =>
                    d.name.IndexOf(_searchFilter, System.StringComparison.OrdinalIgnoreCase) >= 0);

            _cardListScroll = EditorGUILayout.BeginScrollView(_cardListScroll, GUILayout.Height(140));
            foreach (CardDefinition def in filtered)
            {
                bool isSelected = _selectedDefinition == def;
                if (GUILayout.Toggle(isSelected, def.name, "Button") && !isSelected)
                    _selectedDefinition = def;
            }
            EditorGUILayout.EndScrollView();

            if (_selectedDefinition != null)
                EditorGUILayout.LabelField("Selected", _selectedDefinition.name, EditorStyles.miniLabel);
        }

        // ─── Section: spawn controls ─────────────────────────────────────────

        private void DrawSpawnControls()
        {
            EditorGUILayout.LabelField("Spawn", EditorStyles.boldLabel);
            _spawnCount    = EditorGUILayout.IntSlider("Count", _spawnCount, 1, 10);
            _spawnPosition = EditorGUILayout.Vector3Field("Position", _spawnPosition);

            // Use scene-view camera position as a quick "spawn at camera" shortcut
            if (GUILayout.Button("Use Scene Camera Position", EditorStyles.miniButton))
            {
                var sv = SceneView.lastActiveSceneView;
                if (sv != null)
                    _spawnPosition = sv.camera.transform.position.With(y: 0f);
            }

            GUI.enabled = Application.isPlaying && CardManager.Instance != null && _selectedDefinition != null;
            if (GUILayout.Button($"Spawn '{(_selectedDefinition != null ? _selectedDefinition.name : "—")}' ×{_spawnCount}"))
                SpawnCards(_selectedDefinition, _spawnCount);
            GUI.enabled = true;
        }

        // ─── Section: presets ────────────────────────────────────────────────

        private void DrawPresets()
        {
            _showPresets = EditorGUILayout.Foldout(_showPresets, "Preset Groups", true);
            if (!_showPresets) return;

            GUI.enabled = Application.isPlaying && CardManager.Instance != null;

            if (GUILayout.Button("Resources ×5"))   SpawnByCategory(CardCategory.Resource,   5);
            if (GUILayout.Button("Materials ×5"))   SpawnByCategory(CardCategory.Material,   5);
            if (GUILayout.Button("Characters ×3"))  SpawnByCategory(CardCategory.Character,  3);
            if (GUILayout.Button("Mobs ×3"))        SpawnByCategory(CardCategory.Mob,        3);

            GUI.enabled = true;
        }

        // ─── Section: combat tools ───────────────────────────────────────────

        private void DrawCombatTools()
        {
            _showCombat = EditorGUILayout.Foldout(_showCombat, "Card Combat Tools", true);
            if (!_showCombat) return;

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Damage Selected Card (−5 HP)"))
                ApplyToSelectedCard(card => card.TakeDamage(5));
            if (GUILayout.Button("Heal Selected Card (+10 HP)"))
                ApplyToSelectedCard(card => card.Heal(10));
            if (GUILayout.Button("Kill Selected Card"))
                ApplyToSelectedCard(card => card.Kill());

            GUI.enabled = true;
        }

        // ─── Section: defense tools (night vertical slice) ───────────────────

        private void DrawDefenseTools()
        {
            _showDefense = EditorGUILayout.Foldout(_showDefense, "Defense / Night Tools", true);
            if (!_showDefense) return;

            GUI.enabled = Application.isPlaying;

            // Phase control
            EditorGUILayout.LabelField("Phase", EditorStyles.miniLabel);

            var phaseCtrl = DefensePhaseController.Instance;
            string phaseLabel = phaseCtrl != null ? phaseCtrl.CurrentPhase.ToString() : "—";
            EditorGUILayout.LabelField("Current Phase", phaseLabel, EditorStyles.miniLabel);

            if (GUILayout.Button("▶ Start Night"))
                DefensePhaseController.Instance?.StartNight();

            if (GUILayout.Button("✓ Force Victory"))
                DefensePhaseController.Instance?.DeclareVictory();

            if (GUILayout.Button("✗ Force Defeat"))
                DefensePhaseController.Instance?.DeclareDefeat();

            if (GUILayout.Button("↩ Return to Day"))
                DefensePhaseController.Instance?.ReturnToDay();

            EditorGUILayout.Space(2);

            // Base HP controls
            EditorGUILayout.LabelField("Base Core", EditorStyles.miniLabel);
            var baseCore = Object.FindAnyObjectByType<BaseCoreController>();
            if (baseCore != null)
            {
                EditorGUILayout.LabelField("Base HP", $"{baseCore.CurrentHP} / {baseCore.MaxHP}", EditorStyles.miniLabel);
                if (GUILayout.Button("Damage Base (−3 HP)")) baseCore.TakeDamage(3);
                if (GUILayout.Button("Reset Base HP"))        baseCore.ResetHP();
            }
            else
            {
                EditorGUILayout.HelpBox("No BaseCoreController in scene.", MessageType.None);
            }

            GUI.enabled = true;
        }

        // ─── Section: world tools ────────────────────────────────────────────

        private void DrawWorldTools()
        {
            _showWorld = EditorGUILayout.Foldout(_showWorld, "World Tools", true);
            if (!_showWorld) return;

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("Start New Day (TimeManager)"))
            {
                var tm = Object.FindAnyObjectByType<TimeManager>();
                if (tm != null)
                    tm.StartNewDay();
                else
                    Debug.LogWarning("[DevSpawner] No TimeManager in scene.");
            }

            if (GUILayout.Button("Resolve Overlaps"))
                CardManager.Instance?.ResolveOverlaps();

            if (GUILayout.Button("Select All Spawned Cards"))
                SelectSpawnedCards();

            GUI.enabled = true;
        }

        // ─── Section: language ───────────────────────────────────────────────

        private void DrawLanguageTools()
        {
            _showLanguage = EditorGUILayout.Foldout(_showLanguage, "Language", true);
            if (!_showLanguage) return;

            GUI.enabled = Application.isPlaying;

            if (GUILayout.Button("English"))           GameLocalization.SetLanguage(GameLanguage.English);
            if (GUILayout.Button("简体中文"))           GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            if (GUILayout.Button("繁體中文"))           GameLocalization.SetLanguage(GameLanguage.TraditionalChinese);
            if (GUILayout.Button("日本語"))             GameLocalization.SetLanguage(GameLanguage.Japanese);
            if (GUILayout.Button("한국어"))             GameLocalization.SetLanguage(GameLanguage.Korean);
            if (GUILayout.Button("Français"))          GameLocalization.SetLanguage(GameLanguage.French);
            if (GUILayout.Button("Deutsch"))           GameLocalization.SetLanguage(GameLanguage.German);
            if (GUILayout.Button("Español"))           GameLocalization.SetLanguage(GameLanguage.Spanish);

            GUI.enabled = true;
        }

        // ─── Section: clear ───────────────────────────────────────────────────

        private void DrawClearButton()
        {
            // Clean up destroyed references before showing the count
            _spawnedCards.RemoveAll(c => c == null);

            GUI.enabled = Application.isPlaying && _spawnedCards.Count > 0;
            if (GUILayout.Button($"Clear Spawned Cards ({_spawnedCards.Count})"))
                ClearSpawnedCards();
            GUI.enabled = true;
        }

        // ─── Actions ─────────────────────────────────────────────────────────

        /// <summary>
        /// Spawns via CardManager so each card gets a proper CardStack and is
        /// fully registered — it can be dragged without a NullReferenceException.
        /// </summary>
        private void SpawnCards(CardDefinition definition, int count)
        {
            if (CardManager.Instance == null || definition == null) return;

            for (int i = 0; i < count; i++)
            {
                Vector3 pos = _spawnPosition + new Vector3(i * 1.2f, 0f, 0f);
                CardInstance card = CardManager.Instance.CreateCardInstance(definition, pos.Flatten());
                if (card != null)
                    _spawnedCards.Add(card);
            }

            Debug.Log($"[DevSpawner] Spawned {count}× {definition.name}.");
        }

        private void SpawnByCategory(CardCategory category, int max)
        {
            CardDefinition[] matching = _allDefinitions
                .Where(d => d.Category == category)
                .Take(max)
                .ToArray();

            for (int i = 0; i < matching.Length; i++)
            {
                // Offset each category group so cards don't stack on top of each other
                _spawnPosition = _spawnPosition.With(z: _spawnPosition.z + i * 1.4f);
                SpawnCards(matching[i], 1);
            }
        }

        private void ApplyToSelectedCard(System.Action<CardInstance> action)
        {
            bool found = false;
            foreach (GameObject go in Selection.gameObjects)
            {
                CardInstance card = go.GetComponent<CardInstance>();
                if (card != null) { action(card); found = true; }
            }
            if (!found)
                Debug.LogWarning("[DevSpawner] No CardInstance selected in the Hierarchy.");
        }

        private void SelectSpawnedCards()
        {
            _spawnedCards.RemoveAll(c => c == null);
            Selection.objects = _spawnedCards
                .Select(c => c.gameObject)
                .Cast<Object>()
                .ToArray();
        }

        private void ClearSpawnedCards()
        {
            int count = 0;
            foreach (CardInstance card in _spawnedCards)
            {
                if (card != null) { card.Kill(); count++; }
            }
            _spawnedCards.Clear();
            Debug.Log($"[DevSpawner] Cleared {count} spawned card(s).");
        }
    }
}
#endif
