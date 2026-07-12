using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CampusRPG.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace CampusRPG.Editor
{
    public enum ReleaseCandidateArtProfile
    {
        PublicSafe = 0,
        UserOwnedGhostSamurai = 1
    }

    public static class ReleaseCandidateBuildUtility
    {
        private const string GraphicsSettingsPath = "ProjectSettings/GraphicsSettings.asset";
        private const string PackageLockPath = "Packages/packages-lock.json";
        private const string PackageManifestPath = "Packages/manifest.json";
        private const string ProjectSettingsPath = "ProjectSettings/ProjectSettings.asset";
        private const string TagManagerPath = "ProjectSettings/TagManager.asset";
        private const string StandaloneApplicationIdentifier = "com.don.tynew";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string EnemyMeleePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab";
        private const string EnemyMobilePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab";
        private const string EnemyRangedPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab";
        private const string GhostSamuraiAssetRoot = "Assets/GhostSamurai_Animset";
        private const string GhostSamuraiAnimationMetadataPath = GhostSamuraiAssetRoot + "/Animation.meta";
        private const string GhostSamuraiModelMetadataPath = GhostSamuraiAssetRoot + "/Model.meta";

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
            "Assets/GhostSamurai_Animset",
            "Assets/MYFG-Weapon Pack Lite",
            "Assets/Polytope Studio",
            "Assets/LocalPreviewTools",
            "Assets/_Game/Animations/Characters/CombatTest/LocalPreview"
        };

        private static readonly string[] UserOwnedGhostSamuraiAllowedAssetRoots =
        {
            GhostSamuraiAssetRoot,
            "Assets/_Game/Animations/Characters/CombatTest/LocalPreview"
        };

        private static readonly string[] UserOwnedGhostSamuraiRequiredCoreStateNames =
        {
            "Block",
            "Dodge",
            "CombatRoll",
            "AirDodge",
            "Hit",
            "GuardBreak",
            "Death",
            "Light_01",
            "Light_02",
            "Light_03",
            "Heavy_01"
        };

        private const string UserOwnedGhostSamuraiLocomotionStateName = "Locomotion";
        private const string CombatProxyVisualRootName = "CombatProxyVisualRoot";
        private const int UserOwnedGhostSamuraiLocomotionMotionCount = 13;

        private static readonly (string PrefabPath, CombatProxyVisualKind Kind, string RequiredAttackState)[]
            UserOwnedGhostSamuraiEnemyProfiles =
            {
                (EnemyMeleePrefabPath, CombatProxyVisualKind.EnemyMelee, EnemyCombatAnimationPlanUtility.MeleeAttackStateName),
                (EnemyMobilePrefabPath, CombatProxyVisualKind.EnemyMobile, EnemyCombatAnimationPlanUtility.MobileAttackStateName),
                (EnemyRangedPrefabPath, CombatProxyVisualKind.EnemyRanged, EnemyCombatAnimationPlanUtility.RangedAttackStateName)
            };

        public const string ReleaseBuildRoot = "Builds/ReleaseCandidate";
        public const string MacOutputPath = ReleaseBuildRoot + "/Mac/TY_NEW.app";
        public const string WindowsOutputPath = ReleaseBuildRoot + "/Windows/TY_NEW.exe";
        public const string UserOwnedArtBuildRoot = ReleaseBuildRoot + "/UserOwnedArt";
        public const string UserOwnedArtMacOutputPath = UserOwnedArtBuildRoot + "/Mac/TY_NEW.app";
        public const string UserOwnedArtWindowsOutputPath = UserOwnedArtBuildRoot + "/Windows/TY_NEW.exe";

        [MenuItem("CampusRPG/Build/Validate Release Candidate Build Inputs")]
        public static void ValidateReleaseCandidateBuildInputs()
        {
            ValidateBuildInputs(BuildTarget.StandaloneOSX);
            ValidateBuildInputs(BuildTarget.StandaloneWindows64);
            ValidateBuildTargetSupport(BuildTarget.StandaloneOSX);
            ValidateBuildTargetSupport(BuildTarget.StandaloneWindows64);
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

        [MenuItem("CampusRPG/Build/Build macOS User-Owned Art Candidate")]
        public static void BuildMacOSUserOwnedArtCandidate()
        {
            BuildUserOwnedArtCandidate(BuildTarget.StandaloneOSX);
        }

        [MenuItem("CampusRPG/Build/Build Windows User-Owned Art Candidate")]
        public static void BuildWindowsUserOwnedArtCandidate()
        {
            BuildUserOwnedArtCandidate(BuildTarget.StandaloneWindows64);
        }

        [MenuItem("CampusRPG/Build/Validate User-Owned Art Candidate")]
        public static void ValidateUserOwnedArtCandidate()
        {
            ImportedPlayerSourceProfile previousSourceProfile = CombatImportedPlayerVisualUtility.SourceProfile;
            ImportedEnemySourceProfile previousEnemySourceProfile = CombatImportedEnemyVisualUtility.SourceProfile;
            bool previousImportedSourcePreference = CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview;

            try
            {
                PrepareUserOwnedGhostSamuraiArt();
                Debug.Log(
                    "User-owned GhostSamurai art candidate is technically ready: player and three enemy archetypes have validated models, Humanoid Avatars, palettes, real weapons, controllers, and core attack motion graphs. Action feel still requires owner acceptance.");
            }
            finally
            {
                RestorePublicSafeArtBaseline(
                    previousSourceProfile,
                    previousEnemySourceProfile,
                    previousImportedSourcePreference);
            }
        }

        public static BuildPlayerOptions CreateBuildOptions(BuildTarget target)
        {
            return CreateBuildOptions(target, ReleaseCandidateArtProfile.PublicSafe);
        }

        public static BuildPlayerOptions CreateBuildOptions(BuildTarget target, ReleaseCandidateArtProfile artProfile)
        {
            ValidateBuildInputs(target, artProfile);

            return new BuildPlayerOptions
            {
                scenes = GetEnabledScenePaths(),
                locationPathName = GetOutputPath(target, artProfile),
                target = target,
                subtarget = (int)StandaloneBuildSubtarget.Player,
                options = BuildOptions.None
            };
        }

        public static BuildReport Build(BuildTarget target)
        {
            return Build(target, ReleaseCandidateArtProfile.PublicSafe);
        }

        public static BuildReport Build(BuildTarget target, ReleaseCandidateArtProfile artProfile)
        {
            ValidateBuildTargetSupport(target);
            BuildPlayerOptions options = CreateBuildOptions(target, artProfile);
            string outputDirectory = Path.GetDirectoryName(options.locationPathName);

            if (!string.IsNullOrEmpty(outputDirectory))
            {
                Directory.CreateDirectory(outputDirectory);
            }

            BuildReport report = UnityEditor.BuildPipeline.BuildPlayer(options);

            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException(
                    $"Release candidate build failed for {target} ({artProfile}): {report.summary.result}");
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
            return GetOutputPath(target, ReleaseCandidateArtProfile.PublicSafe);
        }

        public static string GetOutputPath(BuildTarget target, ReleaseCandidateArtProfile artProfile)
        {
            if (artProfile == ReleaseCandidateArtProfile.UserOwnedGhostSamurai)
            {
                return target switch
                {
                    BuildTarget.StandaloneOSX => UserOwnedArtMacOutputPath,
                    BuildTarget.StandaloneWindows64 => UserOwnedArtWindowsOutputPath,
                    _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Release candidates only support macOS and Windows.")
                };
            }

            return target switch
            {
                BuildTarget.StandaloneOSX => MacOutputPath,
                BuildTarget.StandaloneWindows64 => WindowsOutputPath,
                _ => throw new ArgumentOutOfRangeException(nameof(target), target, "Release candidates only support macOS and Windows.")
            };
        }

        public static void ValidateBuildInputs(BuildTarget target)
        {
            ValidateBuildInputs(target, ReleaseCandidateArtProfile.PublicSafe);
        }

        public static void ValidateBuildInputs(BuildTarget target, ReleaseCandidateArtProfile artProfile)
        {
            _ = GetOutputPath(target, artProfile);
            ValidateProjectIdentityAndPipeline();

            EditorBuildSettingsScene[] enabledScenes = EditorBuildSettings.scenes
                .Where(scene => scene != null && scene.enabled)
                .ToArray();
            string[] enabledScenePaths = enabledScenes
                .Select(scene => scene.path)
                .ToArray();

            if (!enabledScenePaths.SequenceEqual(RequiredReleaseScenePaths, StringComparer.Ordinal))
            {
                throw new InvalidOperationException(
                    "Release candidate enabled scenes must exactly match the required order: " +
                    string.Join(", ", RequiredReleaseScenePaths));
            }

            for (int index = 0; index < enabledScenes.Length; index++)
            {
                EditorBuildSettingsScene buildScene = enabledScenes[index];
                string scenePath = buildScene.path;

                if (string.IsNullOrWhiteSpace(scenePath))
                {
                    throw new InvalidOperationException("Release candidate build contains an empty scene path.");
                }

                if (!File.Exists(scenePath))
                {
                    throw new FileNotFoundException($"Release candidate scene does not exist: {scenePath}", scenePath);
                }

                string assetGuid = AssetDatabase.AssetPathToGUID(scenePath);

                if (string.IsNullOrEmpty(assetGuid)
                    || !string.Equals(assetGuid, buildScene.guid.ToString(), StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Release candidate scene GUID does not match the project asset: {scenePath}");
                }

                ValidateSceneHasNoDisallowedArtDependencies(scenePath, artProfile);
            }
        }

        public static bool IsSceneDependencyAllowedForArtProfile(
            string dependency,
            ReleaseCandidateArtProfile artProfile)
        {
            string matchingRoot = LocalPreviewOnlyAssetRoots.FirstOrDefault(
                root => IsUnderRoot(dependency, root));

            if (string.IsNullOrEmpty(matchingRoot))
            {
                return true;
            }

            return artProfile == ReleaseCandidateArtProfile.UserOwnedGhostSamurai
                && UserOwnedGhostSamuraiAllowedAssetRoots.Any(root => IsUnderRoot(dependency, root));
        }

        private static void ValidateProjectIdentityAndPipeline()
        {
            if (!string.Equals(PlayerSettings.productName, "TY_NEW", StringComparison.Ordinal)
                || !string.Equals(PlayerSettings.companyName, "TY_NEW Team", StringComparison.Ordinal)
                || !string.Equals(PlayerSettings.bundleVersion, "0.1.0", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Release candidate project identity must be TY_NEW / TY_NEW Team / 0.1.0.");
            }

            string projectSettings = ReadRequiredTextFile(ProjectSettingsPath);
            string graphicsSettings = ReadRequiredTextFile(GraphicsSettingsPath);
            string tagManager = ReadRequiredTextFile(TagManagerPath);
            string packageManifest = ReadRequiredTextFile(PackageManifestPath);
            string packageLock = ReadRequiredTextFile(PackageLockPath);

            if (!projectSettings.Contains($"Standalone: {StandaloneApplicationIdentifier}", StringComparison.Ordinal)
                || projectSettings.Contains("com.unity.template.hdrp-blank", StringComparison.Ordinal)
                || projectSettings.Contains("companyName: DefaultCompany", StringComparison.Ordinal)
                || !graphicsSettings.Contains("m_CustomRenderPipeline: {fileID: 0}", StringComparison.Ordinal)
                || graphicsSettings.Contains("HDRenderPipeline", StringComparison.Ordinal)
                || tagManager.Contains("UnityEngine.Rendering.HighDefinition.HDRenderPipeline", StringComparison.Ordinal)
                || packageManifest.Contains("com.unity.render-pipelines.high-definition", StringComparison.Ordinal)
                || packageLock.Contains("com.unity.render-pipelines.high-definition", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Release candidate project identity or Built-in render-pipeline baseline is invalid.");
            }
        }

        private static void ValidateSceneHasNoDisallowedArtDependencies(
            string scenePath,
            ReleaseCandidateArtProfile artProfile)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(scenePath, true))
            {
                if (!IsSceneDependencyAllowedForArtProfile(dependency, artProfile))
                {
                    throw new InvalidOperationException(
                        $"Release candidate scene {scenePath} depends on an asset that is not allowed " +
                        $"for the {artProfile} profile: {dependency}.");
                }
            }
        }

        private static BuildReport BuildUserOwnedArtCandidate(BuildTarget target)
        {
            ImportedPlayerSourceProfile previousSourceProfile = CombatImportedPlayerVisualUtility.SourceProfile;
            ImportedEnemySourceProfile previousEnemySourceProfile = CombatImportedEnemyVisualUtility.SourceProfile;
            bool previousImportedSourcePreference = CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview;

            try
            {
                PrepareUserOwnedGhostSamuraiArt();
                return Build(target, ReleaseCandidateArtProfile.UserOwnedGhostSamurai);
            }
            finally
            {
                RestorePublicSafeArtBaseline(
                    previousSourceProfile,
                    previousEnemySourceProfile,
                    previousImportedSourcePreference);
            }
        }

        private static void PrepareUserOwnedGhostSamuraiArt()
        {
            ValidateUserOwnedGhostSamuraiSourceProvenance();
            CombatImportedPlayerVisualUtility.SourceProfile = ImportedPlayerSourceProfile.UserOwnedGhostSamurai;
            CombatImportedEnemyVisualUtility.SourceProfile = ImportedEnemySourceProfile.UserOwnedGhostSamurai;
            CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview = true;
            CombatTestSceneBuilder.ApplyImportedVisualsToCombatTestPlayerPrefab();
            CombatTestSceneBuilder.ApplyImportedEnemyAvatarChainToCombatTestEnemyPrefabs();
            ValidatePreparedUserOwnedGhostSamuraiPlayer();
            ValidatePreparedUserOwnedGhostSamuraiEnemies();
        }

        private static void RestorePublicSafeArtBaseline(
            ImportedPlayerSourceProfile previousSourceProfile,
            ImportedEnemySourceProfile previousEnemySourceProfile,
            bool previousImportedSourcePreference)
        {
            try
            {
                CombatTestSceneBuilder.RepairCombatTestPrefabWiring();
            }
            finally
            {
                CombatImportedPlayerVisualUtility.SourceProfile = previousSourceProfile;
                CombatImportedEnemyVisualUtility.SourceProfile = previousEnemySourceProfile;
                CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview = previousImportedSourcePreference;
            }
        }

        private static void ValidateUserOwnedGhostSamuraiSourceProvenance()
        {
            if (!File.Exists(CombatImportedPlayerVisualUtility.GhostSamuraiPlayerModelPath)
                || !File.Exists(CombatImportedPlayerVisualUtility.GhostSamuraiPlayerWeaponPath)
                || !File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiEnemyRangedModelPath)
                || !File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiArrowWeaponPath))
            {
                throw new FileNotFoundException(
                    "The user-owned GhostSamurai art profile requires its player/enemy models, katana, integrated bow, and arrow source assets.");
            }

            string animationMetadata = ReadRequiredTextFile(GhostSamuraiAnimationMetadataPath);
            string modelMetadata = ReadRequiredTextFile(GhostSamuraiModelMetadataPath);

            if (!animationMetadata.Contains("licenseType: Store", StringComparison.Ordinal)
                || !modelMetadata.Contains("licenseType: Store", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "GhostSamurai source metadata is missing the expected Unity Store provenance marker.");
            }
        }

        private static void ValidatePreparedUserOwnedGhostSamuraiPlayer()
        {
            string selectedVisualPath = CombatImportedPlayerVisualUtility.GetSelectedPlayerVisualPrefabPath();
            string selectedWeaponPath = CombatImportedPlayerVisualUtility.GetSelectedPlayerWeaponPrefabPath();

            if (!string.Equals(
                    selectedVisualPath,
                    CombatImportedPlayerVisualUtility.GhostSamuraiPlayerModelPath,
                    StringComparison.Ordinal)
                || !string.Equals(
                    selectedWeaponPath,
                    CombatImportedPlayerVisualUtility.GhostSamuraiPlayerWeaponPath,
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate must use the GhostSamurai player model and katana as one source profile.");
            }

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            Animator animator = playerPrefab != null ? playerPrefab.GetComponent<Animator>() : null;

            if (playerPrefab == null
                || playerPrefab.transform.Find("ImportedVisualRoot") == null
                || animator == null
                || animator.avatar == null
                || !animator.avatar.isValid
                || !animator.avatar.isHuman
                || animator.runtimeAnimatorController == null)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate player prefab is missing its imported visual, Humanoid Avatar, or Animator Controller.");
            }

            bool hasGhostSamuraiDependency = AssetDatabase.GetDependencies(PlayerPrefabPath, true)
                .Any(path => IsUnderRoot(path, GhostSamuraiAssetRoot));

            if (!hasGhostSamuraiDependency)
            {
                throw new InvalidOperationException(
                    "The prepared player prefab does not depend on the selected GhostSamurai source profile.");
            }

            if (!CombatImportedPlayerVisualUtility.HasUserOwnedGhostSamuraiWeaponGripContract(playerPrefab))
            {
                throw new InvalidOperationException(
                    "The user-owned GhostSamurai katana must use the source-authored Weapon_r socket " +
                    "with identity local position, rotation, and scale.");
            }

            ValidateUserOwnedGhostSamuraiMaterials(playerPrefab);
            ValidateUserOwnedGhostSamuraiCoreMotionGraph(animator.runtimeAnimatorController);
        }

        private static void ValidatePreparedUserOwnedGhostSamuraiEnemies()
        {
            for (int profileIndex = 0; profileIndex < UserOwnedGhostSamuraiEnemyProfiles.Length; profileIndex++)
            {
                (string prefabPath, CombatProxyVisualKind kind, string requiredAttackState) =
                    UserOwnedGhostSamuraiEnemyProfiles[profileIndex];
                string expectedVisualPath = kind == CombatProxyVisualKind.EnemyRanged
                    ? CombatImportedEnemyVisualUtility.GhostSamuraiEnemyRangedModelPath
                    : CombatImportedEnemyVisualUtility.GhostSamuraiEnemyMeleeModelPath;
                string selectedVisualPath = CombatImportedEnemyVisualUtility.GetSelectedHumanoidVisualPrefabPath(kind);

                if (!string.Equals(selectedVisualPath, expectedVisualPath, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} must use its strict GhostSamurai visual source. " +
                        $"Selected: {selectedVisualPath ?? "<null>"}.");
                }

                GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                Transform importedRoot = enemyPrefab != null
                    ? enemyPrefab.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName)
                    : null;
                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemyPrefab);
                Animator rootAnimator = enemyPrefab != null ? enemyPrefab.GetComponent<Animator>() : null;
                string importedAvatarPath = importedAnimator != null && importedAnimator.avatar != null
                    ? AssetDatabase.GetAssetPath(importedAnimator.avatar)
                    : string.Empty;
                string importedControllerPath = importedAnimator != null
                    && importedAnimator.runtimeAnimatorController != null
                        ? AssetDatabase.GetAssetPath(importedAnimator.runtimeAnimatorController)
                        : string.Empty;
                string expectedControllerPath =
                    CombatImportedEnemyVisualUtility.GetImportedAvatarPreviewControllerPath(kind);
                EnemyCombatAnimationRelay animationRelay = enemyPrefab != null
                    ? enemyPrefab.GetComponent<EnemyCombatAnimationRelay>()
                    : null;

                if (enemyPrefab == null
                    || importedRoot == null
                    || importedAnimator == null
                    || importedAnimator.avatar == null
                    || !importedAnimator.avatar.isValid
                    || !importedAnimator.avatar.isHuman
                    || !string.Equals(importedAvatarPath, expectedVisualPath, StringComparison.Ordinal)
                    || !string.Equals(importedControllerPath, expectedControllerPath, StringComparison.Ordinal)
                    || (rootAnimator != null
                        && (rootAnimator.enabled
                            || rootAnimator.avatar != null
                            || rootAnimator.runtimeAnimatorController != null))
                    || animationRelay == null)
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} prefab is missing its imported Humanoid chain, " +
                        "controller, or combat animation relay, or it still has an active root Animator.");
                }

                Transform proxyRoot = enemyPrefab.transform.Find(CombatProxyVisualRootName);
                Renderer[] proxyRenderers = proxyRoot != null
                    ? proxyRoot.GetComponentsInChildren<Renderer>(true)
                    : Array.Empty<Renderer>();

                if (proxyRenderers.Any(renderer => renderer.enabled))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} still renders its proxy body or proxy weapon.");
                }

                if (!CombatImportedEnemyVisualUtility.HasUserOwnedGhostSamuraiEnemyWeaponContract(enemyPrefab, kind))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} is missing its source-authored Katana/Bow/Arrow weapon chain.");
                }

                if (!CombatImportedEnemyVisualUtility.HasUserOwnedGhostSamuraiEnemyMaterialContract(enemyPrefab, kind))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} has unresolved body or weapon palette materials.");
                }

                string[] dependencies = AssetDatabase.GetDependencies(prefabPath, true);

                if (!dependencies.Any(path => IsUnderRoot(path, GhostSamuraiAssetRoot))
                    || dependencies.Any(path => !IsSceneDependencyAllowedForArtProfile(
                        path,
                        ReleaseCandidateArtProfile.UserOwnedGhostSamurai)))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} dependency graph is not isolated to the strict GhostSamurai profile.");
                }

                ValidateUserOwnedGhostSamuraiEnemyMotionGraph(
                    importedAnimator.runtimeAnimatorController,
                    kind,
                    requiredAttackState);
            }
        }

        private static void ValidateUserOwnedGhostSamuraiMaterials(GameObject playerPrefab)
        {
            Transform importedVisualRoot = playerPrefab != null
                ? playerPrefab.transform.Find("ImportedVisualRoot")
                : null;
            Renderer[] renderers = importedVisualRoot != null
                ? importedVisualRoot.GetComponentsInChildren<Renderer>(true)
                : Array.Empty<Renderer>();

            if (renderers.Length == 0)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate imported visual has no renderers.");
            }

            HashSet<string> paletteMaterialPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;

                if (materials.Length == 0)
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate renderer {renderers[rendererIndex].name} has no materials.");
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];

                    if (material == null
                        || material.shader == null
                        || !material.shader.isSupported
                        || !CombatImportedPlayerVisualUtility.IsUserOwnedGhostSamuraiPaletteMaterial(material))
                    {
                        throw new InvalidOperationException(
                            $"The user-owned art candidate renderer {renderers[rendererIndex].name} " +
                            $"has an unresolved or unsupported material at slot {materialIndex}.");
                    }

                    paletteMaterialPaths.Add(AssetDatabase.GetAssetPath(material));
                }
            }

            if (paletteMaterialPaths.Count != CombatImportedPlayerVisualUtility.UserOwnedGhostSamuraiExpectedPaletteMaterialCount)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate must resolve all three body and four katana material roles " +
                    $"to its deterministic palette. Found {paletteMaterialPaths.Count} unique palette materials.");
            }
        }

        private static void ValidateUserOwnedGhostSamuraiCoreMotionGraph(RuntimeAnimatorController runtimeController)
        {
            if (runtimeController is not AnimatorController controller)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate requires an editable AnimatorController for motion preflight.");
            }

            Dictionary<string, AnimatorState> states = EnumerateAnimatorStates(controller)
                .GroupBy(state => state.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            if (!states.TryGetValue(UserOwnedGhostSamuraiLocomotionStateName, out AnimatorState locomotionState)
                || locomotionState.motion is not BlendTree locomotionTree)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate is missing its Locomotion BlendTree.");
            }

            List<AnimationClip> locomotionClips = new List<AnimationClip>();
            CollectAnimationClips(locomotionTree, locomotionClips);

            if (locomotionClips.Count != UserOwnedGhostSamuraiLocomotionMotionCount
                || locomotionClips.Select(AssetDatabase.GetAssetPath).Distinct(StringComparer.Ordinal).Count()
                    != UserOwnedGhostSamuraiLocomotionMotionCount)
            {
                throw new InvalidOperationException(
                    "The user-owned art candidate Locomotion BlendTree must contain 13 distinct Idle/Walk/Run motions.");
            }

            ValidateUserOwnedGhostSamuraiMotion(locomotionTree, UserOwnedGhostSamuraiLocomotionStateName);

            for (int stateIndex = 0; stateIndex < UserOwnedGhostSamuraiRequiredCoreStateNames.Length; stateIndex++)
            {
                string stateName = UserOwnedGhostSamuraiRequiredCoreStateNames[stateIndex];

                if (!states.TryGetValue(stateName, out AnimatorState state) || state.motion == null)
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate is missing required Animator state or motion: {stateName}.");
                }

                ValidateUserOwnedGhostSamuraiMotion(state.motion, stateName);
            }
        }

        private static void ValidateUserOwnedGhostSamuraiEnemyMotionGraph(
            RuntimeAnimatorController runtimeController,
            CombatProxyVisualKind kind,
            string requiredAttackState)
        {
            if (runtimeController is not AnimatorController controller)
            {
                throw new InvalidOperationException(
                    $"The user-owned art candidate {kind} requires an editable AnimatorController.");
            }

            Dictionary<string, AnimatorState> states = EnumerateAnimatorStates(controller)
                .GroupBy(state => state.name, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
            string[] requiredStateNames =
            {
                EnemyCombatAnimationPlanUtility.LocomotionStateName,
                EnemyCombatAnimationPlanUtility.HitStateName,
                EnemyCombatAnimationPlanUtility.DeathStateName,
                requiredAttackState,
                EnemyCombatAnimationPlanUtility.AntiAirAttackStateName,
                EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName,
                EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName
            };

            for (int stateIndex = 0; stateIndex < requiredStateNames.Length; stateIndex++)
            {
                string stateName = requiredStateNames[stateIndex];

                if (!states.TryGetValue(stateName, out AnimatorState state) || state.motion == null)
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} is missing required enemy state or motion: {stateName}.");
                }
            }

            if (states[EnemyCombatAnimationPlanUtility.LocomotionStateName].motion is not BlendTree locomotionTree)
            {
                throw new InvalidOperationException(
                    $"The user-owned art candidate {kind} is missing its enemy Locomotion BlendTree.");
            }

            List<AnimationClip> locomotionClips = new List<AnimationClip>();
            CollectAnimationClips(locomotionTree, locomotionClips);

            if (locomotionClips.Count != 3
                || locomotionClips.Select(AssetDatabase.GetAssetPath).Distinct(StringComparer.Ordinal).Count() != 3)
            {
                throw new InvalidOperationException(
                    $"The user-owned art candidate {kind} enemy Locomotion must contain three distinct GhostSamurai motions.");
            }

            foreach (AnimatorState state in states.Values)
            {
                if (state.motion != null)
                {
                    ValidateUserOwnedGhostSamuraiEnemyMotion(state.motion, kind, state.name);
                }
            }
        }

        private static void ValidateUserOwnedGhostSamuraiEnemyMotion(
            Motion motion,
            CombatProxyVisualKind kind,
            string stateName)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            CollectAnimationClips(motion, clips);

            if (clips.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The user-owned art candidate {kind} state {stateName} has no animation clips.");
            }

            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                AnimationClip clip = clips[clipIndex];
                string clipPath = clip != null ? AssetDatabase.GetAssetPath(clip) : string.Empty;

                if (clip == null
                    || !clip.humanMotion
                    || clip.name.StartsWith("__preview__", StringComparison.OrdinalIgnoreCase)
                    || !IsUnderRoot(clipPath, GhostSamuraiAssetRoot)
                    || HasCombatProxyCurves(clip))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate {kind} state {stateName} contains a missing, non-Humanoid, " +
                        $"non-Ghost, generated-preview, or proxy-placeholder clip: {clip?.name ?? "<null>"}.");
                }
            }
        }

        private static IEnumerable<AnimatorState> EnumerateAnimatorStates(AnimatorController controller)
        {
            for (int layerIndex = 0; layerIndex < controller.layers.Length; layerIndex++)
            {
                foreach (AnimatorState state in EnumerateAnimatorStates(controller.layers[layerIndex].stateMachine))
                {
                    yield return state;
                }
            }
        }

        private static IEnumerable<AnimatorState> EnumerateAnimatorStates(AnimatorStateMachine stateMachine)
        {
            for (int stateIndex = 0; stateIndex < stateMachine.states.Length; stateIndex++)
            {
                yield return stateMachine.states[stateIndex].state;
            }

            for (int childIndex = 0; childIndex < stateMachine.stateMachines.Length; childIndex++)
            {
                foreach (AnimatorState state in EnumerateAnimatorStates(stateMachine.stateMachines[childIndex].stateMachine))
                {
                    yield return state;
                }
            }
        }

        private static void CollectAnimationClips(Motion motion, List<AnimationClip> clips)
        {
            if (motion is AnimationClip animationClip)
            {
                clips.Add(animationClip);
                return;
            }

            if (motion is not BlendTree blendTree)
            {
                return;
            }

            ChildMotion[] children = blendTree.children;

            for (int childIndex = 0; childIndex < children.Length; childIndex++)
            {
                if (children[childIndex].motion != null)
                {
                    CollectAnimationClips(children[childIndex].motion, clips);
                }
            }
        }

        private static void ValidateUserOwnedGhostSamuraiMotion(Motion motion, string stateName)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            CollectAnimationClips(motion, clips);

            if (clips.Count == 0)
            {
                throw new InvalidOperationException(
                    $"The user-owned art candidate state {stateName} has no animation clips.");
            }

            for (int clipIndex = 0; clipIndex < clips.Count; clipIndex++)
            {
                AnimationClip clip = clips[clipIndex];

                if (clip == null || !clip.humanMotion || HasCombatProxyCurves(clip))
                {
                    throw new InvalidOperationException(
                        $"The user-owned art candidate state {stateName} contains a missing, non-Humanoid, " +
                        $"or proxy-placeholder clip: {clip?.name ?? "<null>"}.");
                }
            }
        }

        private static bool HasCombatProxyCurves(AnimationClip clip)
        {
            return AnimationUtility.GetCurveBindings(clip)
                    .Any(binding => IsCombatProxyCurvePath(binding.path))
                || AnimationUtility.GetObjectReferenceCurveBindings(clip)
                    .Any(binding => IsCombatProxyCurvePath(binding.path));
        }

        private static bool IsCombatProxyCurvePath(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && (path.Equals(CombatProxyVisualRootName, StringComparison.Ordinal)
                    || path.StartsWith(CombatProxyVisualRootName + "/", StringComparison.Ordinal));
        }

        private static bool IsUnderRoot(string assetPath, string rootPath)
        {
            return assetPath.Equals(rootPath, StringComparison.OrdinalIgnoreCase)
                || assetPath.StartsWith(rootPath + "/", StringComparison.OrdinalIgnoreCase);
        }

        private static string ReadRequiredTextFile(string path)
        {
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Release candidate preflight file does not exist: {path}", path);
            }

            return File.ReadAllText(path);
        }

        public static void ValidateBuildTargetSupport(BuildTarget target)
        {
            _ = GetOutputPath(target);

            if (UnityEditor.BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            {
                return;
            }

            string moduleName = target == BuildTarget.StandaloneWindows64
                ? "Windows Build Support"
                : "Mac Build Support";
            throw new InvalidOperationException(
                $"Release candidate build target {target} is not installed. " +
                $"Install {moduleName} for Unity {Application.unityVersion} before validating or building this target.");
        }
    }
}
