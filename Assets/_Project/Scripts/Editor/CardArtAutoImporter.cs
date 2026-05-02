using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Drop PNGs into CardArt/ or PackArt/ — this fires automatically to:
    ///   1. Apply correct import settings (Point filter, no compression, no mips).
    ///   2. Wire the texture to the matching CardDefinition / PackDefinition.
    ///
    /// Matching priority per dropped file (e.g. "Warrior.png"):
    ///   1. CardDefinition whose artTexture already points to a file with that name (reimport).
    ///   2. CardDefinition whose asset name is "Warrior" or "Card_Warrior" / "Pack_Warrior".
    ///   3. CardDefinition whose raw serialized displayName equals "Warrior".
    /// </summary>
    public class CardArtAutoImporter : AssetPostprocessor
    {
        private const string CardArtPath = "Assets/_Project/Art/Sprites/CardArt";
        private const string PackArtPath = "Assets/_Project/Art/Sprites/PackArt";

        // ── Import settings ───────────────────────────────────────────────────
        // Runs before Unity reimports the texture — sets Point filter, no compression.

        private void OnPreprocessTexture()
        {
            bool isCardArt = assetPath.StartsWith(CardArtPath);
            bool isPackArt = assetPath.StartsWith(PackArtPath);
            if (!isCardArt && !isPackArt) return;

            var imp = (TextureImporter)assetImporter;
            imp.textureType         = isCardArt ? TextureImporterType.Default : TextureImporterType.Sprite;
            imp.filterMode          = FilterMode.Point;
            imp.mipmapEnabled       = false;
            imp.textureCompression  = TextureImporterCompression.Uncompressed;
            imp.alphaIsTransparency = true;
            imp.maxTextureSize      = 1024;
        }

        // ── Auto-wire ─────────────────────────────────────────────────────────
        // Runs once after the full import batch — finds CardDefinitions to update.

        private static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var cardPool = new Dictionary<string, Texture2D>(System.StringComparer.OrdinalIgnoreCase);
            var packPool = new Dictionary<string, Texture2D>(System.StringComparer.OrdinalIgnoreCase);

            foreach (string path in importedAssets)
            {
                if (!path.EndsWith(".png")) continue;

                if (path.StartsWith(CardArtPath))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null) cardPool[Path.GetFileNameWithoutExtension(path)] = tex;
                }
                else if (path.StartsWith(PackArtPath))
                {
                    var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (tex != null) packPool[Path.GetFileNameWithoutExtension(path)] = tex;
                }
            }

            if (cardPool.Count == 0 && packPool.Count == 0) return;

            WireDefinitions(cardPool, packPool);
        }

        // ── Wiring ────────────────────────────────────────────────────────────

        private static void WireDefinitions(
            Dictionary<string, Texture2D> cardPool,
            Dictionary<string, Texture2D> packPool)
        {
            string[] guids = AssetDatabase.FindAssets("t:CardDefinition", new[] { "Assets/_Project" });
            int wired = 0;

            foreach (string guid in guids)
            {
                string defPath = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(defPath);
                if (card == null) continue;

                var pool = card is PackDefinition ? packPool : cardPool;
                if (pool.Count == 0) continue;

                var so      = new SerializedObject(card);
                var artProp = so.FindProperty("artTexture");

                Texture2D match = FindMatch(card, so, pool);
                if (match == null || artProp.objectReferenceValue == match) continue;

                artProp.objectReferenceValue = match;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(card);
                wired++;
                Debug.Log($"[CardArt] {match.name}.png  →  {card.name}");
            }

            if (wired > 0)
            {
                AssetDatabase.SaveAssets();
                Debug.Log($"[CardArt] Auto-wired {wired} definition(s). Open Card Art Pipeline to verify.");
            }
        }

        private static Texture2D FindMatch(
            CardDefinition card,
            SerializedObject so,
            Dictionary<string, Texture2D> pool)
        {
            // 1. Existing artTexture name — handles reimport of already-wired cards
            if (so.FindProperty("artTexture").objectReferenceValue is Texture2D existing &&
                pool.TryGetValue(existing.name, out var byExisting))
                return byExisting;

            // 2. Asset filename, stripping "Card_" or "Pack_" prefix
            string assetName = card.name;
            foreach (string prefix in new[] { "Card_", "Pack_" })
            {
                if (assetName.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
                {
                    assetName = assetName[prefix.Length..];
                    break;
                }
            }
            if (pool.TryGetValue(assetName, out var byAssetName))
                return byAssetName;

            // 3. Raw serialized displayName (fallback for atypically named assets)
            string rawDisplayName = so.FindProperty("displayName").stringValue;
            if (!string.IsNullOrWhiteSpace(rawDisplayName) &&
                pool.TryGetValue(rawDisplayName, out var byDisplayName))
                return byDisplayName;

            return null;
        }
    }
}
