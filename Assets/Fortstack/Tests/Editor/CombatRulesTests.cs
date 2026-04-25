using NUnit.Framework;

namespace Markyu.FortStack.Tests
{
    public class CombatRulesTests
    {
        [Test]
        public void GetAdvantage_UsesExpectedTypeTriangle()
        {
            Assert.That(CombatRules.GetAdvantage(CombatType.Melee, CombatType.Ranged), Is.EqualTo(CombatTypeAdvantage.Advantage));
            Assert.That(CombatRules.GetAdvantage(CombatType.Ranged, CombatType.Magic), Is.EqualTo(CombatTypeAdvantage.Advantage));
            Assert.That(CombatRules.GetAdvantage(CombatType.Magic, CombatType.Melee), Is.EqualTo(CombatTypeAdvantage.Advantage));

            Assert.That(CombatRules.GetAdvantage(CombatType.Ranged, CombatType.Melee), Is.EqualTo(CombatTypeAdvantage.Disadvantage));
            Assert.That(CombatRules.GetAdvantage(CombatType.Melee, CombatType.Melee), Is.EqualTo(CombatTypeAdvantage.None));
            Assert.That(CombatRules.GetAdvantage(CombatType.None, CombatType.Magic), Is.EqualTo(CombatTypeAdvantage.None));
        }

        [Test]
        public void CalculateDamage_ClampsToAtLeastOneAfterMultipliers()
        {
            int damage = CombatRules.CalculateDamage(
                attack: 2,
                defense: 10,
                advantage: CombatTypeAdvantage.Disadvantage,
                advantageMultiplier: 1.5f,
                disadvantageMultiplier: 0.25f,
                isCritical: false,
                criticalMultiplier: 150);

            Assert.That(damage, Is.EqualTo(1));
        }

        [Test]
        public void ResolveAttack_UsesInjectedRollsForDeterministicHitAndCritical()
        {
            var attacker = new CombatStats(10, 8, 0, 100, 100, 0, 100, 200);
            var defender = new CombatStats(10, 2, 2, 100, 100, 0, 0, 150);

            HitResult result = CombatRules.ResolveAttack(
                attacker,
                CombatType.Melee,
                defender,
                CombatType.Ranged,
                advantageMultiplier: 1.5f,
                disadvantageMultiplier: 0.75f,
                hitRoll: 0f,
                criticalRoll: 0f);

            Assert.That(result.Type, Is.EqualTo(HitType.Critical));
            Assert.That(result.Advantage, Is.EqualTo(CombatTypeAdvantage.Advantage));
            Assert.That(result.Damage, Is.EqualTo(18));
        }
    }
}
