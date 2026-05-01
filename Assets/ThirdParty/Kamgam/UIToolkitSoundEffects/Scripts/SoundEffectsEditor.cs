#if UNITY_EDITOR

using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    [CustomEditor(typeof(SoundEffects))]
    public class SoundEffectsEditor : Editor
    {
        
        public override VisualElement CreateInspectorGUI()
        {
            var root = new VisualElement();
            
            InspectorElement.FillDefaultInspector(root, serializedObject, this);

            var effects = target as SoundEffects;

            var defragButton = new Button( () =>
            {
                effects.Defrag();
            });
            defragButton.text = "Defrag";
            root.Add(defragButton);
            
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var runtimeEffectListLbl = new Label("Runtime Effects:");
                root.Add(runtimeEffectListLbl);

                if (effects.UseRuntimeCopies)
                {
                    // List all runtime copies
                    foreach (var effect in effects.Effects)
                    {
                        var effectCtn = new VisualElement();
                        effectCtn.Add(new Label(" * " + effect.Id + (effect.IsCopy ? " (Asset Copy)" : "(Runtime Only)")));
                        root.Add(effectCtn);
                    }    
                }
                else
                {
                    root.Add(new Label(" Runtime Copies are disabled. You are editing the assets directly."));
                }
            }
            
            
            return root;
        }
    }
}
#endif