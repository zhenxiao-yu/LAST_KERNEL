using System.Collections.Generic;
using NUnit.Framework;

namespace Markyu.LastKernel.Tests
{
    public class RunStateDataTests
    {
        [Test]
        public void Clamp_KeepsRuntimeValuesInsideAllowedRanges()
        {
            var state = new RunStateData
            {
                Morale = 140,
                Fatigue = -5,
                InjuredPersonnel = -1,
                StructuralDamage = 120,
                PowerDeficit = 150,
                Corruption = -20,
                LastResolvedDay = -4,
                NightsSurvived = -2,
                Casualties = -3,
                SalvageValue = -8
            };

            state.Clamp();

            Assert.That(state.Morale, Is.EqualTo(100));
            Assert.That(state.Fatigue, Is.EqualTo(0));
            Assert.That(state.InjuredPersonnel, Is.EqualTo(0));
            Assert.That(state.StructuralDamage, Is.EqualTo(100));
            Assert.That(state.PowerDeficit, Is.EqualTo(100));
            Assert.That(state.Corruption, Is.EqualTo(0));
            Assert.That(state.LastResolvedDay, Is.EqualTo(1));
            Assert.That(state.NightsSurvived, Is.EqualTo(0));
            Assert.That(state.Casualties, Is.EqualTo(0));
            Assert.That(state.SalvageValue, Is.EqualTo(0));
        }

        [Test]
        public void ApplyDuskPressure_ReducesMoraleWhenRationsAreShort()
        {
            var state = new RunStateData { Morale = 65, Fatigue = 0 };
            var stats = new StatsSnapshot
            {
                TotalNutrition = 0,
                NutritionNeed = 4,
                TotalCharacters = 2,
                ExcessCards = 3
            };

            state.ApplyDuskPressure(stats);

            Assert.That(state.Morale, Is.LessThan(65));
            Assert.That(state.Fatigue, Is.GreaterThan(0));
        }

        [Test]
        public void ApplyNightCombatResult_AccumulatesOutcomeDeltas()
        {
            var state = new RunStateData { Morale = 50, Fatigue = 10, SalvageValue = 1 };
            var result = new NightCombatResult(
                playerWon: true,
                deadDefenders: new List<CardInstance> { null, null },
                survivorDefenders: new List<CardInstance>(),
                totalDefenders: 2,
                enemiesKilled: 3,
                totalEnemies: 3,
                moraleDelta: 4,
                fatigueDelta: 6,
                salvageDelta: 2);

            state.ApplyNightCombatResult(result);

            Assert.That(state.Morale, Is.EqualTo(54));
            Assert.That(state.Fatigue, Is.EqualTo(16));
            Assert.That(state.SalvageValue, Is.EqualTo(3));
            Assert.That(state.Casualties, Is.EqualTo(2));
            Assert.That(state.NightsSurvived, Is.EqualTo(1));
        }
    }
}
