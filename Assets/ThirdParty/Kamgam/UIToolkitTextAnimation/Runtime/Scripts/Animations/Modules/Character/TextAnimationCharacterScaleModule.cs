using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterScaleModule : ITextAnimationCharacterModule
    {
        public string name = "Scale";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public float Speed = 1f;
        
        [Tooltip("How 'dense' the curve will be (think spread in x direction).")]
        public float Frequency = 3f;
        
        [Tooltip("If enabled then a sinus curve will be used. If disabled then you can configure a custom curve.")]
        public bool UseSinus = true;
        
        [Tooltip("If enabled it takes 1-Sinus or 1-Curve value.")]
        public bool Invert = true;
        
        [ShowIfAttribute("UseSinus", false, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve Curve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(0.4f,1f),
            new Keyframe(0.6f,1f),
            new Keyframe(1f,0f)
        });
        
        [Tooltip("Scale minimum which the scale will oscillate between (if a sinus curve is used).")]
        public Vector3 ScaleMin = new Vector3(1f, 1f, 1f);
        
        [Tooltip("Scale maximum which the scale will oscillate between (if a sinus curve is used).")]
        public Vector3 ScaleMax = new Vector3(2f, 2f, 1f);

        [Tooltip("Center is relative and normalized (if CenterAbsolute is turned OFF). 0/0 is the center of each character. 1/0 would be the right center, 1/1 the bottom right corner, -1/-1 the top left corner.")]
        public Vector3 Center = Vector3.zero;
        public bool CenterAbsolute = false;
        
        [Tooltip("Which corners of the quad of each character are effected by this and by how much. HINT: These are floats ;-)")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;

        
        public void Reset()
        {
            Speed = 1f;
            Frequency = 1f;
            UseSinus = true;
            Invert = true;
            Curve = TextAnimation.GetDefaultCurve();
            ScaleMax = new Vector3(2f, 2f, 1f);
            ScaleMin = new Vector3(1f, 1f, 1f);
            Center = Vector3.zero;
            CenterAbsolute = false;
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 3f);
            Frequency = Random.Range(0.1f, 3f);
            UseSinus = TextAnimation.GetRandomBool();
            Invert = TextAnimation.GetRandomBool();
            Curve = TextAnimation.GetDefaultCurve();
            ScaleMax = TextAnimation.GetRandomVector3(1f, 2f);
            ScaleMin = TextAnimation.GetRandomVector3(0.5f, 0.98f);
            Center = TextAnimation.GetRandomVector3(0f, 1f);
            CenterAbsolute = TextAnimation.GetRandomBool();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterScaleModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                Frequency = typedSource.Frequency;
                UseSinus = typedSource.UseSinus;
                Invert = typedSource.Invert;
                Curve = typedSource.Curve;
                ScaleMax = typedSource.ScaleMax;
                Center = typedSource.Center;
                CenterAbsolute = typedSource.CenterAbsolute;
                AffectedCorners = typedSource.AffectedCorners;
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
            float curveValue = UseSinus ? Mathf.Abs(Mathf.Sin(t)) : Curve.Evaluate(t % 1f);
            if (Invert)
                curveValue = 1f - curveValue;
            var scale = ScaleMin + curveValue * (ScaleMax - ScaleMin);

            // Calc scale center
            Vector3 scaleCenter;
            if (CenterAbsolute)
                scaleCenter = Center;
            else
                scaleCenter = TextAnimation.GetQuadCenter(quadVertexData, Center);
            
            // Apply rotation
            TextAnimation.ScaleQuad(quadVertexData, scaleCenter, scale, AffectedCorners);
            
            return true;
        }
    }
}