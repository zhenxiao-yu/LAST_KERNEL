using UnityEngine;
using UnityEngine.UIElements;
using System;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Kamgam.UIToolkitSoundEffects
{
    public static class VisualElementSchedulerExtensions
    {
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
                        //Debug.Log("Scheduler action manipulator nr " + (Predicate.Target as TextAnimationManipulator).InstanceId + " in context: " + (Predicate.Target as TextAnimationManipulator).TargetTextElement.panel.contextType); 
                        Action();
                    }
                    finally
                    {
                        Release();
                    }
                    return true;
                }

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
    }
}