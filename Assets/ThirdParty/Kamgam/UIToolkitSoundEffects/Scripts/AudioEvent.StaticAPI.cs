using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    public partial class AudioEvent
    {
        private static Stack<AudioEvent> s_pool = new Stack<AudioEvent>();

        /// <summary>
        /// Only valid while executing play and/or sending messages.
        /// </summary>
        public static AudioEvent CurrentEvent;
        
        /// <summary>
        /// Only valid while executing play and/or sending messages.
        /// </summary>
        public static EventBase CurrentUIEvent;

        public static AudioEvent GetFromPool()
        {
            if (s_pool.Count > 0)
            {
                // Null tolerant pool because some instance here may be real assets
                // and if deleted in the editor they become null.
                AudioEvent effect;
                do
                {
                    effect = s_pool.Pop();
                }
                while (effect == null);
                
                return effect;
            }
            else
                return new AudioEvent();
        }

        public static AudioEvent GetCopyFromPool(AudioEvent baseObject)
        {
            if (baseObject == null)
                return null;
            
            if (s_pool.Count > 0 )
            {
                var copy = s_pool.Pop();
                copy.CopyValuesFrom(baseObject);
                return copy;
            }
            else
            {
                var newCopy = new AudioEvent();
                newCopy.CopyValuesFrom(baseObject);
                return newCopy;
            }
        }
        
        public static void ResetAndReturnToPool(AudioEvent evt)
        {
            if (evt == null)
                return;

            evt.Clear();

            s_pool.Push(evt);
        }
        
        public static void ResetAndReturnToPool(IList<AudioEvent> events)
        {
            if (events == null)
                return;
            
            for (int i = events.Count - 1; i >= 0; i--)
            {
                ResetAndReturnToPool(events[i]);
            }

            events.Clear();
        }
    }
}