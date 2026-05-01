// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using UnityEngine.UIElements;
using UnityEngine;
using System;
using System.Collections;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// A component that changes the color randomly every frame. Just for demo purposes.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Typewriter")]
    public class UITypewriter : UIComponentBase<TextElement>
    {
        public bool AutoStart = true;
        [Tooltip("Aka the starting letter.")]
        public int StartCharacter = 0;
        public bool ResetStartCharOnEnable = true;
        protected int _currentCharacter = 0;

        public float CharactersPerSecond = 15f;
        protected float _charPerSecModifier = 1f;
        
        public float InitalDelayInSec = 1f;
        protected bool _waitForInitialDelay;

        public bool IgnoreTimeScale = true;

        [System.NonSerialized]
        protected string _text;
        [System.NonSerialized]
        protected int _textLength;
        public string Text
        {
            get => GetText();
            set => SetText(value);
        }

        protected float _delta;
        protected System.Text.StringBuilder _builder = new System.Text.StringBuilder();

        override public void OnEnable()
        {
            base.OnEnable();

            if (GetComponent<IQuery>() != null)
            {
                Debug.LogWarning("UITypewriter does not support query sources, sorry. Please use a Link stead.");
            }

            if (ResetStartCharOnEnable)
                _currentCharacter = StartCharacter;

            if (Element == null)
                return;

            Element.visible = false;
            _waitForInitialDelay = true;
            StartCoroutine(makeVisible());

            _delta = 0;
            _charPerSecModifier = 1f;

            if (!string.IsNullOrEmpty(_text))
            {
                initBuilderAndShownText();
            }
        }

        protected IEnumerator makeVisible()
        {
            yield return IgnoreTimeScale ? new WaitForSecondsRealtime(InitalDelayInSec) : new WaitForSeconds(InitalDelayInSec);

            Element.visible = true;
            _waitForInitialDelay = false;
        }

        public void OnDisable()
        {
            StopAllCoroutines();
        }

        protected void initBuilderAndShownText()
        {
            _builder.Clear();

            if (_currentCharacter > 0 && _text != null && _currentCharacter < _text.Length)
            {
                var character = _text.Substring(0, _currentCharacter + 1);
                _builder.Append(character);

                setShownText(_builder.ToString());
            }
            else
            {
                setShownText("");
            }
        }

        public void Start()
        {
            if (Element == null)
                return;

            _text = Element.text;
            if (_text != null)
            {
                _textLength = _text.Length;
            }

            initBuilderAndShownText();
        }

        public void SetText(string text)
        {
            if (text == null && !string.IsNullOrEmpty(_text))
            {
                _text = "";
                _textLength = 0;
            }

            if (text == null)
                return;

            _text = text;
            _textLength = text.Length;
        }

        public string GetText()
        {
            return _text;
        }

        void Update()
        {
            if (_currentCharacter < _textLength && !_waitForInitialDelay)
            {
                float deltaTime = IgnoreTimeScale ? Time.unscaledDeltaTime : Time.deltaTime;
                _delta += deltaTime;
                float maxDelta = (1f / CharactersPerSecond) * _charPerSecModifier;
                if (_delta >= maxDelta)
                {
                    _delta = 0;

                    // append
                    var character = _text.Substring(_currentCharacter, 1);
                    _builder.Append(character);

                    // update text
                    setShownText(_builder.ToString());

                    // next
                    _currentCharacter++;

                    // Add variation to the char delay
                    _charPerSecModifier = UnityEngine.Random.Range(0.7f, 2f);
                    if (_currentCharacter < _textLength)
                    {
                        var nextChar = _text.Substring(_currentCharacter, 1);
                        if (string.IsNullOrWhiteSpace(nextChar))
                        {
                            _charPerSecModifier *= UnityEngine.Random.Range(1f, 4f);
                        }
                    }
                }
            }
        }

        protected void setShownText(string text)
        {
            if (!HasElements())
                return;

            Element.text = text;
        }
    }
}

#endif
