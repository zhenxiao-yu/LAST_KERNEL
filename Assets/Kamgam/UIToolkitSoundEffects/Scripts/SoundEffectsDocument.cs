using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitSoundEffects
{
    [RequireComponent(typeof(UIDocument))]
    public class SoundEffectsDocument : MonoBehaviour
    {
        public static List<SoundEffectsDocument> Documents = new List<SoundEffectsDocument>();
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void InitializeOnLoadAttribute()
        {
            // Called upon play mode start (even if domain reload is disabled).
            SceneManager.sceneLoaded -= onSceneLoaded;
            SceneManager.sceneLoaded += onSceneLoaded;
            
            Documents.Clear();
        }

        private static void onSceneLoaded(Scene scene, LoadSceneMode mode)
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
                return;
#endif
            
            var effects = SoundEffects.GetOrCreate();
            
            // Abort if auto-detect is off.
            if (effects == null || !effects.AutoDetectUIDocuments)
                return;
            
            AddToAllUIDocumentsInScene(scene);
        }
        
        public static void AddToAllUIDocumentsInAllScenes(bool allowAbort = true)
        {
            var effects = SoundEffects.GetOrCreate();
            
            // Abort if auto-detect is off.
            if (allowAbort && (effects == null || !effects.AutoDetectUIDocuments))
                return;

            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (scene.IsValid())
                {
                    AddToAllUIDocumentsInScene(scene);
                }
            }
        }

        public static void AddToAllUIDocumentsInScene(Scene scene)
        {
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                var uiDocuments = root.GetComponentsInChildren<UIDocument>(includeInactive: true);
                foreach (var doc in uiDocuments)
                {
                    AddToUIDocument(doc);
                }
            }
        }
        
        public static SoundEffectsDocument AddToUIDocument(UIDocument uiDocument)
        {
            if (uiDocument == null)
                return null;
            
            var soundEffectDocument = uiDocument.gameObject.GetComponent<SoundEffectsDocument>();
            if (soundEffectDocument == null)
            {
                soundEffectDocument = uiDocument.gameObject.AddComponent<SoundEffectsDocument>();
                soundEffectDocument.CreateOrUpdateManipulators();
            }

            return soundEffectDocument;
        }

        protected UIDocument document;

        public UIDocument Document
        {
            get
            {
                if (document == null)
                {
                    document = this.GetComponent<UIDocument>();
                }

                return document;
            }
        }

        public void OnEnable()
        {
            Documents.Add(this);
            SoundEffectManipulator.DefragManipulators();
            CreateOrUpdateManipulators();
        }

        public void OnDisable()
        {
            Documents.Remove(this);
        }
        
        /// <summary>
        /// Calls CreateOrUpdateManipulators() on all known UI Documents.
        /// </summary>
        public static void CreateOrUpdateManipulatorsOnAll()
        {
            foreach (var doc in Documents)
            {
                doc.CreateOrUpdateManipulators();
            }
        }

        /// <summary>
        /// This is called automatically from Start() but you can (should) also call it any time after the visual tree contents have change. 
        /// </summary>
        public void CreateOrUpdateManipulators()
        {
            if (Document == null || Document.rootVisualElement == null)
                return;

            var elements = Document.rootVisualElement.Query(className: SoundEffect.CLASSNAME).Build();
            foreach (var element in elements)
            {
                SoundEffectManipulator.CreateOrUpdate(element);
            }
        }
    }
}