using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterTranslateModule : ITextAnimationCharacterModule
    {
        public string name = "Translate";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;

        public float Speed = 1f;
        
        [Tooltip("Multiplier for the curve what usually is normalized between -1 and 1 (if sinus is used).")]
        public Vector2 Amplitude = new Vector2(0, 10f);
        
        [Tooltip("Offset that is added to the curve value (after amplification).")]
        public Vector2 Offset = new Vector2(0, -5f);
        
        [Tooltip("How 'dense' the curve will be (think spread in x direction).")]
        public float Frequency = 1f;
        
        [Tooltip("If enabled the the wave x-axis will be based on the character x position. Otherwise it is based on the character index.\n" +
                 "Useful for multiline texts to sync the movement of the lines.")]
        public bool UsePosition = false;

        [ShowIfAttribute("UsePosition", true, ShowIfAttribute.DisablingType.DontDraw)]
        [Tooltip("If UsePosition is enabled then this will define a divisor by which the position is divided to make it behave more like a character index (i.e. width of average character in pixels).")]
        public float PixelsPerCharacterWidth = 22f;
        
        [Tooltip("If enabled the a sinus curve will be used. Otherwise you can specify a custom curve.")]
        public bool UseSinus = true;
        
        [ShowIfAttribute("UseSinus", false, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve Curve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(0.4f,1f),
            new Keyframe(0.6f,1f),
            new Keyframe(1f,0f)
        });
        
        [Tooltip("Which corners of the quad of each character are effected by this. HINT: They are floats and fun to play around with ;)")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;

        public void Reset()
        {
            Speed = 1f;
            Amplitude = new Vector2(0, 10f);
            Offset = new Vector2(0, -5f);
            Frequency = 1f;
        
            UsePosition = false;

            PixelsPerCharacterWidth = 22f;
        
            UseSinus = true;
        
            Curve = TextAnimation.GetRandomCurve();
        }

        public void Randomize()
        {
            Speed = Random.Range(0.2f, 3f);
            Amplitude = TextAnimation.GetRandomVector2(0, 10f);
            Offset = TextAnimation.GetRandomVector2(0, 5f);
            Frequency = Random.Range(0.03f, 3f);
        
            UsePosition = TextAnimation.GetRandomBool();

            PixelsPerCharacterWidth = 22f;
        
            UseSinus = TextAnimation.GetRandomBool();
        
            Curve = TextAnimation.GetRandomCurve();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterTranslateModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed; 
                Amplitude = typedSource.Amplitude;
                Offset = typedSource.Offset;
                Frequency = typedSource.Frequency;
                UsePosition = typedSource.UsePosition;
                PixelsPerCharacterWidth = typedSource.PixelsPerCharacterWidth;
                AffectedCorners = typedSource.AffectedCorners;
                UseSinus = typedSource.UseSinus;
                Curve = typedSource.Curve;
            }
        }

        public bool UpdateCharacter(
            TextElement element, TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            float time, float unscaledTime, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            time *= Speed;
            
            // Apply change
            float valueX = characterIndex;
            if (UsePosition)
                valueX = quadVertexData.BottomLeftPosition.x / PixelsPerCharacterWidth;
            float t = time + (10_000 - valueX) * Frequency * (UseSinus ? 1f : 0.1f);
            float curveValue = UseSinus ? Mathf.Sin(t) : Curve.Evaluate(t % 1f);
            var delta = curveValue * Amplitude + Offset;
            
            // -y because the coordinate system starts top left and we want "+" to make the letters go up.
            quadVertexData.BottomLeftPosition += new Vector3(delta.x * AffectedCorners.BottomLeft, -delta.y * AffectedCorners.BottomLeft, 0f);   
            quadVertexData.TopLeftPosition += new Vector3(delta.x * AffectedCorners.TopLeft, -delta.y * AffectedCorners.TopLeft, 0f);
            quadVertexData.TopRightPosition += new Vector3(delta.x * AffectedCorners.TopRight, -delta.y * AffectedCorners.TopRight, 0f);
            quadVertexData.BottomRightPosition += new Vector3(delta.x * AffectedCorners.BottomRight, -delta.y * AffectedCorners.BottomRight, 0f);

            return true;
        }
    }
}