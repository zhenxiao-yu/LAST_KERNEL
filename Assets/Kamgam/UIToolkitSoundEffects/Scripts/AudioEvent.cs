using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    /// <summary>
    /// Plays sounds but can also send messages into scene objects.
    /// </summary>
    [System.Serializable]
    public partial class AudioEvent
    {
        public enum ClipOrderType
        {
            Random, All, Loop
        };
        
        public delegate void PlayAudioClipDelegate(AudioEvent evt);

        /// <summary>
        /// A custom sound play method. If this is set then the normal playback will be ignored and only this
        /// delegate will be called. Useful if you want to customize audio playback.
        /// </summary>
        public static PlayAudioClipDelegate CustomPlayFunc;
        
        /// <summary>
        /// A custom sound stop method. If this is set then the normal playback will be ignored and only this
        /// delegate will be called. Useful if you want to customize audio playback.
        /// </summary>
        public static PlayAudioClipDelegate CustomStopFunc;
        
        [Header("Event")]
        [Tooltip("The type of event this should react to with playing the Audio Clip(s).")]
        public ElementEventType eventType;

        [Tooltip("Min delay between repeated playbacks. Try the PointerMove event ;-). A good default value is: 0.05f")]
        public float RepeatDelay;
        
        [Header("Audio Clips")]
        [Tooltip("If there are multiple clips then this specifies how the clip to play is chosen.")]
        public ClipOrderType ClipOrder;
        
        [Tooltip("The clip(s) to play.")]
        public List<AudioClip> Clips;


        public AudioSourceSettings AudioSourceSettings;
        
        [Header("Code Execution")]
        [Tooltip("A list of messages that should be sent to the Receivers on all AudioEventReceivers in the scene.")]
        public List<string> Messages;
        
        /// <summary>
        /// A public delegate you can subscribe to to get notified of events.<br />
        /// HINT: You can use CurrentEvent and CurrentUIEvent to get some information about where this event came from.
        /// </summary>
        public System.Action<AudioEvent> OnPlay;
        
        /// <summary>
        /// A public unity event you can subscribe to to get notified of events.<br />
        /// HINT: You can use CurrentEvent and CurrentUIEvent to get some information about where this event came from.
        /// </summary>
        public UnityEvent<AudioEvent> OnPlayUnityEvent;

        [System.NonSerialized]
        protected VisualElement _callbackTarget;

        [System.NonSerialized]
        public int LastPlayedClipIndex;

        [System.NonSerialized]
        protected float _lastPlayTime = -1f;
       
        public void Initialize()
        {
            eventType = ElementEventType.OnPointerClick;
            ClipOrder = ClipOrderType.Random;
            RepeatDelay = 0.05f;
            
            Clips = new List<AudioClip>();

            if (AudioSourceSettings == null)
                AudioSourceSettings = new AudioSourceSettings();
            AudioSourceSettings.Initialize();

            Messages = null;

            LastPlayedClipIndex = -1;
        }
        
        public void CopyValuesFrom(AudioEvent evt)
        {
            eventType = evt.eventType;
            ClipOrder = evt.ClipOrder;
            RepeatDelay = evt.RepeatDelay;

            if (Clips == null && evt.Clips != null)
                Clips = new List<AudioClip>();
            
            if (evt.Clips != null)
            {
                Clips.AddRange(evt.Clips);
            }

            if (AudioSourceSettings == null)
                AudioSourceSettings = new AudioSourceSettings();
            AudioSourceSettings.CopyValuesFrom(evt.AudioSourceSettings);
            
            if (Messages != null)
                Messages.Clear();
            if (evt.Messages != null)
            {
                if (Messages == null)
                    Messages = new List<string>();
                Messages.AddRange(evt.Messages);
            }
            
            _lastPlayTime = -1f;
            LastPlayedClipIndex = -1;
            _lastIsDrawnValueDict.Clear();
        }

        /// <summary>
        /// Finds and returns the sound effect of this event.
        /// May return null if the visual element or manipulator does no longer exist.
        /// </summary>
        /// <returns></returns>
        public SoundEffect GetEffect()
        {
            SoundEffect effect = null;
            var manipulator = SoundEffectManipulator.GetManipulator(_callbackTarget);
            if (manipulator != null)
                effect = manipulator.SoundEffect;
            return effect;
        }

        public bool HasMessages => Messages != null && Messages.Count > 0;

        public void SendMessage()
        {
            if (!HasMessages)
                return;
            
            foreach (var msg in Messages)
            {
                if (!string.IsNullOrEmpty(msg))
                    SoundEventReceiver.SendMessageToAll(msg);
            }
        }

        public void Trigger(EventBase evt)
        {
            if (Time.realtimeSinceStartup - _lastPlayTime < RepeatDelay)
                return;
            
            _lastPlayTime = Time.realtimeSinceStartup;
            
            CurrentEvent = this;
            CurrentUIEvent = evt;
            
            Play();
            OnPlay?.Invoke(this);
            OnPlayUnityEvent?.Invoke(this);
            SendMessage();
            
            CurrentEvent = null;
            CurrentUIEvent = null;
        }
        
        public void Play()
        {
            if (CustomPlayFunc != null)
            {
                CustomPlayFunc(this);
            }
            else
            {
                AudioEventPlayer.Instance.Play(this);
            }
        }
        
        public void Stop()
        {
            if (CustomStopFunc != null)
            {
                CustomStopFunc(this);
            }
            else
            {
                AudioEventPlayer.Instance.Stop(this);
            }
        }

        public void Clear()
        {
            Clips.Clear();
            UnregisterCallback(_callbackTarget);
            _callbackTarget = null;
        }

        public void RegisterCallback(VisualElement element)
        {
            if (element == null)
                return;
            
            _callbackTarget = element;
            
            switch (eventType)
            {
                case ElementEventType.OnAttach:
                    element.RegisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                    break;
                case ElementEventType.OnButtonClick:
                    element.RegisterCallback<ClickEvent>(onButtonClick);
                    break;
                case ElementEventType.OnClickOrSubmit:
                    element.RegisterCallback<ClickEvent>(onButtonClick);
                    if (element is DropdownField)
                        element.RegisterCallback<ChangeEvent<string>>(onDropDownChanged);
                    else
                        element.RegisterCallback<NavigationSubmitEvent>(onSubmit);
                    break;
                case ElementEventType.OnChange:
                    element.RegisterCallback<ChangeEvent<string>>(onStringValueChanged);
                    element.RegisterCallback<ChangeEvent<int>>(onIntValueChanged);
                    element.RegisterCallback<ChangeEvent<float>>(onFloatValueChanged);
                    element.RegisterCallback<ChangeEvent<bool>>(onBoolValueChanged);
                    element.RegisterCallback<ChangeEvent<Vector2>>(onVector2ValueChanged);
                    element.RegisterCallback<ChangeEvent<Vector2>>(onSliderMinMaxValueChanged);
                    break;
                case ElementEventType.OnCancel:
                    element.RegisterCallback<NavigationCancelEvent>(onCancel);
                    break;
                case ElementEventType.OnDetach:
                    element.RegisterCallback<DetachFromPanelEvent>(onDetach);
                    break;
                case ElementEventType.OnDropDownValueChanged:
                    element.RegisterCallback<ChangeEvent<string>>(onDropDownChanged);
                    break;
                case ElementEventType.OnHide:
                    element.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);
                    element.RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
                    break;
                case ElementEventType.OnPointerClick:
                    element.RegisterCallback<ClickEvent>(onClick);
                    break;
                case ElementEventType.OnPointerDown:
                    element.RegisterCallback<PointerDownEvent>(onPointerDown);
                    break;
                case ElementEventType.OnPointerEnter:
                    element.RegisterCallback<PointerEnterEvent>(onPointerEnter);
                    break;
                case ElementEventType.OnPointerLeave:
                    element.RegisterCallback<PointerLeaveEvent>(onPointerLeave);
                    break;
                case ElementEventType.OnPointerMove:
                    element.RegisterCallback<PointerMoveEvent>(onPointerMove);
                    break;
                case ElementEventType.OnPointerUp:
                    element.RegisterCallback<PointerUpEvent>(onPointerUp);
                    break;
                case ElementEventType.OnScrollerValueChanged:
                    if(element is Scroller scroller)
                        scroller.RegisterCallback<ChangeEvent<float>>(onScrollerValueChanged);
                    break;
                case ElementEventType.OnShow:
                    element.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);
                    element.RegisterCallback<GeometryChangedEvent>(onGeometryChanged);
                    break;
                case ElementEventType.OnSliderIntValueChanged:
                    if(element is SliderInt sliderInt)
                        sliderInt.RegisterCallback<ChangeEvent<int>>(onSliderIntValueChanged);
                    break;
                case ElementEventType.OnSliderMinMaxValueChanged:
                    if(element is MinMaxSlider sliderMinMax)
                        sliderMinMax.RegisterCallback<ChangeEvent<Vector2>>(onSliderMinMaxValueChanged);
                    break;
                case ElementEventType.OnSliderValueChanged:
                    if(element is BaseSlider<float> slider)
                        slider.RegisterCallback<ChangeEvent<float>>(onSliderFloatValueChanged);
                    break;
                case ElementEventType.OnSubmit:
                    element.RegisterCallback<NavigationSubmitEvent>(onSubmit);
                    break;
                case ElementEventType.OnTextFieldValueChanged:
                    if (element is TextField textfield)
                    {
                        textfield.RegisterValueChangedCallback(onTextFieldValueChanged);
                    }
                    break;
                case ElementEventType.OnTextFieldValueChangedAdd:
                    if (element is TextField textfieldAdd)
                    {
                        textfieldAdd.RegisterValueChangedCallback(onTextFieldValueChangedAdd);
                    }
                    break;
                case ElementEventType.OnTextFieldValueChangedRemove:
                    if (element is TextField textfieldRemove)
                    {
                        textfieldRemove.RegisterValueChangedCallback(onTextFieldValueChangedRemove);
                    }
                    break;
                case ElementEventType.OnToggleValueChanged:
                    element.RegisterCallback<ChangeEvent<bool>>(onToggleValueChanged);
                    break;
                case ElementEventType.ChangeString:
                    element.RegisterCallback<ChangeEvent<string>>(onStringValueChanged);
                    break;
                case ElementEventType.ChangeFloat:
                    element.RegisterCallback<ChangeEvent<float>>(onFloatValueChanged);
                    break;
                case ElementEventType.ChangeInt:
                    element.RegisterCallback<ChangeEvent<int>>(onIntValueChanged);
                    break;
                case ElementEventType.ChangeBool:
                    element.RegisterCallback<ChangeEvent<bool>>(onBoolValueChanged);
                    break;
                case ElementEventType.ChangeBoolToTrue:
                    element.RegisterCallback<ChangeEvent<bool>>(onBoolValueChangedToTrue);
                    break;
                case ElementEventType.ChangeBoolToFalse:
                    element.RegisterCallback<ChangeEvent<bool>>(onBoolValueChangedToFalse);
                    break;
                case ElementEventType.ChangeVector2:
                    element.RegisterCallback<ChangeEvent<Vector2>>(onVector2ValueChanged);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
        }

        public void UnregisterCallback(VisualElement element)
        {
            if (element == null)
                return;
            
            // ElementEventType.OnAttach:
            element.UnregisterCallback<AttachToPanelEvent>(OnAttachToPanel);
                
            // ElementEventType.OnButtonClick:
            element.UnregisterCallback<ClickEvent>(onButtonClick);
                
            // ElementEventType.OnCancel:
            element.UnregisterCallback<NavigationCancelEvent>(onCancel);
                
            // ElementEventType.OnDetach:
            element.UnregisterCallback<DetachFromPanelEvent>(onDetach);
                
            // ElementEventType.OnDropDownValueChanged:
            element.UnregisterCallback<ChangeEvent<string>>(onDropDownChanged);
            
            // ElementEventType.OnHide
            element.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);
                
            // ElementEventType.OnPointerClick:
            element.UnregisterCallback<ClickEvent>(onClick);
                
            // ElementEventType.OnPointerDown:
            element.UnregisterCallback<PointerDownEvent>(onPointerDown);
                
            // ElementEventType.OnPointerEnter:
            element.UnregisterCallback<PointerEnterEvent>(onPointerEnter);
                
            // ElementEventType.OnPointerLeave:
            element.UnregisterCallback<PointerLeaveEvent>(onPointerLeave);
                
            // ElementEventType.OnPointerMove:
            element.UnregisterCallback<PointerMoveEvent>(onPointerMove);
                
            // ElementEventType.OnPointerUp:
            element.UnregisterCallback<PointerUpEvent>(onPointerUp);
                
            // ElementEventType.OnScrollerValueChanged:
            element.UnregisterCallback<ChangeEvent<float>>(onScrollerValueChanged);
            
            // ElementEventType.OnShow
            element.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);

            // ElementEventType.OnSliderIntValueChanged:
            element.UnregisterCallback<ChangeEvent<int>>(onSliderIntValueChanged);

            // ElementEventType.OnSliderMinMaxValueChanged:
            element.UnregisterCallback<ChangeEvent<Vector2>>(onSliderMinMaxValueChanged);

                // ElementEventType.OnSliderValueChanged:
            element.UnregisterCallback<ChangeEvent<float>>(onSliderFloatValueChanged);

                // ElementEventType.OnSubmit:
            element.UnregisterCallback<NavigationSubmitEvent>(onSubmit);
                
            // ElementEventType.OnTextFieldEndEdit:
            if (element is TextField textfield)
            {
                // ElementEventType.OnTextFieldValueChanged:
                textfield.UnregisterValueChangedCallback(onTextFieldValueChanged);
                textfield.UnregisterValueChangedCallback(onTextFieldValueChangedAdd);
                textfield.UnregisterValueChangedCallback(onTextFieldValueChangedRemove);
            }

            // ElementEventType.OnToggleValueChanged:
            element.UnregisterCallback<ChangeEvent<bool>>(onToggleValueChanged);
            
            // ElementEventType.ChangeString:
            element.UnregisterCallback<ChangeEvent<string>>(onStringValueChanged);
                
            // ElementEventType.ChangeFloat:
            element.UnregisterCallback<ChangeEvent<float>>(onFloatValueChanged);
                
            // ElementEventType.ChangeInt:
            element.UnregisterCallback<ChangeEvent<int>>(onIntValueChanged);
                
            // ElementEventType.ChangeBool:
            element.UnregisterCallback<ChangeEvent<bool>>(onBoolValueChanged);
            
            // ElementEventType.ChangeVector2:
            element.UnregisterCallback<ChangeEvent<Vector2>>(onVector2ValueChanged);
            
            // ElementEventType.UIToolkitChangeEventBoolToTrue:
            element.UnregisterCallback<ChangeEvent<bool>>(onBoolValueChangedToTrue);
            
            // ElementEventType.UIToolkitChangeEventBoolToFalse:
            element.UnregisterCallback<ChangeEvent<bool>>(onBoolValueChangedToFalse);
        }

        private Dictionary<VisualElement,Nullable<bool>> _lastIsDrawnValueDict = new ();

        private static List<VisualElement> s_tmpLastDrawnKeys = new();
        
        private void defragLastDrawnValues()
        {
            s_tmpLastDrawnKeys.Clear();
            
            foreach (var key in _lastIsDrawnValueDict.Keys)
            {
                s_tmpLastDrawnKeys.Add(key);           
            }
            
            foreach (var key in s_tmpLastDrawnKeys)
            {
                if (key.panel == null)
                {
                    _lastIsDrawnValueDict.Remove(key);
                }
            }
            
            s_tmpLastDrawnKeys.Clear();
        }
        
        private Nullable<bool> getLastIsDrawnValue(VisualElement element)
        {
            defragLastDrawnValues();
            
            _lastIsDrawnValueDict.TryAdd(element, null);
            return _lastIsDrawnValueDict[element];
        }
        
        private void setLastIsDrawnValue(VisualElement element, Nullable<bool> isDrawn)
        {
            _lastIsDrawnValueDict[element] = isDrawn;
        }

        private void onGeometryChanged(GeometryChangedEvent evt)
        {
            var element = evt.target as VisualElement;
            bool isDrawn = elementIsDrawn(element);

            if (!getLastIsDrawnValue(element).HasValue || getLastIsDrawnValue(element).Value != isDrawn)
            {
                setLastIsDrawnValue(element, isDrawn);

                if (   (eventType == ElementEventType.OnShow && isDrawn)
                    || (eventType == ElementEventType.OnHide && !isDrawn) )
                {
                    Trigger(evt);
                }
            }
        }
        
        /// <summary>
        /// Returns whether or not the image is drawn based on the contentRect size.
        /// It acts like the activeInHierarchy we are used to from game objects (takes parents into account).
        /// See: https://discussions.unity.com/t/event-when-visualelement-gets-hidden/871773/8
        /// </summary>
        /// <returns></returns>
        private bool elementIsDrawn(VisualElement element)
        {
            if (element == null)
                return false;

            var rect = element.contentRect;
            return !float.IsNaN(rect.width) && !Mathf.Approximately(rect.width, 0f) && !Mathf.Approximately(rect.height, 0f);
        }

        public void OnAttachToPanel(AttachToPanelEvent evt)
        {
            Trigger(evt);
        }

        private void onButtonClick(ClickEvent evt)
        {
            Trigger(evt);
        }

        private void onBoolValueChanged(ChangeEvent<bool> evt)
        {
            Trigger(evt);
        }
        
        private void onBoolValueChangedToTrue(ChangeEvent<bool> evt)
        {
            if (evt.newValue)
                Trigger(evt);
        }
        
        private void onBoolValueChangedToFalse(ChangeEvent<bool> evt)
        {
            if (!evt.newValue)
                Trigger(evt);
        }
        
        private void onVector2ValueChanged(ChangeEvent<Vector2> evt)
        {
            Trigger(evt);
        }

        private void onCancel(NavigationCancelEvent evt)
        {
            Trigger(evt);
        }

        private void onClick(ClickEvent evt)
        {
            Trigger(evt);
        }

        private void onDetach(DetachFromPanelEvent evt)
        {
            Trigger(evt);
        }

        private void onDropDownChanged(ChangeEvent<string> evt)
        {
            Trigger(evt);
        }
        
        private void onFloatValueChanged(ChangeEvent<float> evt)
        {
            Trigger(evt);
        }

        private void onIntValueChanged(ChangeEvent<int> evt)
        {
            Trigger(evt);
        }

        private void onPointerDown(PointerDownEvent evt)
        {
            Trigger(evt);
        }

        private void onPointerEnter(PointerEnterEvent evt)
        {
            Trigger(evt);
        }

        private void onPointerLeave(PointerLeaveEvent evt)
        {
            Trigger(evt);
        }

        private void onPointerMove(PointerMoveEvent evt)
        {
            Trigger(evt);
        }

        private void onPointerUp(PointerUpEvent evt)
        {
            Trigger(evt);
        }

        private void onScrollerValueChanged(ChangeEvent<float> evt)
        {
            Trigger(evt);
        }

        private void onSliderFloatValueChanged(ChangeEvent<float> evt)
        {
            Trigger(evt);
        }

        private void onSliderIntValueChanged(ChangeEvent<int> evt)
        {
            Trigger(evt);
        }

        private void onSliderMinMaxValueChanged(ChangeEvent<Vector2> evt)
        {
            Trigger(evt);
        }

        private void onStringValueChanged(ChangeEvent<string> evt)
        {
            Trigger(evt);
        }

        private void onTextFieldValueChanged(ChangeEvent<string> evt)
        {
            Trigger(evt);
        }
        
        private void onTextFieldValueChangedAdd(ChangeEvent<string> evt)
        {
            if (evt.newValue.Length > evt.previousValue.Length)
                Trigger(evt);
        }
        
        private void onTextFieldValueChangedRemove(ChangeEvent<string> evt)
        {
            if (evt.newValue.Length < evt.previousValue.Length)
                Trigger(evt);
        }

        private void onToggleValueChanged(ChangeEvent<bool> evt)
        {
            Trigger(evt);
        }

        private void onSubmit(NavigationSubmitEvent evt)
        {
            Trigger(evt);
        }
    }
}