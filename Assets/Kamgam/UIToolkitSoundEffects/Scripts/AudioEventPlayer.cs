using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Kamgam.UIToolkitSoundEffects
{
    public class AudioEventPlayer : MonoBehaviour
    {
        public class PlayingSound
        {
            public AudioSource Source;
            public AudioEvent Event;
            public Coroutine Coroutine;
        }
        
        private static AudioEventPlayer s_instance;
        
        public AudioSource AudioSourceForOneShot;
        
        [System.NonSerialized]
        public int MAX_NUMBER_OF_CONCURRENT_LOOPS = 10;
        
        [System.NonSerialized]
        public Stack<PlayingSound> SoundPoolForLoops = new Stack<PlayingSound>();
        
        [System.NonSerialized]
        public List<PlayingSound> PlayingLoops = new List<PlayingSound>();

        public AudioListener Listener;
 
        public static AudioEventPlayer Instance
        {
            get
            {
                if (s_instance == null)
                {
                    var go = new GameObject("UI Toolkit AudioEvent Player");
                    GameObject.DontDestroyOnLoad(go);
                    go.hideFlags = HideFlags.NotEditable | HideFlags.DontSave;
                    
                    s_instance = go.AddComponent<AudioEventPlayer>();
                    s_instance.AudioSourceForOneShot = go.AddComponent<AudioSource>();
                    s_instance.AudioSourceForOneShot.playOnAwake = false;
                    s_instance.AudioSourceForOneShot.loop = false;
                }

                return s_instance;
            }
        }
        
        public void Play(AudioEvent evt)
        {
            PlayAt(evt, getPlaybackPosition()); 
        }
       
        public void PlayAt(AudioEvent evt, Vector3 position)
        {
            if (evt.Clips.Count == 0)
                return;

            transform.position = position;

            if (evt.ClipOrder == AudioEvent.ClipOrderType.Random)
                playRandom(evt);
            else if(evt.ClipOrder == AudioEvent.ClipOrderType.All)
                playAll(evt);
            else // if (evt.ClipOrder == AudioEvent.ClipOrderType.Loop)
            {
                evt.LastPlayedClipIndex = (evt.LastPlayedClipIndex + 1) % evt.Clips.Count;
                playClip(evt, evt.Clips[evt.LastPlayedClipIndex]);
            }
        }

        public void StopAll()
        {
            for (int i = PlayingLoops.Count-1; i >= 0 ; i--)
            {
                stopSound(PlayingLoops[i]);
                returnPlayingLoopToPool(PlayingLoops[i]);
            }
        }
        
        public void Stop(AudioEvent evt)
        {
            for (int i = PlayingLoops.Count-1; i >= 0; i--)
            {
                var snd = PlayingLoops[i];
                if (snd.Event == evt)
                {
                    stopSound(snd);
                    returnPlayingLoopToPool(snd);
                }    
            }
        }

        protected void stopSound(PlayingSound snd)
        {
            if (snd != null)
            {
                snd.Source.Stop();
                snd.Source.clip = null;
                if (snd.Coroutine != null)
                {
                    StopCoroutine(snd.Coroutine);
                    snd.Coroutine = null;
                }
            }
        }

        void playRandom(AudioEvent evt)
        {
            int index = Random.Range(0, evt.Clips.Count);
            var clip = evt.Clips[index];
            
            playClip(evt, clip);
        }
        
        void playAll(AudioEvent evt)
        {
            foreach (var clip in evt.Clips)
            {
                playClip(evt, clip);   
            }
        }

        void playClip(AudioEvent evt, AudioClip clip)
        {
            if (clip == null)
                return;

            if (isLooping(evt, clip))
            {
                // Check for loop overflow. If yes, then abort the oldest and use that.
                if (PlayingLoops.Count() >= MAX_NUMBER_OF_CONCURRENT_LOOPS)
                {
                    returnPlayingLoopToPool(PlayingLoops[0]);
                }
                var snd = getOrCreateSoundForLoop(evt);
                var coroutine = StartCoroutine(playLoop(snd, evt, clip));
                snd.Coroutine = coroutine;
            }
            else
            {
                playClipOnSource(evt, clip, AudioSourceForOneShot);
            }
        }

        IEnumerator playLoop(PlayingSound snd, AudioEvent evt, AudioClip clip)
        {
            // Start playing
            playClipOnSource(evt, clip, snd.Source);
            
            // Wait for delay to run out and then stop looping but keep playing
            yield return new WaitForSeconds(evt.AudioSourceSettings.LoopDuration);
            
            // stop looping but play last loop.
            snd.Source.loop = false;

            // Calculate the remaining time for the last loop to finish and then return the audio source.
            float totalDuration = Mathf.Ceil(evt.AudioSourceSettings.LoopDuration / clip.length) * clip.length;
            float timeLeftToEnd = totalDuration - evt.AudioSourceSettings.LoopDuration;
            if (timeLeftToEnd >= 0.01f)
            {
                yield return new WaitForSeconds(timeLeftToEnd + 0.01f);
                returnPlayingLoopToPool(snd);
            }
            else
            {
                returnPlayingLoopToPool(snd);
            }
        }
        
        void playClipOnSource(AudioEvent evt, AudioClip clip, AudioSource source)
        {
            if (clip == null)
                return;

            evt.AudioSourceSettings.ApplyTo(source);
            source.clip = null;

            if (isLooping(evt, clip))
            {
                source.clip = clip;
                source.Play();
            }
            else
            {
                source.PlayOneShot(clip);
            }
        }

        protected bool isLooping(AudioEvent evt, AudioClip clip)
        {
            return evt.AudioSourceSettings.Loop && evt.AudioSourceSettings.LoopDuration > clip.length;
        }
        
        protected Vector3 getPlaybackPosition()
        {
            if (Listener != null && Listener.isActiveAndEnabled && Listener.transform.gameObject.activeInHierarchy)
                return Listener.transform.position;
            
            Listener = GameObjectUtils.FindObjectOfType<AudioListener>(includeInactive: false);
            if (Listener != null)
                return Listener.transform.position;
            
            if (Camera.main != null)
                return Camera.main.transform.position;

            // All hope is lost.
            return Vector3.zero;
        }

        protected PlayingSound getOrCreateSoundForLoop(AudioEvent evt)
        {
            // Null tolerance in case sources get destroyed.
            while (SoundPoolForLoops.Count > 0 && SoundPoolForLoops.Peek() == null)
                SoundPoolForLoops.Pop();
            
            if (SoundPoolForLoops.Count > 0)
            {
                var snd = SoundPoolForLoops.Pop();
                snd.Coroutine = null;
                snd.Event = evt;
                PlayingLoops.Add(snd);
                return snd;
            }
            else
            {
                var source = gameObject.AddComponent<AudioSource>();
                var snd = new PlayingSound();
                snd.Source = source;
                snd.Event = evt;
                PlayingLoops.Add(snd);
                return snd;
            }
        }

        protected void returnPlayingLoopToPool(PlayingSound snd)
        {
            if (snd == null)
                return;
            
            snd.Source.clip = null;
            // snd.Source = null; // <- nope, we have to retain the source!
            snd.Event = null;
            if (snd.Coroutine != null)
            {
                StopCoroutine(snd.Coroutine);
                snd.Coroutine = null;
            }

            if (!SoundPoolForLoops.Contains(snd))
                SoundPoolForLoops.Push(snd);

            if (PlayingLoops.Contains(snd))
            {
                PlayingLoops.Remove(snd);
            }
        }
        
        protected PlayingSound getPlayingLoop(AudioEvent evt, Coroutine coroutine)
        {
            foreach (var snd in PlayingLoops)
            {
                if (snd.Coroutine == coroutine)
                    return snd;
            }

            return null;
        }

        public void OnDisable()
        {
            StopAll();
        }
    }
}