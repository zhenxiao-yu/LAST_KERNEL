// With 2021.2 UIToolkit was integrated with Unity instead of being a package.
#if KAMGAM_UI_ELEMENTS || UNITY_2021_2_OR_NEWER

using UnityEngine.UIElements;
using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections;

namespace Kamgam.UIToolkitScriptComponents
{
    [AddComponentMenu("UI Toolkit/Scripts/Components/UI Slider On Value Changed")]
    public class UISliderOnValueChanged : UIComponentBase<Slider>
    {
        [Header("Events")]

        public UnityEvent<float> OnValueChanged;
        public UnityEvent<ChangeEvent<float>> OnValueChangedWithEvent;

        public override void OnAttach()
        {
            base.OnAttach();
            RegisterEvents();
        }

        public void OnDisable()
        {
            UnregisterEvents();
        }

        public override void OnDestroy()
        {
            UnregisterEvents();
            base.OnDestroy();
        }

        public virtual void RegisterEvents()
        {
            if (OnValueChanged == null && OnValueChangedWithEvent == null)
                return;

            if(!HasElements())
            {
                return;
            }

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                ele.RegisterValueChangedCallback(onValueChanged);
            }
        }

        public virtual void UnregisterEvents()
        {
            if (!HasElements())
                return;

            foreach (var ele in Elements)
            {
                if (ele == null)
                    continue;

                ele.UnregisterValueChangedCallback(onValueChanged);
            }
        }

        protected virtual void onValueChanged(ChangeEvent<float> evt)
        {
            OnValueChanged?.Invoke(evt.newValue);
            OnValueChangedWithEvent?.Invoke(evt);
        }

        public void LogTest(float value)
        {
            Debug.Log(value);
        }
    }
}

#endif
