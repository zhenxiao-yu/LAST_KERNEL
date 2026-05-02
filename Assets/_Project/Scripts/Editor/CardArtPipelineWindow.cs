using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    public class CardArtPipelineWindow : EditorWindow
    {
        [MenuItem("Tools/LAST KERNEL/Card Art Pipeline", priority = 50)]
        public static void Open() => GetWindow<CardArtPipelineWindow>("Card Art Pipeline");

        // ── Prompt style constants ────────────────────────────────────────────

        private const string BASE_PROMPT =
            "pixel art icon, 64x64, dark cyberpunk terminal aesthetic, " +
            "dark navy background #0D0D1A, cyan outline highlights #00FFEE, " +
            "muted magenta secondary #CC44AA, off-white detail #D0D0C8, " +
            "no gradients, dithered shading only, hard pixel edges, " +
            "isometric-ish front-face angle, readable silhouette at small size, " +
            "functional not ornamental, worn-in underground bunker feel, ";

        private static readonly Dictionary<CardCategory, string> s_CategoryStyle = new()
        {
            { CardCategory.None,       "" },
            { CardCategory.Resource,   "muted gray-cyan border, material fragment theme" },
            { CardCategory.Character,  "warm amber-white border, humanoid silhouette" },
            { CardCategory.Consumable, "amber-warm border, flask or ration theme" },
            { CardCategory.Material,   "muted steel border, raw material fragment, worn industrial" },
            { CardCategory.Equipment,  "muted gold-cyan border, worn item finish" },
            { CardCategory.Structure,  "steel gray border, architectural panel, angular geometry" },
            { CardCategory.Currency,   "cyan-gold border, scrap chip or trade token" },
            { CardCategory.Recipe,     "gear plus arrow motif, output highlight, process diagram feel" },
            { CardCategory.Mob,        "red-orange #FF4422 border, threat warning marks, aggressive silhouette" },
            { CardCategory.Area,       "wide environment framing, desolate zone feel, deserted ruins" },
            { CardCategory.Valuable,   "magenta glow border, encrypted artifact feel, rare relic" },
        };

        // ── Entry ─────────────────────────────────────────────────────────────

        private class CardEntry
        {
            public CardDefinition Asset;
            public string         RawName;
            public string         RawDesc;
            public bool           HasArt;
            public string         ArtFile;
            public string         Prompt;
        }

        // ── State ─────────────────────────────────────────────────────────────

        private readonly List<CardEntry> _entries = new();
        private Vector2 _scroll;
        private bool    _missingOnly;
        private string  _status = "";

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void OnEnable() => Refresh();

        private void Refresh()
        {
            _entries.Clear();

            string[] guids = AssetDatabase.FindAssets("t:CardDefinition",
                new[] { "Assets/_Project" });

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var card = AssetDatabase.LoadAssetAtPath<CardDefinition>(path);
                if (card == null || card is PackDefinition) continue;

                var so      = new SerializedObject(card);
                string name = so.FindProperty("displayName").stringValue;
                string desc = so.FindProperty("description").stringValue;
                if (string.IsNullOrWhiteSpace(name)) name = card.name;

                var artProp = so.FindProperty("artTexture");
                bool hasArt = artProp.objectReferenceValue != null;
                string artFile = hasArt
                    ? Path.GetFileName(AssetDatabase.GetAssetPath(artProp.objectReferenceValue))
                    : "";

                _entries.Add(new CardEntry
                {
                    Asset   = card,
                    RawName = name,
                    RawDesc = desc,
                    HasArt  = hasArt,
                    ArtFile = artFile,
                    Prompt  = BuildPrompt(name, desc, card.Category),
                });
            }

            _entries.Sort((a, b) =>
                string.CompareOrdinal(a.Asset.Category.ToString(), b.Asset.Category.ToString()) != 0
                    ? string.CompareOrdinal(a.Asset.Category.ToString(), b.Asset.Category.ToString())
                    : string.CompareOrdinal(a.RawName, b.RawName));

            int missing = 0;
            foreach (var e in _entries) if (!e.HasArt) missing++;
            _status = $"Loaded {_entries.Count} cards — {missing} missing art";
        }

        // ── Prompt builder ────────────────────────────────────────────────────

        private static string BuildPrompt(string name, string desc, CardCategory category)
        {
            var sb = new StringBuilder(BASE_PROMPT);

            if (s_CategoryStyle.TryGetValue(category, out string catStyle) &&
                !string.IsNullOrEmpty(catStyle))
                sb.Append(catStyle).Append(", ");

            sb.Append("subject: ").Append(name);

            if (!string.IsNullOrWhiteSpace(desc))
                sb.Append(" — ").Append(desc.Trim());

            sb.Append(" --ar 1:1 --style raw --no blur glow");
            return sb.ToString();
        }

        // ── GUI ───────────────────────────────────────────────────────────────

        private void OnGUI()
        {
            EditorGUILayout.Space(6);
            GUILayout.Label("LAST KERNEL — Card Art Pipeline", EditorStyles.boldLabel);
            EditorGUILayout.Space(4);

            // Status row
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(_status, EditorStyles.miniLabel);
            if (GUILayout.Button("Refresh", GUILayout.Width(68))) Refresh();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            // Action buttons
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export JSON Manifest")) ExportManifest();
            if (GUILayout.Button("Fix Import Settings"))  CardArtImportFixer.Run();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            // Filter
            _missingOnly = EditorGUILayout.ToggleLeft("Show missing art only", _missingOnly);
            EditorGUILayout.Space(2);

            // Column headers
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            GUILayout.Label("Name",     GUILayout.Width(160));
            GUILayout.Label("Category", GUILayout.Width(90));
            GUILayout.Label("Art",      GUILayout.Width(28));
            GUILayout.Label("Prompt preview", GUILayout.ExpandWidth(true));
            GUILayout.Label("",         GUILayout.Width(48));
            EditorGUILayout.EndHorizontal();

            // Card rows
            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            foreach (var entry in _entries)
            {
                if (_missingOnly && entry.HasArt) continue;

                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button(entry.RawName, EditorStyles.label, GUILayout.Width(160)))
                    EditorGUIUtility.PingObject(entry.Asset);

                GUILayout.Label(entry.Asset.Category.ToString(), GUILayout.Width(90));

                Color prev = GUI.color;
                GUI.color = entry.HasArt ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.4f, 0.3f);
                GUILayout.Label(entry.HasArt ? "✓" : "✗", GUILayout.Width(28));
                GUI.color = prev;

                string preview = entry.Prompt.Length > 90
                    ? entry.Prompt[..87] + "…"
                    : entry.Prompt;
                GUILayout.Label(preview, EditorStyles.miniLabel, GUILayout.ExpandWidth(true));

                if (GUILayout.Button("Copy", EditorStyles.miniButton, GUILayout.Width(48)))
                {
                    GUIUtility.systemCopyBuffer = entry.Prompt;
                    _status = $"Copied prompt for: {entry.RawName}";
                    Repaint();
                }

                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ── Export ────────────────────────────────────────────────────────────

        private void ExportManifest()
        {
            string outDir  = Path.Combine(Application.dataPath, "_Project/Docs");
            Directory.CreateDirectory(outDir);
            string outPath = Path.Combine(outDir, "CardArtManifest.json");

            var sb = new StringBuilder();
            sb.AppendLine("[");
            for (int i = 0; i < _entries.Count; i++)
            {
                var  e    = _entries[i];
                bool last = i == _entries.Count - 1;

                sb.AppendLine("  {");
                sb.AppendLine($"    \"name\":        {Js(e.RawName)},");
                sb.AppendLine($"    \"category\":    {Js(e.Asset.Category.ToString())},");
                sb.AppendLine($"    \"faction\":     {Js(e.Asset.Faction.ToString())},");
                sb.AppendLine($"    \"description\": {Js(e.RawDesc)},");
                sb.AppendLine($"    \"hasArt\":      {(e.HasArt ? "true" : "false")},");
                sb.AppendLine($"    \"artFile\":     {Js(e.ArtFile)},");
                sb.AppendLine($"    \"aiPrompt\":    {Js(e.Prompt)}");
                sb.AppendLine(last ? "  }" : "  },");
            }
            sb.AppendLine("]");

            File.WriteAllText(outPath, sb.ToString(), Encoding.UTF8);
            AssetDatabase.Refresh();

            _status = $"Exported {_entries.Count} cards → Docs/CardArtManifest.json";
            Debug.Log("[CardArtPipeline] " + _status);
        }

        private static string Js(string value)
        {
            if (value == null) return "null";
            return "\"" + value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"")
                .Replace("\n", "\\n")
                .Replace("\r", "")
                .Replace("\t", " ")
                + "\"";
        }
    }
}
