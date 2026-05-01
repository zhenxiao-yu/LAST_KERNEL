using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UIElements;


namespace Kamgam.UIToolkitSoundEffects
{
    [System.Serializable]
    public class AudioSourceSettings
    {
        [Tooltip("Set whether the sound should play through an Audio Mixer first or directly to the Audio Listener.\n\nNOTICE: Clicking this will freak out the inspector and make it all highlighted (blue). I am sorry, it's a Unity bug. Please check the manual for help (click another element to fix it).")]
        public AudioMixerGroup Output;
        
        [Tooltip("Mute the sound.")]
        public bool Mute;
        
        [Tooltip("Bypass/ignore any effects.")]
        public bool BypassEffects;
        
        [Tooltip("Bypass/ignore any effects from listener.")]
        public bool BypassListenerEffects;
        
        [Tooltip("Bypass/ignore any reverb zones.")]
        public bool BypassReverbZones;
        
        [Tooltip("If there are multiple clips then randomize which one to play.")]
        public bool Loop;

        [Tooltip("If loop is enabled then this is the minimum loop duration. A started loop will finish playing after this duration but no new loop will be scheduled.")]
        public float LoopDuration = 2f;
        
        [Tooltip("Priority of the source.")]
        [Range(0,255)]
        public int Priority;
        
        [Tooltip("Volume of the clip. Default: 1")]
        [Range(0,1)]
        public float Volume;

        [Tooltip("Sets the frequency of the sound. Use this to slow down or speed up the sound.")]
        [Range(-3,3)]
        public float Pitch;
        
        [Tooltip("Left or right pan for sounds.")]
        [Range(-1,1)]
        public float StereoPan;
        
        [Tooltip("Blend between 2D (0f) and 3D (1f).")]
        [Range(0,1)]
        public float SpatialBlend;
        
        [Tooltip("Sets how much of the signal this AudioSource is mixing into the global reverb associated with the zones. [0, 1] is a linear range (like volume) while [1, 1.1] lets you boost the reverb mix by 10 dB.")]
        [Range(0,1.1f)]
        public float ReverbZoneMix;

        public void Initialize()
        {
            Output = null;
            Mute = false;
            BypassEffects = false;
            BypassListenerEffects = false;
            BypassReverbZones = false;
            Loop = false;
            LoopDuration = 2f;
            Priority = 128;
            Volume = 1f;
            Pitch = 1f;
            StereoPan = 0f;
            SpatialBlend = 0f;
            ReverbZoneMix = 1f;
        }

        public void CopyValuesFrom(AudioSourceSettings settings)
        {
            if (settings == null)
                return;
            
            Output = settings.Output;
            Mute = settings.Mute;
            BypassEffects = settings.BypassEffects;
            BypassListenerEffects = settings.BypassListenerEffects;
            BypassReverbZones = settings.BypassReverbZones;
            Loop = settings.Loop;
            LoopDuration = settings.LoopDuration;
            Priority = settings.Priority;
            Volume = settings.Volume;
            Pitch = settings.Pitch;
            StereoPan = settings.StereoPan;
            SpatialBlend = settings.SpatialBlend;
            ReverbZoneMix = settings.ReverbZoneMix;
        }

        public void ApplyTo(AudioSource audioSource)
        {
            audioSource.outputAudioMixerGroup = Output;
            audioSource.mute = Mute;
            audioSource.bypassEffects = BypassEffects;
            audioSource.bypassListenerEffects = BypassListenerEffects;
            audioSource.bypassReverbZones = BypassReverbZones;
            audioSource.loop = Loop;
            audioSource.priority = Priority;
            audioSource.volume = Volume;
            audioSource.pitch = Pitch;
            audioSource.panStereo = StereoPan;
            audioSource.spatialBlend = SpatialBlend;
            audioSource.reverbZoneMix = ReverbZoneMix;
        }
    }
}