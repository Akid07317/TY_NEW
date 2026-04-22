using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class SceneScriptReferenceHealthTests
    {
        private const string LegacySkyFogProfileAssetPath = "Assets/Settings/SkyandFogSettingsProfile.asset";
        private const string LegacySkyFogProfileGuid = "8ba92e2dd7f884a0f88b98fa2d235fe7";
        private static readonly string[] FormalSceneRoots =
        {
            "Assets/_Game/Scenes"
        };

        private static readonly Regex ScriptGuidPattern = new(
            @"m_Script: \{fileID: 11500000, guid: ([0-9a-f]{32}), type: 3\}",
            RegexOptions.Compiled);

        [Test]
        public void ProjectScenes_DoNotContainMissingScriptReferences()
        {
            string[] scenePaths = AssetDatabase.FindAssets("t:Scene", FormalSceneRoots)
                .Select(AssetDatabase.GUIDToAssetPath)
                .OrderBy(path => path)
                .ToArray();

            Assert.IsNotEmpty(scenePaths);

            List<string> issues = new();

            foreach (string scenePath in scenePaths)
            {
                string sceneText = File.ReadAllText(GetProjectPath(scenePath));

                foreach (Match match in ScriptGuidPattern.Matches(sceneText))
                {
                    string scriptGuid = match.Groups[1].Value;
                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(scriptGuid)))
                    {
                        issues.Add($"{scenePath}: missing script guid {scriptGuid}");
                    }
                }

                if (sceneText.Contains(LegacySkyFogProfileGuid))
                {
                    issues.Add($"{scenePath}: still references removed legacy sky/fog profile");
                }
            }

            Assert.IsEmpty(issues, string.Join("\n", issues));
        }

        [Test]
        public void LegacySkyFogProfileAsset_HasBeenRemoved()
        {
            Assert.IsEmpty(AssetDatabase.AssetPathToGUID(LegacySkyFogProfileAssetPath));
        }

        private static string GetProjectPath(string assetPath)
        {
            string projectRoot = Directory.GetParent(Application.dataPath).FullName;
            return Path.Combine(projectRoot, assetPath);
        }
    }
}
