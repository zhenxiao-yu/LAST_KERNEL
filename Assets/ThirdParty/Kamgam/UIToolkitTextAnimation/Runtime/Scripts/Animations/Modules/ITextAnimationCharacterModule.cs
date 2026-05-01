using System.Collections.Generic;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public interface ITextAnimationCharacterModule : ITextAnimationModule
    {
        /// <summary>
        /// Animation function called on every quad of the text.
        /// </summary>
        bool UpdateCharacter(
            TextElement element,
            TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            float time, float unscaledTime, float unscaledDeltaTime,
            TextInfoAccessor.QuadVertexData quadVertexData, float preAppliedDelay, float preAppliedSpeed,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos);
    }
}