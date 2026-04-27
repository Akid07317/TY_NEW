using System;
using System.IO;
using System.Linq;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ReleaseCandidatePreflightTests
    {
        private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        private const string PackageLockPath = "Packages/packages-lock.json";
        private const string PackageManifestPath = "Packages/manifest.json";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string StandaloneApplicationIdentifier = "com.don.tynew";

        private static readonly string[] RequiredReleaseScenePaths =
        {
            "Assets/_Game/Scenes/MainMenu.unity",
            "Assets/_Game/Scenes/Chapter01_Combined.unity",
            "Assets/_Game/Scenes/Bootstrap.unity",
            "Assets/_Game/Scenes/CombatTest.unity",
            "Assets/_Game/Scenes/BossTest.unity"
        };

        private static readonly string[] LocalPreviewOnlyAssetRoots =
        {
            "Assets/Kevin Iglesias",
            "Assets/DoubleL",
            "Assets/ithappy",
            "Assets/JC_LP_MedievalCharacters_LITE",
            "Assets/Free medieval weapons",
            "Assets/MYFG-Weapon Pack Lite",
            "Assets/Polytope Studio",
            "Assets/LocalPreviewTools",
            "Assets/_Game/Animations/Characters/CombatTest/LocalPreview"
        };

        [Test]
        public void PlayerSettings_UseProjectReleaseIdentity()
        {
            Assert.AreEqual("TY_NEW", PlayerSettings.productName);
            Assert.AreEqual("TY_NEW Team", PlayerSettings.companyName);
            Assert.AreEqual("0.1.0", PlayerSettings.bundleVersion);

            string settingsYaml = File.ReadAllText(ProjectSettingsPath);
            StringAssert.Contains($"Standalone: {StandaloneApplicationIdentifier}", settingsYaml);
            StringAssert.DoesNotContain("com.unity.template.hdrp-blank", settingsYaml);
            StringAssert.DoesNotContain("companyName: DefaultCompany", settingsYaml);
        }

        [Test]
        public void ProjectSettings_DoNotCarryHdrpTemplateOrPipelineResidue()
        {
            string projectSettingsYaml = File.ReadAllText(ProjectSettingsPath);
            string graphicsSettingsYaml = File.ReadAllText(GraphicsSettingsPath);
            string tagManagerYaml = File.ReadAllText(TagManagerPath);
            string packageManifestJson = File.ReadAllText(PackageManifestPath);
            string packageLockJson = File.ReadAllText(PackageLockPath);

            StringAssert.DoesNotContain("com.unity.template.hdrp-blank", projectSettingsYaml);
            StringAssert.DoesNotContain("templateDefaultScene: Assets/OutdoorsScene.unity", projectSettingsYaml);
            StringAssert.Contains("m_CustomRenderPipeline: {fileID: 0}", graphicsSettingsYaml);
            StringAssert.DoesNotContain("HDRenderPipeline", graphicsSettingsYaml);
            StringAssert.DoesNotContain("UnityEngine.Rendering.HighDefinition.HDRenderPipeline", tagManagerYaml);
            StringAssert.DoesNotContain("com.unity.render-pipelines.high-definition", packageManifestJson);
            StringAssert.DoesNotContain("com.unity.render-pipelines.high-definition", packageLockJson);
        }

        [Test]
        public void BuildSettings_ReleaseScenesExistAndGuidsMatchProjectAssets()
        {
            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .ToArray();

            string[] enabledScenePaths = enabledScenes.Select(scene => scene.path).ToArray();

            for (int i = 0; i < RequiredReleaseScenePaths.Length; i++)
            {
                string scenePath = RequiredReleaseScenePaths[i];
                CollectionAssert.Contains(enabledScenePaths, scenePath);
                Assert.IsTrue(File.Exists(scenePath), scenePath);

                EditorBuildSettingsScene buildScene = enabledScenes.First(scene => scene.path == scenePath);
                string assetGuid = AssetDatabase.AssetPathToGUID(scenePath);

                Assert.IsFalse(string.IsNullOrEmpty(assetGuid), scenePath);
                Assert.AreEqual(assetGuid, buildScene.guid.ToString(), scenePath);
            }
        }

        [Test]
        public void ReleaseScenes_DoNotDependOnLocalPreviewOnlyAssetRoots()
        {
            string[] activeLocalPreviewRoots = LocalPreviewOnlyAssetRoots
                .Where(AssetDatabase.IsValidFolder)
                .ToArray();

            foreach (string scenePath in RequiredReleaseScenePaths)
            {
                string[] dependencies = AssetDatabase.GetDependencies(scenePath, true);

                foreach (string dependency in dependencies)
                {
                    string matchingRoot = activeLocalPreviewRoots.FirstOrDefault(root => IsUnderRoot(dependency, root));
                    Assert.IsNull(matchingRoot, $"{scenePath} depends on local-preview-only asset {dependency} under {matchingRoot}");
                }
            }
        }

        private static bool IsUnderRoot(string assetPath, string rootPath)
        {
            return assetPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        [Test]
        public void ReleaseCandidateBuildOptions_TargetIgnoredOutputRoot_ForMacAndWindows()
        {
            BuildPlayerOptions macOptions = ReleaseCandidateBuildUtility.CreateBuildOptions(BuildTarget.StandaloneOSX);
            BuildPlayerOptions windowsOptions = ReleaseCandidateBuildUtility.CreateBuildOptions(BuildTarget.StandaloneWindows64);

            CollectionAssert.AreEqual(RequiredReleaseScenePaths, macOptions.scenes);
            CollectionAssert.AreEqual(RequiredReleaseScenePaths, windowsOptions.scenes);
            Assert.AreEqual(BuildTarget.StandaloneOSX, macOptions.target);
            Assert.AreEqual(BuildTarget.StandaloneWindows64, windowsOptions.target);
            Assert.AreEqual(BuildOptions.None, macOptions.options);
            Assert.AreEqual(BuildOptions.None, windowsOptions.options);
            Assert.AreEqual(ReleaseCandidateBuildUtility.MacOutputPath, NormalizePath(macOptions.locationPathName));
            Assert.AreEqual(ReleaseCandidateBuildUtility.WindowsOutputPath, NormalizePath(windowsOptions.locationPathName));
            Assert.IsTrue(NormalizePath(macOptions.locationPathName).StartsWith(ReleaseCandidateBuildUtility.ReleaseBuildRoot + "/", StringComparison.Ordinal));
            Assert.IsTrue(NormalizePath(windowsOptions.locationPathName).StartsWith(ReleaseCandidateBuildUtility.ReleaseBuildRoot + "/", StringComparison.Ordinal));
        }

        private static string NormalizePath(string path)
        {
            return path.Replace('\\', '/');
        }
    }
}
