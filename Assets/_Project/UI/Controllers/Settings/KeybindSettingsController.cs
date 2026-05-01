using System;
using UnityEngine;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Markyu.LastKernel
{
    [RequireComponent(typeof(UIDocument))]
    public sealed class KeybindSettingsController : MonoBehaviour
    {
        public static KeybindSettingsController Instance { get; private set; }

        private UIDocument    _doc;
        private VisualElement _backdrop;
        private VisualElement _list;
        private Button        _btnClose;
        private Button        _btnResetAll;

#if ENABLE_INPUT_SYSTEM
        private InputActionRebindingExtensions.RebindingOperation _rebindOp;
#endif

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;

            _doc = GetComponent<UIDocument>();
            var root = _doc.rootVisualElement;

            _backdrop    = root.Q<VisualElement>("keybind-backdrop");
            _list        = root.Q<VisualElement>("keybind-list");
            _btnClose    = root.Q<Button>("btn-close");
            _btnResetAll = root.Q<Button>("btn-reset-all");

            _btnClose?.RegisterCallback<ClickEvent>(_ => Close());
            _btnResetAll?.RegisterCallback<ClickEvent>(_ => ResetAll());

            _backdrop?.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.target == _backdrop) Close();
            });

            GameOptionsUI.OpenControlsMenuOverride = Open;

            SetVisible(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                if (GameOptionsUI.OpenControlsMenuOverride == (Action)Open)
                    GameOptionsUI.OpenControlsMenuOverride = null;
            }
#if ENABLE_INPUT_SYSTEM
            _rebindOp?.Cancel();
            _rebindOp?.Dispose();
#endif
        }

        public void Open()
        {
            BuildRows();
            SetVisible(true);
        }

        public void Close()
        {
#if ENABLE_INPUT_SYSTEM
            _rebindOp?.Cancel();
#endif
            SetVisible(false);
        }

        private void BuildRows()
        {
            _list.Clear();

#if ENABLE_INPUT_SYSTEM
            var handler = GameInputHandler.Instance;
            if (handler == null) return;

            foreach (var entry in handler.AllActions)
                _list.Add(CreateRow(entry));
#else
            _list.Add(new Label { text = "New Input System required for rebinding." });
#endif
        }

#if ENABLE_INPUT_SYSTEM
        private VisualElement CreateRow(ActionEntry entry)
        {
            var row = new VisualElement();
            row.style.flexDirection     = FlexDirection.Row;
            row.style.alignItems        = Align.Center;
            row.style.paddingTop        = row.style.paddingBottom = 4;
            row.style.paddingLeft       = row.style.paddingRight  = 12;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new StyleColor(new Color(1, 1, 1, 0.06f));

            var nameLabel = new Label(entry.DisplayName);
            nameLabel.AddToClassList("lk-label");
            nameLabel.style.width = 200;

            var bindingLabel = new Label(GetDisplayString(entry.Action));
            bindingLabel.AddToClassList("lk-label");
            bindingLabel.style.flexGrow = 1;

            var rebindBtn = new Button { text = "✎" };
            rebindBtn.AddToClassList("lk-button");
            rebindBtn.style.width      = 36;
            rebindBtn.style.marginLeft  = 4;
            rebindBtn.style.marginRight = 2;
            rebindBtn.RegisterCallback<ClickEvent>(_ => StartRebind(entry, bindingLabel, rebindBtn));

            var resetBtn = new Button { text = "↩" };
            resetBtn.AddToClassList("lk-button");
            resetBtn.style.width = 36;
            resetBtn.RegisterCallback<ClickEvent>(_ => ResetSingle(entry, bindingLabel));

            row.Add(nameLabel);
            row.Add(bindingLabel);
            row.Add(rebindBtn);
            row.Add(resetBtn);
            return row;
        }

        private void StartRebind(ActionEntry entry, Label bindingLabel, Button rebindBtn)
        {
            _rebindOp?.Cancel();
            _rebindOp?.Dispose();

            entry.Action.Disable();
            rebindBtn.SetEnabled(false);
            bindingLabel.text = "[ press a key... ]";

            _rebindOp = entry.Action
                .PerformInteractiveRebinding()
                .WithControlsExcluding("<Mouse>/delta")
                .WithControlsExcluding("<Mouse>/scroll")
                .WithControlsExcluding("<Mouse>/position")
                .WithCancelingThrough("<Keyboard>/escape")
                .OnComplete(op =>
                {
                    op.Dispose();
                    _rebindOp = null;
                    entry.Action.Enable();
                    bindingLabel.text = GetDisplayString(entry.Action);
                    rebindBtn.SetEnabled(true);
                    GameInputHandler.Instance?.SaveBindings();
                })
                .OnCancel(op =>
                {
                    op.Dispose();
                    _rebindOp = null;
                    entry.Action.Enable();
                    bindingLabel.text = GetDisplayString(entry.Action);
                    rebindBtn.SetEnabled(true);
                })
                .Start();
        }

        private void ResetSingle(ActionEntry entry, Label bindingLabel)
        {
            _rebindOp?.Cancel();
            GameInputHandler.Instance?.ResetBinding(entry.Action);
            bindingLabel.text = GetDisplayString(entry.Action);
        }

        private static string GetDisplayString(InputAction action)
        {
            if (action == null || action.bindings.Count == 0) return "—";
            return action.GetBindingDisplayString(0);
        }
#endif

        private void ResetAll()
        {
            GameInputHandler.Instance?.ResetAllBindings();
            BuildRows();
        }

        private void SetVisible(bool visible)
        {
            if (_backdrop != null)
                _backdrop.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
    }
}
