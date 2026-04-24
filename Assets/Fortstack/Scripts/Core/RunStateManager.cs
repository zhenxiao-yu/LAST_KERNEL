using UnityEngine;

namespace Markyu.FortStack
{
    [DefaultExecutionOrder(-200)]
    public class RunStateManager : MonoBehaviour
    {
        public static RunStateManager Instance { get; private set; }

        public event System.Action<GamePhase> OnPhaseChanged;
        public event System.Action<RunStateData> OnRunStateChanged;

        public RunStateData State { get; private set; } = new RunStateData();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Bootstrap()
        {
            if (Instance != null)
            {
                return;
            }

            var host = new GameObject(nameof(RunStateManager));
            DontDestroyOnLoad(host);
            host.AddComponent<RunStateManager>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void Bind(GameData gameData)
        {
            State = gameData != null ? gameData.EnsureRunState() : new RunStateData();
            State.Clamp();
            NotifyStateChanged();
        }

        public void SyncToGameData(GameData gameData)
        {
            if (gameData == null)
            {
                return;
            }

            gameData.RunState = State ?? new RunStateData();
            gameData.RunState.Clamp();
        }

        public void SetPhase(GamePhase phase)
        {
            EnsureState();

            if (State.CurrentPhase == phase)
            {
                return;
            }

            State.CurrentPhase = phase;
            State.Clamp();

            OnPhaseChanged?.Invoke(phase);
            NotifyStateChanged();
        }

        public void ApplyDuskPressure(StatsSnapshot stats)
        {
            EnsureState();
            State.ApplyDuskPressure(stats);
            NotifyStateChanged();
        }

        public void RecordNightContact(bool hostileContact)
        {
            EnsureState();
            State.RecordNightContact(hostileContact);
            NotifyStateChanged();
        }

        public void ApplyDawnRecovery(StatsSnapshot stats)
        {
            EnsureState();
            State.ApplyDawnRecovery(stats);
            NotifyStateChanged();
        }

        private void EnsureState()
        {
            if (State == null)
            {
                State = new RunStateData();
            }
        }

        private void NotifyStateChanged()
        {
            EnsureState();
            OnRunStateChanged?.Invoke(State);
        }
    }
}
