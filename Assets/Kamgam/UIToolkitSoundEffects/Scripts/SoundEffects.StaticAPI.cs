using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Serialization;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEditor.Callbacks;
using UnityEditor;
#endif

namespace Kamgam.UIToolkitSoundEffects
{
    public partial class SoundEffects : ScriptableObject
    {
        public static string ASSET_DIR = "UIToolkit/SoundEffects/";
        public static string ASSET_FILE_NAME = "UITK SoundEffects";
        
        public static SoundEffects _asset;
        
        public static SoundEffects GetOrCreate()
        {
#if UNITY_EDITOR
            // In the editor we also check if the actual asset exists.
            if (_asset != null && (EditorApplication.isPlayingOrWillChangePlaymode || AssetDatabase.GetAssetPath(_asset) != null))
            {
                return _asset;
            }
            
            _asset = Resources.Load<SoundEffects>( ASSET_DIR + ASSET_FILE_NAME);
            
            if (_asset == null)
            {
                string dir = "Resources/" + ASSET_DIR.Trim('/');
                createFolder(dir);
                
                _asset = CreateInstance<SoundEffects>();
                _asset.Initialize();
        
                AssetDatabase.CreateAsset(_asset, "Assets/Resources/" + ASSET_DIR + ASSET_FILE_NAME + ".asset");
                
                return _asset;
            }
#else
            if (_asset != null)
                return _asset;

            _asset = Resources.Load<SoundEffects>( ASSET_DIR + ASSET_FILE_NAME);
#endif
            return _asset;
            
        }
        
        
#if UNITY_EDITOR
        static void createFolder(string path)
        {
            var folders = path.Split('/');

            string currentPath = "Assets";

            bool created = false;
            foreach (string folder in folders)
            {
                currentPath = System.IO.Path.Combine(currentPath, folder);
                if (!AssetDatabase.IsValidFolder(currentPath))
                {
                    string parentFolder = System.IO.Path.GetDirectoryName(currentPath);
                    AssetDatabase.CreateFolder(parentFolder, folder);
                    created = true;
                }
            }
            
            if (created)
                Debug.Log($"AudioEffects created folder: '{path}'. Hope that's okay.");
        }

        [DidReloadScripts(-1)]
        public static void onDomainReload()
        {
            EditorApplication.delayCall += () =>
            {
                var effects = GetOrCreate();
                if (effects != null)
                {
                    effects.Defrag();
                }
            };
        }

        public static StyleSheet FindInspectorStyleSheet()
        {
            // Search in asset database and return first found.
            var guids = UnityEditor.AssetDatabase.FindAssets("t:" + nameof(StyleSheet) + " UITKSoundEffectsInspectorStyleSheet");
            if (guids.Length > 0)
            {
                // First try locating if from asset directory.
                string path;
                foreach (var guid in guids)
                {
                    path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);

                    if (path.Contains(Installer.AssetRootPath))
                        return UnityEditor.AssetDatabase.LoadAssetAtPath<StyleSheet>(path);
                }
            }

            return null;
        }
#endif
    }
}