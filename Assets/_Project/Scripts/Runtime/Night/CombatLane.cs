using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Pure simulation of the night combat lane. Plain C# — no MonoBehaviour, no Unity lifecycle.
    /// Driven externally via Tick(). Produces a NightCombatResult when finished.
    /// Does not kill colony cards, mutate the board, or transition game phases.
    /// </summary>
    public class CombatLane
    {
        public event Action<CombatUnit> OnUnitDied;
        public event Action<CombatUnit, CombatUnit, int, bool> OnAttackResolved; // attacker, target, damage, isCrit
        public event Action<bool> OnCombatEnded; // playerWon

        public IReadOnlyList<CombatUnit> Defenders => _defenders;
        public IReadOnlyList<CombatUnit> Enemies => _enemies;

        public bool IsOngoing { get; private set; } = true;
        public bool PlayerWon { get; private set; }

        private readonly List<CombatUnit> _defenders;
        private readonly List<CombatUnit> _enemies;
        private readonly NightWaveDefinition _waveDef;

        private int _enemiesKilled;

        public CombatLane(
            IEnumerable<CombatUnit> defenders,
            IEnumerable<CombatUnit> enemies,
            NightWaveDefinition waveDef)
        {
            _defenders = new List<CombatUnit>(defenders);
            _enemies = new List<CombatUnit>(enemies);
            _waveDef = waveDef;
        }

        // ── Simulation ────────────────────────────────────────────────────────────

        /// <summary>
        /// Advances all unit attack timers by deltaTime and resolves any attacks that fire.
        /// Call once per tick from NightPhaseManager's coroutine.
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsOngoing) return;

            ProcessSide(_defenders, _enemies, deltaTime);
            ProcessSide(_enemies, _defenders, deltaTime);

            CheckEndCondition();
        }

        private void ProcessSide(List<CombatUnit> attackers, List<CombatUnit> targets, float deltaTime)
        {
            foreach (var attacker in attackers)
            {
                if (!attacker.IsAlive) continue;

                attacker.AttackTimer += deltaTime;

                if (attacker.AttackTimer < attacker.AttackCooldown) continue;

                attacker.AttackTimer -= attacker.AttackCooldown;

                var target = GetFrontTarget(targets);
                if (target == null) continue;

                ResolveAttack(attacker, target);
            }
        }

        private void ResolveAttack(CombatUnit attacker, CombatUnit target)
        {
            float effectiveHitChance = Mathf.Clamp(
                (attacker.AccuracyPercent - target.DodgePercent) / 100f,
                0.05f, 1f);

            if (UnityEngine.Random.value > effectiveHitChance)
            {
                OnAttackResolved?.Invoke(attacker, target, 0, false);
                return;
            }

            int baseDamage = Mathf.Max(1, attacker.Attack - target.Defense);

            bool isCrit = UnityEngine.Random.value < attacker.CritChancePercent / 100f;
            int finalDamage = isCrit
                ? Mathf.RoundToInt(baseDamage * (attacker.CritMultiplier / 100f))
                : baseDamage;

            target.TakeDamage(finalDamage);
            OnAttackResolved?.Invoke(attacker, target, finalDamage, isCrit);

            if (!target.IsAlive)
            {
                if (target.Side == CombatUnitSide.Enemy) _enemiesKilled++;
                OnUnitDied?.Invoke(target);
            }
        }

        private static CombatUnit GetFrontTarget(List<CombatUnit> units)
        {
            foreach (var u in units)
            {
                if (u.IsAlive) return u;
            }
            return null;
        }

        private void CheckEndCondition()
        {
            bool defendersAlive = _defenders.Any(u => u.IsAlive);
            bool enemiesAlive = _enemies.Any(u => u.IsAlive);

            if (!defendersAlive || !enemiesAlive)
            {
                IsOngoing = false;
                PlayerWon = !enemiesAlive;
                OnCombatEnded?.Invoke(PlayerWon);
            }
        }

        /// <summary>Forces combat to end in a draw (defenders survive). Used as a safety fallback.</summary>
        public void ForceEnd()
        {
            if (!IsOngoing) return;
            IsOngoing = false;
            PlayerWon = true;
            OnCombatEnded?.Invoke(PlayerWon);
        }

        // ── Result ────────────────────────────────────────────────────────────────

        public NightCombatResult BuildResult()
        {
            var dead = _defenders.Where(u => !u.IsAlive && u.SourceCard != null)
                                 .Select(u => u.SourceCard)
                                 .ToList();

            var survivors = _defenders.Where(u => u.IsAlive && u.SourceCard != null)
                                      .Select(u => u.SourceCard)
                                      .ToList();

            int moraleDelta;
            int fatigueDelta = 0;
            int salvageDelta = _enemiesKilled * (_waveDef?.SalvagePerKill ?? 1);

            if (PlayerWon)
            {
                moraleDelta = _waveDef?.VictoryMoraleDelta ?? 7;
            }
            else
            {
                moraleDelta = _waveDef?.DefeatMoraleDelta ?? -7;
            }

            if (_waveDef != null)
            {
                fatigueDelta = _defenders.Count * _waveDef.FatigueCostPerDefender;
            }

            return new NightCombatResult(
                PlayerWon,
                dead,
                survivors,
                _defenders.Count,
                _enemiesKilled,
                _enemies.Count,
                moraleDelta,
                fatigueDelta,
                salvageDelta
            );
        }
    }
}
