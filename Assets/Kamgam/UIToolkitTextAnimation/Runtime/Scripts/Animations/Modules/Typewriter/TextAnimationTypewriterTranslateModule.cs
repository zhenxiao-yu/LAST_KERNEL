using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationTypewriterTranslateModule : ITextAnimationTypewriterModule
    {
        public string name = "Translate"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        [Header("Position")]
        
        [Tooltip("Center is relative and normalized (if StartDeltaInPixels is turned OFF). 0/0 is the center of each character. 1/0 would be the right center, 1/1 the bottom right corner, -1/-1 the top left corner.\n" +
                 "NOTICE: This is relative to the size of EACH character quad. Meaning the distance will different for and 'i' and an 'M' since they have different quad sizes.")]
        public Vector3 StartDelta = new Vector3(0, -30f, 0);
        
        public bool StartDeltaInPixels = true;
        
        public Easing PositionEasing = Easing.Linear;
        
        [ShowIfAttribute("PositionEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve PositionCustomCurve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(1f,1f)
        });
        
        [Header("Alpha")]
        
        [Tooltip("Should the alpha value of the color be animated too?")]
        public bool AlphaFade = true;
        
        [ShowIfAttribute("AlphaFade", true, ShowIfAttribute.DisablingType.ReadOnly)]
        public Easing AlphaEasing = Easing.Ease;
        
        [ShowIfAttribute("AlphaEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve AlphaCustomCurve = new AnimationCurve( new Keyframe[]
        {
            new Keyframe(0f,0f),
            new Keyframe(1f,1f)
        });

        [Space(10)]
        
        [Tooltip("Which corners of the quad of each character are effected by this.")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        

        public void Reset()
        {
            StartDelta = new Vector3(0, -30f, 0);
            StartDeltaInPixels = true;
            PositionEasing = Easing.Linear;
            PositionCustomCurve = TextAnimation.GetDefaultCurve01();
            AlphaFade = true;
            AlphaEasing = Easing.Ease;
            AlphaCustomCurve = TextAnimation.GetDefaultCurve01();
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            StartDeltaInPixels = TextAnimation.GetRandomBool();
            float mult = StartDeltaInPixels ? 10f : 1f;
            StartDelta = TextAnimation.GetRandomVector3(-2f * mult, 2f * mult);
            PositionEasing = EasingUtils.GetRandom();
            AlphaFade = TextAnimation.GetRandomBool();
            AlphaEasing = EasingUtils.GetRandom();
            AlphaCustomCurve = TextAnimation.GetRandomCurve();
            PositionCustomCurve = TextAnimation.GetRandomCurve();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationTypewriterTranslateModule typedSource)
            {
                StartDelta = typedSource.StartDelta;
                StartDeltaInPixels = typedSource.StartDeltaInPixels;
                PositionEasing = typedSource.PositionEasing;
                AlphaFade = typedSource.AlphaFade;
                AlphaEasing = typedSource.AlphaEasing;
                AlphaCustomCurve = typedSource.AlphaCustomCurve;
                PositionCustomCurve = typedSource.PositionCustomCurve;
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
            // Alpha
            float alphaLerp;
            if (AlphaEasing == Easing.CustomCurve)
            {
                alphaLerp = AlphaCustomCurve.Evaluate(progress);
            }
            else
            {
                alphaLerp = EasingUtils.Ease(AlphaEasing, progress);
            }
            
            if (AlphaFade)
            {
                byte alpha = (byte)Mathf.RoundToInt(255 * alphaLerp);
                quadVertexData.SetAlpha(alpha);
            }
            
            // Position
            float positionLerp;
            if (PositionEasing == Easing.CustomCurve)
            {
                positionLerp = PositionCustomCurve.Evaluate(progress);
            }
            else
            {
                positionLerp = EasingUtils.Ease(PositionEasing, progress);
            }

            Vector3 delta = StartDeltaInPixels ? StartDelta  * (1f - positionLerp) : (TextAnimation.GetQuadCenter(quadVertexData, StartDelta) - TextAnimation.GetQuadCenter(quadVertexData)) * (1f - positionLerp);
            TextAnimation.TranslateQuad(quadVertexData, delta, AffectedCorners);
        }
    }
}