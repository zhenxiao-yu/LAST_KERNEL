using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UIElements;

namespace Markyu.LastKernel
{
    /// <summary>
    /// Scans all loaded assemblies for non-abstract UIToolkitScreenController
    /// subclasses decorated with [UIScreen], then creates any missing GameObjects
    /// in the active scene and auto-wires [SerializeField] Component dependencies.
    ///
    /// Run via: Tools → LAST KERNEL → Wire UI Screens
    /// </summary>
    public static class UIScreenAutoWirer
    {
        // ── Menu entry ─────────────────────────────────────────────────────────

        [MenuItem("Tools/LAST KERNEL/Wire UI Screens")]
        public static void WireActiveScene()
        {
            var report = new StringBuilder();
            int created = 0;
            int wired   = 0;
            int skipped = 0;

            PanelSettings panelSettings = FindPanelSettings();
            if (panelSettings == null)
            {
                EditorUtility.DisplayDialog(
                    "Wire UI Screens",
                    "No PanelSettings asset found in the project. Create one first.",
                    "OK");
                return;
            }

            foreach (var entry in FindScreenEntries())
            {
                var existing = FindExistingInstance(entry.ControllerType);

                if (existing != null)
                {
                    report.AppendLine($"  ✓  {entry.ControllerType.Name}  (already in scene)");
                    skipped++;
                    continue;
                }

                var go         = CreateScreenObject(entry, panelSettings);
                var controller = go.GetComponent(entry.ControllerType) as MonoBehaviour;
                created++;
                report.AppendLine($"  +  {entry.ControllerType.Name}  →  {entry.Attribute.UxmlPath}");

                int autoWired = AutoWireDependencies(controller, entry.ControllerType, report);
                wired += autoWired;
            }

            if (created == 0 && skipped > 0)
            {
                report.Insert(0, "All UI screens are already wired.\n\n");
            }
            else
            {
                report.Insert(0,
                    $"Wire UI Screens complete.\n"
                    + $"  Created: {created}  |  Auto-wired fields: {wired}  |  Already present: {skipped}\n\n");
            }

            if (created > 0)
            {
                EditorSceneManager.MarkSceneDirty(
                    UnityEngine.SceneManagement.SceneManager.GetActiveScene());
            }

            EditorUtility.DisplayDialog("Wire UI Screens", report.ToString(), "OK");
            Debug.Log("[UIScreenAutoWirer]\n" + report);
        }

        // ── Discovery ──────────────────────────────────────────────────────────

        private sealed class ScreenEntry
        {
            public Type            ControllerType;
            public UIScreenAttribute Attribute;
        }

        private static List<ScreenEntry> FindScreenEntries()
        {
            var baseType = typeof(UIToolkitScreenController);
            var entries  = new List<ScreenEntry>();

            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try   { types = assembly.GetTypes(); }
                catch { continue; }

                foreach (var t in types)
                {
                    if (t.IsAbstract || !baseType.IsAssignableFrom(t) || t == baseType)
                        continue;

                    var attr = t.GetCustomAttribute<UIScreenAttribute>(false);
                    if (attr == null) continue;

                    entries.Add(new ScreenEntry { ControllerType = t, Attribute = attr });
                }
            }

            return entries;
        }

        // ── Scene queries ──────────────────────────────────────────────────────

        private static MonoBehaviour FindExistingInstance(Type controllerType)
        {
            var all = UnityEngine.Object.FindObjectsByType(
                controllerType, FindObjectsSortMode.None);
            return all.Length > 0 ? all[0] as MonoBehaviour : null;
        }

        // ── Object creation ────────────────────────────────────────────────────

        private static GameObject CreateScreenObject(ScreenEntry entry, PanelSettings panelSettings)
        {
            string name = entry.ControllerType.Name.Replace("Controller", string.Empty);
            var    go   = new GameObject(name);

            var doc             = go.AddComponent<UIDocument>();
            doc.panelSettings   = panelSettings;
            doc.sortingOrder    = 0; // UIToolkitScreenController.Start() applies the attribute value

            var uxml = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(entry.Attribute.UxmlPath);
            if (uxml != null)
                doc.visualTreeAsset = uxml;
            else
                Debug.LogWarning($"[UIScreenAutoWirer] UXML not found: {entry.Attribute.UxmlPath}");

            go.AddComponent(entry.ControllerType);

            Undo.RegisterCreatedObjectUndo(go, $"Auto-wire {entry.ControllerType.Name}");
            return go;
        }

        // ── Dependency wiring ──────────────────────────────────────────────────

        private static int AutoWireDependencies(
            MonoBehaviour controller, Type controllerType, StringBuilder report)
        {
            int count = 0;

            var fields = controllerType
                .GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
                .Where(f => f.GetCustomAttribute<SerializeField>() != null
                         && typeof(Component).IsAssignableFrom(f.FieldType));

            var so = new SerializedObject(controller);

            foreach (var field in fields)
            {
                var sp = so.FindProperty(field.Name);
                if (sp == null || sp.objectReferenceValue != null) continue;

                var found = UnityEngine.Object
                    .FindObjectsByType(field.FieldType, FindObjectsSortMode.None)
                    .FirstOrDefault() as Component;

                if (found == null) continue;

                sp.objectReferenceValue = found;
                count++;
                report.AppendLine($"       wired  {field.Name}  →  {found.gameObject.name}");
            }

            so.ApplyModifiedProperties();
            return count;
        }

        // ── Project utilities ──────────────────────────────────────────────────

        private static PanelSettings FindPanelSettings()
        {
            var guids = AssetDatabase.FindAssets("t:PanelSettings");
            if (guids.Length == 0) return null;

            // Prefer one explicitly named for the game UI
            foreach (var guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (path.Contains("_Project"))
                    return AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            }

            return AssetDatabase.LoadAssetAtPath<PanelSettings>(
                AssetDatabase.GUIDToAssetPath(guids[0]));
        }
    }
}
