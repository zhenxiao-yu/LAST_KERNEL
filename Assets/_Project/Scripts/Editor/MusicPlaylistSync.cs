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
    //
    // Naming convention inside each context folder:
    //   *_transition.*  →  assigned as transitionIn (stinger, played before the loop)
    //   everything else →  added to the looping clips[] array
    public class MusicPlaylistSync : AssetPostprocessor
    {
        private const string MusicRoot    = "Assets/_Project/Audio/Music";
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

            var newTracks  = new List<MusicPlaylist.ContextTracks>();
            int loopTotal  = 0;
            int stingTotal = 0;

            foreach (MusicContext ctx in Enum.GetValues(typeof(MusicContext)))
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
                var allClips = guids
                    .Select(g => (path: AssetDatabase.GUIDToAssetPath(g),
                                  clip: AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(g))))
                    .Where(x => x.clip != null)
                    .ToList();

                // Files whose name contains "_transition" are stingers; everything else loops.
                var stingerEntry = allClips
                    .FirstOrDefault(x => Path.GetFileNameWithoutExtension(x.path)
                                         .IndexOf("_transition", StringComparison.OrdinalIgnoreCase) >= 0);
                var loopClips = allClips
                    .Where(x => x != stingerEntry)
                    .Select(x => x.clip)
                    .ToArray();

                newTracks.Add(new MusicPlaylist.ContextTracks
                {
                    context      = ctx,
                    transitionIn = stingerEntry.clip,
                    clips        = loopClips,
                });

                loopTotal  += loopClips.Length;
                if (stingerEntry.clip != null) stingTotal++;
            }

            playlist.tracks = newTracks;
            playlist.InvalidateLookup();
            EditorUtility.SetDirty(playlist);
            AssetDatabase.SaveAssets();

            Debug.Log($"[MusicPlaylistSync] Synced {loopTotal} loop track(s) + {stingTotal} stinger(s) across {newTracks.Count} contexts.");
        }
    }
}
