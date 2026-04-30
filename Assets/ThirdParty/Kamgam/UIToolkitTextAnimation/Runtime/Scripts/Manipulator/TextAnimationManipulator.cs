using System.Collections.Generic;
using System.Globalization;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;

namespace Kamgam.UIToolkitTextAnimation
{
    /// <summary>
    /// The text animation manipulator is responsible for updating the vertices in the text mesh.
    /// <br />
    /// There must only ever be one TextAnimationManipulator per TextElement. This single manipulator pumps the update
    /// loop of all the animations. However, it does so indirectly by calling MarkDirtyRepaint() on the element if
    /// there are any animations that are playing. This then triggers generateVisualContent() which triggers the
    /// pre/postGenerateVisualContent() in the manipulator. That's where the vertices of each character are updated.
    /// </br />
    /// Each manipulator uses a scheduled update loop to pump the animations. This loop will run for as long as there is
    /// one or more animations that are not yet completed (as defined by the return values in the updateCharacter() methods
    /// in each animation config. If all animations completed then it will also pause the scheduled loop.
    /// NOTICE: It does NOT stop the scheduled loop if the animations are merely paused because even if  paused we have
    /// to update all the vertices (because they are reset by Unity at the start of every frame). Calling Play() on the
    /// Manipulator will also restart the update loop if needed. Changing the text or adding classes will also call Play()
    /// if AutoPlay is enabled.
    /// </summary>
    public partial class TextAnimationManipulator : MeshManipulator<TextAnimationManipulator>
    {
        private static int s_instanceIdCounter = 0;
#if UNITY_EDITOR
        private static List<string> s_missingIdsWarnedAlready = new List<string>();
#endif
        
        public delegate void OnGenerateVisualContentDelegate(TextElement element, MeshGenerationContext mgc);
        public delegate void OnVisibilityChangedDelegate(TextElement element, bool visible);

        /// <summary>
        /// Append to this to modify the mesh after the animation changes.
        /// </summary>
        public OnGenerateVisualContentDelegate OnAfterAnimationChanges;
        
        /// <summary>
        /// Called if the element visibility changed (not meaning the USS property but whether it is rendered or not).
        /// </summary>
        public OnVisibilityChangedDelegate OnVisibilityChanged;

        public bool RemoveOnPlayModeStateChange = true;
        public TextInfoAccessor TextInfoAccessor;
        
        protected TextElement m_textElement;
        public TextElement TargetTextElement
        {
            get
            {
                if (m_textElement == null || m_textElement != target)
                {
                    m_textElement = target as TextElement;
                }

                return m_textElement;
            }
        }
        
        protected List<TextInfoAccessor.AnimationTagInfo> m_animationTagInfos = new List<TextInfoAccessor.AnimationTagInfo>();
        /// <summary>
        /// For ids that have not been found it will contains null entries. Contains a many entries as there are animation ids in the text (in order).
        /// </summary>
        protected List<TextAnimation> m_characterAnimations = new List<TextAnimation>();
        
        protected List<string> m_typewriterAnimationIds = new List<string>();
        protected List<TextAnimation> m_typewriterAnimations = new List<TextAnimation>();

        public bool HasCharacterAnimations() => m_characterAnimations.Count > 0;
        public bool HasTypewriterAnimations() => m_typewriterAnimations.Count > 0;

        // For debugging
        private int m_instanceId;
        public int InstanceId => m_instanceId;

        protected TextAnimations m_configs;
        public TextAnimations Configs
        {
            get
            {
                if (m_configs == null)
                {
                    m_configs = TextAnimationsProvider.GetAnimations();
                }

                return m_configs;
            }
        }

        protected IVisualElementScheduledItem _scheduledUpdateLoop;

        /// <summary>
        /// If enabled then the animation will start playing as soon as the manipulator is added to the element.<br />
        /// This can be turned off by adding the "text-animation-auto-play-off" class to your element.
        /// </summary>
        public bool AutoPlay = true;
        
        /// <summary>
        /// Whether or not the manipulator animations are playing. This state is stored in the manipulator itself.
        /// It only controls if the animation timer on each animation is increased.<br />
        /// Notice: If all configs are set to paused manually without going through the manipulator
        /// this will still return true. If possible do pause/resume/restart through the manipulator.
        /// </summary>
        public bool IsPlaying { get; private set; }
        
        /// <summary>
        /// Internal time tracking on the manipulator level. Used only to initialize the Time of new animations.
        /// </summary>
        protected float m_animationTime;

#if UNITY_EDITOR
        protected float m_editorLastUpdateTime;
#endif
        
        /// <summary>
        /// Flag to indicate if the accessor text infos should be updated before the next render. This is used
        /// to delay the update until the actual rendering is done. Why? In the past we did this during the onTextChanged
        /// event but it seems the internal text rendering is not yet updated then (possibly because it also listens to
        /// the same event) and thus we have to wait. The rendering (generateVisualContent) callback seems to be a good option.
        /// </summary>
        protected bool m_textInfoIsDirty;
        
        /// <summary>
        /// If m_textInfoIsDirty is true then this may hold a ChangeEvent<string> if the text was changes.
        /// </summary>
        protected ChangeEvent<string> m_tmpTextChangeEvent;

        protected void disposeTmpTextChangeEvent()
        {
            if (m_tmpTextChangeEvent != null)
            {
                m_tmpTextChangeEvent.Dispose();
                m_tmpTextChangeEvent = null;
            }
        }
        
        public TextAnimationManipulator()
        {
            m_instanceId = s_instanceIdCounter;
            s_instanceIdCounter++;
            
#if UNITY_EDITOR
            UnityEditor.EditorApplication.playModeStateChanged -= onPlayModeStateChanged;
            UnityEditor.EditorApplication.playModeStateChanged += onPlayModeStateChanged;
#endif
            Reset();
        }

        public void Reset()
        {
            m_textElement = null;
            m_animationTime = 0f;
#if UNITY_EDITOR
            m_editorLastUpdateTime = 0f;
#endif
            target = null;
            RemoveOnPlayModeStateChange = true;
            AutoPlay = true;
            IsPlaying = false;
            
            m_animationTagInfos.Clear();
            m_characterAnimations.Clear();
            
            m_typewriterAnimationIds.Clear();
            m_typewriterAnimations.Clear();
            
            TextInfoAccessor?.Reset();

            if (_scheduledUpdateLoop != null && _scheduledUpdateLoop.isActive)
            {
                _scheduledUpdateLoop.Pause();
            }

            OnAfterAnimationChanges = null;
            
            m_textInfoIsDirty = false;
            m_tmpTextChangeEvent = null;
        }
        
        /// <summary>
        /// In the editor elements are often recreated without removing the manipulators from their elements. This checks
        /// if the manipulator has a target and if that target is part of a panel. Only then it returns true.
        /// </summary>
        /// <returns></returns>
        public bool HasValidTarget()
        {
            return target != null && target.panel != null;
        }

        protected override void RegisterCallbacksOnTarget()
        {
            base.RegisterCallbacksOnTarget();

            AutoPlay = !target.ClassListContains(TEXT_ANIMATION_DISABLE_AUTO_PLAY_CLASSNAME);
            
            target.RegisterCallback<DetachFromPanelEvent>(onDetach);
            TargetTextElement.RegisterValueChangedCallback(onTextChanged);
            target.RegisterCallback<GeometryChangedEvent>(onGeometryChanged);

            var accessor = getOrCreateTextInfoAccessor();
            accessor.Reuse(TargetTextElement);
            
            m_textInfoIsDirty = true;
            disposeTmpTextChangeEvent();

            _scheduledUpdateLoop = TargetTextElement.EveryFrame(
                scheduleAnimationUpdate,
                // Notice: The frame rate is only used in the EDITOR while editing. At runtime it's the default targetFrameRate. 
                editorTargetFps: Configs == null ? Application.targetFrameRate : Configs.EditorTargetFrameRate);

            // Make sure the loop is paused and resumed according to attach state.
            OnElementAttachToPanel -= onAttachToPanel;
            OnElementAttachToPanel += onAttachToPanel;
            
            OnElementDetachFromPanel -= onDetachFromPanel;
            OnElementDetachFromPanel += onDetachFromPanel;

            Restart(paused: !AutoPlay);
            
            // Mark dirty to force an update.
            TargetTextElement.MarkDirtyRepaint();
        }

        // Element show/hide detection, see: https://discussions.unity.com/t/event-when-visualelement-gets-hidden/871773/8
        System.Nullable<bool> m_lastIsDrawnValue; // Nullable to ensure we catch the initial change.
        
        private void onGeometryChanged(GeometryChangedEvent evt)
        {
            bool isDrawn = IsDrawn();

            if (!m_lastIsDrawnValue.HasValue || m_lastIsDrawnValue.Value != isDrawn)
            {
                m_lastIsDrawnValue = isDrawn;
                OnVisibilityChanged?.Invoke(TargetTextElement, isDrawn);
                
                // Restart on enable.
                Restart(paused: !AutoPlay, time: 0f);
            }
        }
        
        /// <summary>
        /// Returns whether or not the image is drawn based on the contentRect size.
        /// It acts like the activeInHierarchy we are used to from game objects (takes parents into account).
        /// </summary>
        /// <returns></returns>
        public bool IsDrawn()
        {
            var rect = target.contentRect;
            return !float.IsNaN(rect.width) && !Mathf.Approximately(rect.width, 0f) && !Mathf.Approximately(rect.height, 0f);
        }

        private void onAttachToPanel(TextAnimationManipulator arg1, AttachToPanelEvent arg2)
        {
            _scheduledUpdateLoop?.Resume();
        }
        
        private void onDetachFromPanel(TextAnimationManipulator arg1, DetachFromPanelEvent arg2)
        {
            _scheduledUpdateLoop?.Pause();
        }

        private void scheduleAnimationUpdate()
        {
#if UNITY_EDITOR
            IVisualElementSchedulerExtensions.ScheduleGameViewUpdateIfNeeded(TargetTextElement);
#endif
            TargetTextElement.MarkDirtyRepaint();
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            base.UnregisterCallbacksFromTarget();
            target.UnregisterCallback<DetachFromPanelEvent>(onDetach);
            TargetTextElement.UnregisterValueChangedCallback(onTextChanged);
            target.UnregisterCallback<GeometryChangedEvent>(onGeometryChanged);
            TextInfoAccessor?.Reset();
            _scheduledUpdateLoop?.Pause();
        }

        public void Pause()
        {
            IsPlaying = false;
            
            // pause all animation configs in this manipulator
            foreach (var config in m_typewriterAnimations)
            {
                config.Pause();
            }
            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                config.Pause();
            }
        }

        public void Resume()
        {
            Play();
        }

        public void Play()
        {
            IsPlaying = true;

            // resume all animation configs in this manipulator
            foreach (var config in m_typewriterAnimations)
            {
                config.Resume();
            }
            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                config.Resume();
            }

            // Trigger the update loop.
#if UNITY_EDITOR
            m_editorLastUpdateTime = Time.realtimeSinceStartup;
#endif
            
            TargetTextElement.MarkDirtyRepaint();

            // Ensure the scheduled loop starts running again if needed.
            resumeOrPauseAnimationUpdates(true);
        }
        
        public void Restart(bool paused = false, float time = 0f)
        {
            IsPlaying = !paused;
            m_animationTime = time;
            
            // restart all animation configs in this manipulator
            foreach (var config in m_typewriterAnimations)
            {
                config.Restart(paused, time);
            }
            
            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                config.Restart(paused, time);
            }

            // Trigger the update loop.
#if UNITY_EDITOR
            m_editorLastUpdateTime = Time.realtimeSinceStartup;
#endif
            TargetTextElement.MarkDirtyRepaint();
        }

        protected void advanceAnimationTimes(float deltaTime)
        {
            m_animationTime += deltaTime;
            
            foreach (var config in m_typewriterAnimations)
            {
                if (config.IsPlaying)
                    config.AdvanceAnimationTime(deltaTime);
            }
            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                if (config.IsPlaying)
                    config.AdvanceAnimationTime(deltaTime);
            }
        }

        public int AnimationCount => m_characterAnimations.Count + m_typewriterAnimations.Count;

        /// <summary>
        /// Returns the typewriter animations first and then the tag animations.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public TextAnimation GetAnimationAt(int index)
        {
            int tagAnimationIndex = index - m_typewriterAnimations.Count;
            if (tagAnimationIndex >= 0)
            {
                return m_characterAnimations[tagAnimationIndex];
            }
            else
            {
                return m_typewriterAnimations[index];
            }
        }

        /// <summary>
        /// Gets the animation by id. If the text element has multiple animations with the same id the the index can be used to pick one.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetAnimation<T>(string id, int index = 0) where T : TextAnimation
        {
            var config = GetAnimation(id, index);
            if (config == null)
                return null;

            return config as T;
        }

        /// <summary>
        /// Gets the animation by id. If the text element has multiple animations with the same id the the index can be used to pick one.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        public TextAnimation GetAnimation(string id, int index = 0)
        {
            int cIndex = 0;
            foreach (var animation in m_characterAnimations)
            {
                if (animation == null)
                    continue;
                
                if (animation.Id == id)
                {
                    if (cIndex == index)
                        return animation;
                    cIndex++;
                }
            }

            return null;
        }
        
        private TextInfoAccessor getOrCreateTextInfoAccessor()
        {
            if (TextInfoAccessor == null)
                TextInfoAccessor = new TextInfoAccessor(TargetTextElement);

            return TextInfoAccessor;
        }

        private void onTextChanged(ChangeEvent<string> evt)
        {
            m_textInfoIsDirty = true;

            // Fetch a copy from the pool since the original will be disposed.
            disposeTmpTextChangeEvent();
            m_tmpTextChangeEvent = ChangeEvent<string>.GetPooled(evt.previousValue, evt.newValue);
            
            TargetTextElement.MarkDirtyRepaint();

            if (AutoPlay)
                Play();
        }

        private void onDetach(DetachFromPanelEvent evt)
        {
        }

#if UNITY_EDITOR
        private void onPlayModeStateChanged(PlayModeStateChange change)
        {
            if (RemoveOnPlayModeStateChange)
            {
                if (change == PlayModeStateChange.ExitingPlayMode || change == PlayModeStateChange.ExitingEditMode)
                {
                    if (target != null)
                        target.RemoveManipulator(this);
                }
            }
        }
#endif
        
        private bool m_IsWaitingForTargetLinkInfos;
        
        protected void rebuildCharacterAndTagInfos()
        {
            // Init accessor if needed. Abort if failed.
            var accessor = getOrCreateTextInfoAccessor();
            if (!accessor.IsValid)
            {
                TextInfoAccessor.AnimationTagInfo.ReturnToPool(m_animationTagInfos);
                TextAnimation.ResetAndReturnToPool(m_characterAnimations);
                return;
            }

            // Extract character and tag infos (link tags may not be available yet).
            TextInfoAccessor.GetCharacterAndTagInfos(m_animationTagInfos);
            
            // NOTICE: In frame 0 and 1 link tags may not yield any results for TextInfo since those do not yet
            //         exist at that time, but it will fill the animation links based on the RegExp detection.
            //         This means HasAnimationLinks() may return true while HasLinkInfos() returns false.
            if (accessor.HasLinkInfos())
            {
                rebuildAnimationTagConfigs();
            }
            // If there are links detected in the text but the info list is still empty then retry until
            // a link is found or a max number of retries has been reached.
            else if(accessor.HasTagsUsedForAnimation())
            {
                m_IsWaitingForTargetLinkInfos = true;
                
                // Check if the element is already valid (has been layouted and textInfo has been filled).
                // Usually in frame 0 this is not yet done and we have to wait until frame 1.
                m_getAnimationInfoTriesLeft = 10;
                TargetTextElement.ScheduleWhen(getAndUpdateLinkInfoCount, onTargetLinkInfosFound); 
            }
        }

        protected void rebuildAnimationTagConfigs()
        {
            TextAnimation.GetCopiesBasedOnTagInfos(m_animationTagInfos, Configs, m_characterAnimations);

            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                config.Restart(paused: !IsPlaying, m_animationTime);
            }
        }

        /// <summary>
        /// If evt != null then the rebuild was due to a change in text.
        /// </summary>
        /// <param name="prevQuadCount"></param>
        /// <param name="evt"></param>
        protected void rebuildTypeWriterInfo(int prevQuadCount, ChangeEvent<string> evt)
        {
            m_typewriterAnimationIds.Clear();
            TextAnimation.ResetAndReturnToPool(m_typewriterAnimations);

            var classes = TargetTextElement.GetClasses();
            foreach (var className in classes)
            {
                if (className.StartsWith(TEXT_TYPEWRITER_CLASSNAME))
                {
                    // Extract id from class name
                    var animationId = className.Substring(TEXT_TYPEWRITER_CLASSNAME.Length);
                    
                    // Also add a config for each typewriter animation id.
                    var baseConfig = Configs.GetAnimation(animationId);
                    if (baseConfig == null)
                        continue;
                    
                    // Add to typewriter ids list.
                    m_typewriterAnimationIds.Add(animationId);

                    var config = TextAnimation.GetCopyFromPool(baseConfig);
                    m_typewriterAnimations.Add(config);
                    
                    // Ensure animation time consistency
                    if (prevQuadCount != TextInfoAccessor.QuadCount)
                        config.Restart(paused: !IsPlaying, m_animationTime, evt, prevQuadCount, TextInfoAccessor.DelayTagInfos);
                }
            }
        }

        private int m_getAnimationInfoTriesLeft;

        private bool getAndUpdateLinkInfoCount()
        {
            if (TargetTextElement == null || TargetTextElement.panel == null)
                return true;
            
            m_getAnimationInfoTriesLeft--;
            if (m_getAnimationInfoTriesLeft <= 0)
            {
                return true;
            }

            TextInfoAccessor.UpdateLinkInfoCount();
            return TextInfoAccessor.HasLinkInfos();
        }

        private void onTargetLinkInfosFound()
        {
            m_IsWaitingForTargetLinkInfos = false;
                
            if (TargetTextElement == null || TargetTextElement.panel == null)
                return;
            
            TextInfoAccessor.GetCharacterAndTagInfos(m_animationTagInfos);
            if (TextInfoAccessor.HasLinkInfos())
                rebuildAnimationTagConfigs();
            
            TargetTextElement.MarkDirtyRepaint();
        }

        public void UpdateAfterClassChange(bool addRemoveIfNeeded = true)
        {
            if (addRemoveIfNeeded)
            {
                AddOrRemoveManipulator(TargetTextElement);    
            }

            m_textInfoIsDirty = true;

            if (AutoPlay)
                Play();
        }

#if UNITY_2023_1_OR_NEWER
        // Unity 2023+ requires a different implementation and a different time than Unity 2022-.
        protected override void preGenerateVisualContent(MeshGenerationContext mgc)
        {
            // SKip if unnecessary (unless the text infos are dirty).
            if (!m_textInfoIsDirty && string.IsNullOrEmpty(TargetTextElement.text))
                return;
            
            if (!m_textInfoIsDirty && m_animationTagInfos.Count == 0 && m_typewriterAnimationIds.Count == 0)
                return;
            
            var accessor = getOrCreateTextInfoAccessor();
            if (!accessor.IsValid)
                return;
            
            if (accessor.MeshIsDirty())
            {
                accessor.UpdateMesh(); 
            }
            
            // Refresh the access to the native vertex data.
            accessor.CacheMeshVertexAccess();

            if (m_textInfoIsDirty)
            {
                int prevQuadCount = TextInfoAccessor.QuadCount;
                rebuildCharacterAndTagInfos();
                rebuildTypeWriterInfo(prevQuadCount, m_tmpTextChangeEvent);
                disposeTmpTextChangeEvent();
                m_textInfoIsDirty = false;
            }
            
            if (IsPlaying)
            {
                advanceAnimationTimes(getDeltaTime());
            }

            // We set playing to false but if any of the animations is still playing it will reset it to true.
            m_tmpIsAnyAnimationPlaying = false;
            
            // Force to true while waiting for animation infos because otherwise the typewriter could set it to false
            // before any animations have started.
            if (m_animationTagInfos.Count == 0 && m_IsWaitingForTargetLinkInfos)
                m_tmpIsAnyAnimationPlaying = true;

            // Detect any internal changes to the vertices.
            accessor.UpdateVerticesOverwrittenFlag();

            // Typewriter animations will cache all vertices and set this to true in ExecuteOnAllVertices().
            // Used in ExecuteOnTagVertices() to determine cache usage.
            accessor.DidCacheAllOriginalVertices = false;

            // Update configs parent/child
            pullConfigChangedValuesIfNecessary();

            // We store the state so we can restore it at the end of the frame
            var rngState = UnityEngine.Random.state;

            // We update the vertices even if paused because we have to update the vertices after every
            // generateVisualContent() call to make the changes stick (otherwise the default values would overwrite them again).
            // It's important that typewriter animations come FIRST because the order of the character is based on their position
            // and that may be changed by regular animations. Also they cache all the vertices which can then be reused by the
            // tag animations.
            if (m_typewriterAnimationIds.Count > 0)
            {
                accessor.ExecuteOnAllVertices(updateCharacter);
            }
            
            if (m_animationTagInfos.Count > 0)
            {
                accessor.ExecuteOnTagVertices(m_animationTagInfos, updateCharacter);
            }

            // Restore random state.
            UnityEngine.Random.state = rngState;
            
            // Call user defined callback in an error resistant manner.
            try
            {
                OnAfterAnimationChanges?.Invoke(TargetTextElement, mgc);
            }
            finally
            {
                // Update animation state depending on whether or not any animations are still playing.
                resumeOrPauseAnimationUpdates(m_tmpIsAnyAnimationPlaying);
            }
        }
#endif

        private bool m_tmpIsAnyAnimationPlaying;
        
        // Cache delegate to avoid memory allocations.
        protected TextInfoAccessor.ChangeCharacterDelegate _updateCharacterDelegate;

#if !UNITY_2023_1_OR_NEWER        
        protected override void postGenerateVisualContent(MeshGenerationContext mgc)
        {
            var accessor = getOrCreateTextInfoAccessor();
            if (!accessor.IsValid)
                return;

            // Refresh the access to the native vertex data.
            if (m_textInfoIsDirty)
            {
                int prevQuadCount = TextInfoAccessor.QuadCount;
                rebuildCharacterAndTagInfos();
                rebuildTypeWriterInfo(prevQuadCount, m_tmpTextChangeEvent);
                disposeTmpTextChangeEvent();
                accessor.ClearQuadInformation(); // <- triggers quad update in ExecuteOnAllVertices()
                m_textInfoIsDirty = false;
            }

            if (string.IsNullOrEmpty(TargetTextElement.text))
                return;
            
            if (m_animationTagInfos.Count == 0 && m_typewriterAnimationIds.Count == 0)
                return;

            if (IsPlaying)
            {
                advanceAnimationTimes(getDeltaTime());
            }

            // We set playing to false but if any of the animations is still playing it will reset it to true.
            m_tmpIsAnyAnimationPlaying = false;

            // Force to true while waiting for animation infos because otherwise the typewriter could set it to false
            // before any animations have started.
            if (m_animationTagInfos.Count == 0 && m_IsWaitingForTargetLinkInfos)
                m_tmpIsAnyAnimationPlaying = true;

            accessor.OnStartGenerateVisualContent();

            // Cache delegate to avoid memory allocations.
            if (_updateCharacterDelegate == null)
                _updateCharacterDelegate = new TextInfoAccessor.ChangeCharacterDelegate(updateCharacter);

            // Update configs parent/child
            pullConfigChangedValuesIfNecessary();
            
            // We store the state so we can restore it at the end of the frame
            var rngState = UnityEngine.Random.state;

            // We update the vertices even if paused because we have to update the vertices after every
            // generateVisualContent() call to make the changes stick (otherwise the default values would overwrite them again).
            // It's important that typewriter animations come FIRST because the order of the character is based on their position
            // and that may be changed by regular animations.
            if (m_typewriterAnimationIds.Count > 0)
                accessor.ExecuteOnAllVertices(mgc, _updateCharacterDelegate);
            
            if (m_animationTagInfos.Count > 0)
                accessor.ExecuteOnTagVertices(mgc, m_animationTagInfos, _updateCharacterDelegate);

            // Restore random state.
            UnityEngine.Random.state = rngState;
            
            // Advance RNG to enable default Random.Range behaviour inside animations (makes rng tick once).
            UnityEngine.Random.Range(1, 10);

            // Call user defined callback
            OnAfterAnimationChanges?.Invoke(TargetTextElement, mgc);
            
            // Update animation state
            resumeOrPauseAnimationUpdates(m_tmpIsAnyAnimationPlaying);
        }
#endif
        private float getDeltaTime()
        {
#if UNITY_EDITOR
            if (EditorApplication.isPlaying)
            {
#endif
                return DeltaTimeUtils.unscaledDeltaTime;
#if UNITY_EDITOR
            }
            else
            {
                // In editor
                float deltaTime = Time.realtimeSinceStartup - m_editorLastUpdateTime;
                m_editorLastUpdateTime = Time.realtimeSinceStartup;
                return deltaTime;   
            }
#endif
        }
        
        private void resumeOrPauseAnimationUpdates(bool play)
        {
            if (_scheduledUpdateLoop != null)
            {
                if (play)
                {
                    if (!_scheduledUpdateLoop.isActive)
                    {
                        _scheduledUpdateLoop.Resume();
                    }
                }
                else
                {
                    if (_scheduledUpdateLoop.isActive)
                    {
                        _scheduledUpdateLoop.Pause();
                    }
                }
            }
        }

        private void pullConfigChangedValuesIfNecessary()
        {
            foreach (var config in m_typewriterAnimations)
            {
                config.PullChangedValuesIfNecessary();
            }
            
            foreach (var config in m_characterAnimations)
            {
                if (config == null)
                    continue;
                
                config.PullChangedValuesIfNecessary();
            }
        }
        
        /// <summary>
        /// Is called during generateVisualContent() on each character that is part of a "link anim" tag.<br />
        /// The order is not defined. It is based on the internal mesh data structures (batches) which are drawn in
        /// an undefined order. Use characterIndex for ordering animations.
        /// The character index is based on the finally rendered characters including white space chars but no tags.
        /// Each character will update IsPlaying to true if the characters animation is not yet finished.
        /// </summary>
        /// <param name="tagInfo"></param>
        /// <param name="characterIndex">The character index is based on the finally rendered characters including white space chars but no tags.</param>
        /// <param name="quadIndex">The quad index is based on the finally rendered characters excluding white space chars and tags but including sprites.</param>
        /// <param name="totalQuadCount"></param>
        /// <param name="quadVertexData"></param>
        /// <param name="delayTagInfos"></param>
        private void updateCharacter(
            TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            int quadIndex, int totalQuadCount,
            TextInfoAccessor.QuadVertexData quadVertexData,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos)
        {
            if (tagInfo != null)
            {
                // Get the index of the animation config in the linear m_animationTagAnimationConfigs list by
                // counting through all the animation ids (the index is the sum of all that come before).
                int index = m_animationTagInfos.IndexOf(tagInfo);
                int animationIndex = 0;
                for (int i = 0; i < index; i++)
                {
                    animationIndex += m_animationTagInfos[i].Ids.Length;
                }
                
                foreach (var id in tagInfo.Ids)
                {
                    if (animationIndex >= m_characterAnimations.Count)
                        break;
                    
                    var config = m_characterAnimations[animationIndex];
                    if (config != null)
                    {
                        updateCharacterWithConfig(tagInfo, characterIndex,
                            quadIndex, totalQuadCount,
                            quadVertexData, config, id, delayTagInfos);
                    }

                    animationIndex++;
                }
            }
            else
            {
                for (int i = 0; i < m_typewriterAnimationIds.Count; i++)
                {
                    if (i >= m_typewriterAnimationIds.Count)
                        break;
                    
                    string id = m_typewriterAnimationIds[i];
                    var config = m_typewriterAnimations[i];
                    if (config != null)
                    {
                        updateCharacterWithConfig(tagInfo, characterIndex,
                            quadIndex, totalQuadCount,
                            quadVertexData, config, id, delayTagInfos);
                    }
                }   
            }
        }

        private void updateCharacterWithConfig(TextInfoAccessor.AnimationTagInfo tagInfo, int characterIndex,
            int quadIndex, int totalQuadCount,
            TextInfoAccessor.QuadVertexData quadVertexData,
            TextAnimation config,
            string id,
            List<TextInfoAccessor.DelayTagInfo> delayTagInfos
            )
        {
            // TODO: Investigate warning (is shown on play mode exit and was annoying, maybe delete completely?)
//            if (config == null)
//            {
//#if UNITY_EDITOR
//                if (!s_missingIdsWarnedAlready.Contains(id))
//                {
//                    s_missingIdsWarnedAlready.Add(id);
//                    Debug.Log("No TextAnimation with id '" + id + "' found in " + Configs.name + ". Animation will not be added.");
//                }
//#endif
//            }
//            else
            {
                m_tmpIsAnyAnimationPlaying |= config.UpdateCharacter(
                    TargetTextElement, tagInfo, characterIndex, quadIndex, totalQuadCount,
                    config.Time, getDeltaTime(),
                    quadVertexData, delayTagInfos);
            }
        }
    }
}