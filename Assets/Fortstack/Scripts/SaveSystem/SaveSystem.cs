using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

namespace Markyu.FortStack
{
    public static class SaveSystem
    {
        /// <summary>
        /// Saves data to a file as JSON.
        /// </summary>
        public static void SaveData<T>(T data, string fileName)
        {
            string filePath = GetSaveFilePath(Application.persistentDataPath, fileName);

            try
            {
                Directory.CreateDirectory(Application.persistentDataPath);

                // Keep saves readable during active development; release builds can switch this to Formatting.None.
                string json = JsonConvert.SerializeObject(data, Formatting.Indented);
                File.WriteAllText(filePath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"SaveSystem: Failed to save '{fileName}' to '{filePath}'. {ex.Message}");
            }
        }

        /// <summary>
        /// Loads data from a JSON file.
        /// </summary>
        public static T LoadData<T>(string fileName)
        {
            foreach (string directoryPath in GetCandidateSaveDirectories())
            {
                string filePath = GetSaveFilePath(directoryPath, fileName);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    string json = File.ReadAllText(filePath);
                    return JsonConvert.DeserializeObject<T>(json);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"SaveSystem: Failed to load save file '{filePath}'. {ex.Message}");
                    return default;
                }
            }

            return default;
        }

        /// <summary>
        /// Loads all JSON files in the directory and returns a Dictionary of valid data.
        /// Key = File Name (without extension), Value = The Data Object.
        /// </summary>
        public static Dictionary<string, T> LoadAllValidData<T>()
        {
            Dictionary<string, T> validDataDict = new Dictionary<string, T>();
            foreach (string directoryPath in GetCandidateSaveDirectories())
            {
                if (!Directory.Exists(directoryPath))
                {
                    continue;
                }

                foreach (string filePath in Directory.GetFiles(directoryPath, "*.json"))
                {
                    try
                    {
                        string json = File.ReadAllText(filePath);
                        T data = JsonConvert.DeserializeObject<T>(json);
                        string fileName = Path.GetFileNameWithoutExtension(filePath);

                        if (data != null && !validDataDict.ContainsKey(fileName))
                        {
                            validDataDict.Add(fileName, data);
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogWarning($"SaveSystem: Skipped invalid save file at '{filePath}'. {ex.Message}");
                    }
                }
            }

            return validDataDict;
        }

        /// <summary>
        /// Helper to delete a save file.
        /// </summary>
        public static void DeleteSave(string fileName)
        {
            foreach (string directoryPath in GetCandidateSaveDirectories())
            {
                string filePath = GetSaveFilePath(directoryPath, fileName);
                if (!File.Exists(filePath))
                {
                    continue;
                }

                try
                {
                    File.Delete(filePath);
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"SaveSystem: Failed to delete save file '{filePath}'. {ex.Message}");
                }
            }
        }

        private static string GetSaveFilePath(string directoryPath, string fileName)
        {
            return Path.Combine(directoryPath, fileName + ".json");
        }

        private static IEnumerable<string> GetCandidateSaveDirectories()
        {
            string currentPath = Application.persistentDataPath;
            if (!string.IsNullOrWhiteSpace(currentPath))
            {
                yield return currentPath;
            }

            string legacyPath = TryGetLegacyPersistentDataPath(currentPath);
            if (!string.IsNullOrWhiteSpace(legacyPath) && legacyPath != currentPath)
            {
                yield return legacyPath;
            }
        }

        private static string TryGetLegacyPersistentDataPath(string currentPath)
        {
            if (string.IsNullOrWhiteSpace(currentPath))
            {
                return null;
            }

            string normalizedCurrent = TrimTrailingDirectorySeparators(currentPath);
            string currentProductFolder = Path.GetFileName(normalizedCurrent);

            if (string.IsNullOrEmpty(currentProductFolder) ||
                currentProductFolder == GameIdentity.LegacyDisplayName ||
                !currentProductFolder.Contains(GameIdentity.ProductNameNoSpaces) &&
                !currentProductFolder.Contains(GameIdentity.DisplayName))
            {
                return null;
            }

            string parent = Directory.GetParent(normalizedCurrent)?.FullName;
            return string.IsNullOrEmpty(parent)
                ? null
                : Path.Combine(parent, GameIdentity.LegacyDisplayName);
        }

        private static string TrimTrailingDirectorySeparators(string path)
        {
            return path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }
    }
}

