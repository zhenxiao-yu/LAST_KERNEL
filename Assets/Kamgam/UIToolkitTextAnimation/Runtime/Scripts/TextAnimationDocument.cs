using System;
using System.Collections;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// The TextAnimationDocument needs to be added to any UI Document in the scene that want's to use text animations.<br />
    /// It also auto-creates a Panel for the visual tree root (at RUNTIME only). The Document and panel are split because 
    /// we also use the Panel in the UI Builder which has no UIDocument to begin with (see UIEditorPanelObserver).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    [RequireComponent(typeof(TextAnimationsProvider))]
    public partial class TextAnimationDocument : MonoBehaviour
    {
        protected TextAnimationPanel _panel;
        public TextAnimationPanel Panel
        {
            get
            {
                if (_panel == null)
                {
                    _panel = new TextAnimationPanel(Document.rootVisualElement, TextAnimationsProvider.GetAnimations());
                }
                return _panel;
            }

            private set
            {
                _panel = value;
            }
        }

        protected Action _onLateEnable;

        protected Coroutine _waitForPanelToBeReadyCoroutine;

        protected bool _isReady = false;

        protected UIDocument _document;
        public UIDocument Document
        {
            get
            {
                if (_document == null)
                {
                    _document = this.GetComponent<UIDocument>();
                }
                return _document;
            }
        }

        public TextAnimations Configs
        {
            get
            {
                return Panel.Configs;
            }
        }

        public void Awake()
        {
            if (!_documents.Contains(this))
                _documents.Add(this);

            if (Panel.Configs == null)
            {
                Panel.Configs = TextAnimationsProvider.GetAnimations();
            }
        }

        public void OnEnable()
        {
            if (!_documents.Contains(this))
                _documents.Add(this);

            Panel.RootVisualElement = Document.rootVisualElement;

            // Wait for panel to become ready
            if (Panel.RootVisualElement != null && Panel.RootVisualElement.panel != null)
            {
                onPanelIsReady();
            }
            else
            {
                if (_waitForPanelToBeReadyCoroutine != null)
                    StopCoroutine(_waitForPanelToBeReadyCoroutine);
                _waitForPanelToBeReadyCoroutine = StartCoroutine(waitForPanelToBeAttached());
            }
        }

        protected IEnumerator waitForPanelToBeAttached()
        {
            while (Panel.RootVisualElement == null || Panel.RootVisualElement.panel == null)
            {
                yield return null;
            }

            onPanelIsReady();
        }

        private void onPanelIsReady()
        {
            Panel.Enable();

            _onLateEnable?.Invoke();
            _isReady = true;
        }

        public void OnDisable()
        {
            _isReady = false;
        }

        public void OnDestroy()
        {
            Panel?.Destroy();

            if (_documents.Contains(this))
                _documents.Remove(this);
        }

        /// <summary>
        /// Registers the on enable callback. This ensures all the manipulators are already added when this called.<br />
        /// If the document is already enabled then it is called immediately.<br />
        /// Use this if you want to do some changes on the manipulators.
        /// </summary>
        /// <param name="action"></param>
        public void RegisterOnEnable(Action action)
        {
            if (action == null)
                return;

            _onLateEnable -= action;
            _onLateEnable += action;

            // If already enabled the call this action immediately.
            if (_isReady)
            {
                action.Invoke();
            }
        }

        public void UnregisterOnEnable(Action action)
        {
            if (action == null)
                return;

            _onLateEnable -= action;
        }

        public TextAnimation GetConfigAt(int index)
        {
            if (Panel == null)
                return null;

            return Panel.GetConfigAt(index);
        }

        /// <summary>
        /// Goes through the whole visual tree and adds/removes glow manipulators.
        /// </summary>
        public void AddOrRemoveManipulators()
        {
            if (Panel == null)
                return;

            if (Panel.RootVisualElement == null)
                Panel.RootVisualElement = Document.rootVisualElement;

            Panel.AddOrRemoveManipulators();
        }

        /// <summary>
        /// Call this on any element which you have recently added or removed an animation tag.
        /// </summary>
        /// <param name="element"></param>
        public void AddOrRemoveManipulator(TextElement element)
        {
            if (Panel == null)
                return;

            if (Panel.RootVisualElement == null)
                Panel.RootVisualElement = Document.rootVisualElement;

            Panel.AddOrRemoveManipulator(element);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Panel.Configs == null)
            {
                var root = TextAnimationsProvider.GetAnimations();
                if (root != null)
                {
                    Panel.Configs = root;
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.EditorUtility.SetDirty(this.gameObject);
                }
            }

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AddOrRemoveManipulators();
            }
            else
            {
                UIEditorPanelObserver.RefreshPanels();
            }
        }
#endif
    }
}