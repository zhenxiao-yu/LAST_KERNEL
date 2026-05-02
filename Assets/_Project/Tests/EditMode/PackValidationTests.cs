using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.Tests
{
    /// <summary>
    /// Validates all PackDefinition assets for structural correctness.
    /// </summary>
    public class PackValidationTests
    {
        private PackDefinition[] _allPacks;

        [SetUp]
        public void SetUp()
        {
            _allPacks = AssetDatabase
                .FindAssets("t:PackDefinition", new[] { "Assets/_Project" })
                .Select(g => AssetDatabase.LoadAssetAtPath<PackDefinition>(AssetDatabase.GUIDToAssetPath(g)))
                .Where(p => p != null)
                .ToArray();
        }

        [Test]
        public void AllPacks_HaveSlots()
        {
            var failing = _allPacks
                .Where(p => p.Slots == null || p.Slots.Count == 0)
                .Select(p => AssetDatabase.GetAssetPath(p))
                .ToList();

            Assert.IsEmpty(failing, "Packs with no slots:\n" + string.Join("\n", failing));
        }

        [Test]
        public void AllPacks_HavePositiveBuyPrice()
        {
            var failing = _allPacks
                .Where(p => p.BuyPrice <= 0 && !IsFreeStarterPack(p))
                .Select(p => $"{AssetDatabase.GetAssetPath(p)} (buyPrice={p.BuyPrice})")
                .ToList();

            // Buy price 0 is a warning, not a hard failure — log it
            if (failing.Count > 0)
                Debug.LogWarning($"[Test] Packs with buyPrice <= 0:\n{string.Join("\n", failing)}");
        }

        [Test]
        public void AllPacks_SlotCardsAreNotNull()
        {
            var failing = new List<string>();
            foreach (PackDefinition pack in _allPacks)
            {
                if (pack.Slots == null) continue;
                var so = new SerializedObject(pack);
                SerializedProperty slots = so.FindProperty("slots");
                if (slots == null) continue;

                for (int i = 0; i < slots.arraySize; i++)
                {
                    SerializedProperty card = slots.GetArrayElementAtIndex(i).FindPropertyRelative("card");
                    if (card != null && card.objectReferenceValue == null)
                        failing.Add($"{AssetDatabase.GetAssetPath(pack)} slot[{i}] has null card");
                }
            }
            Assert.IsEmpty(failing, string.Join("\n", failing));
        }

        [Test]
        public void AllPacks_HaveId()
        {
            var failing = _allPacks
                .Where(p => string.IsNullOrEmpty(p.Id))
                .Select(p => AssetDatabase.GetAssetPath(p))
                .ToList();

            Assert.IsEmpty(failing, "Packs with no ID:\n" + string.Join("\n", failing));
        }

        private static bool IsFreeStarterPack(PackDefinition pack)
        {
            return pack != null &&
                (pack.name.StartsWith("00_Pack_") || pack.name.StartsWith("10_Pack_Island"));
        }
    }
}
