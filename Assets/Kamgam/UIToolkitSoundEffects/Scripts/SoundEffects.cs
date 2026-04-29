using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor;
#endif

namespace Kamgam.UIToolkitSoundEffects
{
#if KAMGAM_VISUAL_SCRIPTING
    // Why? See: https://forum.unity.com/threads/unable-to-provide-a-default-for-getvalue-on-object-valueinput.1140022/#post-9138727
    [Unity.VisualScripting.Inspectable]
#endif
    [CreateAssetMenu(fileName = "UITK SoundEffects", menuName = "UI Toolkit/UITK SoundEffects", order = 301)]
    public partial class SoundEffects : ScriptableObject
    {
#if UNITY_EDITOR
        [MenuItem("Tools/" + Installer.AssetName + "/Open Effects List", priority = 101)]
        public static void OpenEffectsList()
        {
            Selection.objects = new Object[]
            {
                GetOrCreate()
            };
        }
#endif
        
        // Settings
        [Header("Settings")]
        
        [Tooltip("If enabled then the inspector will only be shown in the 'kamgam-sfx' class is present in the class list of the element.\n\n" +
                 "Usually this is off but in case the tool causes issues in the UI Builder this can be enabled.")]
        public bool InspectorOnlyIfClass;
        
        [Tooltip("Should UI Documents be auto detected after a new scene has been loaded.")]
        public bool AutoDetectUIDocuments;
        
        [Tooltip("Should copies be used at runtime to avoid changing the asset files in the editor?")]
        public bool UseRuntimeCopies;
        
        [Header("Effects")]
        [SerializeField]
        [FormerlySerializedAs("_effects")]
        protected List<SoundEffect> _effectAssets = new List<SoundEffect>();
        
        public List<SoundEffect> EffectAssets => _effectAssets;
        
        [System.NonSerialized]
        protected List<SoundEffect> _effectsRuntimeCopies;

        public bool UsesRuntimeCopies()
        {
            if (!UseRuntimeCopies)
                return false;
            
#if UNITY_EDITOR
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                return true;
#endif
            
            return false;
        }
        
        // An iterator to go through all effects (both assets and runtime objects).
        public List<SoundEffect> Effects
        {
            get
            {
                // In the editor at edit time use the assets directly.
                if (!UsesRuntimeCopies())
                    return _effectAssets;

                // At runtime use a list of copies.
                if (_effectsRuntimeCopies == null)
                {
                    _effectsRuntimeCopies = new List<SoundEffect>();
                    foreach (var effectAsset in _effectAssets)
                    {
                        var copy = SoundEffect.GetCopyFromPool(effectAsset);
                        _effectsRuntimeCopies.Add(copy);
                    }
                }

#if UNITY_EDITOR
                // Sync newly added effects from assets to runtime copies (only necessary in the Editor).
                foreach (var effectAsset in _effectAssets)
                {
                    if (getRuntimeEffect(effectAsset.Id) == null)
                    {
                        var copy = SoundEffect.GetCopyFromPool(effectAsset);
                        _effectsRuntimeCopies.Add(copy);
                    }
                }

                // Sync removed effects from assets to runtime copies (only necessary in the Editor).
                for (int i = _effectsRuntimeCopies.Count - 1; i >= 0; i--)
                {
                    var runtimeEffect = _effectsRuntimeCopies[i];
                    // Is a copy but no longer in the assets list? Then it has been removed.
                    if (runtimeEffect.IsCopy && getEffectAsset(runtimeEffect.Id) == null)
                    {
                        SoundEffect.ReturnToPool(runtimeEffect);
                        _effectsRuntimeCopies.RemoveAt(i);
                    }
                }
#endif

                return _effectsRuntimeCopies;
            }
        }

        public void Initialize()
        {
            InspectorOnlyIfClass = false;
            AutoDetectUIDocuments = true;
            UseRuntimeCopies = true;
        }
        
        public bool Contains(SoundEffect effect)
        {
            if (Effects.Contains(effect))
                return true;

            return false;
        }

        private bool isEditing()
        {
#if UNITY_EDITOR
            return !EditorApplication.isPlayingOrWillChangePlaymode;
#else
            return false;
#endif
        }

        /// <summary>
        /// Gets or creates a new effect.<br />
        /// Will create a new effect asset if used at edit time in the editor.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="addFirstEvent">If true then an empty event will be added for newly created effects.</param>
        /// <returns></returns>
        public SoundEffect GetOrCreateEffect(string id, bool addFirstEvent = true)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            // Return existing
            var effect = GetEffect(id);
            if (effect != null)
                return effect;

            // Create new
            effect = SoundEffect.GetFromPool();
            effect.Id = id;

            if (addFirstEvent)
            {
                effect.CreateFirstEventIfNoneExists(addNullClip: false);
            }
            
            Effects.Add(effect);
            
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                AssetDatabase.CreateAsset(effect, "Assets/Resources/" + ASSET_DIR + "sound-effect-" + id + ".asset");
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
#endif

            return effect;
        }

        public void DestroyEffect(string id)
        {
            for (int i = Effects.Count-1; i >= 0; i--)
            {
                if (Effects[i].Id == id)
                {
                    var asset = Effects[i];
                    Effects.RemoveAt(i);

                    // Delete asset and return instance to pool only if not deleted.
#if UNITY_EDITOR
                    if (!EditorApplication.isPlayingOrWillChangePlaymode)
                    {
                        var assetPath = AssetDatabase.GetAssetPath(asset);
                        if (assetPath != null)
                        {
                            // Special case for demo effects (do not ever destroy them).
                            if (!asset.Id.StartsWith("kamgam-demo"))
                                AssetDatabase.DeleteAsset(assetPath);
                        }
                        else
                        {
                            SoundEffect.ReturnToPool(Effects[i]);
                        }
                    }
                    else
                    {
                        SoundEffect.ReturnToPool(_effectAssets[i]);    
                    }
#else
                    SoundEffect.ReturnToPool(_effectAssets[i]);
#endif
                }
            }
            
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
#endif
        }

        public SoundEffect GetEffect(string id)
        {
            foreach (var effect in Effects)
            {
                if (effect != null && string.CompareOrdinal(effect.Id, id) == 0)
                {
                    return effect;
                }
            }

            return null;
        }
        
        protected SoundEffect getEffectAsset(string id)
        {
            if (_effectAssets == null)
                return null;
            
            foreach (var effect in _effectAssets)
            {
                if (effect != null && string.CompareOrdinal(effect.Id, id) == 0)
                {
                    return effect;
                }
            }

            return null;
        }
        
        protected SoundEffect getRuntimeEffect(string id)
        {
            if (_effectsRuntimeCopies == null)
                return null;
            
            foreach (var effect in _effectsRuntimeCopies)
            {
                if (effect != null && string.CompareOrdinal(effect.Id, id) == 0)
                {
                    return effect;
                }
            }

            return null;
        }

        public void Defrag()
        {
#if UNITY_EDITOR
            bool defragged = false;
#endif
            for (int i = Effects.Count-1; i >= 0; i--)
            {
                if (Effects[i] == null)
                {
                    Effects.RemoveAt(i);
#if UNITY_EDITOR
                    defragged = true;
#endif
                }
            }

#if UNITY_EDITOR
            if (defragged && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                UnityEditor.EditorUtility.SetDirty(this);
                UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
            }
#endif
        }
    }
}