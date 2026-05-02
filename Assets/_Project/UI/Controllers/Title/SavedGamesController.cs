using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Controls the Saved Games sub-panel (#panel-saved-games).
    /// Dynamically creates a slot row per GameData entry.
    /// Load/delete/clear actions are raised via UIEventBus; the
    /// UIEventBusBridge routes them to GameDirector.
    /// </summary>
    public sealed class SavedGamesController : UIToolkitComponentController
    {
        private readonly ModalController _modal;

        private Label          _titleLabel;
        private Label          _subtitleLabel;
        private VisualElement  _savesContainer;
        private Label          _emptyLabel;
        private Button         _clearButton;
        private Button         _closeButton;

        private readonly List<GameData> _trackedSlots = new();

        public SavedGamesController(ModalController modal)
        {
            _modal = modal;
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            _titleLabel     = Root.Q<Label>        ("lbl-saves-title");
            _subtitleLabel  = Root.Q<Label>        ("lbl-saves-subtitle");
            _savesContainer = Root.Q<VisualElement> ("saves-container");
            _emptyLabel     = Root.Q<Label>         ("lbl-saves-empty");
            _clearButton    = Root.Q<Button>        ("btn-saves-clear");
            _closeButton    = Root.Q<Button>        ("btn-saves-close");

            _clearButton.clicked += ShowClearConfirmation;
            _closeButton.clicked += Hide;
        }

        // ── API ────────────────────────────────────────────────────────────────

        public void Show()
        {
            Root.RemoveFromClassList("lk-hidden");
            RebuildSlots();
            OnLocalizationRefresh();
        }

        public void Hide() => Root.AddToClassList("lk-hidden");

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            if (_titleLabel    != null) _titleLabel.text    = GameLocalization.Get("title.loadHeader");
            if (_subtitleLabel != null) _subtitleLabel.text = GameLocalization.GetOptional("title.loadSubtitle", "Select a Save File");
            if (_emptyLabel    != null) _emptyLabel.text    = GameLocalization.Get("title.noSaves");
            if (_clearButton != null) _clearButton.text = GameLocalization.Get("title.clearSaves");
            if (_closeButton != null) _closeButton.text = GameLocalization.Get("common.closeButton");
        }

        // ── Private ────────────────────────────────────────────────────────────

        private void RebuildSlots()
        {
            _savesContainer.Clear();
            _trackedSlots.Clear();

            GameDirector director = GameDirector.Instance;
            if (director != null)
            {
                foreach (GameData save in director.SavedGames.Values.OrderBy(d => d.SlotNumber))
                {
                    _savesContainer.Add(BuildSlotRow(save));
                    _trackedSlots.Add(save);
                }
            }

            _emptyLabel?.EnableInClassList("lk-hidden", _trackedSlots.Count > 0);
        }

        private VisualElement BuildSlotRow(GameData data)
        {
            var row = new VisualElement();
            row.AddToClassList("lk-save-slot");

            var label = new Label { text = BuildSlotText(data) };
            label.AddToClassList("lk-save-slot__label");

            var actions = new VisualElement();
            actions.AddToClassList("lk-save-slot__actions");

            var loadBtn = new Button(() => LoadGame(data))
            {
                text = GameLocalization.Get("common.loadButton")
            };
            loadBtn.AddToClassList("lk-button");

            var deleteBtn = new Button(() => ShowDeleteConfirmation(data))
            {
                text = GameLocalization.Get("common.deleteButton")
            };
            deleteBtn.AddToClassList("lk-button");
            deleteBtn.AddToClassList("lk-button--danger");

            actions.Add(loadBtn);
            actions.Add(deleteBtn);
            row.Add(label);
            row.Add(actions);
            return row;
        }

        private static string BuildSlotText(GameData data)
        {
            string progressSuffix = string.Empty;
            if (data.TryGetScene(out var sceneData))
                progressSuffix = GameLocalization.Format("save.progressSuffix", sceneData.QuestProgress);

            var sb = new StringBuilder();
            sb.Append(GameLocalization.Format("save.slotLabel",
                data.SlotNumber, data.CurrentScene, progressSuffix));
            sb.Append(GameLocalization.Format("save.lastSaved",
                data.LastSaved.ToString("g", GameLocalization.CurrentCulture)));
            return sb.ToString();
        }

        private void LoadGame(GameData data)
        {
            UIEventBus.RaiseLoadGame(data);
            Hide();
        }

        private void ShowDeleteConfirmation(GameData data)
        {
            _modal.Show(
                GameLocalization.Get("save.deleteTitle"),
                GameLocalization.Format("save.deleteBody", data.SlotNumber),
                () =>
                {
                    UIEventBus.RaiseDeleteGame(data);
                    RebuildSlots();
                });
        }

        private void ShowClearConfirmation()
        {
            _modal.Show(
                GameLocalization.Get("save.clearTitle"),
                GameLocalization.Get("save.clearBody"),
                () =>
                {
                    UIEventBus.RaiseClearAllSaves();
                    RebuildSlots();
                });
        }
    }
}
