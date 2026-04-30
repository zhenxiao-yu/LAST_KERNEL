#if UNITY_EDITOR
// Thanks to: https://discussions.unity.com/t/way-to-play-audio-in-editor-using-an-editor-script/473638/14
using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;

namespace Kamgam.UIToolkitSoundEffects
{
    public static class EditorAudioUtils
    {

        public static void PlayClip(AudioClip clip, int startSample = 0, bool loop = false)
        {
            if (clip == null)
                return;

            try
            {

                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod(
                    "PlayPreviewClip",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new Type[] { typeof(AudioClip), typeof(int), typeof(bool) },
                    null
                );

                method.Invoke(
                    null,
                    new object[] { clip, startSample, loop }
                );
            }
            catch
            {
                Debug.LogError("Sorry: Playback failed.");
            }
        }

        public static void StopAllClips()
        {
            try
            {
                Assembly unityEditorAssembly = typeof(AudioImporter).Assembly;

                Type audioUtilClass = unityEditorAssembly.GetType("UnityEditor.AudioUtil");
                MethodInfo method = audioUtilClass.GetMethod(
                    "StopAllPreviewClips",
                    BindingFlags.Static | BindingFlags.Public,
                    null,
                    new Type[] { },
                    null
                );

                Debug.Log(method);
                method.Invoke(
                    null,
                    new object[] { }
                );
            }
            catch
            {
                Debug.LogError("Sorry: Playback stop failed.");
            }
        }
    }
}
#endif