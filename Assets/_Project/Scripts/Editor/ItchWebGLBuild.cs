#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Markyu.LastKernel
{
    public static class ItchWebGLBuild
    {
        private const string DefaultBuildPath = "Build/ItchWebGL";

        [MenuItem("LAST KERNEL/Build/Itch WebGL Test Build")]
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

            EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);
            EditorUserBuildSettings.development = true;
            EditorUserBuildSettings.connectProfiler = false;
            EditorUserBuildSettings.allowDebugging = true;

            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.dataCaching = true;

            BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = BuildOptions.Development
            });

            BuildSummary summary = report.summary;
            if (summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Itch WebGL build failed: {summary.result} with {summary.totalErrors} errors.");
            }

            Debug.Log($"Itch WebGL build complete: {buildPath} ({summary.totalSize} bytes)");
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
