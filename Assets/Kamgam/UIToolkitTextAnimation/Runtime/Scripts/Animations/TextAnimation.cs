using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public struct AffectedCorners
    {
        public float BottomLeft;
        public float TopLeft;
        public float TopRight;
        public float BottomRight;
            
        public static readonly AffectedCorners one = new AffectedCorners(1f, 1f, 1f, 1f);

        public AffectedCorners(float bottomLeft, float topLeft, float topRight, float bottomRight)
        {
            BottomLeft = bottomLeft;
            TopLeft = topLeft;
            TopRight = topRight;
            BottomRight = bottomRight;
        }
        
        public static AffectedCorners operator *(AffectedCorners a, AffectedCorners b)
            => new AffectedCorners(
                a.BottomLeft * b.BottomLeft,
                a.TopLeft * b.TopLeft,
                a.TopRight * b.TopRight,
                a.BottomRight * b.BottomRight
            );
    }
    
    /// <summary>
    /// Each config object serves as two things. A storage of config and state variables (isPlaying, time) and
    /// as the holder of the UpdateCharacter() method which does the actual vertex modifications.
    /// <br /> 
    /// Before returning to the pool or deletion Clear() must be called to unregister the object form its parents.
    /// The static pooling methods will do this for you.
    /// </summary>
    // [CreateAssetMenu(fileName = "UITK TextAnimations", menuName = "UI Toolkit/TextAnimation/Animation Config", order = 402)]
#if KAMGAM_VISUAL_SCRIPTING
    // Why? See: https://forum.unity.com/threads/unable-to-provide-a-default-for-getvalue-on-object-valueinput.1140022/#post-9138727
    [Unity.VisualScripting.Inspectable]
#endif
    [System.Serializable]
    public abstract partial class TextAnimation : ScriptableObject
    {
        [FormerlySerializedAs("id")]
        [Tooltip("Use this as the animation id in your text link tags. For example: if the id is 'shake' then the tag would be <link anim=\"shake\">Shake It!</link>.")]
        [SerializeField]
        protected string m_id = "";
        public string Id
        {
            get => m_id;
            set
            {
                if (string.CompareOrdinal(m_id, value) == 0)
                    return;

                m_id = value;
                MarkAsChanged();
            }
        }

        /// <summary>
        /// Enable if you don't want the config to be changed if the parent changes.
        /// Usually the parents are the configs listed in the config provider and each animation gets a new copy of the config matching for each matching tag in the text.
        /// </summary>
        [System.NonSerialized]
        public bool IgnoreChangesInParent = false;

        /// <summary>
        /// Called if values have been changed either on the object itself or a parent.
        /// </summary>
        //public System.Action<TextAnimationConfig> OnValueChanged;

        [System.NonSerialized]
        public TextAnimation Parent;

        [System.NonSerialized]
        protected float m_time;

        /// <summary>
        /// To set the time please use the Restart() method.
        /// </summary>
        public float Time => m_time;
        
        [System.NonSerialized]
        public bool IsPlaying = true;

        protected abstract TextAnimation createInstance();

        [System.NonSerialized]
        public int ValueChangeIndex = 0;
        
        [System.NonSerialized]
        protected int syncedValueChangeIndex = 0;

        /// <summary>
        /// Returns the module with the given name. If multiple with the same name exists then the index is used.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract T GetModuleGeneric<T>(string name, int index = 0) where T : ITextAnimationModule;
        public abstract ITextAnimationModule GetModuleAt(int index);
        
        /// <summary>
        /// Returns the module at the index or default.
        /// </summary>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public abstract T GetModuleGeneric<T>(int index) where T : ITextAnimationModule;

        public abstract int GetModuleCount();
        
        public void MarkAsChanged()
        {
            ValueChangeIndex++;
        }
        
        public TextAnimation Copy()
        {
            var copy = createInstance();
            copy.internalCopyValuesFrom(this);
            copy.Parent = this;
            
            return copy;
        }

        public virtual void Reset()
        {
            m_time = 0f;
            IgnoreChangesInParent = false;
            Parent = null;
        }
        
        public void PullChangedValuesIfNecessary()
        {
            if (IgnoreChangesInParent || Parent == null)
                return;
            
            // propagate change upwards.
            Parent.PullChangedValuesIfNecessary();

            if (syncedValueChangeIndex != Parent.ValueChangeIndex)
            {
                // copy final values
                internalCopyValuesFrom(Parent);
                syncedValueChangeIndex = Parent.ValueChangeIndex;
                ValueChangeIndex = Parent.ValueChangeIndex;
            }
        }

        protected void internalCopyValuesFrom(TextAnimation source)
        {
            Id = source.Id;
            IgnoreChangesInParent = source.IgnoreChangesInParent;
            CopyValuesFrom(source);
        }

        /// <summary>
        /// Implement this for configs that have custom fields in addition to id.
        /// </summary>
        /// <param name="source"></param>
        public abstract void CopyValuesFrom(TextAnimation source);

        /// <summary>
        /// The character animation function. This is where your animation can change the character mesh properties.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="tagInfo">Will be null for typewriter animations.</param>
        /// <param name="characterIndex">The index on the character in the rendered text. Useful for sequential stuff like typewriter effects. Will be -1 for typewriter animations. Use quadIndex instead.</param>
        /// <param name="quadIndex"></param>
        /// <param name="totalQuadCount"></param>
        /// <param name="time">The total unscaled total animation time since Restart().</param>
        /// <param name="unscaledDeltaTime">The total unscaled delta time of the last frame.</param>
        /// <param name="quadVertexData">Vertex data of 4 vertices combined.</param>
        /// <param name="delayTagInfos">All delay tags that are in the text.</param>
        /// <returns>TRUE if the animation requires on or more steps afterwards before it finished (i.e. it's not finished) or FALSE if the animation is finished and needs no more updates.</returns>
        public abstract bool UpdateCharacter(
            TextElement element,
            TextInfoAccessor.AnimationTagInfo tagInfo,
            int characterIndex,
            int quadIndex,
            int totalQuadCount,
            float time,
            float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos);
        
        
        public virtual void Pause()
        {
            IsPlaying = false;
        }
        
        public virtual void Resume()
        {
            Play();
        }
        
        public virtual void Play()
        {
            IsPlaying = true;
        }
        
        public virtual void Restart(
            bool paused = false,
            float time = 0f,
            // These are useful for typewriter style animations:
            ChangeEvent<string> evt = null,
            int previousQuadCount = -1,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos = null
        )
        {
            IsPlaying = !paused;
            this.m_time = time;
        }

        public void AdvanceAnimationTime(float deltaTime)
        {
            m_time += deltaTime;
        }

        /// <summary>
        /// If implemented it randomizes the parameters of the animation. This is useful for prototyping.
        /// </summary>
        public virtual void Randomize(){}
        
        /// <summary>
        /// If implemented it randomizes the parameters of the animation. This is useful for prototyping.
        /// </summary>
        public virtual void RandomizeTiming(){}

#if UNITY_EDITOR
        protected virtual void OnValidate()
        {
            MarkAsChanged();
        }
#endif
    }
}