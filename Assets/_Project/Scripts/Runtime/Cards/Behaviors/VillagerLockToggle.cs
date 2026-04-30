using UnityEngine;

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(CardInstance))]
    public class VillagerLockToggle : MonoBehaviour, IClickable
    {
        public bool IsLocked { get; private set; }

        private CardInstance _card;
        private CardAI _ai;

        private void Awake()
        {
            _card = GetComponent<CardInstance>();
            _ai = GetComponent<CardAI>();
        }

        public bool OnClick(Vector3 clickPosition)
        {
            var mgr = ColonyAIManager.Instance;
            if (mgr == null || mgr.Settings == null || !mgr.Settings.EnableColonyAutopilot)
                return false;

            SetLocked(!IsLocked);
            return true;
        }

        public void SetLocked(bool value)
        {
            IsLocked = value;
            _ai?.SetLocked(IsLocked);
            _card?.RefreshDisplay();
            AudioManager.Instance?.PlaySFX(AudioId.Click);
        }

        public void RestoreLocked(bool value)
        {
            IsLocked = value;
            if (IsLocked)
            {
                _ai?.SetLocked(true);
                _card?.RefreshDisplay();
            }
        }
    }
}
