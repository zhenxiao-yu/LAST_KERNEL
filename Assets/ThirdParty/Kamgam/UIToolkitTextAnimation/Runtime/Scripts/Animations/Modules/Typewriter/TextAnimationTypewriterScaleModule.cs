using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationTypewriterScaleModule : ITextAnimationTypewriterModule
    {
        public string name = "Scale"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;

        [Header("Scale")]
        
        [Tooltip("StartScale is relative to the character size (1f = not change, 2f = double the size.")]
        public Vector3 StartScale = new Vector3(0f, 0f, 0f);
        
        public Easing ScaleEasing = Easing.Linear;

        [ShowIfAttribute("ScaleEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve ScaleCustomCurve = TextAnimation.GetDefaultCurve01();
        
        [Tooltip("Center is relative and normalized (if CenterAbsolute is turned OFF). 0/0 is the center of each character. 1/0 would be the right center, 1/1 the bottom right corner, -1/-1 the top left corner.")]
        public Vector3 Center = Vector3.zero;
        
        public bool CenterAbsolute = false;
        
        [Header("Alpha")]
        
        [Tooltip("Should the alpha value of the color be animated too?")]
        public bool AlphaFade = true;
        
        [ShowIfAttribute("AlphaFade", true, ShowIfAttribute.DisablingType.DontDraw)]
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
            StartScale = new Vector3(0f, 0f, 0f);
            ScaleEasing = EasingUtils.GetRandom();
            ScaleCustomCurve = TextAnimation.GetDefaultCurve01();
            Center = Vector3.zero;
            CenterAbsolute = false;
            
            AlphaFade = true;
            AlphaEasing = Easing.Ease;
            AlphaCustomCurve = TextAnimation.GetDefaultCurve01();
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            StartScale = TextAnimation.GetRandomVector3(0f, 1.5f, 0f, 1.5f, 1f, 1f);
            CenterAbsolute = TextAnimation.GetRandomBool();
            if (CenterAbsolute)
            {
                Center = TextAnimation.GetRandomVector3(-100, 100);
            }
            else
            {
                Center = TextAnimation.GetRandomVector3(-3, 3);
            }
            ScaleEasing = EasingUtils.GetRandom();
            ScaleCustomCurve = TextAnimation.GetRandomCurve();
            AlphaFade = TextAnimation.GetRandomBool();
            AlphaEasing = EasingUtils.GetRandom();
            AlphaCustomCurve = TextAnimation.GetRandomCurve();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationTypewriterScaleModule typedSource)
            {
                StartScale = typedSource.StartScale;
                Center = typedSource.Center;
                CenterAbsolute = typedSource.CenterAbsolute;
                ScaleEasing = typedSource.ScaleEasing;
                ScaleCustomCurve = typedSource.ScaleCustomCurve;
                AlphaFade = typedSource.AlphaFade;
                AlphaEasing = typedSource.AlphaEasing;
                AlphaCustomCurve = typedSource.AlphaCustomCurve;
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
            
            // Scale
            float scaleLerp;
            if (ScaleEasing == Easing.CustomCurve)
            {
                scaleLerp = ScaleCustomCurve.Evaluate(progress);
            }
            else
            {
                scaleLerp = EasingUtils.Ease(ScaleEasing, progress);
            }
            var scale = Vector3.Lerp(StartScale, Vector3.one, scaleLerp);
            
            // Calc scale center
            Vector3 scaleCenter;
            if (CenterAbsolute)
                scaleCenter = Center;
            else
                scaleCenter = TextAnimation.GetQuadCenter(quadVertexData, Center);

            TextAnimation.ScaleQuad(quadVertexData, scaleCenter, scale, AffectedCorners);
        }
    }
}