using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Markyu.LastKernel.Tests
{
    /// <summary>
    /// Tests GameLocalization runtime behaviour (language switching, text lookup).
    /// These are self-contained and do not depend on the game scene.
    /// </summary>
    public class LocalizationRuntimeTests
    {
        private GameLanguage _originalLanguage;

        [SetUp]
        public void SetUp()
        {
            _originalLanguage = GameLocalization.CurrentLanguage;
        }

        [TearDown]
        public void TearDown()
        {
            // Restore language so other tests are not affected
            GameLocalization.SetLanguage(_originalLanguage);
        }

        [UnityTest]
        public IEnumerator SetLanguage_ChangesCurrentLanguage_ToEnglish()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            yield return null;
            Assert.AreEqual(GameLanguage.English, GameLocalization.CurrentLanguage);
        }

        [UnityTest]
        public IEnumerator SetLanguage_ChangesCurrentLanguage_ToSimplifiedChinese()
        {
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            yield return null;
            Assert.AreEqual(GameLanguage.SimplifiedChinese, GameLocalization.CurrentLanguage);
        }

        [UnityTest]
        public IEnumerator GetOptional_ReturnsNonEmptyString_ForKnownKey()
        {
            GameLocalization.SetLanguage(GameLanguage.English);
            yield return null;

            string result = GameLocalization.GetOptional("language.label", "FALLBACK");
            Assert.IsFalse(string.IsNullOrWhiteSpace(result),
                "GetOptional should return a non-empty string for a known key.");
        }

        [UnityTest]
        public IEnumerator GetOptional_ReturnsFallback_ForUnknownKey()
        {
            yield return null;

            const string fallback = "FALLBACK_TEXT";
            string result = GameLocalization.GetOptional("this.key.does.not.exist.xyz", fallback);
            Assert.AreEqual(fallback, result,
                "GetOptional should return the fallback for an unknown key.");
        }

        [UnityTest]
        public IEnumerator LoadingScreenText_UsesSelectedLanguage()
        {
            GameLocalization.SetLanguage(GameLanguage.SimplifiedChinese);
            yield return null;

            Assert.AreEqual("\u70B9\u51FB\u7EE7\u7EED", GameLocalization.Get("loading.continue"));

            string tip = GameLocalization.GetOptional("loading.tip.01", string.Empty);
            Assert.IsFalse(string.IsNullOrWhiteSpace(tip),
                "Loading screen tips should resolve through the active localization table.");
            Assert.AreNotEqual("loading.tip.01", tip,
                "Loading screen tips should not fall back to their localization keys.");
        }
    }
}
