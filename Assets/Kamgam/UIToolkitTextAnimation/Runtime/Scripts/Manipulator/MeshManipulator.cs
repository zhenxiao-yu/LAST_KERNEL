using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    public class MeshManipulator<T> : ManipulatorBase<T>
        where T : ManipulatorBase<T>
    {
        protected override void RegisterCallbacksOnTarget()
        {
            base.RegisterCallbacksOnTarget();
            
            target.generateVisualContent -= preGenerateVisualContent;
            target.generateVisualContent -= postGenerateVisualContent;
            
            target.generateVisualContent = preGenerateVisualContent + target.generateVisualContent + postGenerateVisualContent;
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.generateVisualContent -= preGenerateVisualContent;
            target.generateVisualContent -= postGenerateVisualContent;
            
            base.UnregisterCallbacksFromTarget();
        }
        
        protected virtual void preGenerateVisualContent(MeshGenerationContext mgc) {}
        protected virtual void postGenerateVisualContent(MeshGenerationContext mgc) {}
    }
}