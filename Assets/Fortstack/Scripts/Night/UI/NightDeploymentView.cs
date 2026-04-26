// NightDeploymentView — runtime-built full-screen overlay for the dusk deployment phase.
//
// Responsibilities:
//   - Show all eligible defenders (living Character cards) as selectable entries
//   - Track selection order (click order = lane order, index 0 = frontline)
//   - Show a lane-order preview panel that updates live
//   - Expose Confirm and Skip buttons; fire the appropriate callback when pressed
//   - Highlight selected cards on the board via CardInstance.SetHighlighted
//   - Clean up all card highlights on Close()
//
// Explicitly does NOT:
//   - Run or influence combat simulation
//   - Mutate RunState or board card counts
//   - Permanently remove cards from the board
//   - Own any game-state transitions

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Markyu.LastKernel
{
    public class NightDeploymentView : MonoBehaviour
    {
        // ── Colour palette — matches CombatLaneView for visual consistency ─────────
        private static readonly Color BgColor          = new Color(0.05f, 0.05f, 0.08f, 0.95f);
        private static readonly Color PanelColor       = new Color(0.08f, 0.08f, 0.12f, 1f);
        private static readonly Color HeaderColor      = new Color(0.9f,  0.7f,  0.2f,  1f);
        private static readonly Color ColHeaderColor   = new Color(0.2f,  0.6f,  1.0f,  1f);
        private static readonly Color UnselectedBg     = new Color(0.10f, 0.10f, 0.15f, 1f);
        private static readonly Color SelectedBg       = new Color(0.08f, 0.22f, 0.12f, 1f);
        private static readonly Color LabelSelected    = new Color(0.35f, 0.95f, 0.45f, 1f);
        private static readonly Color LabelUnselected  = new Color(0.60f, 0.60f, 0.65f, 1f);
        private static readonly Color LaneSlotFilled   = new Color(0.25f, 0.65f, 1.0f,  1f);
        private static readonly Color LaneSlotEmpty    = new Color(0.30f, 0.30f, 0.35f, 1f);
        private static readonly Color ConfirmBgColor   = new Color(0.10f, 0.45f, 0.18f, 1f);
        private static readonly Color SkipBgColor      = new Color(0.30f, 0.12f, 0.10f, 1f);

        // Maximum defender entries rendered in the left column without scrolling.
        // Cards beyond this cap are auto-included when the player confirms
        // (they are listed in ConfirmedPlan as selected by default if they exceed the cap).
        private const int MaxVisible = 8;

        // ── Runtime state ─────────────────────────────────────────────────────────
        private Canvas       _canvas;
        private GameObject   _root;

        private List<CardInstance>                     _eligible;
        private readonly List<CardInstance>            _selectedInOrder  = new();
        private readonly Dictionary<CardInstance, EntryDisplay> _entries = new();
        private readonly List<TextMeshProUGUI>         _laneNameLabels   = new();

        private TextMeshProUGUI _confirmLabel;

        private Action<List<CardInstance>> _onConfirm;
        private Action                     _onCancel;

        // Per-entry UI references
        private class EntryDisplay
        {
            public CardInstance     Card;
            public Image            Background;
            public TextMeshProUGUI  NameLabel;
            public TextMeshProUGUI  StatsLabel;
            public TextMeshProUGUI  SlotIndexLabel;
        }

        // ── Public API ────────────────────────────────────────────────────────────

        /// <summary>
        /// Opens the deployment UI.
        /// <paramref name="onConfirm"/> receives the player's selected defenders in lane order.
        /// <paramref name="onCancel"/> is fired when the player presses Skip (auto-deploy will be used).
        /// </summary>
        public void Open(
            List<CardInstance>            eligible,
            Action<List<CardInstance>>    onConfirm,
            Action                        onCancel)
        {
            _eligible = eligible;
            _onConfirm = onConfirm;
            _onCancel  = onCancel;

            _selectedInOrder.Clear();
            _entries.Clear();
            _laneNameLabels.Clear();

            EnsureCanvas();
            BuildUI();
            _root.SetActive(true);
        }

        /// <summary>
        /// Hides the panel and removes all board card highlights.
        /// Called by NightDeploymentController after the player confirms or skips.
        /// </summary>
        public void Close()
        {
            if (_eligible != null)
            {
                foreach (var card in _eligible)
                {
                    if (card != null) card.SetHighlighted(false);
                }
            }

            if (_root != null) _root.SetActive(false);

            _selectedInOrder.Clear();
            _entries.Clear();
        }

        // ── Canvas setup ──────────────────────────────────────────────────────────

        private void EnsureCanvas()
        {
            if (_canvas != null) return;

            var go = new GameObject("NightDeploymentCanvas");
            go.transform.SetParent(transform, false);

            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 99; // below CombatLaneView (100) if both somehow coexist

            // Scale with reference resolution so text sizes are consistent
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode        = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode    = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
        }

        // ── UI construction ───────────────────────────────────────────────────────

        private void BuildUI()
        {
            if (_root != null) Destroy(_root);

            // Full-screen dark overlay
            _root = CreatePanel(_canvas.gameObject, "DeploymentRoot",
                Vector2.zero, Vector2.one, BgColor);

            BuildHeader();
            BuildEligibleColumn();
            BuildLaneColumn();
            BuildFooter();
        }

        private void BuildHeader()
        {
            var hdr = CreateTMP(_root, "Header",
                GameLocalization.GetOptional("dusk.deployTitle", "DUSK — DEPLOYMENT"),
                new Vector2(0f, 0.92f), new Vector2(1f, 1f),
                26, HeaderColor, TextAlignmentOptions.Center);
            hdr.fontStyle = FontStyles.Bold;
        }

        private void BuildEligibleColumn()
        {
            var col = CreatePanel(_root, "EligibleCol",
                new Vector2(0.02f, 0.11f), new Vector2(0.49f, 0.91f), PanelColor);

            CreateTMP(col, "ColHeader",
                GameLocalization.GetOptional("dusk.eligibleHeader", "ELIGIBLE DEFENDERS"),
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                16, ColHeaderColor, TextAlignmentOptions.Center);

            if (_eligible == null || _eligible.Count == 0)
            {
                CreateTMP(col, "EmptyNote",
                    GameLocalization.GetOptional("dusk.noDefenders", "No eligible defenders."),
                    new Vector2(0.05f, 0.4f), new Vector2(0.95f, 0.6f),
                    14, LabelUnselected, TextAlignmentOptions.Center);
                return;
            }

            int visible    = Mathf.Min(_eligible.Count, MaxVisible);
            float rowH     = 0.90f / visible;

            for (int i = 0; i < visible; i++)
            {
                var card = _eligible[i];
                float yMax = 0.92f - i * rowH;
                float yMin = yMax - rowH + 0.01f;

                var display = BuildEntry(col, card,
                    new Vector2(0.02f, yMin), new Vector2(0.98f, yMax));
                _entries[card] = display;
            }

            if (_eligible.Count > MaxVisible)
            {
                int overflow = _eligible.Count - MaxVisible;
                CreateTMP(col, "OverflowNote",
                    $"+ {overflow} more — included automatically",
                    new Vector2(0.02f, 0.005f), new Vector2(0.98f, rowH * 0.6f),
                    11, LabelUnselected, TextAlignmentOptions.Center);
            }
        }

        private EntryDisplay BuildEntry(
            GameObject parent, CardInstance card,
            Vector2 anchorMin, Vector2 anchorMax)
        {
            var bg = CreatePanel(parent, $"Entry_{card.Definition?.DisplayName ?? "?"}",
                anchorMin, anchorMax, UnselectedBg);

            // Name (left 58 %)
            var nameLabel = CreateTMP(bg, "Name",
                card.Definition?.DisplayName ?? "?",
                new Vector2(0f, 0.45f), new Vector2(0.58f, 1f),
                13, LabelUnselected, TextAlignmentOptions.MidlineLeft);
            nameLabel.margin = new Vector4(6, 0, 0, 0);
            nameLabel.fontStyle = FontStyles.Bold;

            // Stats: HP / ATK (centre 26 %)
            int hp  = card.CurrentHealth;
            int atk = card.Stats?.Attack.Value ?? 0;
            var statsLabel = CreateTMP(bg, "Stats",
                $"HP {hp}  ATK {atk}",
                new Vector2(0.58f, 0.45f), new Vector2(0.84f, 1f),
                11, LabelUnselected, TextAlignmentOptions.MidlineRight);

            // Lane-position badge (right 16 %) — shown only when selected
            var slotLabel = CreateTMP(bg, "SlotBadge",
                "",
                new Vector2(0.84f, 0f), new Vector2(1f, 1f),
                11, HeaderColor, TextAlignmentOptions.Center);
            slotLabel.fontStyle = FontStyles.Bold;

            // Full-panel button — Unity Button handles click; colours override Image
            var btn = bg.AddComponent<Button>();
            btn.targetGraphic = bg.GetComponent<Image>();
            var cb = btn.colors;
            cb.normalColor      = Color.white;
            cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
            cb.pressedColor     = new Color(0.80f, 0.80f, 0.80f, 1f);
            btn.colors = cb;
            btn.onClick.AddListener(() => ToggleDefender(card));

            return new EntryDisplay
            {
                Card           = card,
                Background     = bg.GetComponent<Image>(),
                NameLabel      = nameLabel,
                StatsLabel     = statsLabel,
                SlotIndexLabel = slotLabel,
            };
        }

        private void BuildLaneColumn()
        {
            var col = CreatePanel(_root, "LaneCol",
                new Vector2(0.51f, 0.11f), new Vector2(0.98f, 0.91f), PanelColor);

            CreateTMP(col, "ColHeader",
                GameLocalization.GetOptional("dusk.laneHeader", "COMMITTED TO LANE"),
                new Vector2(0f, 0.94f), new Vector2(1f, 1f),
                16, ColHeaderColor, TextAlignmentOptions.Center);

            // Show the same number of slots as MaxVisible defenders
            float rowH = 0.90f / MaxVisible;

            for (int i = 0; i < MaxVisible; i++)
            {
                float yMax = 0.92f - i * rowH;
                float yMin = yMax - rowH + 0.01f;

                var slotBg = CreatePanel(col, $"Slot_{i}",
                    new Vector2(0.03f, yMin), new Vector2(0.97f, yMax),
                    new Color(0.06f, 0.06f, 0.10f, 1f));

                // Position label: FRONT for index 0, #N for the rest
                string posText = i == 0
                    ? GameLocalization.GetOptional("dusk.frontline", "FRONT")
                    : $"#{i + 1}";

                CreateTMP(slotBg, "Pos", posText,
                    new Vector2(0f, 0f), new Vector2(0.22f, 1f),
                    11, LaneSlotEmpty, TextAlignmentOptions.Center);

                // Defender name label — populated by RefreshLanePreview()
                var nameLabel = CreateTMP(slotBg, "Name",
                    GameLocalization.GetOptional("dusk.emptySlot", "—"),
                    new Vector2(0.24f, 0f), new Vector2(1f, 1f),
                    13, LaneSlotEmpty, TextAlignmentOptions.MidlineLeft);
                nameLabel.margin = new Vector4(4, 0, 0, 0);

                _laneNameLabels.Add(nameLabel);
            }
        }

        private void BuildFooter()
        {
            // CONFIRM button (left-of-centre)
            var confirmBg = CreatePanel(_root, "ConfirmBtn",
                new Vector2(0.28f, 0.01f), new Vector2(0.60f, 0.10f), ConfirmBgColor);

            _confirmLabel = CreateTMP(confirmBg, "ConfirmLabel",
                BuildConfirmText(),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                17, Color.white, TextAlignmentOptions.Center);
            _confirmLabel.fontStyle = FontStyles.Bold;

            var confirmBtn = confirmBg.AddComponent<Button>();
            confirmBtn.targetGraphic = confirmBg.GetComponent<Image>();
            confirmBtn.onClick.AddListener(Confirm);

            // SKIP button (right-of-centre) — uses automatic plan
            var skipBg = CreatePanel(_root, "SkipBtn",
                new Vector2(0.62f, 0.01f), new Vector2(0.83f, 0.10f), SkipBgColor);

            CreateTMP(skipBg, "SkipLabel",
                GameLocalization.GetOptional("dusk.skipButton", "SKIP (AUTO)"),
                new Vector2(0f, 0f), new Vector2(1f, 1f),
                15, new Color(1f, 0.6f, 0.6f, 1f), TextAlignmentOptions.Center);

            var skipBtn = skipBg.AddComponent<Button>();
            skipBtn.targetGraphic = skipBg.GetComponent<Image>();
            skipBtn.onClick.AddListener(Cancel);
        }

        // ── Interaction ───────────────────────────────────────────────────────────

        private void ToggleDefender(CardInstance card)
        {
            if (card == null || card.CurrentHealth <= 0) return;

            if (_selectedInOrder.Contains(card))
            {
                _selectedInOrder.Remove(card);
                card.SetHighlighted(false);
            }
            else
            {
                _selectedInOrder.Add(card);
                card.SetHighlighted(true);
            }

            AudioManager.Instance?.PlaySFX(AudioId.Click);

            RefreshEntryDisplays();
            RefreshLanePreview();
            RefreshConfirmButton();
        }

        private void Confirm()
        {
            // Pass a snapshot of the selection so the view can safely clear its state.
            _onConfirm?.Invoke(new List<CardInstance>(_selectedInOrder));
        }

        private void Cancel()
        {
            _onCancel?.Invoke();
        }

        // ── Refresh helpers ───────────────────────────────────────────────────────

        private void RefreshEntryDisplays()
        {
            foreach (var (card, d) in _entries)
            {
                bool isSelected = _selectedInOrder.Contains(card);
                int  laneIdx    = _selectedInOrder.IndexOf(card);

                d.Background.color = isSelected ? SelectedBg    : UnselectedBg;
                d.NameLabel.color  = isSelected ? LabelSelected  : LabelUnselected;
                d.StatsLabel.color = isSelected ? LabelSelected  : LabelUnselected;

                d.SlotIndexLabel.text = isSelected
                    ? (laneIdx == 0
                        ? GameLocalization.GetOptional("dusk.frontline", "FRONT")
                        : $"#{laneIdx + 1}")
                    : "";
            }
        }

        private void RefreshLanePreview()
        {
            for (int i = 0; i < _laneNameLabels.Count; i++)
            {
                var label = _laneNameLabels[i];
                if (i < _selectedInOrder.Count)
                {
                    label.text  = _selectedInOrder[i].Definition?.DisplayName ?? "?";
                    label.color = LaneSlotFilled;
                }
                else
                {
                    label.text  = GameLocalization.GetOptional("dusk.emptySlot", "—");
                    label.color = LaneSlotEmpty;
                }
            }
        }

        private void RefreshConfirmButton()
        {
            if (_confirmLabel != null)
                _confirmLabel.text = BuildConfirmText();
        }

        private string BuildConfirmText()
        {
            int n = _selectedInOrder.Count;
            return n == 0
                ? GameLocalization.GetOptional("dusk.confirmNone", "CONFIRM (no defenders)")
                : $"CONFIRM ({n} defender{(n == 1 ? "" : "s")})";
        }

        // ── UI primitive helpers (mirror CombatLaneView pattern) ──────────────────

        private static GameObject CreatePanel(
            GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax,
            Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

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
            tmp.text         = text;
            tmp.fontSize     = fontSize;
            tmp.color        = color;
            tmp.alignment    = alignment;
            tmp.overflowMode = TextOverflowModes.Ellipsis;

            return tmp;
        }
    }
}
