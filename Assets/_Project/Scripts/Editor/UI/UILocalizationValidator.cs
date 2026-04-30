using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Validates GameLocalization.TextEntries for EN and Simplified Chinese coverage.
    /// Run via LAST KERNEL → Validate → Inline Strings.
    /// </summary>
    public static class UILocalizationValidator
    {
        [MenuItem("LAST KERNEL/Validate/Inline Strings", false, 101)]
        public static void ValidateKeys()
        {
            GameLocalization.Initialize();

            var results = new List<KeyResult>();

            foreach (string key in GameLocalization.LegacyKeys)
            {
                bool hasEN = GameLocalization.TryGetLegacyText(key, GameLanguage.English, out string en);
                bool hasZH = GameLocalization.TryGetLegacyText(key, GameLanguage.SimplifiedChinese, out string zh);

                bool enMissing = !hasEN || string.IsNullOrWhiteSpace(en);
                bool zhMissing = !hasZH || string.IsNullOrWhiteSpace(zh);
                bool zhIdenticalToEn = !zhMissing && !enMissing &&
                    string.Equals(zh?.Trim(), en?.Trim(), System.StringComparison.Ordinal);

                results.Add(new KeyResult
                {
                    Key            = key,
                    EN             = en ?? string.Empty,
                    ZH             = zh ?? string.Empty,
                    EnMissing      = enMissing,
                    ZhMissing      = zhMissing,
                    ZhSameAsEn     = zhIdenticalToEn,
                });
            }

            int enErrors  = results.Count(r => r.EnMissing);
            int zhErrors  = results.Count(r => r.ZhMissing);
            int zhSame    = results.Count(r => r.ZhSameAsEn);
            int total     = results.Count;

            if (enErrors == 0 && zhErrors == 0 && zhSame == 0)
            {
                Debug.Log($"[UILocalizationValidator] All {total} keys OK — EN and ZH present and distinct.");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine($"[UILocalizationValidator] {total} keys checked — " +
                          $"{enErrors} EN missing, {zhErrors} ZH missing, {zhSame} ZH identical to EN.\n");

            foreach (KeyResult r in results.Where(r => r.EnMissing))
                sb.AppendLine($"  [EN MISSING]  {r.Key}");

            foreach (KeyResult r in results.Where(r => r.ZhMissing))
                sb.AppendLine($"  [ZH MISSING]  {r.Key}  en={r.EN}");

            foreach (KeyResult r in results.Where(r => r.ZhSameAsEn))
                sb.AppendLine($"  [ZH=EN]       {r.Key}  value={r.EN}");

            if (enErrors > 0 || zhErrors > 0)
                Debug.LogWarning(sb.ToString());
            else
                Debug.Log(sb.ToString());
        }

        private struct KeyResult
        {
            public string Key;
            public string EN;
            public string ZH;
            public bool   EnMissing;
            public bool   ZhMissing;
            public bool   ZhSameAsEn;
        }
    }
}
