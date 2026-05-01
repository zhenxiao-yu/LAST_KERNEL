using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public interface ITextAnimationTypewriterModule : ITextAnimationModule
    {
        /// <summary>
        /// Animation function called on every quad of the text.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="quadIndex"></param>
        /// <param name="totalQuadCount"></param>
        /// <param name="time"></param>
        /// <param name="unscaledTime"></param>
        /// <param name="unscaledDeltaTime"></param>
        /// <param name="progress">Normalized progress of the quad animation (float between 0 and 1)</param>
        /// <param name="quadVertexData"></param>
        /// <param name="preAppliedDelay"></param>
        /// <param name="preAppliedSpeed"></param>
        /// <param name="delayTagInfo"></param>
        void UpdateCharacterNormalized(
            TextElement element,
            int quadIndex, int totalQuadCount,
            float time, float unscaledTime, float unscaledDeltaTime, float progress, 
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos);
    }
}