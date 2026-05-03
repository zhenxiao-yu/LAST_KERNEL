using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

namespace Markyu.LastKernel
{
    public class AudioManager : MonoBehaviour, IAudioService
    {
        public static AudioManager Instance { get; private set; }

        [BoxGroup("SFX Settings")]
        [SerializeField, Tooltip("Number of AudioSource objects in the pool for playing SFX.")]
        private int _SFXPoolSize = 8;

        [BoxGroup("SFX Settings")]
        [SerializeField, Tooltip("Randomly vary pitch for sound effects for a more natural feel.")]
        private bool _randomizeSFXPitch = true;

        [BoxGroup("SFX Settings")]
        [SerializeField, Tooltip("Minimum delay (in seconds) before the same SFX can be played again.")]
        private float _SFXCooldown = 0.05f;

        [BoxGroup("BGM Settings")]
        [SerializeField, Tooltip("All music tracks, organised by game context. Populated automatically via Tools > LAST KERNEL > Sync Music Playlist.")]
        private MusicPlaylist _musicPlaylist;

        [BoxGroup("BGM Settings")]
        [SerializeField, Tooltip("Source volume for each BGM AudioSource (0–1). Mixer group controls perceived loudness separately.")]
        private float _defaultMusicVolume = 0.7f;

        [BoxGroup("BGM Settings")]
        [SerializeField, Min(0f), Tooltip("Default crossfade duration in seconds when switching contexts.")]
        private float _defaultFadeDuration = 1.5f;

        [BoxGroup("Audio Mixer")]
        [SerializeField, Tooltip("Main AudioMixer controlling overall game audio.")]
        private AudioMixer _audioMixer;

        [BoxGroup("Audio Mixer")]
        [SerializeField, Tooltip("AudioMixerGroup assigned to all sound effects.")]
        private AudioMixerGroup _SFXAudioGroup;

        [BoxGroup("Audio Mixer")]
        [SerializeField, Tooltip("AudioMixerGroup assigned to background music.")]
        private AudioMixerGroup _BGMAudioGroup;

        [BoxGroup("Sound Effects")]
        [SerializeField, Tooltip("One entry per AudioId enum value. Duplicate IDs and missing clips are flagged here.")]
        [TableList(AlwaysExpanded = true, ShowIndexLabels = true)]
        [ValidateInput("ValidateSFXList")]
        private List<AudioData> _SFXDataList;

        private Dictionary<AudioId, AudioClip> _SFXClipLookup;
        private Dictionary<AudioId, float> _lastPlayedTime = new();
        private List<AudioSource> _SFXSourcePool;
        private int _nextSFXSourceIndex = 0;

        // Two looping sources for crossfading (A/B flip).
        private AudioSource _bgmA;
        private AudioSource _bgmB;
        private AudioSource _stingerSource;   // one-shot, non-looping, plays before the loop starts
        private bool _onA = true;
        private MusicContext _currentContext = MusicContext.None;
        private Tween _bgmFadeTween;
        private Coroutine _stingerCoroutine;

        private const string SFX_VOL_KEY = "VolumeSFX";
        private const string BGM_VOL_KEY = "VolumeBGM";

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            _SFXClipLookup = _SFXDataList.ToDictionary(
                data => data.audioId,
                data => data.audioClip
            );

            // Create SFX AudioSource Pool
            _SFXSourcePool = new List<AudioSource>(_SFXPoolSize);
            for (int i = 0; i < _SFXPoolSize; i++)
            {
                var sourceObj = new GameObject($"SFX_AudioSource_{i + 1}");
                sourceObj.transform.SetParent(transform);
                var source = sourceObj.AddComponent<AudioSource>();
                source.outputAudioMixerGroup = _SFXAudioGroup;
                source.playOnAwake = false;
                source.spatialBlend = 0f;
                _SFXSourcePool.Add(source);
            }

            // Initialize A/B BGM sources for crossfading plus a one-shot stinger source.
            _bgmA          = CreateBGMSource("BGM_A",       loop: true);
            _bgmB          = CreateBGMSource("BGM_B",       loop: true);
            _stingerSource = CreateBGMSource("BGM_Stinger", loop: false);

            SceneManager.sceneLoaded += OnSceneLoaded;
            EnsureAudioListener();
        }

        private void Start()
        {
            InitAudioMixerVolumes();
        }

        private void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _bgmFadeTween?.Kill();
            StopAllCoroutines();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            EnsureAudioListener();
        }

        // Adds a fallback AudioListener to this GameObject if the scene has none.
        // Removes it again if a proper listener already exists elsewhere.
        private void EnsureAudioListener()
        {
            var existing = FindObjectsByType<AudioListener>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            var ours = GetComponent<AudioListener>();

            if (existing.Length == 0 || (existing.Length == 1 && ours != null))
            {
                if (ours == null)
                    gameObject.AddComponent<AudioListener>();
            }
            else if (existing.Length > 1 && ours != null)
            {
                Destroy(ours);
            }
        }

        // ── BGM ───────────────────────────────────────────────────────────────────

        /// <summary>
        /// Transitions to a random loop track from the given context.
        /// If the playlist has a transitionIn stinger for the context, that plays first
        /// (at full volume), then the loop fades in underneath once the stinger ends.
        /// No-ops if the same context is already playing.
        /// </summary>
        public void PlayBGM(MusicContext context, float fadeDuration = -1f)
        {
            if (context == MusicContext.None || context == _currentContext) return;

            var loopClip = _musicPlaylist?.PickTrack(context);
            if (loopClip == null)
            {
                Debug.LogWarning($"[AudioManager] No track for MusicContext.{context}. Drop a file into Audio/Music/{context}/");
                return;
            }

            _currentContext = context;
            float duration  = fadeDuration < 0f ? _defaultFadeDuration : fadeDuration;

            // Cancel any previous stinger-then-loop coroutine.
            if (_stingerCoroutine != null)
            {
                StopCoroutine(_stingerCoroutine);
                _stingerCoroutine = null;
            }

            var stinger = _musicPlaylist?.PickTransition(context);
            if (stinger != null)
            {
                // Fade out current loop, play stinger, then start new loop.
                FadeOutCurrent(duration * 0.5f);
                _stingerSource.Stop();
                _stingerSource.volume = _defaultMusicVolume;
                _stingerSource.clip   = stinger;
                _stingerSource.Play();
                // Start the loop slightly before the stinger ends so they blend.
                float loopDelay = Mathf.Max(0f, stinger.length - duration * 0.5f);
                _stingerCoroutine = StartCoroutine(DelayedLoop(loopClip, loopDelay, duration * 0.5f));
            }
            else
            {
                CrossfadeToLoop(loopClip, duration);
            }
        }

        private void FadeOutCurrent(float duration)
        {
            _bgmFadeTween?.Kill();
            AudioSource outgoing = _onA ? _bgmB : _bgmA;  // the one currently playing
            if (!outgoing.isPlaying) return;
            _bgmFadeTween = DOTween.To(() => outgoing.volume, v => outgoing.volume = v, 0f, duration)
                .OnComplete(() => { outgoing.Stop(); outgoing.clip = null; })
                .SetUpdate(true).SetLink(gameObject);
        }

        private IEnumerator DelayedLoop(AudioClip clip, float delay, float fadeDuration)
        {
            if (delay > 0f)
                yield return new WaitForSecondsRealtime(delay);
            CrossfadeToLoop(clip, fadeDuration);
            _stingerCoroutine = null;
        }

        private void CrossfadeToLoop(AudioClip clip, float duration)
        {
            AudioSource incoming = _onA ? _bgmA : _bgmB;
            AudioSource outgoing = _onA ? _bgmB : _bgmA;
            _onA = !_onA;

            incoming.clip   = clip;
            incoming.volume = 0f;
            incoming.Play();

            _bgmFadeTween?.Kill();

            if (duration <= 0f)
            {
                incoming.volume = _defaultMusicVolume;
                outgoing.Stop();
                outgoing.clip = null;
                return;
            }

            _bgmFadeTween = DOTween.Sequence()
                .Append(DOTween.To(() => outgoing.volume, v => outgoing.volume = v, 0f, duration))
                .Join(DOTween.To(() => incoming.volume,   v => incoming.volume = v, _defaultMusicVolume, duration))
                .OnComplete(() => { outgoing.Stop(); outgoing.clip = null; })
                .SetUpdate(true)
                .SetLink(gameObject);
        }

        private AudioSource CreateBGMSource(string label, bool loop)
        {
            var go  = new GameObject(label);
            go.transform.SetParent(transform);
            var src = go.AddComponent<AudioSource>();
            src.outputAudioMixerGroup = _BGMAudioGroup;
            src.volume      = 0f;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            src.loop = loop;
            return src;
        }

        /// <summary>
        /// Initializes the volume levels of the Sound Effects (SFX) and Background Music (BGM)
        /// mixer groups by loading the saved decibel values from PlayerPrefs.
        /// </summary>
        /// <remarks>
        /// If no saved value is found for a mixer group, it defaults the volume to 0 dB.
        /// This method ensures the AudioMixer reflects the user's last saved volume preferences upon startup.
        /// </remarks>
        public void InitAudioMixerVolumes()
        {
            _audioMixer.SetFloat(SFX_VOL_KEY, PlayerPrefs.GetFloat(SFX_VOL_KEY, 0f));
            _audioMixer.SetFloat(BGM_VOL_KEY, PlayerPrefs.GetFloat(BGM_VOL_KEY, 0f));
        }

        private AudioSource GetNextSource()
        {
            var source = _SFXSourcePool[_nextSFXSourceIndex];
            _nextSFXSourceIndex = (_nextSFXSourceIndex + 1) % _SFXPoolSize;

            if (_randomizeSFXPitch)
                source.pitch = Random.Range(0.95f, 1.05f);
            else
                source.pitch = 1f;

            return source;
        }

        /// <summary>
        /// Plays a sound effect (SFX) based on the given AudioId.
        /// Uses a pooled AudioSource to prevent allocating new sources at runtime.
        /// Supports both 2D (UI / general) and 3D positional sound effects.
        /// Optionally lowers or pauses background music if interruptBGM is true.
        /// </summary>
        /// <param name="audioId">
        /// The identifier of the sound effect to play.
        /// </param>
        /// <param name="worldPosition">
        /// If null, the SFX is played as a 2D sound (spatialBlend = 0).  
        /// If a value is provided, the AudioSource is positioned in 3D space and played as a positional sound.
        /// </param>
        /// <param name="interruptBGM">
        /// If true, temporarily lowers the BGM volume for the duration of the SFX.  
        /// Useful for important or emphasized sounds (e.g., win / lose events).
        /// </param>
        public void PlaySFX(AudioId audioId, Vector3? worldPosition = null, bool interruptBGM = false)
        {
            // Cooldown check to avoid rapid spam of the same SFX
            if (_lastPlayedTime.TryGetValue(audioId, out float lastTime))
            {
                if (Time.unscaledTime - lastTime < _SFXCooldown)
                    return;
            }
            _lastPlayedTime[audioId] = Time.unscaledTime;

            // Attempt to look up the AudioClip by its AudioId
            if (!_SFXClipLookup.TryGetValue(audioId, out var clip))
            {
                Debug.LogWarning($"No clip found for: {audioId}");
                return;
            }

            // Retrieve the next pooled AudioSource
            var source = GetNextSource();

            // 2D UI / general SFX (non-spatial)
            if (!worldPosition.HasValue)
            {
                source.spatialBlend = 0f;
            }
            else
            {
                // 3D positional SFX (world-space audio)
                source.transform.position = worldPosition.Value;
                source.spatialBlend = 1f;
            }

            // Play the clip once without looping
            source.PlayOneShot(clip);

            // Optionally interrupt or lower background music
            if (interruptBGM)
            {
                _audioMixer.GetFloat(BGM_VOL_KEY, out float value);

                // Only pause BGM if it is currently audible
                if (value >= 0f)
                    StartCoroutine(PauseBGM(clip.length));
            }
        }

        private IEnumerator PauseBGM(float duration)
        {
            _audioMixer.GetFloat(BGM_VOL_KEY, out float originalVolume);
            _audioMixer.SetFloat(BGM_VOL_KEY, -80f);
            yield return new WaitForSeconds(duration);
            _audioMixer.SetFloat(BGM_VOL_KEY, originalVolume);
        }

        /// <summary>
        /// Sets the Sound Effects (SFX) volume using a normalized slider value (0–1).
        /// Updates the AudioMixer and persists the value using PlayerPrefs.
        /// </summary>
        /// <param name="value">
        /// Normalized slider value.
        /// 0 = muted, 1 = full volume.
        /// </param>
        public void SetSFXVolume(float value)
        {
            float dB = LinearToDecibels(value);

            _audioMixer.SetFloat(SFX_VOL_KEY, dB);
            PlayerPrefs.SetFloat(SFX_VOL_KEY, dB);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Sets the Background Music (BGM) volume using a normalized slider value (0–1).
        /// Updates the AudioMixer and persists the value using PlayerPrefs.
        /// </summary>
        /// <param name="value">
        /// Normalized slider value.
        /// 0 = muted, 1 = full volume.
        /// </param>
        public void SetBGMVolume(float value)
        {
            float dB = LinearToDecibels(value);

            _audioMixer.SetFloat(BGM_VOL_KEY, dB);
            PlayerPrefs.SetFloat(BGM_VOL_KEY, dB);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// Converts a normalized slider value (0–1) into decibels for the AudioMixer.
        /// Uses a floor value to avoid -Infinity when slider is at 0.
        /// </summary>
        /// /// <param name="value">
        /// Normalized slider value.
        /// 0 = muted, 1 = full volume.
        /// </param>
        private float LinearToDecibels(float value)
        {
            // Slider at 0 = mute
            if (value <= 0.0001f)
                return -80f;

            // Convert to logarithmic dB scale
            return Mathf.Log10(value) * 20f;
        }

        /// <summary>
        /// Retrieves the current Sound Effects (SFX) volume from the AudioMixer and converts
        /// the decibel value back into a normalized slider value (0 to 1).
        /// </summary>
        /// <returns>A float between 0 (muted) and 1 (full volume) for use with a UI slider.</returns>
        public float GetSavedSFXVolumeSlider()
        {
            _audioMixer.GetFloat(SFX_VOL_KEY, out float dB);
            return Mathf.Clamp01(Mathf.Pow(10f, dB / 20f));
        }

        /// <summary>
        /// Retrieves the current Background Music (BGM) volume from the AudioMixer and converts
        /// the decibel value back into a normalized slider value (0 to 1).
        /// </summary>
        /// <returns>A float between 0 (muted) and 1 (full volume) for use with a UI slider.</returns>
        public float GetSavedBGMVolumeSlider()
        {
            _audioMixer.GetFloat(BGM_VOL_KEY, out float dB);
            return Mathf.Clamp01(Mathf.Pow(10f, dB / 20f));
        }

        // Called by [ValidateInput] in the Inspector — never at runtime.
        private bool ValidateSFXList(List<AudioData> list, ref string message)
        {
            if (list == null || list.Count == 0)
            {
                message = "SFX list is empty — no sound effects will play.";
                return false;
            }

            var issues = new System.Text.StringBuilder();
            var seen = new HashSet<AudioId>();

            for (int i = 0; i < list.Count; i++)
            {
                var entry = list[i];
                if (entry == null) continue;
                if (entry.audioClip == null)
                    issues.AppendLine($"[{i}] {entry.audioId}: no AudioClip assigned.");
                if (!seen.Add(entry.audioId))
                    issues.AppendLine($"[{i}] Duplicate AudioId: {entry.audioId}.");
            }

            int enumCount = System.Enum.GetValues(typeof(AudioId)).Length;
            if (list.Count < enumCount)
                issues.AppendLine($"Only {list.Count} of {enumCount} AudioId values registered.");

            if (issues.Length == 0) return true;
            message = issues.ToString().TrimEnd();
            return false;
        }
    }

    public enum AudioId
    {
        CardPick, CardDrop, CardSwipe,
        Coin, Coins, CashRegister,
        AttackMelee, AttackRanged, AttackMagic,
        HitMelee, HitRanged, HitMagic,
        Miss, Critical,
        Pop,
        Eat,
        Click,
        Puff
    }

    [System.Serializable]
    public class AudioData
    {
        [TableColumnWidth(160, Resizable = false)]
        public AudioId audioId;

        [TableColumnWidth(220)]
        [Required]
        public AudioClip audioClip;
    }
}

