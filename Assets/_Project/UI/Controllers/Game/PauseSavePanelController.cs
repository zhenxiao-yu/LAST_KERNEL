using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the Save Slots sub-panel inside PauseMenuView.
    /// Owned by PauseMenuController; receives ShowConfirm as a delegate so it
    /// can request overwrite confirmation without knowing about the confirm panel.
    /// </summary>
    public sealed class PauseSavePanelController : UIToolkitComponentController
    {
        private readonly Action<string, string, Action> _showConfirm;

        private VisualElement _panel;
        private Label         _panelTitle;
        private VisualElement _slotsContainer;
        private Label         _emptyLabel;
        private Button        _newSlotButton;
        private Button        _closeButton;

        private int _pendingSaveSlot;

        public PauseSavePanelController(Action<string, string, Action> showConfirm)
        {
            _showConfirm = showConfirm;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _panel          = Root;
            _panelTitle     = Root.Q<Label>        ("lbl-save-panel-title");
            _slotsContainer = Root.Q<VisualElement>("save-slots-container");
            _emptyLabel     = Root.Q<Label>        ("lbl-save-empty");
            _newSlotButton  = Root.Q<Button>       ("btn-new-save-slot");
            _closeButton    = Root.Q<Button>       ("btn-save-close");

            if (_newSlotButton != null) _newSlotButton.clicked += OnNewSlotClicked;
            if (_closeButton   != null) _closeButton.clicked   += Close;
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_panelTitle    != null) _panelTitle.text    = GameLocalization.Get("save.saveHeader");
            if (_emptyLabel    != null) _emptyLabel.text    = GameLocalization.Get("title.noSaves");
            if (_newSlotButton != null) _newSlotButton.text = GameLocalization.Get("save.newSlot");
            if (_closeButton   != null) _closeButton.text   = GameLocalization.Get("common.closeButton");
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Open()
        {
            RebuildSlots();
            if (_panel != null)
            {
                bool wasHidden = _panel.ClassListContains("lk-hidden");
                _panel.RemoveFromClassList("lk-hidden");
                if (wasHidden) LKUIInteractionPolisher.PlayPanelOpen();
            }
        }

        public void Close()
        {
            if (_panel != null)
            {
                bool wasVisible = !_panel.ClassListContains("lk-hidden");
                _panel.AddToClassList("lk-hidden");
                if (wasVisible) LKUIInteractionPolisher.PlayPanelClose();
            }
        }

        // ── Save slots ─────────────────────────────────────────────────────────

        private void RebuildSlots()
        {
            if (_slotsContainer == null) return;

            _slotsContainer.Clear();

            GameDirector director = GameDirector.Instance;
            if (director != null && director.SavedGames != null)
            {
                var saves = new List<GameData>(director.SavedGames.Values);
                saves.Sort(CompareBySlot);

                GameData current = director.GameData;
                foreach (GameData save in saves)
                    _slotsContainer.Add(BuildSlotRow(save, current));
            }

            bool hasSlots = _slotsContainer.childCount > 0;
            if (_emptyLabel != null) _emptyLabel.EnableInClassList("lk-hidden", hasSlots);
            LKUIInteractionPolisher.Refresh(Root);
        }

        private VisualElement BuildSlotRow(GameData data, GameData current)
        {
            bool isCurrent = current != null && data.SlotNumber == current.SlotNumber;

            var row = new VisualElement();
            row.AddToClassList("lk-save-slot");

            string suffix = isCurrent ? " " + GameLocalization.Get("save.currentSlot") : string.Empty;

            var label = new Label();
            label.text = BuildSlotText(data) + suffix;
            label.AddToClassList("lk-save-slot__label");

            var btn = new Button();
            btn.userData = data.SlotNumber;
            btn.text     = isCurrent
                ? GameLocalization.Get("pause.save")
                : GameLocalization.Get("save.overwrite");
            btn.AddToClassList("lk-button");
            btn.AddToClassList(isCurrent ? "lk-button--utility" : "lk-button--critical");
            btn.RegisterCallback<ClickEvent>(OnSlotClicked);

            var actions = new VisualElement();
            actions.AddToClassList("lk-save-slot__actions");
            actions.Add(btn);

            row.Add(label);
            row.Add(actions);
            return row;
        }

        private void OnSlotClicked(ClickEvent evt)
        {
            Button b = evt.currentTarget as Button;
            if (b == null) return;
            if (!(b.userData is int)) return;
            ConfirmThenSave((int)b.userData);
        }

        private void OnNewSlotClicked()
        {
            ConfirmThenSave(0);
        }

        private void ConfirmThenSave(int targetSlot)
        {
            GameDirector director = GameDirector.Instance;
            bool directorHasData  = director != null && director.GameData != null;
            bool isCurrent        = targetSlot == 0
                || (directorHasData && director.GameData.SlotNumber == targetSlot);

            if (isCurrent)
            {
                UIEventBus.RaiseSaveToSlotRequested(targetSlot);
                Close();
            }
            else
            {
                _pendingSaveSlot = targetSlot;
                if (_showConfirm != null)
                    _showConfirm(
                        GameLocalization.Get("save.overwriteTitle"),
                        GameLocalization.Format("save.overwriteBody", targetSlot),
                        ExecutePendingSave);
            }
        }

        private void ExecutePendingSave()
        {
            UIEventBus.RaiseSaveToSlotRequested(_pendingSaveSlot);
            Close();
        }

        // ── Helpers ────────────────────────────────────────────────────────────

        private static string BuildSlotText(GameData data)
        {
            string progressSuffix = string.Empty;
            SceneData scene;
            if (data.TryGetScene(out scene))
                progressSuffix = GameLocalization.Format("save.progressSuffix", scene.QuestProgress);

            var sb = new StringBuilder();
            sb.Append(GameLocalization.Format("save.slotLabel",
                data.SlotNumber, data.CurrentScene, progressSuffix));
            sb.Append(GameLocalization.Format("save.lastSaved",
                data.LastSaved.ToString("g", GameLocalization.CurrentCulture)));
            return sb.ToString();
        }

        private static int CompareBySlot(GameData a, GameData b)
        {
            return a.SlotNumber.CompareTo(b.SlotNumber);
        }
    }
}
