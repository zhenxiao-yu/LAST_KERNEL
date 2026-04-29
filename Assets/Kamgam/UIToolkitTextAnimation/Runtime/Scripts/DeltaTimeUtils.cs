using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kamgam.UIToolkitTextAnimation
{
    public static class DeltaTimeUtils
    {
        public static float unscaledDeltaTime
        {
            get
            {
#if UNITY_EDITOR
                // Lot's of shenanigans to make unscaled delta time work as expected in the editor, see:
                // https://discussions.unity.com/t/resource-time-unscaleddeltatime-with-reliable-in-editor-behaviour/1607638
                if (EditorApplication.isPlaying)
                {
                    // Avoid division by zero
                    if (Mathf.Approximately(Time.timeScale, 0f))
                        return Time.unscaledDeltaTime;
                    
                    // Why not also use Time.unscaledDeltaTime here? Because in the first frame after unpausing in the
                    // editor the unscaledTime has a big value (duration of the pause).
                    return Time.deltaTime / Time.timeScale;
                }
#endif
                // In builds if it is the first frame(s) then do not use unscaledDeltaTime since it is potentially very big here.
                if (Time.frameCount <= 1)
                    return Time.deltaTime;
                else
                    return Time.unscaledDeltaTime;
            }
        }        
    }
}