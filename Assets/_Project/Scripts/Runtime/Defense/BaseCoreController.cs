// BaseCoreController — Health for the player's base during night defense.
//
// When enemies reach the end of a lane they call TakeDamage() here.
// OnBaseDestroyed fires once and immediately triggers DefensePhaseController.DeclareDefeat().

using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Tracks the base's hit points during the night phase.
    /// Subscribe to <see cref="OnDamaged"/> for HP-bar updates and
    /// <see cref="OnBaseDestroyed"/> for game-over triggers.
    /// </summary>
    public class BaseCoreController : MonoBehaviour
    {
        public static BaseCoreController Instance { get; private set; }

        [SerializeField, Min(1)] private int maxHP = 20;

        public int MaxHP => maxHP;
        public int CurrentHP { get; private set; }
        public bool IsDestroyed => CurrentHP <= 0;

        /// <summary>Fired after damage is applied. Parameters: (currentHP, maxHP).</summary>
        public event System.Action<int, int> OnDamaged;

        /// <summary>Fired once when CurrentHP first reaches zero.</summary>
        public event System.Action OnBaseDestroyed;

        private void Awake()
        {
            Instance = this;
            CurrentHP = maxHP;
        }

        /// <summary>Reset HP to full — call at the start of each night.</summary>
        public void ResetHP()
        {
            CurrentHP = maxHP;
            OnDamaged?.Invoke(CurrentHP, maxHP);
        }

        /// <param name="amount">Raw damage (before any future armour calculations).</param>
        public void TakeDamage(int amount)
        {
            if (IsDestroyed) return;

            CurrentHP = Mathf.Max(0, CurrentHP - amount);
            OnDamaged?.Invoke(CurrentHP, maxHP);

            if (IsDestroyed)
            {
                OnBaseDestroyed?.Invoke();
                DefensePhaseController.Instance?.DeclareDefeat();
            }
        }
    }
}
