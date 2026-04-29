using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterGradientModule : ITextAnimationCharacterModule
    {
        public string name = "Gradient"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public float Speed = 1f;
        
        [Tooltip("How 'dense' the gradient will be.")]
        public float Frequency = 1f;
        
        [Tooltip("What direction the gradient will move.")]
        public float Direction = 1f;
        
        [Tooltip("If enabled the each character will have only a single color instead of a gradient.")]
        public bool SingleColorPerCharacter = false;
        
        [Range(0,1)]
        [Tooltip("Lerp between the original color and the rainbow color. 0 = rainbow color is ignored, 1 = original color is ignored.")]
        public float Lerp = 1f;
        
        [Tooltip("Which corners of the quad of each character are effected by this. HINT: They are floats and fun to play around with ;)")] 
        public AffectedCorners AffectedCorners = AffectedCorners.one;

        public Gradient Gradient = new Gradient();

        [Tooltip("Set only the alpha value. If false then RGB and A are changed. If true the only the Alpha is changed.")]
        public bool AlphaOnly = false;
        
        [Tooltip("Set the alpha value too. If false only RGB are changed.")]
        public bool SetAlpha = false;
        
        public void Reset()
        {
            Speed = 1f;
            Frequency = 1f;
            Direction = 1f;
            SingleColorPerCharacter = false;
            Lerp = 1f;
            AffectedCorners = AffectedCorners.one;
            AlphaOnly = false;
            SetAlpha = false;
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 3f);
            Frequency = Random.Range(0.1f, 3f);
            Direction = Mathf.Sign(Random.Range(-1f, 1f));
            SingleColorPerCharacter = TextAnimation.GetRandomBool();
            
            Lerp = Random.value;
            
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
            
            AlphaOnly = TextAnimation.GetRandomBool();
            SetAlpha = TextAnimation.GetRandomBool();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterGradientModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                Frequency = typedSource.Frequency;
                Direction = typedSource.Direction;
                SingleColorPerCharacter = typedSource.SingleColorPerCharacter;
                Lerp = typedSource.Lerp;
                AffectedCorners = typedSource.AffectedCorners;
                Gradient = typedSource.Gradient;
                AlphaOnly = typedSource.AlphaOnly;
                SetAlpha = typedSource.SetAlpha;
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
            float hueLeft, hueRight;
            if (SingleColorPerCharacter)
            {
                hueLeft = Mathf.Abs(time * -Direction + characterIndex * Frequency * 0.1f) % 1f;
                hueRight = hueLeft;
            }
            else
            {
                hueLeft = Mathf.Abs(time * -Direction +(quadVertexData.BottomLeftPosition.x / 100f) * Frequency * 0.1f) % 1f;
                hueRight = Mathf.Abs(time * -Direction +(quadVertexData.BottomRightPosition.x / 100f) * Frequency * 0.1f) % 1f;
            }
            
            Color32 colorLeft = (Color32)Gradient.Evaluate(hueLeft);
            Color32 colorRight = (Color32)Gradient.Evaluate(hueRight);

            quadVertexData.BottomLeftColor = TextAnimation.SetVertexColor(quadVertexData.BottomLeftColor, colorLeft, AffectedCorners.BottomLeft, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.TopLeftColor = TextAnimation.SetVertexColor(quadVertexData.TopLeftColor, colorLeft, AffectedCorners.TopLeft, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.TopRightColor = TextAnimation.SetVertexColor(quadVertexData.TopRightColor, colorRight, AffectedCorners.TopRight, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.BottomRightColor = TextAnimation.SetVertexColor(quadVertexData.BottomRightColor, colorRight, AffectedCorners.BottomRight, SetAlpha, AlphaOnly, Lerp);

            return true;
        }
    }
}