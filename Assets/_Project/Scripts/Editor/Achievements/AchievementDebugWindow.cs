using System.Linq;
using Markyu.LastKernel.Achievements;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace Markyu.LastKernel.EditorTools
{
    public class AchievementDebugWindow : OdinEditorWindow
    {
        [MenuItem("Tools/LAST KERNEL/Achievement Debugger")]
        private static void OpenWindow()
        {
            var window = GetWindow<AchievementDebugWindow>();
            window.titleContent = new GUIContent("Achievement Debugger");
            window.Show();
        }

        protected override void OnGUI()
        {
            SirenixEditorGUI.Title("Achievement Debugger", "Play-mode only", TextAlignment.Left, true);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("Enter Play Mode to use this tool.", MessageType.Info);
                return;
            }

            var service = AchievementService.Instance;
            if (service == null)
            {
                EditorGUILayout.HelpBox("AchievementService not found in scene.", MessageType.Warning);
                return;
            }

            var db = GetDatabase(service);
            if (db == null || db.All.Count == 0)
            {
                EditorGUILayout.HelpBox("No AchievementDatabase or definitions found.", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(4);

            foreach (var def in db.All)
            {
                if (def == null) continue;

                bool unlocked = service.IsUnlocked(def);
                float progress = service.GetProgressNormalized(def);
                int count = service.GetProgressCount(def);

                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    if (def.Icon != null)
                    {
                        var tex = AssetPreview.GetAssetPreview(def.Icon) ?? AssetPreview.GetMiniThumbnail(def.Icon);
                        GUILayout.Label(tex, GUILayout.Width(32), GUILayout.Height(32));
                    }

                    using (new EditorGUILayout.VerticalScope())
                    {
                        Color original = GUI.color;
                        GUI.color = unlocked ? Color.green : Color.white;
                        GUILayout.Label($"{def.Id}  {(unlocked ? "✓" : $"{count}/{def.TargetCount}")}", EditorStyles.boldLabel);
                        GUI.color = original;

                        if (def.ShowProgressBar && !unlocked)
                            EditorGUI.ProgressBar(EditorGUILayout.GetControlRect(false, 12), progress, string.Empty);
                    }

                    GUILayout.FlexibleSpace();

                    using (new EditorGUI.DisabledScope(unlocked))
                    {
                        if (GUILayout.Button("Unlock", GUILayout.Width(70)))
                            service.ForceUnlock(def.Id);
                    }
                }
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Unlock All"))
                {
                    foreach (var d in db.All)
                        if (d != null) service.ForceUnlock(d.Id);
                }
                if (GUILayout.Button("Reset All"))
                    Debug.LogWarning("[AchievementDebugger] Use 'Debug — Reset All' on the AchievementService Inspector instead.");
            }

            Repaint();
        }

        private static AchievementDatabase GetDatabase(AchievementService service)
        {
            // Reflect private field — editor only, acceptable.
            var field = typeof(AchievementService)
                .GetField("database", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(service) as AchievementDatabase;
        }
    }
}
