namespace Kamgam.UIToolkitTextAnimation
{
    public interface ITextAnimationModule
    {
        public void SetName(string name);
        public string GetName();
        
        public void CopyValuesFrom(ITextAnimationModule source);

        /// <summary>
        /// Resets the internal state before returning to pool.
        /// </summary>
        void Reset();
        
        void Randomize();
    }
}