using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    /// <summary>
    /// It's a scriptable object so we can easily draw inspectors for it and binding is also more convenient.
    /// </summary>
    public partial class SoundEffect : ScriptableObject
    {
        /// <summary>
        /// Do NOT change the id unless you want to risk loosing the effect to element connections.
        /// </summary>
	    public string Id;
        
        public List<AudioEvent> Events = new List<AudioEvent>();

        [SerializeField, HideInInspector]
        protected bool _editorInitialized = false;
        
        /// <summary>
        /// Keeps track of how many VisualElements are using this effect asset.
        /// If removed and the UsedByCount is 1 then it will be destroyed (if done via the UI Builder).
        /// </summary>
        [SerializeField, HideInInspector]
        public int UIBuilderUsedByCount = 0;

        /// <summary>
        /// A list of all the copies that were made for runtime use.
        /// </summary>
        [System.NonSerialized]
        public List<SoundEffect> Copies = new List<SoundEffect>();

        [System.NonSerialized]
        public bool IsCopy = false;

        // Used at runtime to avoid changes to the scriptable objects in the editor.
        public void CopyValuesFrom(SoundEffect effect)
        {
            Id = effect.Id;

            AudioEvent.ResetAndReturnToPool(Events);
            
            if (effect.Events != null)
            {
                foreach (var evt in effect.Events)
                {
                    var copy = AudioEvent.GetCopyFromPool(evt);
                    Events.Add(copy);
                }
            }
        }

        public void Clear()
        {
            Id = null;
            AudioEvent.ResetAndReturnToPool(Events);
            _editorInitialized = false;
            Copies.Clear();
            IsCopy = false;
        }

        /// <summary>
        /// Registers callbacks for the events listed in the 'Events' field.
        /// </summary>
        /// <param name="visualElement"></param>
        public void RegisterCallbacks(VisualElement visualElement)
        {
            foreach (var evt in Events)
            {
                evt.RegisterCallback(visualElement);
            }
        }
        
        /// <summary>
        /// Unregisters all callbacks.
        /// </summary>
        /// <param name="visualElement"></param>
        public void UnregisterCallbacks(VisualElement visualElement)
        {
            foreach (var evt in Events)
            {
                evt.UnregisterCallback(visualElement);
            }
        }

        public void OnManipulatorAdded()
        {
            // Trigger on attach if the manipulator is added because usually the attach
            // event has already been fired before and would not be fired at all if not here.
            foreach (var evt in Events)
            {
                if (evt != null && evt.eventType == ElementEventType.OnAttach)
                {
                    evt.OnAttachToPanel(null);
                }
            }
        }

        public void EditorInitIfNecessary()
        {
            if (!_editorInitialized)
            {
                _editorInitialized = true;

                CreateFirstEventIfNoneExists(addNullClip: true);
            }
        }

        public void CreateFirstEventIfNoneExists(bool addNullClip)
        {
            if (Events == null)
                Events = new List<AudioEvent>();

            if (Events.Count == 0)
            {
                AddNewEvent(addNullClip);
            }
            else
            {
                // Add null to clips if empty
                var evt = Events[Events.Count - 1];
                if (evt.Clips == null)
                    evt.Clips = new List<AudioClip>();
                if (evt.Clips.Count == 0)
                {
                    evt.Clips.Add(null);
                }
            }
        }

        /// <summary>
        /// Adds a new event to the effect.
        /// </summary>
        /// <param name="addNullClip">Used by the editor inspector to add a clip with value null to trigger the generation of an inspector field.</param>
        public void AddNewEvent(bool addNullClip = false)
        {
            if (Events == null)
                Events = new List<AudioEvent>();

            var evt = new AudioEvent();
            evt.Initialize();
            Events.Add(evt);

            if (addNullClip)
            {
                evt.Clips.Add(null);
            }
        }

        public void Play()
        {
            if (Events == null)
                return;
            
            foreach (var evt in Events)
            {
                if (evt != null)
                    evt.Play();
            }
        }
        
        public void Stop()
        {
            if (Events == null)
                return;
            
            foreach (var evt in Events)
            {
                if (evt != null)
                    evt.Stop();
            }
        }

        private static List<SoundEffectManipulator> s_tmpListOfManipulators = new List<SoundEffectManipulator>();

        public void OnValidate()
        {
            EditorInitIfNecessary();

            // Update all copies on Validate. Important to keep runtime copies in
            // sync with the assets (if the assets change).
            foreach (var copy in Copies)
            {
                copy.CopyValuesFrom(this);
                
#if UNITY_EDITOR
                // Force a reconnect to update events listener changes.
                if (EditorApplication.isPlayingOrWillChangePlaymode)
                {
                    var manipulators = SoundEffectManipulator.GetManipulators(copy.Id, s_tmpListOfManipulators);
                    foreach (var manipulator in manipulators)
                    {
                        if (manipulator != null)
                        {
                            manipulator.ConnectToAudioEffect(copy.Id);
                        }
                    }
                    s_tmpListOfManipulators.Clear();
                }
#endif
            }
        }
    }
}