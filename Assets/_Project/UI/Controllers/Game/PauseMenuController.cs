using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// UI Toolkit replacement for the legacy UGUI PauseMenu.
    /// Handles: nav (resume / save / options / back-to-title),
    ///          save-slot picker with overwrite confirmation,
    ///          input lock and time freeze while paused.
    /// Setup: add to a GameObject with a UIDocument set to PauseMenuView.uxml.
    /// </summary>
    public sealed class PauseMenuController : UIToolkitScreenController
    {
        // ── Nav ────────────────────────────────────────────────────────────────

        private VisualElement _backdrop;
        private Label         _titleLabel;
        private Button        _resumeButton;
        private Button        _saveButton;
        private Button        _optionsButton;
        private Button        _backToTitleButton;

        // ── Save slots panel ───────────────────────────────────────────────────

        private VisualElement _saveSlotsPanel;
        private Label         _savePanelTitle;
        private VisualElement _saveSlotsContainer;
        private Label         _saveEmptyLabel;
        private Button        _newSaveSlotButton;
        private Button        _saveCloseButton;

        // ── Confirmation modal ─────────────────────────────────────────────────

        private VisualElement _confirmPanel;
        private Label         _confirmTitle;
        private Label         _confirmBody;
        private Button        _confirmCancel;
        private Button        _confirmAccept;
        private Action        _pendingConfirm;

        // ── State ──────────────────────────────────────────────────────────────

        private bool _isPaused;
        private int  _pendingSaveSlot;

        // ── Lifecycle ──────────────────────────────────────────────────────────

        protected override void OnEnable()
        {
            base.OnEnable();
            UIEventBus.OnResumeRequested += HandleResume;
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            UIEventBus.OnResumeRequested -= HandleResume;

            if (_isPaused)
            {
                _isPaused = false;
                if (InputManager.Instance != null) InputManager.Instance.RemoveLock(this);
                if (TimeManager.Instance != null) TimeManager.Instance.SetExternalPause(false);
            }
        }

        private void Update()
        {
            if (InputManager.Instance == null) return;
            if (!InputManager.Instance.WasPausePressedThisFrame()) return;
            if (DayCycleManager.Instance != null && DayCycleManager.Instance.IsEndingCycle) return;

            if (_isPaused)
                UIEventBus.RaiseResumeRequested();
            else
                SetPaused(true);
        }

        // ── Binding ────────────────────────────────────────────────────────────

        protected override void OnBind()
        {
            Document.sortingOrder = 100;

            _backdrop          = Root.Q<VisualElement>("pause-backdrop");
            _titleLabel        = Root.Q<Label>        ("lbl-pause-title");
            _resumeButton      = Root.Q<Button>       ("btn-resume");
            _saveButton        = Root.Q<Button>       ("btn-save");
            _optionsButton     = Root.Q<Button>       ("btn-options");
            _backToTitleButton = Root.Q<Button>       ("btn-back-to-title");

            _saveSlotsPanel     = Root.Q<VisualElement>("panel-save-slots");
            _savePanelTitle     = Root.Q<Label>        ("lbl-save-panel-title");
            _saveSlotsContainer = Root.Q<VisualElement>("save-slots-container");
            _saveEmptyLabel     = Root.Q<Label>        ("lbl-save-empty");
            _newSaveSlotButton  = Root.Q<Button>       ("btn-new-save-slot");
            _saveCloseButton    = Root.Q<Button>       ("btn-save-close");

            _confirmPanel  = Root.Q<VisualElement>("panel-confirm");
            _confirmTitle  = Root.Q<Label>        ("lbl-confirm-title");
            _confirmBody   = Root.Q<Label>        ("lbl-confirm-body");
            _confirmCancel = Root.Q<Button>       ("btn-confirm-cancel");
            _confirmAccept = Root.Q<Button>       ("btn-confirm-accept");

            if (_resumeButton      != null) _resumeButton.clicked      += UIEventBus.RaiseResumeRequested;
            if (_saveButton        != null) _saveButton.clicked        += OpenSavePanel;
            if (_optionsButton     != null) _optionsButton.clicked     += UIEventBus.RaiseOptionsRequested;
            if (_backToTitleButton != null) _backToTitleButton.clicked += OnBackToTitleClicked;

            if (_newSaveSlotButton != null) _newSaveSlotButton.clicked += OnNewSaveSlotClicked;
            if (_saveCloseButton   != null) _saveCloseButton.clicked   += CloseSavePanel;
            if (_confirmCancel     != null) _confirmCancel.clicked     += CloseConfirm;
            if (_confirmAccept     != null) _confirmAccept.clicked     += AcceptConfirm;

            if (_backdrop != null) _backdrop.EnableInClassList("lk-hidden", true);
        }

        // ── Localization ───────────────────────────────────────────────────────

        public override void OnLocalizationRefresh()
        {
            base.OnLocalizationRefresh();

            if (_titleLabel        != null) _titleLabel.text        = GameLocalization.Get("pause.header");
            if (_resumeButton      != null) _resumeButton.text      = GameLocalization.Get("pause.resume");
            if (_saveButton        != null) _saveButton.text        = GameLocalization.Get("pause.save");
            if (_optionsButton     != null) _optionsButton.text     = GameLocalization.Get("options.header");
            if (_backToTitleButton != null) _backToTitleButton.text = GameLocalization.Get("pause.backToTitle");

            if (_savePanelTitle    != null) _savePanelTitle.text    = GameLocalization.Get("save.saveHeader");
            if (_saveEmptyLabel    != null) _saveEmptyLabel.text    = GameLocalization.Get("title.noSaves");
            if (_newSaveSlotButton != null) _newSaveSlotButton.text = GameLocalization.Get("save.newSlot");
            if (_saveCloseButton   != null) _saveCloseButton.text   = GameLocalization.Get("common.closeButton");

            if (_confirmCancel != null) _confirmCancel.text = GameLocalization.Get("common.cancelButton");
            if (_confirmAccept != null) _confirmAccept.text = GameLocalization.Get("common.confirmButton");
        }

        // ── Nav ────────────────────────────────────────────────────────────────

        private void HandleResume() => SetPaused(false);

        private void OnBackToTitleClicked()
        {
            CloseSavePanel();
            UIEventBus.RaiseBackToTitleRequested();
        }

        private void SetPaused(bool paused)
        {
            _isPaused = paused;
            if (_backdrop != null) _backdrop.EnableInClassList("lk-hidden", !paused);
            if (TimeManager.Instance != null) TimeManager.Instance.SetExternalPause(paused);

            if (paused)
            {
                if (InputManager.Instance != null) InputManager.Instance.AddLock(this);
            }
            else
            {
                CloseSavePanel();
                if (InputManager.Instance != null) InputManager.Instance.RemoveLock(this);
            }
        }

        // ── Save panel ─────────────────────────────────────────────────────────

        private void OpenSavePanel()
        {
            RebuildSaveSlots();
            if (_saveSlotsPanel != null) _saveSlotsPanel.RemoveFromClassList("lk-hidden");
        }

        private void CloseSavePanel()
        {
            CloseConfirm();
            if (_saveSlotsPanel != null) _saveSlotsPanel.AddToClassList("lk-hidden");
        }

        private void RebuildSaveSlots()
        {
            if (_saveSlotsContainer != null)
            {
                _saveSlotsContainer.Clear();

                GameDirector director = GameDirector.Instance;
                if (director != null && director.SavedGames != null)
                {
                    var saves = new List<GameData>(director.SavedGames.Values);
                    saves.Sort(CompareBySlot);

                    GameData current = director.GameData;
                    foreach (GameData save in saves)
                        _saveSlotsContainer.Add(BuildSaveSlotRow(save, current));
                }
            }

            bool hasSlots = _saveSlotsContainer != null && _saveSlotsContainer.childCount > 0;
            if (_saveEmptyLabel != null)
                _saveEmptyLabel.EnableInClassList("lk-hidden", hasSlots);
        }

        private static int CompareBySlot(GameData a, GameData b)
        {
            return a.SlotNumber.CompareTo(b.SlotNumber);
        }

        private VisualElement BuildSaveSlotRow(GameData data, GameData current)
        {
            bool isCurrent = current != null && data.SlotNumber == current.SlotNumber;

            var row = new VisualElement();
            row.AddToClassList("lk-save-slot");

            string suffix = string.Empty;
            if (isCurrent)
                suffix = " " + GameLocalization.Get("save.currentSlot");

            var label = new Label();
            label.text = BuildSlotText(data) + suffix;
            label.AddToClassList("lk-save-slot__label");

            var btn = new Button();
            btn.userData = data.SlotNumber;
            if (isCurrent)
                btn.text = GameLocalization.Get("pause.save");
            else
                btn.text = GameLocalization.Get("save.overwrite");
            btn.AddToClassList("lk-button");
            if (!isCurrent) btn.AddToClassList("lk-button--danger");
            btn.RegisterCallback<ClickEvent>(OnSaveSlotClicked);

            var actions = new VisualElement();
            actions.AddToClassList("lk-save-slot__actions");
            actions.Add(btn);

            row.Add(label);
            row.Add(actions);
            return row;
        }

        private void OnSaveSlotClicked(ClickEvent evt)
        {
            Button b = evt.currentTarget as Button;
            if (b == null) return;
            if (!(b.userData is int)) return;
            ConfirmThenSave((int)b.userData);
        }

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

        private void OnNewSaveSlotClicked()
        {
            ConfirmThenSave(0);
        }

        private void ConfirmThenSave(int targetSlot)
        {
            GameDirector director = GameDirector.Instance;
            bool directorHasData = director != null && director.GameData != null;
            bool isCurrent = targetSlot == 0 || (directorHasData && director.GameData.SlotNumber == targetSlot);

            if (isCurrent)
            {
                UIEventBus.RaiseSaveToSlotRequested(targetSlot);
                CloseSavePanel();
            }
            else
            {
                _pendingSaveSlot = targetSlot;
                ShowConfirm(
                    GameLocalization.Get("save.overwriteTitle"),
                    GameLocalization.Format("save.overwriteBody", targetSlot),
                    ExecutePendingSave);
            }
        }

        private void ExecutePendingSave()
        {
            UIEventBus.RaiseSaveToSlotRequested(_pendingSaveSlot);
            CloseSavePanel();
        }

        // ── Confirm modal ──────────────────────────────────────────────────────

        private void ShowConfirm(string title, string body, Action onAccept)
        {
            if (_confirmTitle != null) _confirmTitle.text = title;
            if (_confirmBody  != null) _confirmBody.text  = body;
            _pendingConfirm = onAccept;
            if (_confirmPanel != null) _confirmPanel.RemoveFromClassList("lk-hidden");
        }

        private void CloseConfirm()
        {
            _pendingConfirm = null;
            if (_confirmPanel != null) _confirmPanel.AddToClassList("lk-hidden");
        }

        private void AcceptConfirm()
        {
            Action action = _pendingConfirm;
            CloseConfirm();
            if (action != null) action.Invoke();
        }
    }
}
