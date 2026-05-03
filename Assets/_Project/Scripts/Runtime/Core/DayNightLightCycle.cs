using DG.Tweening;
using UnityEngine;

namespace Markyu.LastKernel
{
    // Attach to any GameObject in the Game scene alongside (or near) the Directional Light.
    // Assign _sun in the Inspector; if left null it falls back to FindAnyObjectByType<Light>().
    //
    // Behaviour:
    //   • Each frame during the day phase: lerps sun color/intensity/angle + ambient
    //     from Dawn values to Dusk values, following NormalizedTime (0→1).
    //   • OnDayEnded  → tweens to Night preset over _toNightSecs.
    //   • OnDayStarted→ tweens back to Dawn preset over _toDawnSecs,
    //     then resumes per-frame lerp for the new day.
    [DisallowMultipleComponent]
    public sealed class DayNightLightCycle : MonoBehaviour
    {
        [SerializeField] Light _sun;

        [Header("Sun Direction (fixed Y/Z, only X changes)")]
        [SerializeField] float _sunYAngle = -30f;
        [SerializeField] float _sunZAngle =   0f;

        [Header("Dawn — start of each day")]
        [SerializeField] Color _dawnColor     = new Color(0.98f, 0.94f, 0.86f);
        [SerializeField] float _dawnIntensity = 1.10f;
        [SerializeField] float _dawnAngleX    = 52f;
        [SerializeField] Color _dawnAmbient   = new Color(0.22f, 0.28f, 0.38f);

        [Header("Dusk — end of each day")]
        [SerializeField] Color _duskColor     = new Color(1.00f, 0.52f, 0.18f);
        [SerializeField] float _duskIntensity = 0.65f;
        [SerializeField] float _duskAngleX    = 12f;
        [SerializeField] Color _duskAmbient   = new Color(0.16f, 0.10f, 0.20f);

        [Header("Night — combat phase")]
        [SerializeField] Color _nightColor     = new Color(0.25f, 0.35f, 0.65f);
        [SerializeField] float _nightIntensity = 0.28f;
        [SerializeField] float _nightAngleX    = 6f;
        [SerializeField] Color _nightAmbient   = new Color(0.04f, 0.05f, 0.14f);

        [Header("Transition durations (seconds)")]
        [SerializeField] float _toNightSecs = 2.2f;
        [SerializeField] float _toDawnSecs  = 1.8f;

        [Header("Day progression curve (X=NormalizedTime, Y=lerp t)")]
        [SerializeField] AnimationCurve _dayCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

        // ── state ──────────────────────────────────────────────────────────
        bool     _isNight;
        Sequence _seq;

        // ── lifecycle ──────────────────────────────────────────────────────
        void Awake()
        {
            if (_sun == null)
                _sun = FindAnyObjectByType<Light>();
        }

        void Start()
        {
            ApplyDirect(_dawnColor, _dawnIntensity, _dawnAngleX, _dawnAmbient);

            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnDayEnded   += HandleDayEnded;
            TimeManager.Instance.OnDayStarted += HandleDayStarted;
        }

        void OnDestroy()
        {
            _seq?.Kill();
            if (TimeManager.Instance == null) return;
            TimeManager.Instance.OnDayEnded   -= HandleDayEnded;
            TimeManager.Instance.OnDayStarted -= HandleDayStarted;
        }

        // ── per-frame lerp (day phase only) ───────────────────────────────
        void Update()
        {
            // Suppress while in night or while a tween is running
            if (_isNight || (_seq != null && _seq.IsActive()) || TimeManager.Instance == null || _sun == null)
                return;

            float t = _dayCurve.Evaluate(TimeManager.Instance.NormalizedTime);
            _sun.color     = Color.Lerp(_dawnColor, _duskColor, t);
            _sun.intensity = Mathf.Lerp(_dawnIntensity, _duskIntensity, t);
            SetSunX(Mathf.Lerp(_dawnAngleX, _duskAngleX, t));
            RenderSettings.ambientLight = Color.Lerp(_dawnAmbient, _duskAmbient, t);
        }

        // ── event handlers ────────────────────────────────────────────────
        void HandleDayEnded(int _)
        {
            _isNight = true;
            TweenTo(_nightColor, _nightIntensity, _nightAngleX, _nightAmbient, _toNightSecs);
        }

        void HandleDayStarted(int _)
        {
            _isNight = false;
            TweenTo(_dawnColor, _dawnIntensity, _dawnAngleX, _dawnAmbient, _toDawnSecs);
        }

        // ── helpers ────────────────────────────────────────────────────────
        void TweenTo(Color col, float intensity, float angleX, Color ambient, float dur)
        {
            if (_sun == null) return;
            _seq?.Kill();

            // Snapshot ambient at tween start (Color is a value type — safe to capture)
            Color startAmbient = RenderSettings.ambientLight;

            _seq = DOTween.Sequence()
                .SetLink(gameObject)
                .SetEase(Ease.InOutSine);

            _seq.Join(_sun.DOColor(col, dur));
            _seq.Join(_sun.DOIntensity(intensity, dur));
            // DOLocalRotate handles euler angle interpolation correctly
            _seq.Join(_sun.transform.DOLocalRotate(
                new Vector3(angleX, _sunYAngle, _sunZAngle), dur, RotateMode.Fast));
            // Ambient: use snapshot so getter is stable
            float progress = 0f;
            _seq.Join(DOTween.To(
                () => progress,
                t  => { progress = t; RenderSettings.ambientLight = Color.Lerp(startAmbient, ambient, t); },
                1f, dur));
        }

        void ApplyDirect(Color col, float intensity, float angleX, Color ambient)
        {
            if (_sun == null) return;
            _sun.color     = col;
            _sun.intensity = intensity;
            SetSunX(angleX);
            RenderSettings.ambientLight = ambient;
        }

        void SetSunX(float x)
        {
            if (float.IsNaN(x)) return;
            _sun.transform.localEulerAngles = new Vector3(x, _sunYAngle, _sunZAngle);
        }
    }
}
