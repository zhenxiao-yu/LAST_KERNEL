using Markyu.LastKernel.Achievements;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.EditorTools
{
    public class AchievementDebugWindow : EditorWindow
    {
        [MenuItem("Tools/LAST KERNEL/Achievement Debugger")]
        private static void OpenWindow()
        {
            var window = GetWindow<AchievementDebugWindow>("Achievement Debugger");
            window.minSize = new Vector2(340, 200);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Achievement Debugger", EditorStyles.boldLabel);
            EditorGUILayout.Space(2);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Info);
                return;
            }

            var service = AchievementService.Instance;
            if (service == null)
            {
                EditorGUILayout.HelpBox("AchievementService not found. Is GameDirector in the scene?", MessageType.Warning);
                return;
            }

            var db = GetDatabase(service);
            if (db == null || db.All.Count == 0)
            {
                EditorGUILayout.HelpBox("AchievementDatabase is empty or not assigned.", MessageType.Warning);
                return;
            }

            foreach (var def in db.All)
            {
                if (def == null) continue;

                bool unlocked = service.IsUnlocked(def);
                int count     = service.GetProgressCount(def);
                float norm    = service.GetProgressNormalized(def);

                using (new EditorGUILayout.HorizontalScope(GUI.skin.box))
                {
                    // Status icon
                    var prevColor = GUI.color;
                    GUI.color = unlocked ? Color.green : Color.grey;
                    GUILayout.Label(unlocked ? "✓" : "○", GUILayout.Width(18));
                    GUI.color = prevColor;

                    // ID + progress
                    using (new EditorGUILayout.VerticalScope())
                    {
                        GUILayout.Label(def.Id, EditorStyles.boldLabel);
                        if (!unlocked && def.TargetCount > 1)
                            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 10), norm, $"{count}/{def.TargetCount}");
                    }

                    GUILayout.FlexibleSpace();

                    // Unlock button — enabled only when not yet unlocked
                    EditorGUI.BeginDisabledGroup(unlocked);
                    if (GUILayout.Button("Unlock", GUILayout.Width(64)))
                        service.ForceUnlock(def.Id);
                    EditorGUI.EndDisabledGroup();
                }
            }

            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Unlock All"))
                    service.DebugUnlockAll();

                if (GUILayout.Button("Reset All"))
                    service.DebugResetAll();
            }

            // Repaint every frame so progress stays live.
            Repaint();
        }

        private static AchievementDatabase GetDatabase(AchievementService service)
        {
            var field = typeof(AchievementService)
                .GetField("database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(service) as AchievementDatabase;
        }
    }
}
