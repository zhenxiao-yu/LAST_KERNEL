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
            [TableColumnWidth(100, Resizable = false)]
            public MusicContext context;
            [Tooltip("All tracks for this context. One is picked at random on each transition.")]
            public AudioClip[] clips = Array.Empty<AudioClip>();
        }

        [TableList(AlwaysExpanded = true)]
        public List<ContextTracks> tracks = new();

        private Dictionary<MusicContext, AudioClip[]> _lookup;

        public AudioClip PickTrack(MusicContext context)
        {
            BuildLookup();
            if (!_lookup.TryGetValue(context, out var clips) || clips == null || clips.Length == 0)
                return null;
            return clips[UnityEngine.Random.Range(0, clips.Length)];
        }

        public void InvalidateLookup() => _lookup = null;

        private void BuildLookup()
        {
            if (_lookup != null) return;
            _lookup = new Dictionary<MusicContext, AudioClip[]>();
            foreach (var entry in tracks)
                if (entry?.clips != null)
                    _lookup[entry.context] = entry.clips;
        }
    }
}
