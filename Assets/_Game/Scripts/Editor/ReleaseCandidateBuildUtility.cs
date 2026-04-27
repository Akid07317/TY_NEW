using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CampusRPG.Editor
{
    public static class ReleaseCandidateBuildUtility
    {
        public const string ReleaseBuildRoot = "Builds/ReleaseCandidate";
        public const string MacOutputPath = ReleaseBuildRoot + "/Mac/TY_NEW.app";
        public const string WindowsOutputPath = ReleaseBuildRoot + "/Windows/TY_NEW.exe";

        [MenuItem("CampusRPG/Build/Validate Release Candidate Build Inputs")]
        public static void ValidateReleaseCandidateBuildInputs()
        {
            ValidateBuildInputs(BuildTarget.StandaloneOSX);
            ValidateBuildInputs(BuildTarget.StandaloneWindows64);
            Debug.Log("Release candidate build inputs are valid for macOS and Windows.");
        }

        [MenuItem("CampusRPG/Build/Build macOS Release Candidate")]
        public static void BuildMacOSReleaseCandidate()
        {
            Build(BuildTarget.StandaloneOSX);
        }

        [MenuItem("CampusRPG/Build/Build Windows Release Candidate")]
        public static void BuildWindowsReleaseCandidate()
        {
            Build(BuildTarget.StandaloneWindows64);
        }

        public static BuildPlayerOptions CreateBuildOptions(BuildTarget target)
        {
            ValidateBuildInputs(target);

            return new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = GetOutputPath(target),
                target = target,
                options = BuildOptions.None
            };
        }

        public static BuildReport Build(BuildTarget target)
        {
            BuildPlayerOptions options = CreateBuildOptions(target);
            string outputDirectory = Path.GetDirectoryName(options.locationPathName);

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Release candidate build failed for {target}: {report.summary.result}");
            }

            return report;
        }

        public static string[] GetEnabledScenePaths()
        {
            return EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .Select(scene => scene.path)
                .ToArray();
        }

        public static string GetOutputPath(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneOSX => MacOutputPath,
                BuildTarget.StandaloneWindows64 => WindowsOutputPath,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Release candidates only support macOS and Windows.")
            };
        }

        public static void ValidateBuildInputs(BuildTarget target)
        {
            _ = GetOutputPath(target);

            string[] enabledScenePaths = GetEnabledScenePaths();

            if (enabledScenePaths.Length == 0)
            {
                throw new InvalidOperationException("Release candidate build requires at least one enabled scene.");
            }

            foreach (string scenePath in enabledScenePaths)
            {
                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    throw new InvalidOperationException("Release candidate build contains an empty scene path.");
                }

                if (!File.Exists(scenePath))
                {
                    throw new FileNotFoundException($"Release candidate scene does not exist: {scenePath}", scenePath);
                }
            }
        }
    }
}
