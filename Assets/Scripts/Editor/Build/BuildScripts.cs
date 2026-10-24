using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Game.Editor.Build
{
    /// <summary>
    /// Batch-mode player builds (Docs/10): <c>unity build . --target iOS --execute-method
    /// Game.Editor.Build.BuildScripts.Ios -o Builds/iOS</c>. Builds the enabled scenes of the build
    /// settings; exits non-zero on failure so CI and scripts can tell.
    /// </summary>
    public static class BuildScripts
    {
        public static void Ios()
        {
            Build(BuildTarget.iOS, "Builds/iOS");
        }

        public static void Android()
        {
            Build(BuildTarget.Android, "Builds/Android/WheelShooter.apk");
        }

        private static void Build(BuildTarget target, string fallbackOutput)
        {
            var output = Argument("-buildOutput") ?? fallbackOutput;
            var options = new BuildPlayerOptions
            {
                scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
                locationPathName = output,
                target = target,
                targetGroup = BuildPipeline.GetBuildTargetGroup(target),
                options = Argument("-development") != null ? BuildOptions.Development : BuildOptions.None,
            };

            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;
            Debug.Log($"[Build] {target} {summary.result} in {summary.totalTime} → {output} " +
                      $"({summary.totalSize / (1024 * 1024)} MB, {summary.totalErrors} errors)");

            if (Application.isBatchMode)
            {
                EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
            }
        }

        private static string Argument(string name)
        {
            var args = Environment.GetCommandLineArgs();
            var index = Array.IndexOf(args, name);
            if (index < 0)
            {
                return null;
            }

            return index + 1 < args.Length && !args[index + 1].StartsWith("-") ? args[index + 1] : string.Empty;
        }
    }
}
