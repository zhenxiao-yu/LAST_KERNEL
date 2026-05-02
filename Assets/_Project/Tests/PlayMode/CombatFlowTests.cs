using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        [UnityTest]
        public IEnumerator NightBattleManager_RunNight_OffersThreeNonCharacterRewardsAfterVictory()
        {
            var managerGo = new GameObject("NightBattleManager");
            NightBattleManager manager = managerGo.AddComponent<NightBattleManager>();

            CardDefinition defenderDefinition = MakeTestCard("night_defender", "Night Defender", CardCategory.Character);
            var defenderGo = new GameObject("NightDefenderCard");
            defenderGo.AddComponent<MeshRenderer>();
            defenderGo.AddComponent<BoxCollider>();
            CardInstance defender = defenderGo.AddComponent<CardInstance>();
            PrimeCardForNight(defender, defenderDefinition, 15);

            NightWaveDefinition emptyWave = NightWaveDefinition.CreateRuntime(
                "Empty Test Wave",
                "No enemies for reward flow verification.",
                new List<EnemyEntry>());

            CardDefinition wood = MakeTestCard("reward_wood", "Wood", CardCategory.Material);
            CardDefinition food = MakeTestCard("reward_food", "Ration", CardCategory.Consumable);
            CardDefinition blade = MakeTestCard("reward_blade", "Blade", CardCategory.Equipment);
            CardDefinition recruit = MakeTestCard("reward_recruit", "Recruit", CardCategory.Character);

            SetField(manager, "defaultWave", emptyWave);
            SetField(manager, "rewardPool", new[] { wood, food, blade, recruit });
            SetField(manager, "rewardChoiceCount", 3);

            yield return manager.RunNight(new List<CardInstance> { defender });

            Assert.IsTrue(manager.LastResult.PlayerWon, "The empty wave should resolve as a night victory.");
            Assert.AreEqual(3, manager.CurrentRewardChoices.Count, "Victory should offer exactly three reward cards.");
            Assert.IsFalse(manager.CurrentRewardChoices.Any(card => card.Category == CardCategory.Character),
                "Character cards should not be offered as night rewards because a full wipe must still end the run.");
            Assert.Contains(manager.SelectedReward, manager.CurrentRewardChoices.ToList(),
                "Headless battle flow should auto-select one valid reward so tests and fallback scenes do not hang.");

            Object.Destroy(managerGo);
            Object.Destroy(defenderGo);
            Object.Destroy(defenderDefinition);
            Object.Destroy(emptyWave);
            Object.Destroy(wood);
            Object.Destroy(food);
            Object.Destroy(blade);
            Object.Destroy(recruit);
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

        private static CardDefinition MakeTestCard(string id, string displayName, CardCategory category)
        {
            CardDefinition definition = ScriptableObject.CreateInstance<CardDefinition>();
            definition.SetId(id);
            definition.SetDisplayName(displayName);
            SetField(definition, "category", category);
            return definition;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(
                fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            Assert.IsNotNull(field, $"Field '{fieldName}' not found on {target.GetType().Name}.");
            field.SetValue(target, value);
        }
    }
}
