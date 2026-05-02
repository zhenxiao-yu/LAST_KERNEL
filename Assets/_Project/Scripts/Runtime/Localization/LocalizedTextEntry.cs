using System.Collections.Generic;

namespace Markyu.LastKernel
{
    internal readonly struct LocalizedTextEntry
    {
        private readonly string simplifiedChinese;
        private readonly string english;
        private readonly IReadOnlyDictionary<GameLanguage, string> translations;

        public LocalizedTextEntry(string simplifiedChinese, string english)
        {
            this.simplifiedChinese = simplifiedChinese;
            this.english = english;
            translations = null;
        }

        public LocalizedTextEntry(
            string english,
            string simplifiedChinese,
            string traditionalChinese,
            string japanese,
            string korean,
            string french,
            string german,
            string spanish)
        {
            this.simplifiedChinese = simplifiedChinese;
            this.english = english;
            translations = new Dictionary<GameLanguage, string>
            {
                [GameLanguage.English] = english,
                [GameLanguage.SimplifiedChinese] = simplifiedChinese,
                [GameLanguage.TraditionalChinese] = traditionalChinese,
                [GameLanguage.Japanese] = japanese,
                [GameLanguage.Korean] = korean,
                [GameLanguage.French] = french,
                [GameLanguage.German] = german,
                [GameLanguage.Spanish] = spanish
            };
        }

        public string GetText(GameLanguage language)
        {
            if (translations != null &&
                translations.TryGetValue(language, out string translatedText) &&
                !string.IsNullOrWhiteSpace(translatedText))
            {
                return translatedText;
            }

            return language == GameLanguage.SimplifiedChinese ? simplifiedChinese : english;
        }
    }
}
