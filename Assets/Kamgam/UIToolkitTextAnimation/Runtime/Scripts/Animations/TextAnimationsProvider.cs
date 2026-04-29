using UnityEngine;
using UnityEngine.Serialization;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// The provider is just a wrapper around a animation configs object so we can use it in UI Toolkit custom elements and on TextAnimationDocuments.
    /// </summary>
    // We modify the execution order to ensure Awake() is called before UI Toolkit
    // instantiates the UI Elements (which it does via the event system which is
    // set to execution order -1000 by default).
    [DefaultExecutionOrder(-1001)]
    public class TextAnimationsProvider : MonoBehaviour
    {
        static TextAnimationsProvider _provider;

        public static TextAnimations GetAnimations()
        {
#if UNITY_EDITOR
            if (UnityEditor.EditorApplication.isPlaying)
            {
#endif
                if (_provider != null && _provider.Animations != null)
                {
                    return _provider.Animations;
                }
                return null;
#if UNITY_EDITOR
            }
            else
            {
#if UNITY_2023_1_OR_NEWER
                var provider = GameObject.FindFirstObjectByType<TextAnimationsProvider>(FindObjectsInactive.Include);
#else
                var provider = GameObject.FindObjectOfType<TextAnimationsProvider>(includeInactive: true);
#endif
                if (provider != null && provider.Animations != null)
                {
                    return provider.Animations;
                }
                else
                {
                    return TextAnimations.FindAssetInEditor();
                }
            }
#endif
        }

        public static TextAnimation GetAnimation(string id)
        {
            var animations = GetAnimations();
            if (animations == null)
                return null;
            
            return animations.GetAnimation(id);
        }
        
        public static T GetAnimation<T>(string id) where T : TextAnimation
        {
            var configs = GetAnimations();
            if (configs == null)
                return null;
            
            return configs.GetAnimation<T>(id);
        }
        
        public static string GetTypewriterClassName(string id)
        {
            var config = GetAnimation<TextAnimationTypewriter>(id);
            if (config == null)
                return null;

            return config.GetClassName();
        }

        [FormerlySerializedAs("Configs")]
        public TextAnimations Animations;

        public void Awake()
        {
            _provider = this;
        }

        public void OnEnable()
        {
            _provider = this;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (Animations == null)
            {
                var root = GetAnimations();
                if (root != null)
                {
                    Animations = root;
                    UnityEditor.EditorUtility.SetDirty(this);
                    UnityEditor.EditorUtility.SetDirty(this.gameObject);
                }
            }
        }
#endif
    }
}