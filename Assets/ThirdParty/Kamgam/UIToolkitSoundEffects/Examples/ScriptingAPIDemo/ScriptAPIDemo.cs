using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects.Examples
{
    public class ScriptAPIDemo : MonoBehaviour
    {
        public UIDocument Doc;
        public AudioClip ClipClick;
        public AudioClip ClipLoop;
        
        public void Start()
        {
            // Add SoundEffectDocument to UIDocument if not yet added.
            // Will als prepare any UIE Element with sound effect that are part of the document at this time.
            // This is optional if you use the SEAddEffect() extension method since that will do it for you. 
            // Doc.SEInitializeHierarchy();
            
            // Ad a click sound event.
            var btnA = Doc.rootVisualElement.Q<Button>("ButtonA");
            btnA.SEAddEffect(clip: ClipClick, eventType: ElementEventType.OnPointerClick);
            
            // Add a looping sound on click.
            var btnB = Doc.rootVisualElement.Q<Button>("ButtonB");
            var effectB = btnB.SEAddEffect(clip: ClipLoop, eventType: ElementEventType.OnPointerClick);
            effectB.Events[0].AudioSourceSettings.Loop = true;
            effectB.Events[0].AudioSourceSettings.LoopDuration = 10f; // <- loop for 10 seconds.

            // Stop the loop early.
            btnB.RegisterCallback<ClickEvent>((e) =>
            {
                StartCoroutine(StopLoopInTwo());
            });
        }
        
        public IEnumerator StopLoopInTwo()
        {
            yield return new WaitForSeconds(2f);
            
            var btnB = Doc.rootVisualElement.Q<Button>("ButtonB");
            var effectB = btnB.SEGetEffect();
            effectB.Events[0].Stop();
        }
    }
}
