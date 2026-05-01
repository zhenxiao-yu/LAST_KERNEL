using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class CardArtImportFixer
    {
        [MenuItem("Tools/LAST KERNEL/Fix Card Art Import Settings")]
        public static void Run()
        {
            var log = new StringBuilder();
            log.AppendLine("=== Card Art Import Fix — " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ===\n");

            var targets = new[]
            {
                ("Assets/_Project/Art/Sprites/CardArt",    TextureImporterType.Default, "CardArt (Portraits)"),
                ("Assets/_Project/Art/Sprites/CardFrames", TextureImporterType.Sprite,  "CardFrames"),
            };

            int totalFixed = 0, totalSkipped = 0;

            try
            {
                AssetDatabase.StartAssetEditing();

                foreach (var (folder, targetType, label) in targets)
                {
                    log.AppendLine($"--- {label} ---");
                    string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { folder });
                    int folderFixed = 0;

                    foreach (string guid in guids)
                    {
                        string path = AssetDatabase.GUIDToAssetPath(guid);
                        var imp = AssetImporter.GetAtPath(path) as TextureImporter;
                        if (imp == null) { log.AppendLine($"  SKIP  {Path.GetFileName(path)}"); continue; }

                        bool changed = false;
                        var diff = new List<string>();

                        if (imp.textureType != targetType)
                        { imp.textureType = targetType; diff.Add($"type={targetType}"); changed = true; }

                        if (imp.filterMode != FilterMode.Point)
                        { imp.filterMode = FilterMode.Point; diff.Add("filter=Point"); changed = true; }

                        if (imp.mipmapEnabled)
                        { imp.mipmapEnabled = false; diff.Add("mips=Off"); changed = true; }

                        if (imp.textureCompression != TextureImporterCompression.Uncompressed)
                        { imp.textureCompression = TextureImporterCompression.Uncompressed; diff.Add("compression=None"); changed = true; }

                        if (!imp.alphaIsTransparency)
                        { imp.alphaIsTransparency = true; diff.Add("alpha=true"); changed = true; }

                        if (changed)
                        {
                            imp.SaveAndReimport();
                            log.AppendLine($"  FIXED  {Path.GetFileName(path)}: {string.Join(", ", diff)}");
                            folderFixed++;
                            totalFixed++;
                        }
                        else
                        {
                            log.AppendLine($"  OK     {Path.GetFileName(path)}");
                            totalSkipped++;
                        }
                    }

                    log.AppendLine($"  => {folderFixed} fixed\n");
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }

            log.AppendLine($"=== DONE: {totalFixed} fixed, {totalSkipped} already correct ===");

            string logPath = Path.Combine(Application.dataPath, "_Project/Docs/ArtImportFixLog.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(logPath));
            File.WriteAllText(logPath, log.ToString());

            AssetDatabase.Refresh();

            Debug.Log("[CardArtImportFixer] " + totalFixed + " textures fixed, " + totalSkipped + " already correct. Log: " + logPath);
            Debug.Log(log.ToString());
        }
    }
}
