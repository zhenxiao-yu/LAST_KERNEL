using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationCharacterColorModule : ITextAnimationCharacterModule
    {
        public string name = "Color";
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        public Color32 ColorBottomLeft = new Color32(255, 255, 255, 255);
        public Color32 ColorTopLeft = new Color32(255, 255, 255, 255);
        public Color32 ColorTopRight = new Color32(255, 255, 255, 255);
        public Color32 ColorBottomRight = new Color32(255, 255, 255, 255);
        
        [Range(0,1)]
        [Tooltip("Lerp between the original color and the rainbow color. 0 = rainbow color is ignored, 1 = original color is ignored.")]
        public float Lerp = 1f;
        
        [Tooltip("Which corners of the quad of each character are effected by this. HINT: They are floats and fun to play around with ;)")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        
        [Tooltip("Set only the alpha value. If false then RGB and A are changed. If true the only the Alpha is changed.")]
        public bool AlphaOnly = false;
        
        [Tooltip("Set the alpha value too. If false only RGB are changed.")]
        public bool SetAlpha = false;

        public void Reset()
        {
            ColorBottomLeft = new Color32(255, 255, 255, 255);
            ColorTopLeft = new Color32(255, 255, 255, 255);
            ColorTopRight = new Color32(255, 255, 255, 255);
            ColorBottomRight = new Color32(255, 255, 255, 255);
        
            Lerp = 1f;
            
            AffectedCorners = AffectedCorners.one;
            
            AlphaOnly = false;
            SetAlpha = false;
        }

        public void Randomize()
        {
            ColorBottomLeft = TextAnimation.GetRandomColor(0, 255, randomizeAlpha: false);
            ColorTopLeft = TextAnimation.GetRandomColor(0, 255, randomizeAlpha: false);
            ColorTopRight = TextAnimation.GetRandomColor(0, 255, randomizeAlpha: false);
            ColorBottomRight = TextAnimation.GetRandomColor(0, 255, randomizeAlpha: false);
        
            Lerp = Random.value;
            
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
            
            AlphaOnly = TextAnimation.GetRandomBool();
            SetAlpha = TextAnimation.GetRandomBool();
        }

        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationCharacterColorModule typedSource)
            {
                name = typedSource.GetName();
                ColorBottomLeft = typedSource.ColorBottomLeft;
                ColorTopLeft = typedSource.ColorTopLeft;
                ColorTopRight = typedSource.ColorTopRight;
                ColorBottomRight = typedSource.ColorBottomRight;
                Lerp = typedSource.Lerp;
                AffectedCorners = typedSource.AffectedCorners;
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
            quadVertexData.BottomLeftColor = TextAnimation.SetVertexColor(quadVertexData.BottomLeftColor, ColorBottomLeft, AffectedCorners.BottomLeft, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.TopLeftColor = TextAnimation.SetVertexColor(quadVertexData.TopLeftColor, ColorTopLeft, AffectedCorners.BottomLeft, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.TopRightColor = TextAnimation.SetVertexColor(quadVertexData.TopRightColor, ColorTopRight, AffectedCorners.BottomLeft, SetAlpha, AlphaOnly, Lerp);
            quadVertexData.BottomRightColor = TextAnimation.SetVertexColor(quadVertexData.BottomRightColor, ColorBottomRight, AffectedCorners.BottomLeft, SetAlpha, AlphaOnly, Lerp);

            return true;
        }
    }
}