// GameDirector — Persistent session authority and scene transition controller.
//
// Survives scene loads (DontDestroyOnLoad). Responsible for:
//   • Starting new games and loading/deleting save slots
//   • Scene transitions: fade out → async load → fade in (TravelSequence)
//   • Preserving "traveler" cards across scene boundaries as serialised CardData
//   • Triggering auto-save on application quit and before every travel
//   • Broadcasting OnSceneDataReady so systems can init without coupling to this class
//
// Key dependencies:
//   SaveSystem      — disk read/write
//   ScreenFader     — visual transition during scene load
//   TimeManager     — paused during the load window
//   RunStateManager — binds to GameData for run-phase persistence
//   CardManager     — spawns incoming traveler cards after scene load

using System.Collections;
using System.Collections.Generic;
using Michsky.LSS;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    public class GameDirector : MonoBehaviour
    {
        public static GameDirector Instance { get; private set; }

        public event System.Action<SceneData, bool> OnSceneDataReady;
        public event System.Action<GameData> OnBeforeSave;
        public static event System.Action OnGameOver;

        [SerializeField, Tooltip("The name of the scene that serves as the game's main menu or entry point.")]
        private string titleScene = "MainMenu";

        [SerializeField, Tooltip("The name of the default gameplay scene to load when starting a new game.")]
        private string defaultScene = "Game";

        public Dictionary<string, GameData> SavedGames { get; private set; }
        public GameData GameData { get; private set; }

        // Cards carried between scenes. Populated by TravelSequence and consumed once
        // by SpawnTravelers on the first frame after the new scene loads.
        private List<CardData> incomingTravelers = new List<CardData>();

        #region Unity Lifecycle
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += HandleSceneLoaded;
            SavedGames = new System.Collections.Generic.Dictionary<string, GameData>();
            LoadSavesAsync();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private async void LoadSavesAsync()
        {
            try
            {
                string savePath = Application.persistentDataPath; // Must capture on main thread
                var saves = await System.Threading.Tasks.Task.Run(
                    () => SaveSystem.LoadAllValidData<GameData>(savePath));
                if (this != null)
                    SavedGames = saves;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"GameDirector: Save load failed. {ex.Message}");
            }
        }

        private void OnApplicationQuit()
        {
            if (SceneManager.GetActiveScene().name != titleScene)
            {
                if (DayCycleManager.Instance != null && DayCycleManager.Instance.IsEndingCycle) return;

                SaveGame();
            }
        }
        #endregion

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetExternalPause(false);

            StartCoroutine(FadeInAfterLoad());

            if (incomingTravelers.Count > 0)
                SpawnTravelers();

            if (scene.name != titleScene)
            {
                if (GameData == null)
                {
                    Debug.LogWarning($"GameDirector: Scene '{scene.name}' loaded without active GameData. Scene data initialization skipped.", this);
                    return;
                }

                GameData.CurrentScene = scene.name;
                bool wasLoaded = GameData.TryGetScene(out SceneData sceneData);
                OnSceneDataReady?.Invoke(sceneData, wasLoaded);
            }
        }

        private IEnumerator FadeInAfterLoad()
        {
            // One frame grace period so the new scene's Awake/Start complete first.
            yield return null;
            float startAlpha = ScreenFader.Instance?.CurrentAlpha ?? 0f;
            if (startAlpha > 0.01f)
                yield return ScreenFader.Instance?.Fade(startAlpha, 0f);
        }

        #region Core Game Flow
        /// <summary>
        /// Initializes a new game session. It automatically finds the next available save slot
        /// number, creates a new GameData object with the provided preferences, and starts 
        /// the travel sequence to load the default game scene.
        /// </summary>
        /// <param name="prefs">The gameplay settings for the new session.</param>
        public void NewGame(GameplayPrefs prefs)
        {
            HashSet<int> takenSlots = new HashSet<int>();

            foreach (var data in SavedGames.Values)
            {
                takenSlots.Add(data.SlotNumber);
            }

            int candidateSlot = 1;

            while (takenSlots.Contains(candidateSlot))
            {
                candidateSlot++;
            }

            GameData = new GameData(candidateSlot, prefs);
            RunStateManager.Instance?.Bind(GameData);
            StartCoroutine(TravelSequence(defaultScene, null));
        }

        /// <summary>
        /// Saves the current state of the active game session to the disk.
        /// </summary>
        /// <remarks>
        /// This method first invokes the OnBeforeSave event, allowing other systems (managers)
        /// to populate the GameData.
        /// </remarks>
        public void SaveGame()
        {
            if (GameData == null) return;
            RunStateManager.Instance?.SyncToGameData(GameData);
            OnBeforeSave?.Invoke(GameData);
            GameData.LastSaved = System.DateTime.Now;
            string fileName = $"SaveSlot{GameData.SlotNumber:D3}";
            SaveSystem.SaveData<GameData>(GameData, fileName);
            SavedGames?.TryAdd(fileName, GameData);
        }

        /// <summary>
        /// Saves the current game to a specific slot.
        /// Pass 0 to auto-select the next available slot number.
        /// If the target differs from the current slot, the current slot file is removed.
        /// </summary>
        public void SaveToSlot(int targetSlot)
        {
            if (GameData == null) return;

            if (targetSlot <= 0)
            {
                var taken = new HashSet<int>();
                foreach (var d in SavedGames.Values) taken.Add(d.SlotNumber);
                targetSlot = 1;
                while (taken.Contains(targetSlot)) targetSlot++;
            }

            if (GameData.SlotNumber != targetSlot)
            {
                string oldKey = $"SaveSlot{GameData.SlotNumber:D3}";
                SavedGames.Remove(oldKey);
                SaveSystem.DeleteSave(oldKey);
                GameData.SlotNumber = targetSlot;
            }

            SaveGame();
        }

        /// <summary>
        /// Loads a previously saved game session.
        /// </summary>
        /// <param name="gameData">The GameData object loaded from a save file.</param>
        public void LoadGame(GameData gameData)
        {
            if (gameData == null)
            {
                Debug.LogWarning("GameDirector: Ignoring load request for null GameData.", this);
                return;
            }

            this.GameData = gameData;
            RunStateManager.Instance?.Bind(GameData);

            string sceneToLoad = string.IsNullOrWhiteSpace(gameData.CurrentScene)
                ? defaultScene
                : gameData.CurrentScene;

            StartCoroutine(TravelSequence(sceneToLoad, null));
        }

        /// <summary>
        /// Deletes a specified saved game session from both the in-memory list of saved games 
        /// and the physical save file on disk.
        /// </summary>
        /// <param name="gameData">The GameData object corresponding to the save file to be deleted.</param>
        public void DeleteGame(GameData gameData)
        {
            if (gameData == null)
            {
                Debug.LogWarning("GameDirector: Ignoring delete request for null GameData.", this);
                return;
            }

            string fileName = $"SaveSlot{gameData.SlotNumber:D3}";
            SavedGames.Remove(fileName);
            SaveSystem.DeleteSave(fileName);
        }

        /// <summary>
        /// Saves the current game state and initiates the process of returning to the title scene.
        /// </summary>
        public void BackToTitle()
        {
            SaveGame();
            StartCoroutine(TravelSequence(titleScene, null));
        }

        /// <summary>
        /// Handles the final 'game over' state.
        /// </summary>
        /// <remarks>
        /// This method deletes the current game save file and immediately transitions the player 
        /// back to the title scene.
        /// </remarks>
        public void GameOver()
        {
            OnGameOver?.Invoke();

            if (GameData == null)
            {
                StartCoroutine(TravelSequence(titleScene, null));
                return;
            }

            DeleteGame(this.GameData);
            StartCoroutine(TravelSequence(titleScene, null));
        }
        #endregion

        #region Scene & Travel Management
        /// <summary>
        /// Starts a scene transition sequence, handling saving and transporting traveler cards.
        /// </summary>
        /// <remarks>
        /// It determines the next scene by finding the current scene in the targetScenes list and 
        /// moving to the next one cyclically. The transition involves a screen fade and scene load, 
        /// during which card data for all travelers is preserved.
        /// </remarks>
        /// <param name="targetScenes">A list defining the order of scenes in a travel cycle.</param>
        /// <param name="travelers">The list of CardInstances that should be carried over to the new scene.</param>
        public void InitiateTravel(List<string> targetScenes, List<CardInstance> travelers)
        {
            if (targetScenes == null || targetScenes.Count == 0)
            {
                Debug.LogError("Target scene list is empty.");
                return;
            }

            string currentScene = ResolveSceneName(SceneManager.GetActiveScene().name);
            List<string> resolvedTargetScenes = new List<string>(targetScenes.Count);
            foreach (string targetSceneName in targetScenes)
            {
                resolvedTargetScenes.Add(ResolveSceneName(targetSceneName));
            }

            int currentIndex = resolvedTargetScenes.IndexOf(currentScene);

            if (currentIndex < 0)
            {
                Debug.LogWarning($"Current scene '{currentScene}' not found in travel list.");
                return;
            }

            SaveGame();

            string targetScene = resolvedTargetScenes[(currentIndex + 1) % resolvedTargetScenes.Count];

            List<CardData> travelersData = new List<CardData>();
            if (travelers != null)
            {
                foreach (var card in travelers)
                {
                    travelersData.Add(new CardData(card));
                }
            }

            StartCoroutine(TravelSequence(targetScene, travelersData));
        }

        private const string LssPreset = "LastKernel";

        private IEnumerator TravelSequence(string sceneName, List<CardData> travelers)
        {
            if (TimeManager.Instance != null)
                TimeManager.Instance.SetExternalPause(true);

            yield return ScreenFader.Instance?.Fade(0f, 1f);

            if (travelers != null)
                incomingTravelers = new List<CardData>(travelers);

            // LSS instantiates the loading screen overlay, then async-loads the scene
            // and controls scene activation. HandleSceneLoaded picks up from there.
            LSS_LoadingScreen.LoadScene(ResolveSceneName(sceneName), LssPreset);
        }

        private void SpawnTravelers()
        {
            if (CardManager.Instance == null)
            {
                Debug.LogWarning("GameDirector: CardManager was not ready, so incoming travelers could not be restored.", this);
                incomingTravelers.Clear();
                return;
            }

            foreach (var data in incomingTravelers)
            {
                var randomPos = Random.insideUnitSphere.Flatten();
                CardManager.Instance.RestoreTraveler(data, randomPos);
            }

            incomingTravelers.Clear();
        }

        // Maps shorthand and legacy scene names to their current build-settings names.
        // TravelRecipe ScriptableObjects created before the rename still reference "Title"
        // and "Main"; this keeps them functional without requiring a data migration.
        private string ResolveSceneName(string sceneName)
        {
            return sceneName switch
            {
                "Title" => "MainMenu",
                "Main" => "Game",
                _ => sceneName
            };
        }
        #endregion
    }
}

