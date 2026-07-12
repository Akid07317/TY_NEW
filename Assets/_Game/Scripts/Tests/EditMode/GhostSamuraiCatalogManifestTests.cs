using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class GhostSamuraiCatalogManifestTests
    {
        private const string GhostSamuraiPackagePath = "Assets/GhostSamurai_Animset";
        private const string ManifestRelativePath = "Tools/ghostsamurai/clip_mappings.json";

        [Test]
        public void CatalogManifest_ReferencedGhostSamuraiClipsResolveFromCurrentPackage()
        {
            if (!AssetDatabase.IsValidFolder(GhostSamuraiPackagePath))
            {
                Assert.Ignore("GhostSamurai package is not available in this workspace.");
            }

            ManifestDocument manifest = LoadManifest();
            Dictionary<string, List<string>> clipPathIndex = BuildGhostSamuraiClipPathIndex();
            HashSet<string> referencedClips = new HashSet<string>(
                manifest.try_first.SelectMany(group => group.clips)
                    .Concat(manifest.mapping_sections.SelectMany(section => section.entries).SelectMany(entry => entry.candidates)));
            referencedClips.UnionWith(manifest.execution_research.SelectMany(entry => entry.lead_candidates));

            foreach (string clipStem in referencedClips)
            {
                string clipPath = ResolveReferencedClipPath(clipPathIndex, clipStem);
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(clipPath), clipStem);
            }
        }

        [Test]
        public void CatalogManifest_CodeBackedResolversStayAlignedWithDocumentedGhostSamuraiOrder()
        {
            if (!AssetDatabase.IsValidFolder(GhostSamuraiPackagePath))
            {
                Assert.Ignore("GhostSamurai package is not available in this workspace.");
            }

            ManifestDocument manifest = LoadManifest();
            Dictionary<string, List<string>> clipPathIndex = BuildGhostSamuraiClipPathIndex();

            foreach (MappingSection section in manifest.mapping_sections)
            {
                foreach (MappingEntry entry in section.entries)
                {
                    if (string.IsNullOrEmpty(entry.resolver_owner) || string.IsNullOrEmpty(entry.resolver_method))
                    {
                        continue;
                    }

                    string[] candidatePaths = InvokeResolver(entry);
                    string context = section.title + "/" + entry.action;

                    Assert.IsNotNull(candidatePaths, context);
                    Assert.That(candidatePaths.Length, Is.GreaterThanOrEqualTo(entry.candidates.Length), context);

                    for (int i = 0; i < entry.candidates.Length; i++)
                    {
                        string expectedPath = ResolveReferencedClipPath(clipPathIndex, entry.candidates[i]);
                        Assert.AreEqual(expectedPath, candidatePaths[i], context + " candidate " + i);
                    }

                    if (entry.tracked_fallback_paths == null)
                    {
                        continue;
                    }

                    foreach (string fallbackPath in entry.tracked_fallback_paths)
                    {
                        Assert.That(candidatePaths, Has.Some.EqualTo(fallbackPath), context + " fallback");
                        Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(fallbackPath), fallbackPath);
                    }
                }
            }
        }

        [Test]
        public void CatalogManifest_ExecutionResearchRolesResolveDistinctGhostSamuraiFamilies()
        {
            if (!AssetDatabase.IsValidFolder(GhostSamuraiPackagePath))
            {
                Assert.Ignore("GhostSamurai package is not available in this workspace.");
            }

            ManifestDocument manifest = LoadManifest();
            Dictionary<string, List<string>> clipPathIndex = BuildGhostSamuraiClipPathIndex();
            string[] expectedActions =
            {
                "Execution_Attacker",
                "Executed_Victim",
                "Ambush_Attacker",
                "Ambushed_Victim"
            };

            CollectionAssert.AreEqual(
                expectedActions,
                manifest.execution_research.Select(entry => entry.action).ToArray());

            foreach (ExecutionResearchEntry entry in manifest.execution_research)
            {
                Regex regex = new Regex(entry.stem_regex);
                string context = entry.action + " / " + entry.role;
                bool matchedAny = false;

                foreach (string clipStem in entry.lead_candidates)
                {
                    string clipPath = ResolveReferencedClipPath(clipPathIndex, clipStem);
                    Assert.IsTrue(regex.IsMatch(clipStem), context + " lead clip pattern");
                    Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(clipPath), context + " asset");
                    matchedAny = true;
                }

                Assert.IsTrue(matchedAny, context + " lead candidates");
                Assert.That(
                    clipPathIndex.Keys.Any(regex.IsMatch),
                    Is.True,
                    context + " family coverage");
            }
        }

        private static ManifestDocument LoadManifest()
        {
            string manifestPath = Path.Combine(ProjectRootPath, ManifestRelativePath);
            string json = File.ReadAllText(manifestPath);
            ManifestDocument manifest = JsonUtility.FromJson<ManifestDocument>(json);

            Assert.IsNotNull(manifest, manifestPath);
            Assert.IsNotNull(manifest.try_first, manifestPath);
            Assert.IsNotNull(manifest.execution_research, manifestPath);
            Assert.IsNotNull(manifest.mapping_sections, manifestPath);
            return manifest;
        }

        private static string[] InvokeResolver(MappingEntry entry)
        {
            Type ownerType = entry.resolver_owner switch
            {
                "CombatTestAssetGenerator" => typeof(CombatTestAssetGenerator),
                "CombatImportedEnemyVisualUtility" => typeof(CombatImportedEnemyVisualUtility),
                _ => null
            };

            Assert.IsNotNull(ownerType, entry.resolver_owner);

            MethodInfo method = ownerType.GetMethod(entry.resolver_method, BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(method, entry.resolver_method);

            ParameterInfo[] parameters = method.GetParameters();
            object[] arguments = Array.Empty<object>();

            if (parameters.Length > 0)
            {
                Assert.IsNotNull(entry.resolver_arguments, entry.action);
                Assert.AreEqual(parameters.Length, entry.resolver_arguments.Length, entry.action);
                arguments = new object[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    arguments[i] = ConvertResolverArgument(parameters[i].ParameterType, entry.resolver_arguments[i]);
                }
            }

            return method.Invoke(null, arguments) as string[];
        }

        private static object ConvertResolverArgument(Type parameterType, string value)
        {
            if (parameterType == typeof(string))
            {
                return value;
            }

            if (parameterType.IsEnum)
            {
                return Enum.Parse(parameterType, value);
            }

            Assert.Fail("Unsupported resolver parameter type: " + parameterType.FullName);
            return null;
        }

        private static string ResolveReferencedClipPath(Dictionary<string, List<string>> clipPathIndex, string clipStem)
        {
            Assert.That(clipPathIndex.ContainsKey(clipStem), clipStem);
            List<string> paths = clipPathIndex[clipStem];

            Assert.That(paths, Has.Count.EqualTo(1), clipStem);
            return paths[0];
        }

        private static Dictionary<string, List<string>> BuildGhostSamuraiClipPathIndex()
        {
            string rootPath = Path.Combine(ProjectRootPath, GhostSamuraiPackagePath);
            Dictionary<string, List<string>> index = new Dictionary<string, List<string>>(StringComparer.Ordinal);

            foreach (string filePath in Directory.EnumerateFiles(rootPath, "*.FBX", SearchOption.AllDirectories))
            {
                string relativePath = Path.GetRelativePath(ProjectRootPath, filePath).Replace('\\', '/');
                string clipStem = Path.GetFileNameWithoutExtension(filePath);

                if (!index.TryGetValue(clipStem, out List<string> paths))
                {
                    paths = new List<string>();
                    index.Add(clipStem, paths);
                }

                paths.Add(relativePath);
            }

            return index;
        }

        private static string ProjectRootPath => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

        [Serializable]
        private sealed class ManifestDocument
        {
            public TryFirstGroup[] try_first;
            public ExecutionResearchEntry[] execution_research;
            public MappingSection[] mapping_sections;
        }

        [Serializable]
        private sealed class TryFirstGroup
        {
            public string category;
            public string[] clips;
        }

        [Serializable]
        private sealed class MappingSection
        {
            public string title;
            public MappingEntry[] entries;
        }

        [Serializable]
        private sealed class ExecutionResearchEntry
        {
            public string action;
            public string role;
            public string stem_regex;
            public string[] lead_candidates;
        }

        [Serializable]
        private sealed class MappingEntry
        {
            public string action;
            public string category;
            public string[] candidates;
            public string goal;
            public string resolver_owner;
            public string resolver_method;
            public string[] resolver_arguments;
            public string[] tracked_fallback_paths;
        }
    }
}
