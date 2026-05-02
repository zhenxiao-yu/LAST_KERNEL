using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Markyu.LastKernel.Tests
{
    /// <summary>
    /// Tests CombatStats creation and CardDefinition combat data.
    /// Full combat flow (CombatManager, CombatTask) requires a game scene and is
    /// covered by manual sandbox testing in Sandbox_Combat.unity.
    /// </summary>
    public class CombatFlowTests
    {
        [UnityTest]
        public IEnumerator CombatStats_CreatedFromDefinition_IsValid()
        {
            CardDefinition def = ScriptableObject.CreateInstance<CardDefinition>();
            def.SetId("fighter_test");
            def.SetDisplayName("Test Fighter");

            yield return null;

            CombatStats stats = def.CreateCombatStats();
            Assert.IsNotNull(stats, "CreateCombatStats should not return null.");

            Object.Destroy(def);
        }

        [UnityTest]
        public IEnumerator CombatStats_DefaultMaxHealth_IsPositive()
        {
            CardDefinition def = ScriptableObject.CreateInstance<CardDefinition>();
            def.SetId("fighter_default");
            def.SetDisplayName("Default Fighter");

            yield return null;

            CombatStats stats = def.CreateCombatStats();
            // MaxHealth is a Stat wrapper — use .Value for the raw int.
            Assert.IsTrue(stats.MaxHealth.Value > 0,
                $"Default MaxHealth.Value should be > 0, got {stats.MaxHealth.Value}.");

            Object.Destroy(def);
        }

        [UnityTest]
        public IEnumerator CardInstance_CurrentHealth_IsZero_BeforeInitialize()
        {
            var go = new GameObject("TestCard");
            go.AddComponent<MeshRenderer>();
            go.AddComponent<BoxCollider>();
            CardInstance card = go.AddComponent<CardInstance>();

            yield return null;

            Assert.AreEqual(0, card.CurrentHealth,
                "CurrentHealth should be 0 before Initialize is called.");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator NightShopItem_ConfigureRuntime_UpdatesDisplayFields()
        {
            NightShopItemDefinition item = ScriptableObject.CreateInstance<NightShopItemDefinition>();
            item.ConfigureRuntime("Scrap Blade", "+1 ATK to fighter", 5, NightShopEffect.AddAttack, 1, true);

            yield return null;

            Assert.AreEqual("Scrap Blade", item.DisplayName);
            Assert.AreEqual("+1 ATK to fighter", item.Description);
            Assert.AreEqual(5, item.goldCost);
            Assert.AreEqual(NightShopEffect.AddAttack, item.effect);
            Assert.IsTrue(item.requiresTarget);

            Object.Destroy(item);
        }

        [UnityTest]
        public IEnumerator NightBattleManager_TrySpendGold_RejectsNegativeCost()
        {
            var go = new GameObject("NightBattleManager");
            NightBattleManager manager = go.AddComponent<NightBattleManager>();

            yield return null;

            Assert.IsFalse(manager.TrySpendGold(-1), "Negative shop costs must never add gold.");

            Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator NightFighter_FromCard_UsesUniqueInstanceIdsForSameDefinition()
        {
            CardDefinition definition = ScriptableObject.CreateInstance<CardDefinition>();
            definition.SetId("worker");
            definition.SetDisplayName("Worker");

            var goA = new GameObject("WorkerA");
            goA.AddComponent<MeshRenderer>();
            goA.AddComponent<BoxCollider>();
            CardInstance cardA = goA.AddComponent<CardInstance>();
            PrimeCardForNight(cardA, definition, 12);

            var goB = new GameObject("WorkerB");
            goB.AddComponent<MeshRenderer>();
            goB.AddComponent<BoxCollider>();
            CardInstance cardB = goB.AddComponent<CardInstance>();
            PrimeCardForNight(cardB, definition, 12);

            yield return null;

            NightFighter fighterA = NightFighter.FromCard(cardA);
            NightFighter fighterB = NightFighter.FromCard(cardB);

            Assert.AreNotEqual(fighterA.Id, fighterB.Id,
                "Two card instances from the same definition need separate night fighter IDs.");
            Assert.That(fighterA.Id, Does.StartWith("worker:"));
            Assert.That(fighterB.Id, Does.StartWith("worker:"));

            Object.Destroy(goA);
            Object.Destroy(goB);
            Object.Destroy(definition);
        }

        private static void PrimeCardForNight(CardInstance card, CardDefinition definition, int currentHealth)
        {
            SetProperty(card, nameof(CardInstance.Definition), definition);
            SetProperty(card, nameof(CardInstance.Stats), definition.CreateCombatStats());
            SetProperty(card, nameof(CardInstance.CurrentHealth), currentHealth);
        }

        private static void SetProperty(object target, string propertyName, object value)
        {
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(property, $"Property '{propertyName}' not found on {target.GetType().Name}.");

            MethodInfo setter = property.GetSetMethod(true);
            Assert.IsNotNull(setter, $"Property '{propertyName}' does not expose a setter.");
            setter.Invoke(target, new[] { value });
        }
    }
}
