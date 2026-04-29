using Kamgam.UIToolkitSoundEffects;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects.Examples
{
    public class CustomScriptReceivingAMessage : MonoBehaviour
    {
        public void HelloWorld()
        {
            var evt = AudioEvent.CurrentEvent;
            var effect = evt.GetEffect();
            var visualElement = AudioEvent.CurrentUIEvent.target as VisualElement;
            Debug.Log($"This message was triggered by the audio event: " +
                      $"{evt.eventType} and effect: {effect.Id} and element {visualElement}");
        }
    }
}
