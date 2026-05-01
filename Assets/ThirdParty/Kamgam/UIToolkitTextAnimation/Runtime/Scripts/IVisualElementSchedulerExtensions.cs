using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
using UnityEditor;

namespace Kamgam.UIToolkitTextAnimation
{
    public static class IVisualElementSchedulerExtensions
    {
    #if UNITY_EDITOR
        public class VisualElementScheduledItemInEditor : IVisualElementScheduledItem
        {
            static int s_instanceIdCounter = 0;
            public int InstanceId = 0;
            
            public bool isPlaying;
            public int targetFps = 30;
            public Action action;
            public VisualElement targetElement;

            protected double _lastUpdateTime;

            public VisualElementScheduledItemInEditor()
            {
                InstanceId = s_instanceIdCounter++;
            }
            
            public void Resume()
            {
                isPlaying = true;
                EditorApplication.update -= update;
                EditorApplication.update += update;
            }

            public void Pause()
            {
                isPlaying = false;
                EditorApplication.update -= update;
            }

            protected void update()
            {
                if (EditorApplication.timeSinceStartup - _lastUpdateTime >= 1f / targetFps)
                {
                    _lastUpdateTime = EditorApplication.timeSinceStartup;
                    action?.Invoke();
                }
            }

            public void ExecuteLater(long delayMs)
            {
                throw new NotImplementedException();
            }

            public IVisualElementScheduledItem StartingIn(long delayMs)
            {
                throw new NotImplementedException();
            }

            public IVisualElementScheduledItem Every(long intervalMs)
            {
                throw new NotImplementedException();
            }

            public IVisualElementScheduledItem Until(Func<bool> stopCondition)
            {
                throw new NotImplementedException();
            }

            public IVisualElementScheduledItem ForDuration(long durationMs)
            {
                throw new NotImplementedException();
            }

            public VisualElement element => targetElement;
            public bool isActive => element.panel != null && isPlaying;
        }
#endif
        
        public class Condition
        {
            public Func<bool> Predicate;
            public Action Action;
            public bool IsReleased => Predicate == null;
            
#if UNITY_EDITOR
            protected bool m_isUsingEditorUpdate;
            
            public void RegisterUpdateInEditor()
            {
                m_isUsingEditorUpdate = true;
                EditorApplication.update += UpdateInEditor;
            }
            
            public void UnregisterUpdateInEditor()
            {
                if (m_isUsingEditorUpdate)
                {
                    m_isUsingEditorUpdate = false;
                    EditorApplication.update -= UpdateInEditor;
                }
            }
            
            public void UpdateInEditor()
            {
                Update();
            }
    #endif
            
            public bool Update()
            {
                if (Predicate())
                {
                    try
                    {

                        Action();
                    }
                    finally
                    {
                        Release();
                    }
                    return true;
                }
                
                // Another solution would be this:
                // EditorApplication.delayCall += EditorApplication.QueuePlayerLoopUpdate;
                // See: https://discussions.unity.com/t/scheduled-action-is-not-executed-in-gameview-in-edit-mode/1602479/3

                return false;
            }

            public void Release()
            {
                Predicate = null;
                Action = null;
#if UNITY_EDITOR
                UnregisterUpdateInEditor();
#endif
            }
        }

        static List<Condition> s_pool = new List<Condition>();

        static Condition getFromPool(System.Func<bool> predicate, System.Action action)
        {
            // Reuse
            int count = s_pool.Count;
            for (int i = 0; i < count; i++)
            {
                if (s_pool[i].IsReleased)
                {
                    s_pool[i].Predicate = predicate;
                    s_pool[i].Action = action;
                    return s_pool[i];
                }
            }

            // New 
            var cond = new Condition()
            {
                Predicate = predicate,
                Action = action
            };
            s_pool.Add(cond);
            return cond;
        }


        static void doNothing() { }

        static bool forEver() => false;

        public static IVisualElementScheduledItem When(this IVisualElementScheduler scheduler, System.Func<bool> predicate, System.Action action)
        {
            long frameRateInMS = Mathf.FloorToInt(1000f / Mathf.Max(30, Application.targetFrameRate));
            return scheduler.Execute(doNothing).Every(frameRateInMS).Until(getFromPool(predicate, action).Update); 
        }
        
        public static IVisualElementScheduledItem ScheduleWhen(this VisualElement element, System.Func<bool> predicate, System.Action action)
        {
#if UNITY_EDITOR
            // The scheduler does not run reliable in the GameView during EDIT MODE, see:
            // https://discussions.unity.com/t/scheduled-action-is-not-executed-in-gameview-in-edit-mode/1602479
            // Therefor we use the good'ol EditorApplication.update event.
            bool isGameViewInEditMode = element.panel.contextType == ContextType.Player && !EditorApplication.isPlayingOrWillChangePlaymode;
            if (isGameViewInEditMode)
            {
                var condition = getFromPool(predicate, action);
                condition.RegisterUpdateInEditor();

                return null;
            }
            else
            {
#endif
                // In play mode or builds or if in UIBuilder only this line is run.
                return element.schedule.When(predicate, action);
#if UNITY_EDITOR
            }
#endif
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="element"></param>
        /// <param name="action"></param>
        /// <param name="editorTargetFps">The target fps that is used only if in EDITOR MODE and if not playing.</param>
        /// <returns></returns>
        public static IVisualElementScheduledItem EveryFrame(this VisualElement element, System.Action action, int editorTargetFps = 30)
        {
#if !UNITY_EDITOR
            return element.schedule.Execute(action).Until(forEver);
#else
            if (EditorApplication.isPlaying)
            {
                return element.schedule.Execute(action).Until(forEver);
            }
            else
            {
                var scheduledItem = new VisualElementScheduledItemInEditor();
                scheduledItem.targetElement = element;
                scheduledItem.targetFps = editorTargetFps;
                scheduledItem.action = action;
                scheduledItem.Resume();
                return scheduledItem;   
            }
#endif
        }


#if UNITY_EDITOR
        public static void ScheduleGameViewUpdateIfNeeded(VisualElement element)
        {
            if (!EditorApplication.isPlaying && element != null && element.panel != null && element.panel.contextType == ContextType.Player)
            {
                // Fix scheduling in GameView in the Editor if not in play mode (usually it's updated just once).
                // See: https://discussions.unity.com/t/scheduled-action-is-not-executed-in-gameview-in-edit-mode/1602479/3
                EditorApplication.delayCall += EditorApplication.QueuePlayerLoopUpdate;
            }
        }
        #endif
    }
}