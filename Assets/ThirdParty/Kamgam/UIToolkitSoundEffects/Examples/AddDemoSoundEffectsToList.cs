using System;
using UnityEditor;
using UnityEngine;

namespace Kamgam.UIToolkitSoundEffects.Examples
{
    [ExecuteInEditMode]
    public class AddDemoSoundEffectsToList : MonoBehaviour
    {
#if UNITY_EDITOR
        // IN the editor we add the necessary sound effects to the list if needed for the demos to work.
        public void Awake()
        {
            var effects = SoundEffects.GetOrCreate();
            if (effects == null)
                throw new Exception("Could not create sound effect list. Oh oh :-(");
                    
            var guids = AssetDatabase.FindAssets("t:SoundEffect sound-effect-kamgam-demo");
            foreach (var guid in guids)
            {
                var effect = AssetDatabase.LoadAssetAtPath<SoundEffect>(AssetDatabase.GUIDToAssetPath(guid));
                if (effect != null && !effects.Effects.Contains(effect))
                {
                    effects.Effects.Add(effect);
                }
            }
        }
#endif
    }
}
