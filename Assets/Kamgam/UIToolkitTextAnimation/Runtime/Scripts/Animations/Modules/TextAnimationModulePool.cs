using System;
using System.Collections.Generic;

namespace Kamgam.UIToolkitTextAnimation
{
    public static class TextAnimationModulePool
    {
        private static Dictionary<System.Type, Stack<ITextAnimationModule>> s_pool = new Dictionary<System.Type, Stack<ITextAnimationModule>>();

        public static int PoolSize => s_pool.Count;

        public static ITextAnimationModule GetFromPool(System.Type type)
        {
            if (s_pool.TryGetValue(type, out var pool))
            {
                if (pool.Count > 0)
                {
                    var copy = pool.Pop();
                    return copy;
                }
            }
            
            var newCopy = (ITextAnimationModule) Activator.CreateInstance(type);
            return newCopy;
        }
        
        public static ITextAnimationModule GetCopyFromPool(System.Type type, ITextAnimationModule baseModule)
        {
            if (baseModule == null)
                return null;
            
            if (s_pool.TryGetValue(baseModule.GetType(), out var pool))
            {
                if (pool.Count > 0)
                {
                    var copy = pool.Pop();
                    copy.CopyValuesFrom(baseModule);
                    return copy;
                }
            }

            var newCopy = (ITextAnimationModule) Activator.CreateInstance(type);
            newCopy.CopyValuesFrom(baseModule);
            return newCopy;
        }
        
        public static void ResetAndReturnToPool(ITextAnimationModule module)
        {
            if (module == null)
                return;

            var key = module.GetType();
            
            if (!s_pool.ContainsKey(key))
                s_pool.Add(key, new Stack<ITextAnimationModule>());
            
            module.Reset();
            s_pool[key].Push(module);
        }
        
        public static void ResetAndReturnListToPool<T>(IList<T> modules) where T : ITextAnimationModule
        {
            if (modules == null)
                return;
            
            for (int i = modules.Count - 1; i >= 0; i--)
            {
                ResetAndReturnToPool(modules[i]);
            }

            modules.Clear();
        }
    }
}