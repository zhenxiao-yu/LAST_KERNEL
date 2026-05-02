#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class ItchMacBuild
    {
        private const string DefaultBuildPath = "Build/ItchMac";
        private const string AppName = "LastKernel.app";

        [MenuItem("LAST KERNEL/Build/Itch macOS Test Build")]
        public static void BuildFromMenu()
        {
            Build(DefaultBuildPath);
        }

        public static void Build()
        {
            Build(GetBuildPathFromCommandLine());
        }

        private static void Build(string buildPath)
        {
            string[] scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            if (scenes.Length == 0)
                throw new InvalidOperationException("No enabled scenes are configured in EditorBuildSettings.");

            buildPath = string.IsNullOrWhiteSpace(buildPath) ? DefaultBuildPath : buildPath;
            buildPath = Path.GetFullPath(buildPath);

            if (Directory.Exists(buildPath))
                Directory.Delete(buildPath, recursive: true);

            Directory.CreateDirectory(buildPath);

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.Standalone, BuildTarget.StandaloneOSX);
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = true;

            string appPath = Path.Combine(buildPath, AppName);

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = appPath,
                target = BuildTarget.StandaloneOSX,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.Development
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Itch macOS build failed: {summary.result} with {summary.totalErrors} errors.");
            }

            Debug.Log($"Itch macOS build complete: {appPath} ({summary.totalSize} bytes)");
        }

        private static string GetBuildPathFromCommandLine()
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (string.Equals(args[i], "-buildPath", StringComparison.OrdinalIgnoreCase))
                    return args[i + 1];
            }

            return DefaultBuildPath;
        }
    }
}
#endif
