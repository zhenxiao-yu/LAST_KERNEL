using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable]
    public class TextAnimationTypewriterRotateModule : ITextAnimationTypewriterModule
    {
        public string name = "Rotate"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        [Header("Rotation")]
        public float StartDelta = -180f;
        
        [Tooltip("Center is relative and normalized (if CenterAbsolute is turned OFF). 0/0 is the center of each character. 1/0 would be the right center, 1/1 the bottom right corner, -1/-1 the top left corner.")]
        public Vector3 Center = Vector3.zero;
        
        public bool CenterAbsolute = false;
        
        public Easing RotationEasing = Easing.Linear;
        
        [ShowIfAttribute("RotationEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve RotationCustomCurve = TextAnimation.GetDefaultCurve01();
        
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
        
        /// <summary>
        /// The axis to rotate around. Usually z-axis (0/0/1) is fine. It's here for future use. Sadly right now the quads are clipped if rotated by anything but the z-axis.
        /// </summary>
        [System.NonSerialized]
        public Vector3 RotationAxis = new Vector3(0, 0, 1f);
        

        public void Reset()
        {
            StartDelta = -180f;
            Center = Vector3.zero;
            CenterAbsolute = false;
            RotationEasing = Easing.Linear;
            RotationCustomCurve = TextAnimation.GetDefaultCurve01();
            AlphaFade = true;
            AlphaEasing = Easing.Ease;
            AlphaCustomCurve = TextAnimation.GetDefaultCurve01();
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            StartDelta = Random.Range(-180f, 180f);
            CenterAbsolute = TextAnimation.GetRandomBool();
            if (CenterAbsolute)
            {
                Center = TextAnimation.GetRandomVector3(-100, 100);
            }
            else
            {
                Center = TextAnimation.GetRandomVector3(-3, 3);
            }
            RotationEasing = EasingUtils.GetRandom();
            RotationCustomCurve = TextAnimation.GetRandomCurve();
            AlphaFade = TextAnimation.GetRandomBool();
            AlphaEasing = EasingUtils.GetRandom();
            AlphaCustomCurve = TextAnimation.GetRandomCurve();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationTypewriterRotateModule typedSource)
            {
                StartDelta = typedSource.StartDelta;
                Center = typedSource.Center;
                CenterAbsolute = typedSource.CenterAbsolute;
                RotationEasing = typedSource.RotationEasing;
                RotationCustomCurve = typedSource.RotationCustomCurve;
                AlphaFade = typedSource.AlphaFade;
                AlphaEasing = typedSource.AlphaEasing;
                AlphaCustomCurve = typedSource.AlphaCustomCurve;
                AffectedCorners = typedSource.AffectedCorners;
                RotationAxis = typedSource.RotationAxis;
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
            
            // Rotation
            float rotationLerp;
            if (RotationEasing == Easing.CustomCurve)
            {
                rotationLerp = RotationCustomCurve.Evaluate(progress);
            }
            else
            {
                rotationLerp = EasingUtils.Ease(RotationEasing, progress);
            }

            // Calc rotation center
            Vector3 rotationCenter;
            if (CenterAbsolute)
                rotationCenter = Center;
            else
                rotationCenter = TextAnimation.GetQuadCenter(quadVertexData, Center);
            
            // Apply rotation
            TextAnimation.RotateQuadAround(quadVertexData, rotationCenter, RotationAxis * StartDelta * (1f - rotationLerp), AffectedCorners);
        }
    }
}