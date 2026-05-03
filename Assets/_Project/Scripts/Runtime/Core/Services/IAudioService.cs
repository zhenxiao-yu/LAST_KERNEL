using UnityEngine;

namespace Markyu.LastKernel
{
    public interface IAudioService
    {
        void PlaySFX(AudioId audioId, Vector3? worldPosition = null, bool interruptBGM = false);
        void PlayBGM(MusicContext context, float fadeDuration = 1f);
        void SetSFXVolume(float value);
        void SetBGMVolume(float value);
        void InitAudioMixerVolumes();
        float GetSavedSFXVolumeSlider();
        float GetSavedBGMVolumeSlider();
    }
}
