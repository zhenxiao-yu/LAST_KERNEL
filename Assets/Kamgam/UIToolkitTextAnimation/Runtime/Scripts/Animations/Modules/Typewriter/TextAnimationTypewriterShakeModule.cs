using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    [System.Serializable] 
    public class TextAnimationTypewriterShakeModule : ITextAnimationTypewriterModule
    {
        public string name = "Shake"; 
        public void SetName(string name) => this.name = name;
        public string GetName() => name;
        
        [Header("Shake")]
        
        [Tooltip("The maximum displacement in pixels.")]
        public Vector3 MaxDisplacement = new Vector3(0, -30f, 0);
        
        [Tooltip("Offset in pixels. This is added to the position after the shake. You can use it to offset the shake to only shake upward for example.")]
        public Vector2 Offset = new Vector2(0, 0f);
        
        [Tooltip("How often to shake per second at the beginning. It will slow down to 0.")]
        public float StartFrequency  = 10f;
        
        [Tooltip("The shaking will become less and less over time. This controls how that will be done.")]
        public Easing ShakeDissipationEasing = Easing.QuadOut;
        
        [Tooltip("Should the offset and displacement be changed only if a shake change appears or should it be continuous? Setting this to false will give a smoother look.")]
        public bool StepOffsetAndDisplacement = true;

        [Header("Alpha")]
        
        [Tooltip("Should the alpha value of the color be animated too?")]
        public bool AlphaFade = true;
        
        [ShowIfAttribute("AlphaFade", true, ShowIfAttribute.DisablingType.DontDraw)]
        public Easing AlphaEasing = Easing.Ease;

        [ShowIfAttribute("AlphaEasing", Easing.CustomCurve, ShowIfAttribute.DisablingType.DontDraw)]
        public AnimationCurve AlphaCustomCurve = TextAnimation.GetDefaultCurve01();

        [Space(10)]
        
        [Tooltip("Which corners of the quad of each character are effected by this.")]
        public AffectedCorners AffectedCorners = AffectedCorners.one;
        
        
        [System.NonSerialized]
        protected float m_lastChangeTime = 0f; 

        [System.NonSerialized]
        protected int m_lastFrame = -1;
        
        [System.NonSerialized]
        Random.State m_rndState;
        
        
        public void Reset()
        {
            MaxDisplacement = new Vector3(0, -30f, 0);
            Offset = new Vector2(0, 0f);
            StartFrequency  = 10f;
            ShakeDissipationEasing = Easing.QuadOut;
            StepOffsetAndDisplacement = true;
            AlphaFade = true;
            AlphaEasing = Easing.Ease;
            AlphaCustomCurve = TextAnimation.GetDefaultCurve01();
            AffectedCorners = AffectedCorners.one;
        }

        public void Randomize()
        {
            MaxDisplacement = TextAnimation.GetRandomVector3(-30f, 30f);
            Offset = TextAnimation.GetRandomVector3(-15f, 15f);
            StartFrequency  = Random.Range(0.2f, 20f);
            ShakeDissipationEasing = EasingUtils.GetRandom();
            StepOffsetAndDisplacement = TextAnimation.GetRandomBool();
            AlphaFade = TextAnimation.GetRandomBool();
            AlphaEasing = EasingUtils.GetRandom();
            AlphaCustomCurve = TextAnimation.GetRandomCurve();
            AffectedCorners = TextAnimation.GetRandomAffectedCorners();
        }
        
        public void CopyValuesFrom(ITextAnimationModule source)
        {
            if (source is TextAnimationTypewriterShakeModule typedSource)
            {
                MaxDisplacement = typedSource.MaxDisplacement;
                Offset = typedSource.Offset;
                StartFrequency = typedSource.StartFrequency;
                ShakeDissipationEasing = typedSource.ShakeDissipationEasing;
                StepOffsetAndDisplacement = typedSource.StepOffsetAndDisplacement;
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

            float steppedProgress = stairFunc(progress, StartFrequency, ShakeDissipationEasing);

            // Randomized indices to pick from a random list. This leads to nice random numbers every time but in a deterministic way.
            float rndIndexX = GetRandomValue(quadIndex) * RANDOM_LIST_SIZE + steppedProgress * RANDOM_LIST_SIZE;
            float rndIndexY = GetRandomValue(quadIndex+1) * RANDOM_LIST_SIZE + steppedProgress * RANDOM_LIST_SIZE;

            // Reduce displacement over time.
            
            // Position
            var offset = Vector2.Lerp(Offset, Vector2.zero, StepOffsetAndDisplacement ? steppedProgress : progress);
            var maxDisplacement = Vector2.Lerp(MaxDisplacement, Vector2.zero, StepOffsetAndDisplacement ? steppedProgress : progress);
            var rndDir = new Vector2(
                GetRandomValue(rndIndexX) - 0.5f,
                GetRandomValue(rndIndexY) - 0.5f
                ).normalized;
            float deltaX = rndDir.x * maxDisplacement.x + offset.x;
            float deltaY = -(rndDir.y * maxDisplacement.y) - offset.y; // invert axis

            // Apply change
            TextAnimation.TranslateQuad(quadVertexData, new Vector3(deltaX, deltaY, 0), AffectedCorners);
        }

        /// <summary>
        /// Returns the result of a function like this:
        /// ^
        /// 1                ---------
        /// |           -----
        /// |       ----
        /// |    ---
        /// |  --
        /// | -
        /// 0--------------------1>
        /// </summary>
        /// <param name="t">progress between 0 and 1</param>
        /// <param name="steps">the number of steps</param>
        /// <param name="easing"></param>
        /// <returns></returns>
        static float stairFunc(float t, float steps, Easing easing)
        {
            // Paste "f(x) = Round((1-(x-1)^2)*10)/10, 0 to 1" into Wolfram Alpha to understand what this does.
            // https://www.wolframalpha.com/input?i=f%28x%29+%3D+Round%28%281-%28x-1%29%5E2%29*10%29%2F10%2C+0+to+1
            // return Mathf.Round((1 - Mathf.Pow(t - 1, 2)) * steps) / steps;
            
            // Update: Use easing as input for more flexibility.
            return Mathf.Round(EasingUtils.Ease(easing, t) * steps) / steps;
        }
        
        
        // STATIC RANDOMIZATION API
        public const int RANDOM_LIST_SIZE = 100;
        public static List<float> RandomValues = new List<float>(RANDOM_LIST_SIZE);

        public static void FillRandomListIfNecessary()
        {
            if (RandomValues.Count != 0)
                return;

            FillRandomList();
        }
        
        public static void FillRandomList()
        {
            RandomValues.Clear();

            for (int i = 0; i < RANDOM_LIST_SIZE; i++)
            {
                RandomValues.Add(Random.value);
            }
        }

        /// <summary>
        /// Picks a value between 0 and 1 from a list of random values.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public static float GetRandomValue(float index)
        {
            FillRandomListIfNecessary();
            
            int i = (int)Mathf.Abs(index) % RANDOM_LIST_SIZE;
            return RandomValues[i];
        }
    }
}