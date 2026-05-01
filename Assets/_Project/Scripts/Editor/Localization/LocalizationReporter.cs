using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Tables;

namespace Markyu.LastKernel.Localization.EditorTools
{
    /// <summary>
    /// Scans the GameText string tables and reports keys where the zh-Hans value is
    /// identical to the English source (i.e. still a placeholder) or empty.
    /// Run via Last Kernel > Localization > Report Missing Chinese Translations.
    /// The report is printed to the Console and exported to
    /// Assets/_Project/Localization/Docs/MissingChineseTranslations.csv.
    /// </summary>
    public static class LocalizationReporter
    {
        private const string ReportPath = "Assets/_Project/Localization/Docs/MissingChineseTranslations.csv";

        // Asset-backed key prefixes, ordered for grouping in the report.
        private static readonly string[] AssetPrefixes =
        {
            "card.",
            "pack.",
            "quest.",
            "recipe.",
            "encounter.",
            "night.enemy.",
            "night.wave.",
        };

        [MenuItem("LAST KERNEL/Localization/Report Missing CN Translations", false, 2)]
        public static void ReportMissingChineseTranslations()
        {
            StringTableCollection collection =
                LocalizationEditorSettings.GetStringTableCollection(UnityLocalizationBridge.DefaultStringTable);

            if (collection == null)
            {
                Debug.LogError(
                    "LocalizationReporter: GameText string table collection not found. " +
                    "Run 'Last Kernel > Localization > Rebuild GameText Tables' first.");
                return;
            }

            StringTable enTable  = collection.GetTable(new LocaleIdentifier("en"))       as StringTable;
            StringTable zhTable  = collection.GetTable(new LocaleIdentifier("zh-Hans"))  as StringTable;

            if (enTable == null)
            {
                Debug.LogError("LocalizationReporter: English ('en') string table not found.");
                return;
            }

            if (zhTable == null)
            {
                Debug.LogError("LocalizationReporter: Simplified Chinese ('zh-Hans') string table not found.");
                return;
            }

            // Collect all entries that need translation.
            var missing = new List<MissingEntry>();

            foreach (SharedTableData.SharedTableEntry shared in collection.SharedData.Entries)
            {
                string key = shared.Key;

                StringTableEntry enEntry = enTable.GetEntry(key);
                StringTableEntry zhEntry = zhTable.GetEntry(key);

                string enValue = enEntry?.Value ?? string.Empty;
                string zhValue = zhEntry?.Value ?? string.Empty;

                bool isMissing = string.IsNullOrWhiteSpace(zhValue) ||
                                 string.Equals(zhValue.Trim(), enValue.Trim(), System.StringComparison.Ordinal);

                if (!isMissing)
                    continue;

                string group = ClassifyKey(key);
                missing.Add(new MissingEntry(group, key, enValue, zhValue));
            }

            if (missing.Count == 0)
            {
                Debug.Log("LocalizationReporter: All zh-Hans translations are present and differ from English. Nothing to do.");
                return;
            }

            // Sort: group first, then key.
            missing.Sort((a, b) =>
            {
                int cg = string.Compare(a.Group, b.Group, System.StringComparison.Ordinal);
                return cg != 0 ? cg : string.Compare(a.Key, b.Key, System.StringComparison.Ordinal);
            });

            // ── Console summary ───────────────────────────────────────────────

            var sb = new StringBuilder();
            sb.AppendLine($"LocalizationReporter: {missing.Count} zh-Hans keys need translation.\n");

            string currentGroup = null;
            foreach (MissingEntry entry in missing)
            {
                if (entry.Group != currentGroup)
                {
                    currentGroup = entry.Group;
                    int groupCount = missing.Count(e => e.Group == currentGroup);
                    sb.AppendLine($"── {currentGroup} ({groupCount}) ──────────────────────────");
                }

                string status = string.IsNullOrWhiteSpace(entry.ZhValue) ? "[EMPTY]" : "[SAME AS EN]";
                sb.AppendLine($"  {status}  {entry.Key}");
                if (!string.IsNullOrWhiteSpace(entry.EnValue))
                    sb.AppendLine($"           en: {entry.EnValue}");
            }

            Debug.Log(sb.ToString());

            // ── CSV export ────────────────────────────────────────────────────

            WriteCsv(missing);

            Debug.Log($"LocalizationReporter: Report written to {ReportPath}");
            AssetDatabase.Refresh();
        }

        private static void WriteCsv(IEnumerable<MissingEntry> entries)
        {
            string fullPath = Path.Combine(Application.dataPath, "..", ReportPath);
            fullPath = Path.GetFullPath(fullPath);

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);

            using var writer = new StreamWriter(fullPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.WriteLine("group,key,en,zh-Hans");
            foreach (MissingEntry entry in entries)
            {
                writer.WriteLine(
                    $"{CsvEscape(entry.Group)},{CsvEscape(entry.Key)},{CsvEscape(entry.EnValue)},{CsvEscape(entry.ZhValue)}");
            }
        }

        private static string CsvEscape(string value)
        {
            if (string.IsNullOrEmpty(value))
                return string.Empty;

            bool needsQuotes = value.Contains(',') || value.Contains('"') || value.Contains('\n');
            if (!needsQuotes)
                return value;

            return '"' + value.Replace("\"", "\"\"") + '"';
        }

        private static string ClassifyKey(string key)
        {
            foreach (string prefix in AssetPrefixes)
            {
                if (key.StartsWith(prefix, System.StringComparison.Ordinal))
                    return prefix.TrimEnd('.');
            }

            return "ui";
        }

        private readonly struct MissingEntry
        {
            public MissingEntry(string group, string key, string enValue, string zhValue)
            {
                Group   = group;
                Key     = key;
                EnValue = enValue;
                ZhValue = zhValue;
            }

            public string Group   { get; }
            public string Key     { get; }
            public string EnValue { get; }
            public string ZhValue { get; }
        }
    }
}
