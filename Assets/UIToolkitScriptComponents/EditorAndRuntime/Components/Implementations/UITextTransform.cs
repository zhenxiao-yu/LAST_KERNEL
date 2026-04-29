// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitScriptComponents
{
    /// <summary>
    /// Applies the text transform OnEnable and (if this is enabled) applies it automatically whenever the text is changed.<br />
    /// Cally .Apply() to trigger it manually.
    /// </summary>
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Text Transform")]
    public class UITextTransform : UIComponentBase<TextElement>
    {
        [Tooltip("Applies the text transform OnEnable and (if this is enabled) applies it automatically whenever the text is changed.")]
        public bool AutoUpdate = true;

        public enum TextTransform { None, Upper, Lower }
        public TextTransform Transform = TextTransform.None;

        Dictionary<VisualElement, string> _originalTexts = new Dictionary<VisualElement, string>();
        Dictionary<VisualElement, string> _texts = new Dictionary<VisualElement, string>();
        TextTransform _lastTransform = TextTransform.None;

        public override void OnEnable()
        {
            base.OnEnable();
            Update();
        }

        void Update()
        {
            if (AutoUpdate)
            {
                Apply();
            }
        }

        public void Apply()
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                if (!_originalTexts.ContainsKey(ele))
                {
                    _originalTexts.Add(ele, ele.text);
                }

                if (!_texts.ContainsKey(ele))
                {
                    _texts.Add(ele, null);
                }

                // Apply transforms if text changed
                bool textChanged = _texts[ele] != ele.text;
                if (textChanged || didTransformChange())
                {
                    if (textChanged)
                    {
                        _originalTexts[ele] = ele.text;
                    }
                    
                    switch (Transform)
                    {
                        case TextTransform.Upper:
                            _texts[ele] = _originalTexts[ele].ToUpper();
                            break;

                        case TextTransform.Lower:
                            _texts[ele] = _originalTexts[ele].ToLower();
                            break;

                        case TextTransform.None:
                        default:
                            _texts[ele] = _originalTexts[ele];
                            break;
                    }

                    ele.text = _texts[ele];
                }
            }
        }

        public override void OnDetach()
        {
            base.OnDetach();

            foreach (var ele in _elements)
            {
                if (ele == null)
                    return;

                if (_originalTexts.ContainsKey(ele))
                {
                    ele.text = _originalTexts[ele];
                }
            }

            _originalTexts.Clear();
            _texts.Clear();
        }

        protected bool didTransformChange()
        {
            bool didChange = _lastTransform != Transform;
            if (didChange)
                _lastTransform = Transform;

            return didChange;
        }
    }
}

#endif
