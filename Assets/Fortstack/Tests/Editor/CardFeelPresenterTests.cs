using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.Tests
{
    public class CardFeelPresenterTests
    {
        [Test]
        public void EnsureOn_AddsPresenterOnce()
        {
            var gameObject = new GameObject("RuntimeCard");
            gameObject.AddComponent<MeshRenderer>();
            gameObject.AddComponent<BoxCollider>();
            gameObject.AddComponent<CardInstance>();

            CardFeelPresenter first = CardFeelPresenter.EnsureOn(gameObject);
            CardFeelPresenter second = CardFeelPresenter.EnsureOn(gameObject);

            Assert.NotNull(first);
            Assert.AreSame(first, second);
            Assert.AreEqual(1, gameObject.GetComponents<CardFeelPresenter>().Length);

            Object.DestroyImmediate(gameObject);
        }

        [Test]
        public void CardAndPackPrefabs_CanResolveRuntimePresenter()
        {
            string[] prefabGuids = AssetDatabase.FindAssets(
                "t:Prefab",
                new[] { "Assets/Fortstack/Prefabs/Cards", "Assets/Fortstack/Prefabs" });

            Assert.IsNotEmpty(prefabGuids);

            foreach (string guid in prefabGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.Contains("Card_") && !path.EndsWith("PackInstance.prefab"))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                GameObject instance = Object.Instantiate(prefab);

                try
                {
                    CardInstance card = instance.GetComponent<CardInstance>();
                    Assert.NotNull(card, $"{path} should expose a CardInstance.");

                    CardFeelPresenter presenter = CardFeelPresenter.EnsureOn(instance);
                    Assert.NotNull(presenter, $"{path} should be able to resolve a CardFeelPresenter at runtime.");
                    Assert.AreEqual(1, instance.GetComponents<CardFeelPresenter>().Length);
                }
                finally
                {
                    Object.DestroyImmediate(instance);
                }
            }
        }
    }
}
