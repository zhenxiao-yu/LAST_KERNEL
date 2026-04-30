using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Kamgam.UIToolkitSoundEffects
{
    public class SoundEventReceiver : MonoBehaviour
    {
        [System.NonSerialized]
        public static List<SoundEventReceiver> Registry = new List<SoundEventReceiver>();

        [Tooltip("If enabled then the messages will be sent even if this object is disabled.")]
        public bool TriggerIfInactive = false;
        
        [Tooltip("These events will be executed if the object receives a message from an audio event.")]
        public UnityEvent<string> Events;

        public List<GameObject> Receivers = new List<GameObject>();
        
        public virtual void Awake()
        {
            Registry.Add(this);
        }
        
        public virtual void OnDestroy()
        {
            Registry.Remove(this);
        }

        public virtual void TriggerSendMessage(string methodName)
        {
            if (gameObject.activeInHierarchy || TriggerIfInactive)
            {
                foreach (var receiver in Receivers)
                {
                    if (receiver == null)
                        continue;

                    if (receiver.activeInHierarchy || TriggerIfInactive)
                    {
                        receiver.SendMessage(methodName);
                    }
                }
            }
        }

        public static void SendMessageToAll(string methodName)
        {
            foreach (var receiver in Registry)
            {
                if (receiver == null)
                    continue;
                
                foreach (var go in receiver.Receivers)
                {
                    if(go == null)
                        continue;
                    
                    go.SendMessage(methodName);
                }
            }
        }
    }
}