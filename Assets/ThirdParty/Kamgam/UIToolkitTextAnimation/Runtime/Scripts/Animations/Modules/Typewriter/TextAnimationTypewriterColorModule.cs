using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationTypewriterColorModule : ITextAnimationTypewriterModule
    {
        public string name = "Color"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        [Header("Color")]
        
        [Tooltip("The starting color from which to fade the text from.")]
        public Color StartColor = new Color32(255, 255, 255, 0);

        [Tooltip("Set only the alpha value. If false then RGB and A are changed. If true the only the Alpha is changed.")]
        public bool AlphaOnly = false;
        
        [Tooltip("Easing method of the color. Cubic is a nice one.")]
        public Easing ColorEasing = Easing.Ease;
        
        [ShowIfAttribute("ColorEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)] 
        public AnimationCurve ColorCustomCurve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(1f,1f)
        });
        
        [Space(10)]
        
        [Tooltip("Which corners of the quad of each character are effected by this.")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        
        
        public void Reset()
        {
            StartColor = new Color32(255, 255, 255, 0);
            AlphaOnly = false;
            ColorEasing = Easing.Ease;
            ColorCustomCurve = TextAnimation.GetDefaultCurve01();
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            StartColor = TextAnimation.GetRandomColor();
            AlphaOnly = TextAnimation.GetRandomBool();
            ColorEasing = EasingUtils.GetRandom();
            ColorCustomCurve = TextAnimation.GetRandomCurve();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationTypewriterColorModule typedSource)
            {
                StartColor = typedSource.StartColor;
                AlphaOnly = typedSource.AlphaOnly;
                ColorEasing = typedSource.ColorEasing;
                ColorCustomCurve = typedSource.ColorCustomCurve;
                AffectedCorners = typedSource.AffectedCorners;
            }
        }

        public void UpdateCharacterNormalized(
            TextElement element,
            int quadIndex, int totalQuadCount,
            float time, float unscaledTime, float unscaledDeltaTime, float progress,
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            // Alpha fade in
            float lerp;
            if (ColorEasing == Easing.CustomCurve)
            {
                lerp = ColorCustomCurve.Evaluate(progress);
            }
            else
            {
                lerp = EasingUtils.Ease(ColorEasing, progress);
            }

            if (AlphaOnly)
            {
                byte alpha = (byte)Mathf.RoundToInt(255 * lerp);
                quadVertexData.SetAlpha(alpha);   
            }
            else
            {
                quadVertexData.BottomLeftColor = Color32.Lerp(StartColor, quadVertexData.BottomLeftColor, lerp + (1f - lerp) * (1f - AffectedCorners.BottomLeft));
                quadVertexData.TopLeftColor = Color32.Lerp(StartColor, quadVertexData.TopLeftColor, lerp + (1f - lerp) * (1f - AffectedCorners.BottomLeft));
                quadVertexData.TopRightColor = Color32.Lerp(StartColor, quadVertexData.TopRightColor, lerp + (1f - lerp) * (1f - AffectedCorners.BottomLeft));
                quadVertexData.BottomRightColor = Color32.Lerp(StartColor, quadVertexData.BottomRightColor, lerp + (1f - lerp) * (1f - AffectedCorners.BottomLeft));
            }
        }
    }
}