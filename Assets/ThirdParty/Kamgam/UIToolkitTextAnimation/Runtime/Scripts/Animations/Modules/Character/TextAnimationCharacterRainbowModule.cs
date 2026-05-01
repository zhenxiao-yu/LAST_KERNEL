using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterRainbowModule : ITextAnimationCharacterModule
    {
        public string name = "Rainbow";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public float Speed = 1f;
        
        [Tooltip("How 'dense' the gradient will be.")]
        public float Frequency = 1f;
        
        [Tooltip("What direction the gradient will move. The gradient will change the Hue value in HSV color space.")]
        public float ScrollDirection = -1f;
        
        [Tooltip("Saturation in HSV color space.")]
        [Range(0,1)]
        public float Saturation = 1f;
        
        [Tooltip("Value in HSV color space.")]
        [Range(0,1)]
        public float Value = 1f;
        
        [Tooltip("If enabled the each character will have only a single color instead of a gradient.")]
        public bool SingleColorPerCharacter = false;
        
        [Range(0,1)]
        [Tooltip("Lerp between the original color and the rainbow color. 0 = rainbow color is ignored, 1 = original color is ignored.")]
        public float Lerp = 1f;
        
        [Tooltip("Which corners of the quad of each character are effected by this. HINT: They are floats and fun to play around with ;)")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;

        [Tooltip("Set the alpha value too. If false only RGB are changed.")]
        public bool SetAlpha = false;
        
        public void Reset()
        {
            Speed = 1f;
            Frequency = 1f;
            ScrollDirection = -1f;
            Saturation = 1f;
            Value = 1f;
            SingleColorPerCharacter = false;
            Lerp = 1f;
            AffectedCorners = AffectedCorners.one;
            SetAlpha = false;
        }

        public void Randomize()
        {
            Speed = Random.Range(0.1f, 3f);
            Frequency = Random.Range(0.1f, 3f);
            ScrollDirection = Mathf.Sign(Random.Range(-1f, 1f));
            Saturation = Random.Range(0f, 1f);
            Value = Random.Range(0f, 1f);
            SingleColorPerCharacter = TextAnimation.GetRandomBool();
            
            Lerp = Random.value;
            
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
            
            SetAlpha = TextAnimation.GetRandomBool();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterRainbowModule typedSource)
            {
                name = typedSource.GetName();
                Speed = typedSource.Speed;
                Saturation = typedSource.Saturation;
                Value = typedSource.Value;
                Frequency = typedSource.Frequency;
                ScrollDirection = typedSource.ScrollDirection;
                SingleColorPerCharacter = typedSource.SingleColorPerCharacter;
                Lerp = typedSource.Lerp;
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
            
            // Apply change 
            float hueLeft, hueRight;
            if (SingleColorPerCharacter)
            {
                hueLeft = Mathf.Abs(time + characterIndex * Frequency * 0.1f) % 1f;
                hueRight = hueLeft;
            }
            else
            {
                hueLeft = Mathf.Abs(time * ScrollDirection +(quadVertexData.BottomLeftPosition.x / 100f) * Frequency * 0.1f) % 1f;
                hueRight = Mathf.Abs(time * ScrollDirection +(quadVertexData.BottomRightPosition.x / 100f) * Frequency * 0.1f) % 1f;
            }

            Color32 colorLeft = (Color32)Color.HSVToRGB(hueLeft, Saturation, Value);
            Color32 colorRight = (Color32)Color.HSVToRGB(hueRight, Saturation, Value);

            quadVertexData.BottomLeftColor = TextAnimation.SetVertexColor(quadVertexData.BottomLeftColor, colorLeft, AffectedCorners.BottomLeft, SetAlpha, false, Lerp);
            quadVertexData.TopLeftColor = TextAnimation.SetVertexColor(quadVertexData.TopLeftColor, colorLeft, AffectedCorners.TopLeft, SetAlpha, false, Lerp);
            
            quadVertexData.BottomRightColor = TextAnimation.SetVertexColor(quadVertexData.BottomRightColor, colorRight, AffectedCorners.BottomRight, SetAlpha, false, Lerp);
            quadVertexData.TopRightColor = TextAnimation.SetVertexColor(quadVertexData.TopRightColor, colorRight, AffectedCorners.TopRight, SetAlpha, false, Lerp);
            
            return true;
        }
    }
}