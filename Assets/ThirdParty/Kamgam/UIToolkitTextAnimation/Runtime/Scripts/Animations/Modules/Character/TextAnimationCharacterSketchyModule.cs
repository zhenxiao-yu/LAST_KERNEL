using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterSketchyModule : ITextAnimationCharacterModule
    {
        public string name = "Sketchy";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        [Tooltip("How often the displacement will change per second.")]
        public float Speed = 1f;
        
        [FormerlySerializedAs("MaxDisplacement")]
        [Tooltip("The maximum difference in scale each character can have.")]
        public Vector2 MaxScaleDifference = new Vector2(0.1f, 0.1f);
        
        [Tooltip("The maximum angle each character can rotate in degrees.")]
        public float MaxRotation = 5f;
        
        [Tooltip("Which corners of the quad of each character are effected by this.")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;

        public void Reset()
        {
            MaxScaleDifference = new Vector2(10, 10f);
            AffectedCorners = AffectedCorners.one;
            
            m_lastChangeTime = 0f;
            m_lastFrame = -1;
            m_rndState = default;
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 2f);
            MaxScaleDifference = TextAnimation.GetRandomVector2(0f, 30f);
            MaxRotation = Random.Range(1f, 45f);
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterSketchyModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                MaxScaleDifference = typedSource.MaxScaleDifference;
                MaxRotation = typedSource.MaxRotation;
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
            // time *= Speed;
            
            // Ensure it is reset if the animation is restarted.
            if (m_lastChangeTime > unscaledTime)
                m_lastChangeTime = unscaledTime;

            // New random seed after a delay (controlled by Speed).
            if (unscaledTime - m_lastChangeTime > 1f / (preAppliedSpeed * Speed))
            {
                m_lastChangeTime = unscaledTime;
                Random.InitState((int)System.DateTime.Now.Ticks);
                m_rndState = Random.state;
            }
            
            // We restart the random sequence by resetting the state for the duration of one frame to ensure
            // consistent results until a new state is initialized after a certain time (defined by Speed).
            if (UnityEngine.Time.frameCount != m_lastFrame)
            {
                m_lastFrame = UnityEngine.Time.frameCount;
                Random.state = m_rndState;
            }

            // Apply change
            var scaleCenter = TextAnimation.GetQuadCenter(quadVertexData);
            
            float rndScaleX = Random.Range(1f - MaxScaleDifference.x, 1f + MaxScaleDifference.x);
            float rndScaleY = Random.Range(1f - MaxScaleDifference.y, 1f + MaxScaleDifference.y);
            var scaleFactors = new Vector3(rndScaleX, rndScaleY, 1f);
            TextAnimation.ScaleQuad(quadVertexData, scaleCenter, scaleFactors);

            float rotationZ = Random.Range(-MaxRotation, MaxRotation); 
            TextAnimation.RotateQuadAround(quadVertexData, scaleCenter, new Vector3(0f, 0f, rotationZ));
            
            return true;
        }
    }
}