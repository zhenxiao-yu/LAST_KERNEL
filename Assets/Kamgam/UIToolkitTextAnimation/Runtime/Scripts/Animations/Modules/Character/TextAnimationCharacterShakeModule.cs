using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterShakeModule : ITextAnimationCharacterModule
    {
        public string name = "Shake";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public float Speed = 30f;
        
        [Tooltip("The maximum displacement in pixels.")]
        public Vector2 MaxDisplacement = new Vector2(1, 3f);
        
        [Tooltip("Offset in pixels. This is added to the position after the shake. You can use it to offset the shake to only shake upward for example.")]
        public Vector2 Offset = new Vector2(0, 0f);
        
        [Tooltip("Which corners of the quad of each character are effected by this and by how much. HINT: These are floats ;-)")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        
        public void Reset()
        {
            Speed = 1f;
            MaxDisplacement = new Vector2(10, 10f);
            Offset = new Vector2(0, 0f);
            AffectedCorners = AffectedCorners.one;
            
            m_lastChangeTime = 0f;
            m_lastFrame = -1;
            m_rndState = default;
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 3f);
            MaxDisplacement = TextAnimation.GetRandomVector2(0f, 30f);
            Offset = TextAnimation.GetRandomVector2(0f, 3f);
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterShakeModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                MaxDisplacement = typedSource.MaxDisplacement;
                Offset = typedSource.Offset;
                AffectedCorners = typedSource.AffectedCorners;
            }
            
            m_lastChangeTime = 0f;
            m_lastFrame = -1;
        }
        
        [System.NonSerialized]
        protected float m_lastChangeTime = 0f; 

        [System.NonSerialized]
        protected int m_lastFrame = -1;
        
        [System.NonSerialized]
        Random.State m_rndState;
        
        public bool UpdateCharacter(
            TextElement element, TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            float time, float unscaledTime, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            //time *= Speed;
            
            // Ensure it is reset if the animation is restarted.
            if (m_lastChangeTime > unscaledTime)
                m_lastChangeTime = unscaledTime;
            
            // New random seed after a delay (controlled by frequency).
            if (unscaledTime - m_lastChangeTime > 1 / (preAppliedSpeed * Speed)) 
            {
                m_lastChangeTime = unscaledTime;
                Random.InitState((int)System.DateTime.Now.Ticks);
                m_rndState = Random.state;
            }
            
            // We restart the random sequence by resetting the state for the duration of one frame to ensure
            // consistent positioning until a new state is initialized after a certain time (defined by Speed).
            if (UnityEngine.Time.frameCount != m_lastFrame)
            {
                m_lastFrame = UnityEngine.Time.frameCount;
                Random.state = m_rndState;
            }
            
            var rndDir = Random.insideUnitCircle.normalized;
            float deltaX = rndDir.x * MaxDisplacement.x + Offset.x;
            float deltaY = -rndDir.y * MaxDisplacement.y - Offset.y; // invert axis

            // Apply change
            quadVertexData.BottomLeftPosition += new Vector3(deltaX * AffectedCorners.BottomLeft, deltaY * AffectedCorners.BottomLeft, 0f);
            quadVertexData.TopLeftPosition += new Vector3(deltaX * AffectedCorners.TopLeft, deltaY * AffectedCorners.TopLeft, 0f);
            quadVertexData.TopRightPosition += new Vector3(deltaX * AffectedCorners.TopRight, deltaY * AffectedCorners.TopRight, 0f);
            quadVertexData.BottomRightPosition += new Vector3(deltaX * AffectedCorners.BottomRight, deltaY * AffectedCorners.BottomRight, 0f);

            return true;
        }
    }
}