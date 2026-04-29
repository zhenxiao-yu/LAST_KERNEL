using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [CreateAssetMenu(fileName = "UITK TextAnimation Character", menuName = "UI Toolkit/TextAnimation/Character Animation", order = 402)]
    public class TextAnimationCharacter : TextAnimation
    {
        [Tooltip("The delay before the animation starts in seconds.\nNOTICE: This is not affected by the speed.")]
        public float Delay = 0f;
        
        [Tooltip("A speed multiplier that is applied to the speed of all modules.")]
        public float Speed = 1f;

        [SerializeReference]
        public List<ITextAnimationCharacterModule> Modules = new List<ITextAnimationCharacterModule>();

        public override int GetModuleCount() => Modules.Count;
        public override ITextAnimationModule GetModuleAt(int index) => Modules[index];
        
        public ITextAnimationCharacterModule GetModule(string name, int index = 0)
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

        public ITextAnimationCharacterModule GetModule(int index)
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
        
        protected override TextAnimation createInstance()
        {
            return CreateInstance<TextAnimationCharacter>();
        }

        public override void CopyValuesFrom(TextAnimation source)
        {
            if (source is TextAnimationCharacter src)
            {
                Delay = src.Delay;
                Speed = src.Speed;
                
                copyModulesFrom(src);
            }
        }

        public override void Reset()
        {
            base.Reset();

            foreach (var module in Modules)
            {
                module.Reset();
            }
        }

        private void copyModulesFrom(TextAnimationCharacter src)
        {
            int max = Mathf.Max(src.Modules.Count, Modules.Count);
            for (int i = 0; i < max; i++)
            {
                if (i < src.Modules.Count)
                {
                    if (src.Modules[i] == null)
                        continue;

                    bool moduleMissing = Modules.Count <= i;
                    bool typesDiffer = Modules.Count > i && Modules[i].GetType() != src.Modules[i].GetType();
                    if (typesDiffer)
                    {
                        // Return module and replace with new one with matching type.
                        TextAnimationModulePool.ResetAndReturnToPool(Modules[i]);
                        Modules[i] = null;
                        var newModule =
                            (ITextAnimationCharacterModule)TextAnimationModulePool.GetCopyFromPool(
                                src.Modules[i].GetType(), src.Modules[i]);
                        Modules[i] = newModule;
                    }
                    else if (moduleMissing)
                    {
                        // Get new module and add.
                        var newModule =
                            (ITextAnimationCharacterModule)TextAnimationModulePool.GetCopyFromPool(
                                src.Modules[i].GetType(), src.Modules[i]);
                        Modules.Add(newModule);
                    }
                    else
                    {
                        // Just copy the values.
                        Modules[i].CopyValuesFrom(src.Modules[i]);
                    }
                }
                else
                {
                    var m = Modules[i];
                    TextAnimationModulePool.ResetAndReturnToPool(m);
                    Modules.RemoveAt(i);
                }
            }
        }

        public override void RandomizeTiming()
        {
            base.RandomizeTiming();

            Speed = Random.Range(0.1f, 2f);
        }

        public override bool UpdateCharacter(
            TextElement element, TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            int quadIndex, int totalQuadCount,
            float time, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            float unscaledTime = time;
            time -= Delay;
            time *= Speed;

            if (time < 0f)
                time = 0f;
            
            return updateCharacter(element, tagInfo, characterIndex,
                /* int quadIndex, int totalQuadCount, */ // <- these are bogus data in character animations.
                time, unscaledTime, unscaledDeltaTime,
                quadVertexData, Delay, Speed, delayTagInfos);
        }

        /// <summary>
        /// Animation function called on every quad of the text. White space characters and tags are ignored.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="tagInfo"></param>
        /// <param name="characterIndex">The character index is based on the finally rendered characters including white space chars but no tags.</param>
        /// <param name="time"></param>
        /// <param name="unscaledTime"></param>
        /// <param name="unscaledDeltaTime"></param>
        /// <param name="quadVertexData"></param>
        /// <param name="delay"></param>
        /// <param name="speed"></param>
        /// <returns></returns>
        protected bool updateCharacter(
            TextElement element,
            TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            float time, float unscaledTime, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData, float delay, float speed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            bool playing = false;
            foreach (var module in Modules)
            {
                playing |= module.UpdateCharacter(
                    element, tagInfo, characterIndex, time, unscaledTime,
                    unscaledDeltaTime, quadVertexData, delay, speed, delayTagInfos);
            }
            
            return playing;
        }
    }
}