using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Animated UGUI card for one combatant in the BattleArenaView.
    /// Built entirely at runtime — no prefab required.
    /// Uses only Transform-level DOTween + DOTween.To lambdas (no DOTween UI module needed).
    /// </summary>
    public class BattleUnitView
    {
        // ── Colors ────────────────────────────────────────────────────────────
        private static readonly Color DefenderAccent = new Color(0.35f, 0.80f, 1.00f);
        private static readonly Color EnemyAccent    = new Color(1.00f, 0.35f, 0.25f);
        private static readonly Color HitFlash       = new Color(1.00f, 0.10f, 0.10f, 1f);
        private static readonly Color CritFlash      = new Color(1.00f, 0.85f, 0.05f, 1f);
        private static readonly Color ArtNormal      = Color.white;
        private static readonly Color HpGreen        = new Color(0.15f, 0.90f, 0.35f);
        private static readonly Color HpYellow       = new Color(0.95f, 0.80f, 0.10f);
        private static readonly Color HpRed          = new Color(0.90f, 0.15f, 0.15f);

        public static readonly Vector2 CardSize = new Vector2(82f, 118f);

        // ── Public state ──────────────────────────────────────────────────────
        public CombatUnit    Unit { get; private set; }
        public RectTransform Root { get; private set; }

        // ── Private refs ──────────────────────────────────────────────────────
        private RawImage          _art;
        private Image             _bg;
        private Image             _hpFill;
        private TextMeshProUGUI   _nameLabel;
        private TextMeshProUGUI   _hpText;
        private CanvasGroup       _canvasGroup;
        private Outline           _border;

        private bool  _fastForward;
        private Color _accent;

        // ── Factory ───────────────────────────────────────────────────────────

        public static BattleUnitView Create(RectTransform parent, CombatUnit unit)
        {
            var v    = new BattleUnitView();
            v._accent = unit.Side == CombatUnitSide.Defender ? DefenderAccent : EnemyAccent;
            v.Unit    = unit;
            v.Build(parent, unit);
            return v;
        }

        // ── Build ─────────────────────────────────────────────────────────────

        private void Build(RectTransform parent, CombatUnit unit)
        {
            // Root
            var go = new GameObject($"Unit_{unit.DisplayName}");
            go.transform.SetParent(parent, false);
            Root         = go.AddComponent<RectTransform>();
            Root.sizeDelta = CardSize;
            Root.pivot     = new Vector2(0.5f, 0.5f);
            Root.anchorMin = Root.anchorMax = new Vector2(0.5f, 0.5f);

            _canvasGroup       = go.AddComponent<CanvasGroup>();
            _bg                = go.AddComponent<Image>();
            _bg.color          = new Color(0.05f, 0.07f, 0.11f, 0.97f);
            _border            = go.AddComponent<Outline>();
            _border.effectColor    = _accent * 0.5f;
            _border.effectDistance = new Vector2(1.5f, 1.5f);

            // Art (RawImage — compatible with non-readable Texture2D)
            var artGO = Child(go, "Art");
            Anchored(artGO, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.96f));
            _art       = artGO.AddComponent<RawImage>();
            var tex    = unit.SourceCard?.Definition?.ArtTexture;
            if (tex != null) { _art.texture = tex; _art.color = Color.white; }
            else               _art.color   = _accent * 0.2f;

            // Name
            _nameLabel = TMP(go, "Name", unit.DisplayName.ToUpper(),
                new Vector2(0f, 0.29f), new Vector2(1f, 0.45f), 8, _accent);

            // ATK
            TMP(go, "Atk", $"⚔ {unit.Attack}",
                new Vector2(0.02f, 0.17f), new Vector2(0.55f, 0.30f), 8, Color.white);

            // HP bar bg
            var hpBg = Child(go, "HpBg");
            Anchored(hpBg, new Vector2(0.05f, 0.05f), new Vector2(0.95f, 0.17f));
            hpBg.AddComponent<Image>().color = new Color(0.08f, 0.08f, 0.10f);

            // HP fill
            var hpFillGO = Child(hpBg, "Fill");
            Anchored(hpFillGO, Vector2.zero, Vector2.one);
            _hpFill            = hpFillGO.AddComponent<Image>();
            _hpFill.type       = Image.Type.Filled;
            _hpFill.fillMethod = Image.FillMethod.Horizontal;
            _hpFill.fillAmount = 1f;
            _hpFill.color      = HpGreen;

            // HP text
            _hpText = TMP(go, "HpTxt", $"{unit.CurrentHP}/{unit.MaxHP}",
                new Vector2(0.52f, 0.17f), new Vector2(0.98f, 0.30f), 7, Color.white * 0.75f);
        }

        // ── Animations ────────────────────────────────────────────────────────

        public void SetFastForward(bool fast) => _fastForward = fast;

        /// <summary>Lunge toward the enemy side and snap back.</summary>
        public Sequence AnimateAttack()
        {
            if (Root == null) return null;
            float dir  = Unit.Side == CombatUnitSide.Defender ? 1f : -1f;
            Vector2 home = Root.anchoredPosition;
            Vector2 tip  = home + new Vector2(dir * 26f, 0f);

            return DOTween.Sequence().SetLink(Root.gameObject)
                .Append(AnchorTo(tip,  T(0.10f)).SetEase(Ease.OutQuad))
                .Append(AnchorTo(home, T(0.15f)).SetEase(Ease.OutElastic));
        }

        /// <summary>Wobble-shake and flash red (or gold for crits).</summary>
        public void AnimateHit(bool isCrit)
        {
            if (Root == null) return;

            // Rotation wobble — works in both 2D and 3D canvas space
            float angle = isCrit ? 14f : 7f;
            var shakeSq = DOTween.Sequence().SetLink(Root.gameObject);
            shakeSq.Append(Root.DOLocalRotate(new Vector3(0f, 0f,  angle), T(0.05f)));
            shakeSq.Append(Root.DOLocalRotate(new Vector3(0f, 0f, -angle), T(0.05f)));
            shakeSq.Append(Root.DOLocalRotate(new Vector3(0f, 0f,  angle * 0.5f), T(0.04f)));
            shakeSq.Append(Root.DOLocalRotate(Vector3.zero, T(0.06f)));

            // Art color flash
            if (_art != null)
            {
                Color flash = isCrit ? CritFlash : HitFlash;
                var artSeq  = DOTween.Sequence().SetLink(Root.gameObject);
                artSeq.Append(TweenColor(_art, flash,      T(0.04f)));
                artSeq.Append(TweenColor(_art, ArtNormal,  T(0.16f)));
            }

            // Background pulse on crit
            if (isCrit && _bg != null)
            {
                Color bgFlash = new Color(0.22f, 0.18f, 0.02f, 0.97f);
                Color bgNorm  = new Color(0.05f, 0.07f, 0.11f, 0.97f);
                var bgSeq     = DOTween.Sequence().SetLink(Root.gameObject);
                bgSeq.Append(TweenColor(_bg, bgFlash, T(0.04f)));
                bgSeq.Append(TweenColor(_bg, bgNorm,  T(0.22f)));
            }
        }

        /// <summary>Smoothly drain HP bar to the unit's current fraction.</summary>
        public void AnimateHP()
        {
            if (_hpFill == null) return;

            float frac  = Unit.HPFraction;
            Color col   = frac > 0.55f ? HpGreen : frac > 0.28f ? HpYellow : HpRed;

            DOTween.To(() => _hpFill.fillAmount, x => _hpFill.fillAmount = x, frac,  T(0.22f))
                   .SetLink(Root.gameObject);
            TweenColor(_hpFill, col, T(0.18f)).SetLink(Root.gameObject);

            if (_hpText != null)
                _hpText.text = $"{Unit.CurrentHP}/{Unit.MaxHP}";
        }

        /// <summary>SAP-style pop-and-vanish death sequence.</summary>
        public Sequence AnimateDeath()
        {
            if (Root == null) return null;

            var seq = DOTween.Sequence().SetLink(Root.gameObject);
            seq.Append(Root.DOLocalRotate(new Vector3(0f, 0f, 18f), T(0.06f)));
            seq.Append(Root.DOScale(Vector3.zero, T(0.22f)).SetEase(Ease.InBack));
            seq.Join(DOTween.To(() => _canvasGroup.alpha, x => _canvasGroup.alpha = x, 0f, T(0.20f)));
            return seq;
        }

        /// <summary>Highlight this unit's border when it is the current front unit.</summary>
        public void SetFrontHighlight(bool on)
        {
            if (_border == null) return;
            _border.effectColor = on ? _accent : _accent * 0.25f;
        }

        // ── Private helpers ───────────────────────────────────────────────────

        private float T(float t) => _fastForward ? t * 0.06f : t;

        private Tweener AnchorTo(Vector2 target, float duration)
            => DOTween.To(() => Root.anchoredPosition,
                          x  => Root.anchoredPosition = x,
                          target, duration)
                      .SetLink(Root.gameObject);

        // Works for both Image and RawImage via a shared Graphic base class approach.
        private static Tweener TweenColor(Image img, Color to, float dur)
            => DOTween.To(() => img.color, x => img.color = x, to, dur);

        private static Tweener TweenColor(RawImage img, Color to, float dur)
            => DOTween.To(() => img.color, x => img.color = x, to, dur);

        private static GameObject Child(GameObject parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            return go;
        }

        private static void Anchored(GameObject go, Vector2 min, Vector2 max)
        {
            var rt       = go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
            rt.anchorMin = min;
            rt.anchorMax = max;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        private static TextMeshProUGUI TMP(
            GameObject parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax, int fontSize, Color color)
        {
            var go = Child(parent, name);
            Anchored(go, anchorMin, anchorMax);
            var t          = go.AddComponent<TextMeshProUGUI>();
            t.text         = text;
            t.fontSize     = fontSize;
            t.color        = color;
            t.alignment    = TextAlignmentOptions.Center;
            t.overflowMode = TextOverflowModes.Ellipsis;
            return t;
        }
    }
}
