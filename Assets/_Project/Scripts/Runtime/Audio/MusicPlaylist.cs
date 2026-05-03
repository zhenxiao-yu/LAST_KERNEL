using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Markyu.LastKernel
{
    [CreateAssetMenu(menuName = "LastKernel/Audio/Music Playlist", fileName = "MusicPlaylist")]
    public class MusicPlaylist : ScriptableObject
    {
        [Serializable]
        public class ContextTracks
        {
            [TableColumnWidth(90, Resizable = false)]
            public MusicContext context;

            [Tooltip("Short stinger played before the loop starts when entering this context. " +
                     "Auto-filled by Sync if a file named *_transition.* exists in the context folder. " +
                     "2–5 s, non-looping.")]
            public AudioClip transitionIn;

            [Tooltip("All looping tracks for this context. One is picked at random each transition. " +
                     "Auto-filled by Sync from Audio/Music/{Context}/.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
        }

        [TableList(AlwaysExpanded = true)]
        public List<ContextTracks> tracks = new();

        private Dictionary<MusicContext, ContextTracks> _lookup;

        public AudioClip PickTrack(MusicContext context)
        {
            var entry = GetEntry(context);
            if (entry == null || entry.clips == null || entry.clips.Length == 0) return null;
            return entry.clips[UnityEngine.Random.Range(0, entry.clips.Length)];
        }

        public AudioClip PickTransition(MusicContext context) => GetEntry(context)?.transitionIn;

        public void InvalidateLookup() => _lookup = null;

        private ContextTracks GetEntry(MusicContext context)
        {
            BuildLookup();
            return _lookup.TryGetValue(context, out var entry) ? entry : null;
        }

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<MusicContext, ContextTracks>();
            foreach (var entry in tracks)
                if (entry != null)
                    _lookup[entry.context] = entry;
        }
    }
}
