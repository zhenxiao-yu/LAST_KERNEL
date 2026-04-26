using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.Tests
{
    public sealed class LastKernelContentIdentityTests
    {
        private static readonly Regex CjkCharacters = new Regex(@"[\u4E00-\u9FFF]", RegexOptions.Compiled);

        private static readonly Regex LegacyPlayerFacingTerms = new Regex(
            @"\b(Demon|Goblin|Troll|Sacrificial|Altar|Chalice|Farmstead|Blacksmith|Revelations|Villager|Warrior|Mage|Ranger|Berry|Acorn|Turnip|Potato|Chicken|Cow|Squirrel|Slime|Satyr)\b|地精|霓虹|魔君|献祭|圣杯",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        [Test]
        public void PlayerFacingContent_UsesLastKernelLanguage()
        {
            var failures = new List<string>();

            foreach ((string path, CardDefinition card) in LoadAssets<CardDefinition>("Assets/Fortstack/Resources/Cards"))
            {
                CheckText(failures, path, "displayName", card.DisplayName);
                CheckText(failures, path, "description", card.Description);
            }

            foreach ((string path, PackDefinition pack) in LoadAssets<PackDefinition>("Assets/Fortstack/Resources/Packs"))
            {
                CheckText(failures, path, "displayName", pack.DisplayName);
                CheckText(failures, path, "description", pack.Description);
            }

            foreach ((string path, RecipeDefinition recipe) in LoadAssets<RecipeDefinition>("Assets/Fortstack/Resources/Recipes"))
            {
                CheckText(failures, path, "displayName", recipe.DisplayName);
            }

            foreach ((string path, Quest quest) in LoadAssets<Quest>("Assets/Fortstack/Resources/Quests"))
            {
                CheckText(failures, path, "title", quest.Title);
                CheckText(failures, path, "description", quest.Description);
            }

            foreach ((string path, EncounterDefinition encounter) in LoadAssets<EncounterDefinition>("Assets/Fortstack/Resources/Encounters"))
            {
                CheckText(failures, path, "notificationMessage", encounter.NotificationMessage);
            }

            Assert.That(failures, Is.Empty, string.Join(Environment.NewLine, failures));
        }

        private static IEnumerable<(string path, T asset)> LoadAssets<T>(string folder)
            where T : UnityEngine.Object
        {
            foreach (string guid in AssetDatabase.FindAssets("t:ScriptableObject", new[] { folder }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset != null)
                {
                    yield return (path, asset);
                }
            }
        }

        private static void CheckText(List<string> failures, string path, string fieldName, string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return;

            if (CjkCharacters.IsMatch(value))
            {
                failures.Add($"{path}::{fieldName} contains CJK text: {value}");
            }

            if (LegacyPlayerFacingTerms.IsMatch(value))
            {
                failures.Add($"{path}::{fieldName} contains legacy content language: {value}");
            }
        }
    }
}
