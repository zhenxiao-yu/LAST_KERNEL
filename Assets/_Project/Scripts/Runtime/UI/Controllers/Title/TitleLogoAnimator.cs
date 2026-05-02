using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Kamgam.UIToolkitParticles;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Creates the animated "LAST KERNEL" title logo at runtime.
    /// Each letter is a separate Label so it can independently float,
    /// respond to hover, and enter with a staggered animation.
    /// Particle sparks are rendered via Kamgam ParticleImage.
    /// </summary>
    public class TitleLogoAnimator
    {
        private static readonly string[] Words = { "LAST", "KERNEL" };

        private VisualElement        _container;
        private VisualElement        _lettersRow;
        private ParticleImage        _particles;
        private readonly List<Label> _letters = new();

        // ── Init ───────────────────────────────────────────────────────────────

        public void Init(VisualElement container)
        {
            _container = container;
            _container.Clear();

            SetupParticles();
            BuildLettersRow();
            RegisterParallax();
            ScheduleEntrance();
        }

        // ── Particles ──────────────────────────────────────────────────────────

        private void SetupParticles()
        {
            _particles = new ParticleImage();
            _particles.AddToClassList("lk-logo-particles");
            _particles.PlayOnShow    = true;
            _particles.RestartOnShow = true;
            _container.Add(_particles);

            // Defer init until the element is in a live panel
            _container.schedule.Execute(() =>
                _particles.InitializeIfNecessary(immediate: true, onInitialized: ConfigureParticles)
            ).StartingIn(400);
        }

        private void ConfigureParticles()
        {
            var ps = _particles?.ParticleSystem;
            if (ps == null) return;

            // Must stop before editing main.duration on an already-playing system
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            _particles.Texture = BuildCircleTexture(16);

            var main = ps.main;
            main.loop          = true;
            main.duration      = 5f;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.6f, 3.2f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(22f,  55f);
            main.startSize     = new ParticleSystem.MinMaxCurve(1f,   3.5f);
            main.maxParticles  = 55;
            main.startColor    = new ParticleSystem.MinMaxGradient(
                new Color(0f,    0.86f, 1f,    0.85f),
                new Color(0.63f, 0.20f, 0.57f, 0.65f)
            );

            var emission = ps.emission;
            emission.rateOverTime = 9f;

            var shape = ps.shape;
            shape.enabled   = true;
            shape.shapeType = ParticleSystemShapeType.Rectangle;
            shape.scale     = new Vector3(380f, 24f, 1f);
            shape.position  = Vector3.zero;

            // All three axes must use the same MinMaxCurve mode
            var vel = ps.velocityOverLifetime;
            vel.enabled = true;
            vel.x = new ParticleSystem.MinMaxCurve(-14f, 14f);
            vel.y = new ParticleSystem.MinMaxCurve(30f,  75f);
            vel.z = new ParticleSystem.MinMaxCurve(0f,   0f);

            ps.Play();
        }

        // ── Letters ────────────────────────────────────────────────────────────

        private void BuildLettersRow()
        {
            _lettersRow = new VisualElement();
            _lettersRow.AddToClassList("lk-logo-letters-row");
            _container.Add(_lettersRow);

            for (int w = 0; w < Words.Length; w++)
            {
                if (w > 0)
                {
                    var gap = new VisualElement();
                    gap.AddToClassList("lk-logo-word-gap");
                    _lettersRow.Add(gap);
                }

                var wordEl = new VisualElement();
                wordEl.AddToClassList("lk-logo-word");
                _lettersRow.Add(wordEl);

                foreach (char c in Words[w])
                {
                    var lbl = new Label(c.ToString());
                    lbl.AddToClassList("lk-logo-letter");
                    lbl.AddToClassList("lk-logo-letter--pre-enter");

                    UIFonts.DisplayHeavy(lbl);
                    _letters.Add(lbl);
                    wordEl.Add(lbl);

                    lbl.RegisterCallback<MouseEnterEvent>(_ => OnLetterEnter(lbl));
                    lbl.RegisterCallback<MouseLeaveEvent>(_ => OnLetterLeave(lbl));
                }
            }
        }

        private void OnLetterEnter(Label lbl)
        {
            lbl.RemoveFromClassList("lk-logo-letter--idle");
            lbl.AddToClassList("lk-logo-letter--hovered");
        }

        private void OnLetterLeave(Label lbl)
        {
            lbl.RemoveFromClassList("lk-logo-letter--hovered");

            // Wait for hover-exit transition (120 ms) then resume float from current position
            _container.schedule.Execute(() =>
            {
                if (lbl.ClassListContains("lk-logo-letter--hovered")  ||
                    lbl.ClassListContains("lk-logo-letter--entering")  ||
                    lbl.ClassListContains("lk-logo-letter--pre-enter"))
                    return;

                lbl.AddToClassList("lk-logo-letter--idle");
            }).StartingIn(130);
        }

        // ── Parallax ───────────────────────────────────────────────────────────

        private void RegisterParallax()
        {
            _container.RegisterCallback<MouseMoveEvent>(evt =>
            {
                if (_lettersRow == null) return;
                float w = _container.resolvedStyle.width;
                float h = _container.resolvedStyle.height;
                if (w <= 0 || h <= 0) return;

                float nx = evt.localMousePosition.x / w - 0.5f;
                float ny = evt.localMousePosition.y / h - 0.5f;

                _lettersRow.style.translate = new StyleTranslate(
                    new Translate(nx * 12f, ny * 4f));
            });

            _container.RegisterCallback<MouseLeaveEvent>(_ =>
            {
                if (_lettersRow != null)
                    _lettersRow.style.translate = new StyleTranslate(new Translate(0f, 0f));
            });
        }

        // ── Entrance sequence ──────────────────────────────────────────────────

        private void ScheduleEntrance()
        {
            // Letters enter left-to-right with a 65 ms stagger (360 ms rise animation each).
            // Idle float starts at idleBaseMs + i * waveStepMs, which staggers each letter's
            // start point in the 2800 ms cycle by ~155 ms — roughly 50% phase spread total.
            // This creates a visible wave without needing CSS animationDelay (not in IStyle).
            const long baseMs      = 200L;
            const long stepMs      = 65L;
            const long durationMs  = 400L;  // entrance anim 360 ms + small buffer
            const long idleBaseMs  = 600L;  // earliest a letter transitions to idle
            const long waveStepMs  = 155L;  // phase offset per letter across 2800 ms cycle

            for (int i = 0; i < _letters.Count; i++)
            {
                var  lbl        = _letters[i];
                long entranceAt = baseMs + i * stepMs;
                long idleAt     = System.Math.Max(entranceAt + durationMs,
                                                  idleBaseMs  + i * waveStepMs);

                _container.schedule.Execute(() =>
                {
                    lbl.RemoveFromClassList("lk-logo-letter--pre-enter");
                    lbl.AddToClassList("lk-logo-letter--entering");
                }).StartingIn(entranceAt);

                _container.schedule.Execute(() =>
                {
                    // Add idle before removing entering so opacity:1 is never interrupted
                    lbl.AddToClassList("lk-logo-letter--idle");
                    lbl.RemoveFromClassList("lk-logo-letter--entering");
                }).StartingIn(idleAt);
            }
        }

        // ── Cleanup ────────────────────────────────────────────────────────────

        public void Cleanup()
        {
            _particles?.Stop();
            _letters.Clear();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static Texture2D _circleTexture16;

        private static Texture2D BuildCircleTexture(int size)
        {
            if (_circleTexture16 != null)
                return _circleTexture16;

            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float r  = size * 0.5f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dist = Mathf.Sqrt((x - r + 0.5f) * (x - r + 0.5f) +
                                        (y - r + 0.5f) * (y - r + 0.5f));
                float a = Mathf.Clamp01(1f - dist / r);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a * a));
            }
            tex.Apply();
            _circleTexture16 = tex;
            return tex;
        }
    }
}
