using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterRotateModule : ITextAnimationCharacterModule
    {
        public string name = "Rotate";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public float Speed = 1f;
        
        [Tooltip("How 'dense' the curve will be (think spread in x direction).")]
        public float Frequency = 3f;
        
        [Tooltip("If enabled then a sinus curve will be used. If disabled then you can configure a custom curve.")]
        public bool UseSinus = true;
        
        [ShowIfAttribute("UseSinus", false, ShowIfAttribute.DisablingType.DontDraw)]
        [Tooltip("To get a nice continuous animation the first and last value should have a value of 0 and a time of 0 -> 1.")]
        public AnimationCurve Curve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(0.4f,1f),
            new Keyframe(0.6f,1f),
            new Keyframe(1f,0f)
        });
        
        [Tooltip("Multiplier for the curve value which usually is normalized between -1 and +1 (if sinus is used).")]
        public float Amplitude = 1f;
        
        [Tooltip("Center is relative and normalized (if CenterAbsolute is turned OFF). 0/0 is the center of each character. 1/0 would be the right center, 1/1 the bottom right corner, -1/-1 the top left corner.")]
        public Vector3 Center = Vector3.zero;
        public bool CenterAbsolute = false;
        
        [Tooltip("Which corners of the quad of each character are effected by this.")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        
        /// <summary>
        /// The axis to rotate around. Usually z-axis (0/0/1) is fine. It's here for future use. Sadly right now the quads are clipped if rotated by anything but the z-axis.
        /// </summary>
        [System.NonSerialized]
        public Vector3 RotationAxis = new Vector3(0, 0, 1f);
        
        public void Reset()
        {
            Speed = 1f;
            Frequency = 1f;
            UseSinus = true;
            Curve = TextAnimation.GetDefaultCurve();
            Amplitude = 1f;
            Center = Vector3.zero;
            CenterAbsolute = false;
            AffectedCorners = AffectedCorners.one;
            RotationAxis = new Vector3(0, 0, 1f);
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 3f);
            Frequency = Random.Range(0.1f, 3f);
            UseSinus = TextAnimation.GetRandomBool();
            Curve = TextAnimation.GetDefaultCurve();
            Amplitude = Random.Range(0.1f, 2f);
            Center = TextAnimation.GetRandomVector3(0f, 1f);
            CenterAbsolute = TextAnimation.GetRandomBool();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
            RotationAxis = new Vector3(0, 0, 1f);
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterRotateModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                Frequency = typedSource.Frequency;
                AffectedCorners = typedSource.AffectedCorners;
                UseSinus = typedSource.UseSinus;
                Curve = typedSource.Curve;
                Amplitude = typedSource.Amplitude;
                Center = typedSource.Center;
                CenterAbsolute = typedSource.CenterAbsolute;
                RotationAxis = typedSource.RotationAxis;
            }
        }

        public bool UpdateCharacter(
            TextElement element, TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            float time, float unscaledTime, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            time *= Speed;
            
            // Calc rotation amount
            float t = time + (10_000 - characterIndex) * Frequency * 0.01f;
            float curveValue = UseSinus ? Mathf.Sin(t) : Curve.Evaluate(t % 1f);
            var rotation = curveValue * Amplitude * 180f;

            // Calc rotation center
            Vector3 rotationCenter;
            if (CenterAbsolute)
                rotationCenter = Center;
            else
                rotationCenter = TextAnimation.GetQuadCenter(quadVertexData, Center);
            
            // Apply rotation
            TextAnimation.RotateQuadAround(quadVertexData, rotationCenter, RotationAxis * rotation, AffectedCorners);
            
            return true;
        }
    }
}