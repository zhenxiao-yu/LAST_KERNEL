using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Markyu.FortStack
{
    /// <summary>
    /// Minimal combat lane overlay. Builds its own canvas at runtime — no prefab setup required.
    ///
    /// Layout:
    ///   [NIGHT COMBAT header]
    ///   [Defender row]  |  [Enemy row]
    ///   Each unit: Name label + HP bar + HP text
    ///
    /// Subscribes to CombatLane events.
    /// Does not own simulation logic; pure observer / display.
    /// </summary>
    public class CombatLaneView : MonoBehaviour
    {
        // ── Colour palette ────────────────────────────────────────────────────────
        private static readonly Color BgColor         = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        private static readonly Color DefenderColor   = new Color(0.2f,  0.6f,  1.0f,  1f);
        private static readonly Color EnemyColor      = new Color(1.0f,  0.25f, 0.2f,  1f);
        private static readonly Color DeadColor       = new Color(0.35f, 0.35f, 0.35f, 1f);
        private static readonly Color HpBarBg         = new Color(0.15f, 0.15f, 0.15f, 1f);
        private static readonly Color HeaderColor      = new Color(0.9f,  0.7f,  0.2f,  1f);
        private static readonly Color VsColor          = new Color(0.9f,  0.3f,  0.3f,  1f);

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Canvas _canvas;
        private GameObject _root;

        private CombatLane _lane;
        private readonly List<UnitDisplay> _defenderDisplays = new();
        private readonly List<UnitDisplay> _enemyDisplays    = new();

        // ─────────────────────────────────────────────────────────────────────────

        private class UnitDisplay
        {
            public CombatUnit Unit;
            public TextMeshProUGUI NameLabel;
            public Image HpFill;
            public TextMeshProUGUI HpLabel;
            public Image Background;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        public void Bind(CombatLane lane)
        {
            _lane = lane;
            lane.OnUnitDied += OnUnitDied;
        }

        public void Show()
        {
            EnsureCanvas();
            BuildLaneUI();
            _root.SetActive(true);
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            _lane = null;
        }

        public void RefreshDisplay()
        {
            foreach (var d in _defenderDisplays) UpdateDisplay(d);
            foreach (var d in _enemyDisplays)    UpdateDisplay(d);
        }

        // ── Canvas creation ───────────────────────────────────────────────────────

        private void EnsureCanvas()
        {
            if (_canvas != null) return;

            var canvasGO = new GameObject("NightCombatCanvas");
            canvasGO.transform.SetParent(transform, false);

            _canvas = canvasGO.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }

        // ── UI construction ───────────────────────────────────────────────────────

        private void BuildLaneUI()
        {
            _defenderDisplays.Clear();
            _enemyDisplays.Clear();

            if (_root != null) Destroy(_root);

            // Outer panel
            _root = CreatePanel(_canvas.gameObject, "LaneRoot",
                new Vector2(0f, 0f), new Vector2(1f, 1f), BgColor);

            // Header
            var header = CreateTMP(_root, "Header", "NIGHT INCURSION",
                new Vector2(0f, 0.88f), new Vector2(1f, 1f), 28, HeaderColor, TextAlignmentOptions.Center);
            header.fontStyle = FontStyles.Bold;

            // VS divider
            var vs = CreateTMP(_root, "VS", "VS",
                new Vector2(0.45f, 0.1f), new Vector2(0.55f, 0.9f), 22, VsColor, TextAlignmentOptions.Center);
            vs.fontStyle = FontStyles.Bold;

            // Defender column header
            CreateTMP(_root, "DefHeader", "DEFENDERS",
                new Vector2(0f, 0.82f), new Vector2(0.45f, 0.88f), 18, DefenderColor, TextAlignmentOptions.Center);

            // Enemy column header
            CreateTMP(_root, "EnemyHeader", "ENEMIES",
                new Vector2(0.55f, 0.82f), new Vector2(1f, 0.88f), 18, EnemyColor, TextAlignmentOptions.Center);

            // Unit rows
            if (_lane != null)
            {
                int defCount = _lane.Defenders.Count;
                int eneCount = _lane.Enemies.Count;
                int maxRows  = Mathf.Max(defCount, eneCount, 1);

                for (int i = 0; i < defCount; i++)
                {
                    float t = GetRowT(i, maxRows);
                    var d = CreateUnitDisplay(_root, _lane.Defenders[i], DefenderColor,
                        new Vector2(0.02f, t - 0.06f), new Vector2(0.44f, t + 0.02f));
                    _defenderDisplays.Add(d);
                }

                for (int i = 0; i < eneCount; i++)
                {
                    float t = GetRowT(i, maxRows);
                    var d = CreateUnitDisplay(_root, _lane.Enemies[i], EnemyColor,
                        new Vector2(0.56f, t - 0.06f), new Vector2(0.98f, t + 0.02f));
                    _enemyDisplays.Add(d);
                }
            }
        }

        private static float GetRowT(int index, int maxRows)
        {
            float topY  = 0.78f;
            float span  = 0.62f;
            float step  = maxRows > 1 ? span / (maxRows - 1) : 0f;
            return topY - index * step;
        }

        private UnitDisplay CreateUnitDisplay(
            GameObject parent, CombatUnit unit, Color accentColor,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var bg = CreatePanel(parent, $"Unit_{unit.DisplayName}",
                anchorMin, anchorMax, new Color(0.1f, 0.1f, 0.15f, 1f));

            // Name label (left half)
            var nameLabel = CreateTMP(bg, "Name", unit.DisplayName,
                new Vector2(0f, 0.4f), new Vector2(0.55f, 1f),
                14, accentColor, TextAlignmentOptions.MidlineLeft);
            nameLabel.fontStyle = FontStyles.Bold;
            nameLabel.margin = new Vector4(4, 0, 0, 0);

            // HP bar background
            var hpBarBgGO = CreatePanel(bg, "HpBg",
                new Vector2(0f, 0.05f), new Vector2(1f, 0.38f), HpBarBg);

            // HP bar fill
            var fillGO = CreatePanel(hpBarBgGO, "HpFill",
                new Vector2(0f, 0f), new Vector2(1f, 1f), accentColor);
            var fillImg = fillGO.GetComponent<Image>();

            // HP text (right side)
            var hpLabel = CreateTMP(bg, "HpText",
                $"{unit.CurrentHP}/{unit.MaxHP}",
                new Vector2(0.55f, 0.4f), new Vector2(1f, 1f),
                12, Color.white, TextAlignmentOptions.MidlineRight);
            hpLabel.margin = new Vector4(0, 0, 4, 0);

            return new UnitDisplay
            {
                Unit       = unit,
                NameLabel  = nameLabel,
                HpFill     = fillImg,
                HpLabel    = hpLabel,
                Background = bg.GetComponent<Image>()
            };
        }

        private static void UpdateDisplay(UnitDisplay d)
        {
            if (d?.Unit == null) return;

            if (!d.Unit.IsAlive)
            {
                if (d.Background != null) d.Background.color = new Color(0.08f, 0.08f, 0.1f, 1f);
                if (d.NameLabel  != null) d.NameLabel.color  = DeadColor;
                if (d.HpFill     != null) d.HpFill.color     = DeadColor;
                if (d.HpLabel    != null) d.HpLabel.text      = "DEAD";
            }
            else
            {
                if (d.HpFill != null)
                {
                    var rt = d.HpFill.GetComponent<RectTransform>();
                    rt.anchorMax = new Vector2(Mathf.Clamp01(d.Unit.HPFraction), 1f);
                }

                if (d.HpLabel != null)
                    d.HpLabel.text = $"{d.Unit.CurrentHP}/{d.Unit.MaxHP}";
            }
        }

        private void OnUnitDied(CombatUnit unit)
        {
            // Immediately grey out the dead unit on the event rather than waiting for RefreshDisplay
            var all = new List<UnitDisplay>(_defenderDisplays);
            all.AddRange(_enemyDisplays);

            foreach (var d in all)
            {
                if (d.Unit == unit) UpdateDisplay(d);
            }
        }

        // ── UI primitive helpers ──────────────────────────────────────────────────

        private static GameObject CreatePanel(
            GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin  = anchorMin;
            rt.anchorMax  = anchorMax;
            rt.offsetMin  = Vector2.zero;
            rt.offsetMax  = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.color = color;

            return go;
        }

        private static TextMeshProUGUI CreateTMP(
            GameObject parent, string name, string text,
            Vector2 anchorMin, Vector2 anchorMax,
            int fontSize, Color color,
            TextAlignmentOptions alignment)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text      = text;
            tmp.fontSize  = fontSize;
            tmp.color     = color;
            tmp.alignment = alignment;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }
    }
}
