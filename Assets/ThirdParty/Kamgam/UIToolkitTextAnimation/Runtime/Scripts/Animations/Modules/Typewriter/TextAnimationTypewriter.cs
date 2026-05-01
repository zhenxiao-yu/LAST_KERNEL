using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [CreateAssetMenu(fileName = "UITK TextAnimation Typewriter", menuName = "UI Toolkit/TextAnimation/Typewriter Animation", order = 402)]
    public class TextAnimationTypewriter : TextAnimation
    {
        [Tooltip("If enabled it will restart if a new text is set. NOTICE: It will NOT restart if new text is appended.")]
        public bool RestartOnNewText = true;
        
        [Tooltip("Inverted animations are useful for fading stuff out after a certain period of time.")]
        public bool InvertAnimation = false;
        
        [Header("Timing")]

        [Tooltip("Inverts the order in which the characters are animated. Default is first to last.")]
        public bool InvertOrder = false;
        
        [Tooltip("The delay before the animation starts in seconds.")]
        public float Delay = 0f;
        
        [Tooltip("A speed multiplier for convenience.")]
        public float Speed = 1f;
        
        [Tooltip("The delay between characters in seconds.")]
        public float CharacterDelay = 0.02f;
        
        [Tooltip("The animation duration per character in seconds.")]
        public float CharacterDuration = 0.3f;
        
        [SerializeReference]
        public List<ITextAnimationTypewriterModule> Modules = new List<ITextAnimationTypewriterModule>();
        
        public override int GetModuleCount() => Modules.Count;
        public override ITextAnimationModule GetModuleAt(int index) => Modules[index];
        
        public ITextAnimationTypewriterModule GetModule(string name, int index = 0)
        {
            int cIndex = 0;
            foreach (var module in Modules)
            {
                if (module.GetName() == name)
                {
                    if (cIndex == index)
                        return module;
                    cIndex++;
                }
            }

            return default;
        }

        public ITextAnimationTypewriterModule GetModule(int index)
        {
            if (Modules.Count > index)
                return Modules[index];

            return default;
        }
        
        public override T GetModuleGeneric<T>(string name, int index = 0)
        {
            return (T)GetModule(name, index);
        }

        /// <summary>
        /// The index is the number of elements ot type T, not the overall index.<br />
        /// If you want to pick by overall index then use the non generic GetModule().
        /// </summary>
        /// <param name="index">The index is the number of elements ot type T, not the overall index.</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public override T GetModuleGeneric<T>(int index)
        {
            var type = typeof(T);
            
            int cIndex = 0;
            foreach (var module in Modules)
            {
                if (module.GetType() == type)
                {
                    if (cIndex == index)
                        return (T) module;
                    cIndex++;
                }
            }

            return default;
        }
        
        private int m_completedQuads;
        
        protected override TextAnimation createInstance()
        {
            return CreateInstance<TextAnimationTypewriter>();
        }

        /// <summary>
        /// Clear the internal state before returning to pool.
        /// </summary>
        public override void Reset()
        {
            base.Reset();
            TextAnimationModulePool.ResetAndReturnListToPool(Modules);
            m_completedQuads = 0;
        }

        public string GetClassName()
        {
            return TextAnimationManipulator.TEXT_TYPEWRITER_CLASSNAME + Id;
        }

        public override void CopyValuesFrom(TextAnimation source)
        {
            if (source is TextAnimationTypewriter src)
            {
                Delay = src.Delay;
                Speed = src.Speed;
                CharacterDelay = src.CharacterDelay;
                CharacterDuration = src.CharacterDuration;
                RestartOnNewText = src.RestartOnNewText;
                InvertAnimation = src.InvertAnimation;
                InvertOrder = src.InvertOrder;
                
                copyModulesFrom(src);
            }
        }
        
        private void copyModulesFrom(TextAnimationTypewriter src)
        {
            for (int i = 0; i < src.Modules.Count; i++)
            {
                bool moduleMissing = Modules.Count <= i;  
                bool typesDiffer = Modules.Count > i && Modules[i].GetType() != src.Modules[i].GetType();
                if (typesDiffer)
                {
                    // Return module and replace with new one with matching type.
                    TextAnimationModulePool.ResetAndReturnToPool(Modules[i]);
                    Modules[i] = null;
                    var newModule = (ITextAnimationTypewriterModule)TextAnimationModulePool.GetCopyFromPool(src.Modules[i].GetType(), src.Modules[i]);
                    newModule.CopyValuesFrom(src.Modules[i]);
                    Modules[i] = newModule;
                }
                else if (moduleMissing)
                {
                    // Get new module and add.
                    var newModule = (ITextAnimationTypewriterModule)TextAnimationModulePool.GetCopyFromPool(src.Modules[i].GetType(), src.Modules[i]);
                    newModule.CopyValuesFrom(src.Modules[i]);
                    Modules.Add(newModule);
                }
                else
                {
                    // Just copy the values.
                    Modules[i].CopyValuesFrom(src.Modules[i]);
                }
            }
        }
        
        public override void RandomizeTiming()
        {
            base.RandomizeTiming();

            Speed = Random.Range(0.1f, 10f);
            CharacterDelay = Random.Range(0.01f, 0.1f);
            CharacterDuration = Random.Range(0.1f, 0.5f);
        }

        public override void Restart(
            bool paused = false,
            float time = 0,
            ChangeEvent<string> evt = null,
            int previousQuadCount = -1,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos = null
            )
        {
            m_completedQuads = 0;

            // If an event was given then this means the animation was updated due to a change in text.
            if (evt != null && evt.previousValue != evt.newValue)
            {
                // First we check if text was appended?
                bool textWasAppended = evt.newValue.Length > evt.previousValue.Length && evt.newValue.StartsWith(evt.previousValue);
                bool textWasShortened = evt.previousValue.StartsWith(evt.newValue);
                if (textWasAppended || textWasShortened)
                {
                    // Mathf.Min because we do not want to fast forward if the normal animation time is behind.
                    if( time >= previousQuadCount * CharacterDelay)
                        m_completedQuads = previousQuadCount;
                    
                    // Sum up all the delays from delay tags in front of the current quad
                    float delayFromTags = calcDelaySumFromTags(previousQuadCount, delayTagInfos);
                    time = Delay + delayFromTags / Speed + Mathf.Min(time, previousQuadCount * CharacterDelay) * Speed;
                }
                else if (RestartOnNewText)
                {
                    time = Delay;
                }
            }
            
            base.Restart(paused, time, evt);
        }

        /// <summary>
        /// Returns whether or not the animation is still playing.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="tagInfo"></param>
        /// <param name="characterIndex"></param>
        /// <param name="quadIndex"></param>
        /// <param name="totalQuadCount"></param>
        /// <param name="time"></param>
        /// <param name="unscaledDeltaTime"></param>
        /// <param name="quadVertexData"></param>
        /// <param name="delayTagInfos"></param>
        /// <returns></returns>
        public override bool UpdateCharacter(
            TextElement element, TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            int quadIndex, int totalQuadCount,
            float time, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            float unscaledTime;
            
            // Sum up all the delays from delay tags in front of the current quad
            float delayFromTags = calcDelaySumFromTags(quadIndex, delayTagInfos);

            // Ensure delays at the start never make time go below 0.
            time = Mathf.Max(0f, time - Delay - delayFromTags/Speed);

            if (InvertAnimation)
            {
                // If inverted then do nothing until the animation starts to reduce
                // collisions with other animations.
                if (time == 0f)
                    return true;

                // Invert time
                float totalDuration = (totalQuadCount - 1) * CharacterDelay + CharacterDuration;
                unscaledTime = Mathf.Max(0f, totalDuration - time);
                time = Mathf.Max(0f, totalDuration - time * Speed);
            }
            else
            {
                unscaledTime = time;
                time *= Speed;
            }

            if (InvertOrder)
                quadIndex = totalQuadCount - 1 - quadIndex;

            float quadStartTime = quadIndex * CharacterDelay;
            float quadTimeRaw = (time - quadStartTime) / CharacterDuration;

            // Don't do anything if finished animating this character
            bool quadFinished = (!InvertAnimation && quadTimeRaw > 1f) || (InvertAnimation && quadTimeRaw < 0f); 
            if (quadFinished || m_completedQuads > quadIndex)
            {
                m_completedQuads = Mathf.Max(m_completedQuads, quadIndex);
                
                // Sadly if the animation is inverted we have to constantly set the end state (which means the animation never finishes)
                if (!InvertAnimation)
                {
                    // Only in normal animation mode we can stop the constant updating of text.
                    return false;
                }
            }

            // Value between 0 and 1
            float progress = Mathf.Clamp01(quadTimeRaw);

            updateCharacterNormalized(
                element, /* tagInfo, characterIndex, */ // <- These are bogus in typewriters anyways
                quadIndex, totalQuadCount,
                time, unscaledTime, unscaledDeltaTime, progress,
                quadVertexData, Delay, Speed, delayTagInfos);
            return progress < 1f;
        }

        protected float calcDelaySumFromTags(int quadIndex, List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            float delayFromTags = 0f;
                
            foreach (var info in delayTagInfos)
            {
                //Debug.Log(info.QuadIndex);
                if (info.QuadIndex <= quadIndex)
                    delayFromTags += info.DelayInSec;
            }

            return delayFromTags;
        }

        /// <summary>
        /// Animation function called on every quad of the text.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="quadIndex"></param>
        /// <param name="totalQuadCount"></param>
        /// <param name="time"></param>
        /// <param name="unscaledTime"></param>
        /// <param name="unscaledDeltaTime"></param>
        /// <param name="progress">Normalized progress of the quad animation (float between 0 and 1)</param>
        /// <param name="quadVertexData"></param>
        /// <param name="delay"></param>
        /// <param name="speed"></param>
        /// <param name="delayTagInfos"></param>
        protected void updateCharacterNormalized(
            TextElement element,
            int quadIndex, int totalQuadCount,
            float time, float unscaledTime, float unscaledDeltaTime, float progress,
            TextInfoAccessor.QuadVertexData quadVertexData, float delay, float speed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            foreach (var module in Modules)
            {
                module.UpdateCharacterNormalized(
                    element,
                    quadIndex, totalQuadCount,
                    time, unscaledTime, unscaledDeltaTime, progress,
                    quadVertexData, delay, speed, delayTagInfos);
            }
        }
    }
}