using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Localization;
using UnityEditor.Localization.Plugins.CSV;
using UnityEditor.Localization.Plugins.CSV.Columns;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Metadata;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace Markyu.LastKernel.Localization.EditorTools
{
    public static class LocalizationAssetBuilder
    {
        private const string LocalizationRoot = "Assets/Fortstack/Localization";
        private const string LocalesFolder = LocalizationRoot + "/Locales";
        private const string StringTablesFolder = LocalizationRoot + "/StringTables";
        private const string AssetTablesFolder = LocalizationRoot + "/AssetTables";
        private const string DocsFolder = LocalizationRoot + "/Docs";
        private const string SettingsAssetPath = LocalizationRoot + "/Localization Settings.asset";
        private const string SourceCsvPath = DocsFolder + "/GameText_Localization_Source.csv";
        private const string ResourcesRoot = "Assets/Fortstack/Resources";

        private static readonly LocaleSpec[] LocaleSpecs =
        {
            new("English", "en"),
            new("Simplified Chinese", "zh-Hans"),
            new("Traditional Chinese", "zh-Hant"),
            new("Japanese", "ja"),
            new("Korean", "ko"),
            new("French", "fr"),
            new("German", "de"),
            new("Spanish", "es")
        };

        [MenuItem("Last Kernel/Localization/Rebuild GameText Tables")]
        public static void RebuildGameTextTables()
        {
            EnsureFolders();

            Locale[] locales = EnsureLocales();
            LocalizationSettings settings = EnsureSettings(locales[0]);
            StringTableCollection collection = EnsureGameTextCollection(locales);

            if (File.Exists(SourceCsvPath))
            {
                ImportCsv(collection);
            }
            else
            {
                Debug.LogWarning($"LocalizationAssetBuilder: CSV source was not found at {SourceCsvPath}.");
            }

            int legacyCount = AddLegacyCompatibilityEntries(collection);
            int assetCount = AddAssetBackedEntries(collection);

            ValidateNoDuplicateKeys(collection);
            ExportCsv(collection);

            EditorUtility.SetDirty(settings);
            EditorUtility.SetDirty(collection);
            EditorUtility.SetDirty(collection.SharedData);

            foreach (StringTable table in collection.StringTables)
            {
                EditorUtility.SetDirty(table);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                $"LocalizationAssetBuilder: Rebuilt '{UnityLocalizationBridge.DefaultStringTable}' with " +
                $"{collection.SharedData.Entries.Count} keys, {locales.Length} locales, " +
                $"{legacyCount} legacy rows checked, and {assetCount} asset-backed rows checked.");
        }

        public static void RebuildGameTextTablesBatch()
        {
            RebuildGameTextTables();
        }

        private static void EnsureFolders()
        {
            EnsureFolder(LocalizationRoot);
            EnsureFolder(LocalesFolder);
            EnsureFolder(StringTablesFolder);
            EnsureFolder(AssetTablesFolder);
            EnsureFolder(DocsFolder);
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] parts = folderPath.Split('/');
            string current = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Locale[] EnsureLocales()
        {
            var locales = new List<Locale>(LocaleSpecs.Length);

            for (int i = 0; i < LocaleSpecs.Length; i++)
            {
                LocaleSpec spec = LocaleSpecs[i];
                Locale locale = FindLocaleByCode(spec.Code);
                if (locale == null)
                {
                    locale = Locale.CreateLocale(spec.Code);
                    locale.name = spec.AssetName;
                    locale.LocaleName = spec.DisplayName;
                    locale.SortOrder = (ushort)i;

                    string path = AssetDatabase.GenerateUniqueAssetPath($"{LocalesFolder}/{spec.AssetName}.asset");
                    AssetDatabase.CreateAsset(locale, path);
                }
                else
                {
                    locale.LocaleName = spec.DisplayName;
                    locale.SortOrder = (ushort)i;
                }

                if (!LocalizationEditorSettings.GetLocales().Any(projectLocale => projectLocale.Identifier == locale.Identifier))
                {
                    LocalizationEditorSettings.AddLocale(locale);
                }

                EditorUtility.SetDirty(locale);
                locales.Add(locale);
            }

            return locales.ToArray();
        }

        private static Locale FindLocaleByCode(string code)
        {
            Locale locale = LocalizationEditorSettings.GetLocale(code);
            if (locale != null)
                return locale;

            string[] guids = AssetDatabase.FindAssets("t:Locale", new[] { LocalizationRoot, "Assets" });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                locale = AssetDatabase.LoadAssetAtPath<Locale>(path);
                if (locale != null && locale.Identifier.Code == code)
                    return locale;
            }

            return null;
        }

        private static LocalizationSettings EnsureSettings(Locale englishLocale)
        {
            LocalizationSettings settings = AssetDatabase.LoadAssetAtPath<LocalizationSettings>(SettingsAssetPath);
            if (settings == null)
            {
                settings = ScriptableObject.CreateInstance<LocalizationSettings>();
                settings.name = "Localization Settings";
                AssetDatabase.CreateAsset(settings, SettingsAssetPath);
            }

            LocalizationEditorSettings.ActiveLocalizationSettings = settings;
            LocalizationSettings.ProjectLocale = englishLocale;
            settings.GetStringDatabase().DefaultTable = UnityLocalizationBridge.DefaultStringTable;

            return settings;
        }

        private static StringTableCollection EnsureGameTextCollection(IList<Locale> locales)
        {
            StringTableCollection collection = LocalizationEditorSettings.GetStringTableCollection(UnityLocalizationBridge.DefaultStringTable);
            if (collection == null)
            {
                collection = LocalizationEditorSettings.CreateStringTableCollection(
                    UnityLocalizationBridge.DefaultStringTable,
                    StringTablesFolder,
                    locales);
            }

            foreach (Locale locale in locales)
            {
                if (collection.GetTable(locale.Identifier) == null)
                {
                    collection.AddNewTable(locale.Identifier);
                }
            }

            foreach (StringTable table in collection.StringTables)
            {
                LocalizationEditorSettings.SetPreloadTableFlag(table, true);
            }

            return collection;
        }

        private static void ImportCsv(StringTableCollection collection)
        {
            using var reader = new StreamReader(SourceCsvPath, Encoding.UTF8);
            Csv.ImportInto(reader, collection, CreateCsvColumns(), createUndo: false, reporter: null, removeMissingEntries: false);
        }

        private static void ExportCsv(StringTableCollection collection)
        {
            using var writer = new StreamWriter(SourceCsvPath, append: false, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            Csv.Export(writer, collection, CreateCsvColumns());
        }

        private static List<CsvColumns> CreateCsvColumns()
        {
            var columns = new List<CsvColumns>
            {
                new KeyIdColumns
                {
                    KeyFieldName = "key",
                    IncludeId = false,
                    IncludeSharedComments = true,
                    SharedCommentFieldName = "notes"
                }
            };

            foreach (LocaleSpec spec in LocaleSpecs)
            {
                columns.Add(new LocaleColumns
                {
                    LocaleIdentifier = spec.Code,
                    FieldName = spec.Code,
                    IncludeComments = false
                });
            }

            return columns;
        }

        private static int AddLegacyCompatibilityEntries(StringTableCollection collection)
        {
            int count = 0;
            foreach (string key in GameLocalization.LegacyKeys)
            {
                count++;
                foreach (LocaleSpec spec in LocaleSpecs)
                {
                    if (!GameLocalization.TryGetLanguageFromCode(spec.Code, out GameLanguage language))
                        continue;

                    if (GameLocalization.TryGetLegacyText(key, language, out string text))
                    {
                        AddOrFill(collection, spec.Code, key, text);
                    }
                }

                AddSharedCommentIfEmpty(
                    collection,
                    key,
                    "Legacy GameLocalization compatibility row. Keep until all direct GameLocalization.Get calls are migrated.");
            }

            return count;
        }

        private static int AddAssetBackedEntries(StringTableCollection collection)
        {
            int count = 0;

            count += AddObjectFieldEntries<PackDefinition>(collection, "pack", new[]
            {
                FieldMapping.Name("displayName"),
                FieldMapping.Description("description")
            });

            count += AddObjectFieldEntries<CardDefinition>(collection, "card", new[]
            {
                FieldMapping.Name("displayName"),
                FieldMapping.Description("description")
            }, asset => asset is not PackDefinition);

            count += AddObjectFieldEntries<RecipeDefinition>(collection, "recipe", new[]
            {
                FieldMapping.Name("displayName")
            });

            count += AddObjectFieldEntries<Quest>(collection, "quest", new[]
            {
                new FieldMapping("title", "title"),
                FieldMapping.Description("description")
            });

            count += AddObjectFieldEntries<EncounterDefinition>(collection, "encounter", new[]
            {
                new FieldMapping("notificationMessage", "notification")
            });

            count += AddObjectFieldEntries<EnemyDefinition>(collection, "night.enemy", new[]
            {
                FieldMapping.Name("displayName")
            });

            count += AddObjectFieldEntries<NightWaveDefinition>(collection, "night.wave", new[]
            {
                new FieldMapping("waveName", "name"),
                new FieldMapping("flavorText", "flavor")
            });

            return count;
        }

        private static int AddObjectFieldEntries<T>(
            StringTableCollection collection,
            string category,
            IReadOnlyList<FieldMapping> fields,
            System.Func<T, bool> predicate = null)
            where T : ScriptableObject
        {
            int count = 0;
            string[] guids = AssetDatabase.FindAssets($"t:{typeof(T).Name}", new[] { ResourcesRoot });
            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
                if (asset == null || predicate?.Invoke(asset) == false)
                    continue;

                var serializedObject = new SerializedObject(asset);
                foreach (FieldMapping field in fields)
                {
                    SerializedProperty property = serializedObject.FindProperty(field.SerializedFieldName);
                    if (property == null || property.propertyType != SerializedPropertyType.String)
                        continue;

                    string englishValue = property.stringValue;
                    if (string.IsNullOrWhiteSpace(englishValue))
                        continue;

                    string key = LocalizationKeyBuilder.ForAsset(asset, category, field.KeySuffix);
                    foreach (LocaleSpec spec in LocaleSpecs)
                    {
                        AddOrFill(collection, spec.Code, key, englishValue);
                    }

                    AddSharedCommentIfEmpty(
                        collection,
                        key,
                        $"Source English from {assetPath}. Non-English values are placeholders until reviewed.");
                    count++;
                }
            }

            return count;
        }

        private static void AddOrFill(StringTableCollection collection, string localeCode, string key, string value)
        {
            if (string.IsNullOrWhiteSpace(key) || string.IsNullOrWhiteSpace(value))
                return;

            var localeIdentifier = new LocaleIdentifier(localeCode);
            var table = collection.GetTable(localeIdentifier) as StringTable;
            if (table == null)
                return;

            StringTableEntry entry = table.GetEntry(key);
            if (entry == null)
            {
                table.AddEntry(key, value);
            }
            else if (string.IsNullOrEmpty(entry.Value))
            {
                entry.Value = value;
            }
        }

        private static void AddSharedCommentIfEmpty(StringTableCollection collection, string key, string commentText)
        {
            SharedTableData.SharedTableEntry sharedEntry = collection.SharedData.GetEntry(key);
            if (sharedEntry == null)
                return;

            Comment comment = sharedEntry.Metadata.GetMetadata<Comment>();
            if (comment == null)
            {
                sharedEntry.Metadata.AddMetadata(new Comment { CommentText = commentText });
                return;
            }

            if (string.IsNullOrWhiteSpace(comment.CommentText))
            {
                comment.CommentText = commentText;
            }
        }

        private static void ValidateNoDuplicateKeys(StringTableCollection collection)
        {
            var duplicateKeys = collection.SharedData.Entries
                .Where(entry => !string.IsNullOrWhiteSpace(entry.Key))
                .GroupBy(entry => entry.Key)
                .Where(group => group.Count() > 1)
                .Select(group => group.Key)
                .ToArray();

            if (duplicateKeys.Length == 0)
                return;

            Debug.LogError("LocalizationAssetBuilder: Duplicate GameText keys found:\n" + string.Join("\n", duplicateKeys));
        }

        private readonly struct LocaleSpec
        {
            public LocaleSpec(string displayName, string code)
            {
                DisplayName = displayName;
                Code = code;
            }

            public string DisplayName { get; }
            public string Code { get; }
            public string AssetName => DisplayName;
        }

        private readonly struct FieldMapping
        {
            public FieldMapping(string serializedFieldName, string keySuffix)
            {
                SerializedFieldName = serializedFieldName;
                KeySuffix = keySuffix;
            }

            public string SerializedFieldName { get; }
            public string KeySuffix { get; }

            public static FieldMapping Name(string serializedFieldName)
            {
                return new FieldMapping(serializedFieldName, "name");
            }

            public static FieldMapping Description(string serializedFieldName)
            {
                return new FieldMapping(serializedFieldName, "description");
            }
        }
    }
}
