using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel
{
    // Watches Assets/_Project/Audio/Music/{Context}/ folders.
    // Runs automatically on import. Also callable via menu.
    public class MusicPlaylistSync : AssetPostprocessor
    {
        private const string MusicRoot   = "Assets/_Project/Audio/Music";
        private const string PlaylistPath = "Assets/_Project/Audio/MusicPlaylist.asset";

        private static readonly HashSet<string> AudioExtensions =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            { ".wav", ".mp3", ".ogg", ".aiff", ".aif", ".flac" };

        static void OnPostprocessAllAssets(
            string[] imported, string[] deleted, string[] moved, string[] movedFrom)
        {
            bool relevant = imported.Concat(deleted).Concat(moved)
                .Any(p => p.StartsWith(MusicRoot) && AudioExtensions.Contains(Path.GetExtension(p)));

            if (relevant)
                SyncPlaylist();
        }

        [MenuItem("Tools/LAST KERNEL/Sync Music Playlist")]
        public static void SyncPlaylist()
        {
            var playlist = AssetDatabase.LoadAssetAtPath<MusicPlaylist>(PlaylistPath);
            if (playlist == null)
            {
                playlist = ScriptableObject.CreateInstance<MusicPlaylist>();
                AssetDatabase.CreateAsset(playlist, PlaylistPath);
                Debug.Log("[MusicPlaylistSync] Created MusicPlaylist.asset");
            }

            var newTracks = new List<MusicPlaylist.ContextTracks>();

            foreach (MusicContext ctx in System.Enum.GetValues(typeof(MusicContext)))
            {
                if (ctx == MusicContext.None) continue;

                string folderRelative = $"{MusicRoot}/{ctx}";
                string folderAbsolute = Path.GetFullPath(folderRelative);
                if (!Directory.Exists(folderAbsolute))
                {
                    Directory.CreateDirectory(folderAbsolute);
                    AssetDatabase.ImportAsset(folderRelative);
                }

                var guids = AssetDatabase.FindAssets("t:AudioClip", new[] { folderRelative });
                var clips = guids
                    .Select(g => AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(c => c != null)
                    .ToArray();

                newTracks.Add(new MusicPlaylist.ContextTracks { context = ctx, clips = clips });
            }

            playlist.tracks = newTracks;
            playlist.InvalidateLookup();
            EditorUtility.SetDirty(playlist);
            AssetDatabase.SaveAssets();

            int total = newTracks.Sum(t => t.clips.Length);
            Debug.Log($"[MusicPlaylistSync] Synced {total} track(s) across {newTracks.Count} contexts.");
        }
    }
}
