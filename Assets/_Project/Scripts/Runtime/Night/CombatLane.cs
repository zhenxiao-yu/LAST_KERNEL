using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Pure simulation of the night combat lane. Plain C# — no MonoBehaviour, no Unity lifecycle.
    /// Driven externally via Tick(). Produces a NightCombatResult when finished.
    ///
    /// Ability hooks are delegated to AbilityResolver:
    ///   Battle-start: shields, GangUp auras initialised.
    ///   Per-tick: DoT, Repair, Healer, GangUp aura refresh.
    ///   Per-attack: Ethereal evasion, Executioner bonus, Poison on-hit.
    ///   On death: Resilient survival check, Veteran/Rally/Infect triggers.
    /// </summary>
    public class CombatLane
    {
        public event Action<CombatUnit>                    OnUnitDied;
        public event Action<CombatUnit, CombatUnit, int, bool> OnAttackResolved; // attacker, target, damage, isCrit
        public event Action<bool>                          OnCombatEnded;       // playerWon

        public IReadOnlyList<CombatUnit> Defenders => _defenders;
        public IReadOnlyList<CombatUnit> Enemies   => _enemies;
        public IEnumerable<CombatUnit>   AllUnits   => _defenders.Concat(_enemies);

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
            _enemies   = new List<CombatUnit>(enemies);
            _waveDef   = waveDef;

            AbilityResolver.ApplyBattleStartAbilities(_defenders, _enemies);
        }

        // ── Simulation ────────────────────────────────────────────────────────────

        public void Tick(float deltaTime)
        {
            if (!IsOngoing) return;

            // Phase 1: time-based effects (DoT, Repair, Healer, aura refresh)
            AbilityResolver.ApplyTickEffects(_defenders, _enemies);
            ProcessAllPendingDeaths();
            if (!IsOngoing) return;

            // Phase 2: attack processing
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

                if (attacker.AttackTimer < attacker.EffectiveAttackCooldown) continue;

                attacker.AttackTimer -= attacker.EffectiveAttackCooldown;

                var target = AbilityResolver.GetTarget(targets);
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

            // Ethereal: one-time complete evasion of the first hit
            if (AbilityResolver.CheckEtherealEvasion(target))
            {
                OnAttackResolved?.Invoke(attacker, target, 0, false);
                return;
            }

            int baseDamage = Mathf.Max(1, attacker.Attack - target.Defense);

            bool isCrit = UnityEngine.Random.value < attacker.CritChancePercent / 100f;
            int finalDamage = isCrit
                ? Mathf.RoundToInt(baseDamage * (attacker.CritMultiplier / 100f))
                : baseDamage;

            // Executioner: bonus damage against low-HP targets
            finalDamage += AbilityResolver.ComputeBonusDamage(attacker, target, finalDamage);

            int actualDamage = target.TakeDamage(finalDamage);
            OnAttackResolved?.Invoke(attacker, target, actualDamage, isCrit);

            AbilityResolver.ResolveOnHitAbilities(attacker, target, actualDamage);

            if (!target.IsAlive)
                ProcessPotentialDeath(target, attacker);
        }

        // ── Death processing ──────────────────────────────────────────────────────

        private void ProcessAllPendingDeaths()
        {
            foreach (var u in _defenders) ProcessPotentialDeath(u);
            foreach (var u in _enemies)   ProcessPotentialDeath(u);
        }

        private void ProcessPotentialDeath(CombatUnit dying, CombatUnit killer = null)
        {
            if (dying.IsAlive || dying.DeathProcessed) return;

            // Resilient: survive at 1 HP once
            if (AbilityResolver.CheckDeathSurvival(dying)) return;

            dying.MarkDeathProcessed();

            if (dying.Side == CombatUnitSide.Enemy) _enemiesKilled++;

            AbilityResolver.ResolveOnKillAbilities(killer, dying);

            var deadAllies = dying.Side == CombatUnitSide.Enemy
                ? (IReadOnlyList<CombatUnit>)_enemies
                : _defenders;
            var deadFoes = dying.Side == CombatUnitSide.Enemy
                ? (IReadOnlyList<CombatUnit>)_defenders
                : _enemies;

            AbilityResolver.ResolveOnDeathAbilities(dying, deadAllies, deadFoes);
            OnUnitDied?.Invoke(dying);

            CheckEndCondition();
        }

        private void CheckEndCondition()
        {
            if (!IsOngoing) return;

            bool defendersAlive = _defenders.Any(u => u.IsAlive);
            bool enemiesAlive   = _enemies.Any(u => u.IsAlive);

            if (!defendersAlive || !enemiesAlive)
            {
                IsOngoing = false;
                PlayerWon = !enemiesAlive;
                OnCombatEnded?.Invoke(PlayerWon);
            }
        }

        /// <summary>Forces combat to end in a draw (defenders survive). Safety fallback.</summary>
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
                moraleDelta = _waveDef?.VictoryMoraleDelta ?? 7;
            else
                moraleDelta = _waveDef?.DefeatMoraleDelta ?? -7;

            if (_waveDef != null)
                fatigueDelta = _defenders.Count * _waveDef.FatigueCostPerDefender;

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
