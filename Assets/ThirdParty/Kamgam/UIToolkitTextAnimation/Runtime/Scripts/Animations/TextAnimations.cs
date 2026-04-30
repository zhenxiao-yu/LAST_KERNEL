using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor;
#endif

namespace Kamgam.UIToolkitTextAnimation
{
#if KAMGAM_VISUAL_SCRIPTING
    // Why? See: https://forum.unity.com/threads/unable-to-provide-a-default-for-getvalue-on-object-valueinput.1140022/#post-9138727
    [Unity.VisualScripting.Inspectable]
#endif
    [CreateAssetMenu(fileName = "UITK TextAnimations", menuName = "UI Toolkit/TextAnimation/Animations List", order = 301)]
    public class TextAnimations : ScriptableObject
    {
        [Tooltip("The frame rate of the preview in the editor during EDIT MODE. In PLAY MODE the Application.targetFrameRate is used.")]
        public int EditorTargetFrameRate = 30;
        
        [System.NonSerialized]
        protected List<TextAnimation> _runtimeCopies;

        [SerializeField]
        [FormerlySerializedAs("_configs")]
        protected List<TextAnimation> _animations;
        
        /// <summary>
        /// Gives you direct access to the config assets no matter what 'UseRuntimeCopy' was set to.
        /// </summary>
        public List<TextAnimation> Assets => _animations;
        
        public List<TextAnimation> Animations
        {
            get
            {
                // We have to deep check too (see IsNullOrEmptyDeep) because in the editor the configs can become missing references (null) after play mode ended.
                if (_runtimeCopies == null || _runtimeCopies.Count != _animations.Count || _runtimeCopies.IsNullOrEmptyDeep())
                {
                    TextAnimation.ResetAndReturnToPool(_runtimeCopies);
                    if(_runtimeCopies == null)
                        _runtimeCopies = new List<TextAnimation>();
                    foreach (var config in _animations)
                    {
                        var copy = TextAnimation.GetCopyFromPool(config);
                        _runtimeCopies.Add(copy);
                    }
                }

                return _runtimeCopies;
            }

            private set {}
        }

        public bool Contains(TextAnimation animation)
        {
            return Animations.Contains(animation);
        }

        public TextAnimation GetAnimation(string id)
        {
            foreach (var config in Animations)
            {
                if (config != null && string.CompareOrdinal(config.Id, id) == 0)
                    return config;
            }

            return null;
        }
        
        public T GetAnimation<T>(string id) where T : TextAnimation
        {
            return GetAnimation(id) as T;
        }
        
        public void Refresh()
        {
            _runtimeCopies.Clear();
        }

        public void DefragAssets()
        {
#if UNITY_EDITOR
            bool defragged = false;
#endif
            
            for (int i = _animations.Count-1; i >= 0; i--)
            {
                if (_animations[i] == null)
                {
                    _animations.RemoveAt(i);
#if UNITY_EDITOR
                    defragged = true;
#endif
                }
            }

#if UNITY_EDITOR
            if (defragged)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
#endif
        }

#if UNITY_EDITOR
        [DidReloadScripts(-1)]
        public static void onDomainReload()
        {
            var animations = FindAssetInEditor();
            if (animations != null)
                animations._runtimeCopies.Clear();
        }
        
        private static TextAnimations s_cachedAnimations;
        
        public static TextAnimations FindAssetInEditor()
        {
            if (s_cachedAnimations != null)
                return s_cachedAnimations;
                
            // Search in asset database and return first found.
            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(TextAnimations));
            if (guids.Length > 0)
            {
                string path;

                // First try locating the default config from asset directory.
                foreach (var guid in guids)
                {
                    path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                    if (path.Contains(Installer.AssetRootPath))
                    {
                        s_cachedAnimations = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAnimations>(path);
                        return s_cachedAnimations;
                    }
                }

                // If not found the simply use the first.
                path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                s_cachedAnimations = UnityEditor.AssetDatabase.LoadAssetAtPath<TextAnimations>(path);
                return s_cachedAnimations;
            }

            s_cachedAnimations = null;
            return s_cachedAnimations;
        }

        public void OnValidate()
        {
            foreach (var animation in Animations)
            {
                if (animation == null)
                    continue;
                
                animation.MarkAsChanged();
            }

            if (UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var doc = TextAnimationDocument.Find(this);
                if(doc != null)
                {
                    doc.AddOrRemoveManipulators(); 
                }
            }
            else
            {
                UIEditorPanelObserver.RefreshPanels();
            }
        }
#endif
    }
}