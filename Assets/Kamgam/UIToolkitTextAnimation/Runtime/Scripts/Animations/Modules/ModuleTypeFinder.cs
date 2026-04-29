using System;
using System.Collections.Generic;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
#endif

namespace Kamgam.UIToolkitTextAnimation
{
    public static class ModuleTypeFinder
    {
        public static List<Type> CharacterTypes = new List<Type>();
        public static List<Type> TypewriterTypes = new List<Type>();

#if UNITY_EDITOR
        [DidReloadScripts]
#endif
        static void didReloadScripts()
        {
            GetTypes<ITextAnimationCharacterModule>(CharacterTypes);
            GetTypes<ITextAnimationTypewriterModule>(TypewriterTypes);
        }
        
        public static List<Type> GetTypes<T>(List<Type> results = null) where T : ITextAnimationModule
        {
            if (results == null)
                results = new List<Type>();

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                var testTypes = types.Where(t => typeof(T).IsAssignableFrom(t) && t.IsClass && !t.IsAbstract);
                results.AddRange(testTypes);
            }

            return results;
        }
    }
}