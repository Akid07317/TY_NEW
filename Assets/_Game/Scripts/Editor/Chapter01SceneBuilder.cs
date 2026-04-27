using System.Collections.Generic;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.AI;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Interaction;
using CampusRPG.Save;
using CampusRPG.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace CampusRPG.Editor
{
    public static class Chapter01SceneBuilder
    {
        private const string RootMenu = "CampusRPG/Setup/Build Chapter01 Combined Scene";
        private const string ForceRebuildMenu = "CampusRPG/Setup/Build Chapter01 Combined Scene (Force Rebuild)";
        private const string RepairBaselineTraversalMenu = "CampusRPG/Setup/Repair Chapter01 Baseline And Traversal Wiring";
        private const string RepairSceneNavMeshMenu = "CampusRPG/Setup/Repair Chapter01 Scene NavMesh";
        private const string LegacyRepairTraversalMenu = "CampusRPG/Setup/Repair Chapter01 Traversal Wiring";
        private const string ScenePath = "Assets/_Game/Scenes/Chapter01_Combined.unity";
        private const string ChapterProgressionPath = "Assets/_Game/Data/Chapter/SO_Chapter01_Progression.asset";
        private const string ChapterMapDefinitionPath = "Assets/_Game/Data/Chapter/SO_Chapter01_MapDefinition.asset";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string EnemyMeleePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab";
        private const string EnemyMobilePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab";
        private const string EnemyRangedPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab";
        private const string InputAssetPath = "Assets/_Game/Data/Input/CampusInputActions.inputactions";
        private const string AudioSettingsPath = "Assets/_Game/Data/Audio/SO_AudioSettings.asset";
        private const string MeleeEnemyArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Melee.asset";
        private const string MobileEnemyArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Mobile.asset";
        private const string RangedEnemyArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Ranged.asset";
        private const string BossEnemyArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Gatekeeper.asset";
        private const string BossTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";
        private const string MantleProbeOriginName = "MantleProbeOrigin";
        private const string InteriorMantleObstacleName = "TraversalMantle_InteriorApproach";
        private const string MapZonesRootName = "Chapter01_MapZones";
        private const string ModularGreyboxRootName = "Chapter01_ModularGreybox";
        private const string Zone01Name = "Zone01_EntranceTutorial";
        private const string Zone02Name = "Zone02_CourtyardArena";
        private const string Zone03Name = "Zone03_InteriorNarrowHall";
        private const string Zone04Name = "Zone04_SideRouteShortcut";
        private const string Zone05Name = "Zone05_BossApproachAndArena";
        private const string Zone01Id = "zone01_entrance_tutorial";
        private const string Zone02Id = "zone02_courtyard_arena";
        private const string Zone03Id = "zone03_interior_narrow_hall";
        private const string Zone04Id = "zone04_side_route_shortcut";
        private const string Zone05Id = "zone05_boss_approach_and_arena";
        private const string RouteGateA01A02Id = "route_gate_a01_to_a02";
        private const string RouteGateA02A03Id = "route_gate_a02_to_a03";
        private const string RouteGateA03ShortcutId = "route_gate_a03_side_shortcut";
        private const string RouteGateA03A04Id = "route_gate_a03_to_a04";
        private const string RouteGateA04RitualCoreId = "route_gate_a04_to_ritual_core";
        private static readonly Vector3 MantleProbeOriginLocalPosition = new Vector3(0f, 1.0f, 0.18f);
        private static readonly Vector3 InteriorMantleObstaclePosition = new Vector3(0f, 0.56f, 47.8f);
        private static readonly Vector3 InteriorMantleObstacleScale = new Vector3(3.2f, 1.12f, 1.6f);

        [MenuItem(RootMenu)]
        public static void BuildChapter01CombinedScene()
        {
            if (!ConfirmOverwriteTargets())
            {
                return;
            }

            BuildChapter01CombinedSceneInternal();
        }

        [MenuItem(ForceRebuildMenu)]
        public static void ForceBuildChapter01CombinedScene()
        {
            BuildChapter01CombinedSceneInternal();
        }

        [MenuItem(RepairBaselineTraversalMenu)]
        public static void RepairChapter01BaselineAndTraversalWiring()
        {
            RepairChapter01BaselineAndTraversalWiringInternal();
        }

        [MenuItem(RepairSceneNavMeshMenu)]
        public static void RepairChapter01SceneNavMesh()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogWarning("Chapter01 scene NavMesh repair skipped because the scene asset does not exist yet.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool built = RebuildChapter01SceneNavMesh(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log(
                built
                    ? "Chapter01 scene NavMesh rebuilt and saved."
                    : "Chapter01 scene NavMesh repair completed, but no baked NavMesh data was produced.");
        }

        [MenuItem(LegacyRepairTraversalMenu)]
        public static void RepairChapter01TraversalWiring()
        {
            RepairChapter01BaselineAndTraversalWiringInternal();
        }

        private static void RepairChapter01BaselineAndTraversalWiringInternal()
        {
            CombatTestSceneBuilder.RepairCombatTestPrefabWiring();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject flowRoot = FindSceneObject("ChapterFlow");
            GameObject player = FindSceneObject("Player");
            GameObject area03 = FindSceneObject(Chapter01Ids.Areas.Interior);

            if (flowRoot == null || player == null || area03 == null)
            {
                Debug.LogError("Failed to repair Chapter01 baseline/traversal wiring because a required scene object is missing.");
                return;
            }

            SaveService saveService = flowRoot.GetComponent<SaveService>();
            ChapterResumeContextView resumeContextView = GetOrAddComponent<ChapterResumeContextView>(flowRoot);
            PlayerCharacter playerCharacter = player.GetComponent<PlayerCharacter>();
            PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            PlayerMovementProbe movementProbe = GetOrAddComponent<PlayerMovementProbe>(player);
            Transform mantleProbeOrigin = FindOrCreateChild(player.transform, MantleProbeOriginName, MantleProbeOriginLocalPosition);

            SetObjectReference(resumeContextView, "saveService", saveService);
            SetObjectReference(playerCharacter, "movementProbe", movementProbe);
            SetObjectReference(playerCharacter, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(playerMotor, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(movementProbe, "probeOrigin", mantleProbeOrigin);
            CreateOrUpdateTraversalMantleObstacle(
                area03.transform,
                InteriorMantleObstacleName,
                InteriorMantleObstaclePosition,
                InteriorMantleObstacleScale);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Chapter01 baseline/traversal wiring repaired: CombatTest prefabs were restored to the public-safe proxy baseline, and the Chapter01 resume/mantle hooks were synchronized.");
        }

        private static void BuildChapter01CombinedSceneInternal()
        {
            CombatTestSceneBuilder.EnsureCombatTestContent();
            EnsureFolder("Assets/_Game/Data");
            EnsureFolder("Assets/_Game/Data/Chapter");
            EnsureFolder("Assets/_Game/Scenes");

            ChapterProgressionSO chapterProgression = CreateOrLoadChapterProgressionAsset();
            ChapterMapDefinitionSO chapterMapDefinition = CreateOrLoadChapterMapDefinitionAsset();
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject meleeEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMeleePrefabPath);
            GameObject mobileEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMobilePrefabPath);
            GameObject rangedEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyRangedPrefabPath);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            AudioSettingsSO audioSettings = AssetDatabase.LoadAssetAtPath<AudioSettingsSO>(AudioSettingsPath);
            EnemyArchetypeSO meleeArchetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(MeleeEnemyArchetypePath);
            EnemyArchetypeSO mobileArchetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(MobileEnemyArchetypePath);
            EnemyArchetypeSO rangedArchetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(RangedEnemyArchetypePath);
            EnemyArchetypeSO bossArchetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(BossEnemyArchetypePath);
            BossTelegraphStyleSO bossTelegraphStyle = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(BossTelegraphStylePath);

            if (playerPrefab == null
                || meleeEnemyPrefab == null
                || mobileEnemyPrefab == null
                || rangedEnemyPrefab == null
                || inputActions == null
                || chapterProgression == null
                || chapterMapDefinition == null)
            {
                Debug.LogError("Failed to build Chapter01 scene because a required asset is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject chapterRoot = new GameObject("Chapter01_Combined");
            GameObject bootstrap = new GameObject("Bootstrap");
            GameBootstrap gameBootstrap = bootstrap.AddComponent<GameBootstrap>();
            InputReader inputReader = bootstrap.AddComponent<InputReader>();
            SetObjectReference(inputReader, "actionsAsset", inputActions);
            SetObjectReference(gameBootstrap, "inputReader", inputReader);
            SetBool(gameBootstrap, "keepAliveAcrossScenes", false);

            GameObject lightObject = new GameObject("Directional Light");
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.25f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -40f, 0f);

            GameObject area01 = CreateAreaRoot(chapterRoot.transform, Chapter01Ids.Areas.Entrance, new Vector3(0f, 0f, 0f));
            GameObject area02 = CreateAreaRoot(chapterRoot.transform, Chapter01Ids.Areas.Courtyard, new Vector3(0f, 0f, 28f));
            GameObject area03 = CreateAreaRoot(chapterRoot.transform, Chapter01Ids.Areas.Interior, new Vector3(0f, 0f, 56f));
            GameObject area04 = CreateAreaRoot(chapterRoot.transform, Chapter01Ids.Areas.Boss, new Vector3(0f, 0f, 84f));

            BuildAreaShell(area01.transform, new Vector3(0f, 0f, 0f));
            BuildAreaShell(area02.transform, new Vector3(0f, 0f, 28f));
            BuildAreaShell(area03.transform, new Vector3(0f, 0f, 56f));
            BuildAreaShell(area04.transform, new Vector3(0f, 0f, 84f));
            BuildChapter01MapZones(chapterRoot.transform, area02.transform, area03.transform, area04.transform, chapterMapDefinition);
            BuildChapter01ModularGreybox(chapterRoot.transform);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = new Vector3(0f, 0.1f, -8f);
            player.transform.rotation = Quaternion.identity;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 300f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            cameraObject.transform.position = new Vector3(0f, 3.2f, -12f);
            cameraObject.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            GameObject flowRoot = new GameObject("ChapterFlow");
            CheckpointRestoreCoordinator checkpointCoordinator = flowRoot.AddComponent<CheckpointRestoreCoordinator>();
            SaveService saveService = flowRoot.GetComponent<SaveService>();
            CheckpointService checkpointService = flowRoot.GetComponent<CheckpointService>();
            ChapterProgressService chapterProgressService = flowRoot.AddComponent<ChapterProgressService>();
            AreaEntryView areaEntryView = flowRoot.AddComponent<AreaEntryView>();
            ChapterResumeContextView resumeContextView = flowRoot.AddComponent<ChapterResumeContextView>();
            CheckpointActivationView checkpointActivationView = flowRoot.AddComponent<CheckpointActivationView>();
            KeyItemAcquisitionView keyItemAcquisitionView = flowRoot.AddComponent<KeyItemAcquisitionView>();
            flowRoot.AddComponent<EncounterSealView>();
            EncounterClearView encounterClearView = flowRoot.AddComponent<EncounterClearView>();
            flowRoot.AddComponent<ChapterRouteBlockHintView>();
            ChapterObjectiveView chapterObjectiveView = flowRoot.AddComponent<ChapterObjectiveView>();
            ChapterTutorialHintView chapterTutorialHintView = flowRoot.AddComponent<ChapterTutorialHintView>();
            SceneRuntimeContext sceneContext = new GameObject("SceneRuntimeContext").AddComponent<SceneRuntimeContext>();
            CombatDebugHUD debugHud = new GameObject("CombatDebugHUD").AddComponent<CombatDebugHUD>();
            ChapterCompleteView chapterCompleteView = new GameObject("ChapterCompleteView").AddComponent<ChapterCompleteView>();
            BossPresentationRig bossPresentationRig = new GameObject("BossPresentationRig").AddComponent<BossPresentationRig>();
            EnemyBrain gatekeeperBoss = null;

            PlayerCharacter playerCharacter = player.GetComponent<PlayerCharacter>();
            PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            PlayerMovementProbe movementProbe = player.GetComponent<PlayerMovementProbe>();
            Transform mantleProbeOrigin = FindOrCreateChild(player.transform, MantleProbeOriginName, MantleProbeOriginLocalPosition);

            SetObjectReference(playerCharacter, "inputReader", inputReader);
            SetObjectReference(playerCharacter, "cameraTransform", cameraObject.transform);
            SetObjectReference(playerCharacter, "movementProbe", movementProbe);
            SetObjectReference(playerCharacter, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(lockOnTargetSelector, "inputReader", inputReader);
            SetObjectReference(lockOnTargetSelector, "cameraController", cameraController);
            SetObjectReference(lockOnTargetSelector, "cameraTransform", cameraObject.transform);
            SetLayerMask(lockOnTargetSelector, "targetMask", ~0);
            SetObjectReference(playerMotor, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(movementProbe, "probeOrigin", mantleProbeOrigin);
            SetObjectReference(cameraController, "followTarget", player.transform);
            SetObjectReference(cameraController, "inputReader", inputReader);

            SetObjectReference(checkpointCoordinator, "player", playerCharacter);
            SetObjectReference(checkpointCoordinator, "saveService", saveService);
            SetObjectReference(checkpointCoordinator, "checkpointService", checkpointService);
            SetObjectReference(checkpointCoordinator, "chapterProgressService", chapterProgressService);
            SetString(checkpointCoordinator, "chapterId", chapterProgression.ChapterId);
            SetString(checkpointCoordinator, "defaultCheckpointId", Chapter01Ids.Checkpoints.Start);
            SetBool(checkpointCoordinator, "autoLoadFromSaveOnStart", true);
            SetBool(checkpointCoordinator, "autoSaveOnStart", true);
            SetFloat(checkpointCoordinator, "respawnDelaySeconds", 0.5f);

            SetObjectReference(chapterProgressService, "progression", chapterProgression);
            SetObjectReference(chapterProgressService, "checkpointRestoreCoordinator", checkpointCoordinator);

            SetString(saveService, "fileName", "slot_auto_chapter01.json");

            SetObjectReference(sceneContext, "bootstrap", gameBootstrap);
            SetObjectReference(sceneContext, "inputReader", inputReader);
            SetObjectReference(sceneContext, "playerCharacter", playerCharacter);
            SetObjectReference(sceneContext, "cameraController", cameraController);
            SetObjectReference(sceneContext, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(sceneContext, "checkpointRestoreCoordinator", checkpointCoordinator);
            SetObjectReference(sceneContext, "chapterProgressService", chapterProgressService);
            SetObjectReference(sceneContext, "audioSettings", audioSettings);

            SetObjectReference(debugHud, "playerCharacter", playerCharacter);
            SetObjectReference(debugHud, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(areaEntryView, "chapterProgressService", chapterProgressService);
            SetObjectReference(resumeContextView, "saveService", saveService);
            SetObjectReference(checkpointActivationView, "checkpointService", checkpointService);
            SetObjectReference(keyItemAcquisitionView, "chapterProgressService", chapterProgressService);
            SetObjectReference(encounterClearView, "chapterProgressService", chapterProgressService);
            SetObjectReference(chapterObjectiveView, "chapterProgressService", chapterProgressService);
            SetObjectReference(chapterTutorialHintView, "chapterProgressService", chapterProgressService);
            SetObjectReference(chapterTutorialHintView, "inputReader", inputReader);
            SetObjectReference(chapterCompleteView, "chapterProgressService", chapterProgressService);

            CreateCheckpoint(area01.transform, "Checkpoint_CP01", Chapter01Ids.Checkpoints.Start, new Vector3(0f, 0.5f, -7f), checkpointCoordinator);
            CreateCheckpoint(area02.transform, "Checkpoint_CP02", Chapter01Ids.Checkpoints.Courtyard, new Vector3(0f, 0.5f, 20f), checkpointCoordinator);
            CreateCheckpoint(area03.transform, "Checkpoint_CP03", Chapter01Ids.Checkpoints.Interior, new Vector3(0f, 0.5f, 64.5f), checkpointCoordinator);
            CreateOrUpdateTraversalMantleObstacle(
                area03.transform,
                InteriorMantleObstacleName,
                InteriorMantleObstaclePosition,
                InteriorMantleObstacleScale);

            CreateTriggerVolume(
                area01.transform,
                "TRG_Area01_Enter",
                new Vector3(0f, 1f, -10f),
                new Vector3(12f, 3f, 2f),
                TriggerVolumeAction.EnterArea,
                Chapter01Ids.Areas.Entrance);
            CreateTriggerVolume(
                area02.transform,
                "TRG_Area02_Enter",
                new Vector3(0f, 1f, 16f),
                new Vector3(12f, 3f, 2f),
                TriggerVolumeAction.EnterArea,
                Chapter01Ids.Areas.Courtyard);
            CreateTriggerVolume(
                area03.transform,
                "TRG_Area03_Enter",
                new Vector3(0f, 1f, 44f),
                new Vector3(12f, 3f, 2f),
                TriggerVolumeAction.EnterArea,
                Chapter01Ids.Areas.Interior);
            CreateTriggerVolume(
                area04.transform,
                "TRG_Area04_Enter",
                new Vector3(0f, 1f, 72f),
                new Vector3(12f, 3f, 2f),
                TriggerVolumeAction.EnterArea,
                Chapter01Ids.Areas.Boss);

            CreateEncounter(
                area01.transform,
                "Encounter_EN_A01_TUTORIAL",
                Chapter01Ids.Encounters.EntranceTutorial,
                new Vector3(0f, 1f, 2f),
                new Vector3(14f, 3f, 10f),
                meleeEnemyPrefab,
                meleeArchetype,
                new[]
                {
                    new EncounterEnemySpec("Enemy_A01_Melee_A", new Vector3(-2.5f, 0f, 6f), Quaternion.Euler(0f, 180f, 0f), Vector3.one),
                    new EncounterEnemySpec("Enemy_A01_Melee_B", new Vector3(2.5f, 0f, 7.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one)
                });
            CreateEncounter(
                area02.transform,
                "Encounter_EN_A02_COURTYARD",
                Chapter01Ids.Encounters.Courtyard,
                new Vector3(0f, 1f, 30f),
                new Vector3(16f, 3f, 12f),
                meleeEnemyPrefab,
                meleeArchetype,
                new[]
                {
                    new EncounterEnemySpec("Enemy_A02_Melee_A", new Vector3(-4f, 0f, 31f), Quaternion.Euler(0f, 180f, 0f), Vector3.one),
                    new EncounterEnemySpec("Enemy_A02_Mobile_A", new Vector3(0f, 0f, 34f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, mobileEnemyPrefab, mobileArchetype),
                    new EncounterEnemySpec("Enemy_A02_Ranged_A", new Vector3(4f, 0f, 31f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, rangedEnemyPrefab, rangedArchetype)
                });
            GameObject interiorEncounterEntryBarrier = CreateEncounterBarrier(
                area03.transform,
                "InteriorEncounterBarrier_Entry",
                new Vector3(0f, 1.5f, 49f),
                new Vector3(8f, 3f, 1f));
            GameObject interiorEncounterSigilBarrier = CreateEncounterBarrier(
                area03.transform,
                "InteriorEncounterBarrier_Sigil",
                new Vector3(0f, 1.5f, 61.5f),
                new Vector3(8f, 3f, 1f));
            CreateEncounter(
                area03.transform,
                "Encounter_EN_A03_INTERIOR",
                Chapter01Ids.Encounters.Interior,
                new Vector3(0f, 1f, 54f),
                new Vector3(16f, 3f, 10f),
                meleeEnemyPrefab,
                meleeArchetype,
                new[]
                {
                    new EncounterEnemySpec("Enemy_A03_Melee_A", new Vector3(-3.5f, 0f, 54.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one),
                    new EncounterEnemySpec("Enemy_A03_Mobile_A", new Vector3(0f, 0f, 57.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, mobileEnemyPrefab, mobileArchetype),
                    new EncounterEnemySpec("Enemy_A03_Ranged_A", new Vector3(3.5f, 0f, 59.5f), Quaternion.Euler(0f, 180f, 0f), Vector3.one, rangedEnemyPrefab, rangedArchetype)
                },
                new[]
                {
                    interiorEncounterEntryBarrier,
                    interiorEncounterSigilBarrier
                });
            GameObject bossArenaBarrier = CreateEncounterBarrier(
                area04.transform,
                "BossArenaBarrier",
                new Vector3(0f, 1.5f, 75.5f),
                new Vector3(8f, 3f, 1f));
            gatekeeperBoss = CreateEncounter(
                area04.transform,
                "Encounter_EN_A04_GATEKEEPER",
                Chapter01Ids.Encounters.Gatekeeper,
                new Vector3(0f, 1f, 82f),
                new Vector3(16f, 3f, 10f),
                meleeEnemyPrefab,
                bossArchetype != null ? bossArchetype : meleeArchetype,
                new[]
                {
                    new EncounterEnemySpec("Boss_Gatekeeper", new Vector3(0f, 0f, 84f), Quaternion.Euler(0f, 180f, 0f), new Vector3(1.6f, 1.8f, 1.6f))
                },
                new[] { bossArenaBarrier });
            EncounterController gatekeeperEncounter = gatekeeperBoss != null
                ? gatekeeperBoss.GetComponentInParent<EncounterController>()
                : null;
            bossPresentationRig.Configure(gatekeeperBoss, gatekeeperEncounter, bossTelegraphStyle);
            bossPresentationRig.ApplyConfiguration();

            CreateDoor(
                chapterRoot.transform,
                "Door_A01_To_A02",
                new Vector3(0f, 1.5f, 13f),
                new Vector3(6f, 3f, 1f),
                string.Empty,
                Chapter01Ids.Encounters.EntranceTutorial,
                string.Empty);
            CreateDoorRequirementHintTrigger(
                chapterRoot.transform,
                "Hint_Door_A01_To_A02",
                new Vector3(0f, 1f, 11f),
                new Vector3(8f, 3f, 2f),
                string.Empty,
                Chapter01Ids.Encounters.EntranceTutorial,
                string.Empty);
            CreateDoor(
                chapterRoot.transform,
                "Door_A02_To_A03",
                new Vector3(0f, 1.5f, 41f),
                new Vector3(6f, 3f, 1f),
                string.Empty,
                Chapter01Ids.Encounters.Courtyard,
                string.Empty);
            CreateDoorRequirementHintTrigger(
                chapterRoot.transform,
                "Hint_Door_A02_To_A03",
                new Vector3(0f, 1f, 39f),
                new Vector3(8f, 3f, 2f),
                string.Empty,
                Chapter01Ids.Encounters.Courtyard,
                string.Empty);
            CreateDoor(
                chapterRoot.transform,
                "Door_A03_To_A04",
                new Vector3(0f, 1.5f, 69f),
                new Vector3(6f, 3f, 1f),
                string.Empty,
                string.Empty,
                chapterProgression.BossGateRequiredKeyItemId);
            CreateDoorRequirementHintTrigger(
                chapterRoot.transform,
                "Hint_Door_A03_To_A04",
                new Vector3(0f, 1f, 67f),
                new Vector3(8f, 3f, 2f),
                string.Empty,
                string.Empty,
                chapterProgression.BossGateRequiredKeyItemId);
            CreateDoor(
                area04.transform,
                "Door_A04_To_RitualCore",
                new Vector3(0f, 1.5f, 87f),
                new Vector3(5f, 3f, 1f),
                string.Empty,
                Chapter01Ids.Encounters.Gatekeeper,
                string.Empty);
            CreateDoorRequirementHintTrigger(
                area04.transform,
                "Hint_Door_A04_To_RitualCore",
                new Vector3(0f, 1f, 1f),
                new Vector3(8f, 3f, 2f),
                string.Empty,
                Chapter01Ids.Encounters.Gatekeeper,
                string.Empty);

            CreateKeyItemPickup(
                area03.transform,
                "Pickup_GateSigil",
                new Vector3(0f, 0.8f, 64f),
                chapterProgression.BossGateRequiredKeyItemId,
                false);
            CreateKeyItemPickup(
                area04.transform,
                "Pickup_RitualCore",
                new Vector3(0f, 0.8f, 88f),
                chapterProgression.ChapterCompletionKeyItemId,
                true,
                Chapter01Ids.Encounters.Gatekeeper);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            bool navMeshBuilt = RebuildChapter01SceneNavMesh(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!navMeshBuilt)
            {
                Debug.LogWarning("Chapter01 combined scene was generated, but no baked NavMesh data was produced.");
            }

            Debug.Log("Chapter01 combined scene graybox and progression asset were generated using the public-safe proxy baseline.");
        }

        private static ChapterProgressionSO CreateOrLoadChapterProgressionAsset()
        {
            ChapterProgressionSO asset = AssetDatabase.LoadAssetAtPath<ChapterProgressionSO>(ChapterProgressionPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChapterProgressionSO>();
                AssetDatabase.CreateAsset(asset, ChapterProgressionPath);
            }

            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("chapterId").stringValue = Chapter01Ids.Chapter;
            serializedObject.FindProperty("bossGateRequiredKeyItemId").stringValue = Chapter01Ids.KeyItems.GateSigil;
            serializedObject.FindProperty("chapterCompletionKeyItemId").stringValue = Chapter01Ids.KeyItems.RitualCore;

            SerializedProperty areasProperty = serializedObject.FindProperty("areas");
            areasProperty.arraySize = 4;
            SetAreaEntry(areasProperty, 0, Chapter01Ids.Areas.Entrance, "Entrance Tutorial");
            SetAreaEntry(areasProperty, 1, Chapter01Ids.Areas.Courtyard, "Outdoor Courtyard");
            SetAreaEntry(areasProperty, 2, Chapter01Ids.Areas.Interior, "School Interior");
            SetAreaEntry(areasProperty, 3, Chapter01Ids.Areas.Boss, "Boss Arena");

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ChapterMapDefinitionSO CreateOrLoadChapterMapDefinitionAsset()
        {
            ChapterMapDefinitionSO asset = AssetDatabase.LoadAssetAtPath<ChapterMapDefinitionSO>(ChapterMapDefinitionPath);

            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChapterMapDefinitionSO>();
                AssetDatabase.CreateAsset(asset, ChapterMapDefinitionPath);
            }

            asset.Configure(
                Chapter01Ids.Chapter,
                CreateChapterMapZoneDefinitions(),
                CreateChapterMapRouteGateDefinitions());
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static ChapterMapDefinitionSO.MapZoneDefinition[] CreateChapterMapZoneDefinitions()
        {
            return new[]
            {
                new ChapterMapDefinitionSO.MapZoneDefinition(
                    Zone01Id,
                    Zone01Name,
                    "Entrance Tutorial",
                    Chapter01Ids.Areas.Entrance,
                    "Learn movement, lock-on, light attacks, guard and dodge before the first route gate.",
                    Chapter01Ids.Encounters.EntranceTutorial,
                    Chapter01Ids.Checkpoints.Start,
                    string.Empty,
                    false,
                    new Vector3(0f, 1f, 0f),
                    new Vector3(14f, 2.5f, 18f)),
                new ChapterMapDefinitionSO.MapZoneDefinition(
                    Zone02Id,
                    Zone02Name,
                    "Courtyard Arena",
                    Chapter01Ids.Areas.Courtyard,
                    "Use wide lanes, cover, ranged pressure and mixed enemies to test roll and dodge spacing.",
                    Chapter01Ids.Encounters.Courtyard,
                    Chapter01Ids.Checkpoints.Courtyard,
                    string.Empty,
                    false,
                    new Vector3(0f, 1f, 28f),
                    new Vector3(20f, 2.5f, 22f)),
                new ChapterMapDefinitionSO.MapZoneDefinition(
                    Zone03Id,
                    Zone03Name,
                    "Interior Narrow Hall",
                    Chapter01Ids.Areas.Interior,
                    "Fight through camera pressure, mantle cover and the sigil lock-room before the boss gate.",
                    Chapter01Ids.Encounters.Interior,
                    Chapter01Ids.Checkpoints.Interior,
                    Chapter01Ids.KeyItems.GateSigil,
                    false,
                    new Vector3(0f, 1f, 50f),
                    new Vector3(9f, 2.5f, 12f)),
                new ChapterMapDefinitionSO.MapZoneDefinition(
                    Zone04Id,
                    Zone04Name,
                    "Side Route Shortcut",
                    Chapter01Ids.Areas.Interior,
                    "Optional side lane that reads as a shortcut return after the interior lock-room is solved.",
                    string.Empty,
                    Chapter01Ids.Checkpoints.Interior,
                    Chapter01Ids.KeyItems.SideRouteCache,
                    true,
                    new Vector3(-5.25f, 1f, 58f),
                    new Vector3(5.5f, 2.5f, 15f)),
                new ChapterMapDefinitionSO.MapZoneDefinition(
                    Zone05Id,
                    Zone05Name,
                    "Boss Approach And Arena",
                    Chapter01Ids.Areas.Boss,
                    "Prepare in the antechamber, fight Gatekeeper, then claim the Ritual Core to finish Chapter01.",
                    Chapter01Ids.Encounters.Gatekeeper,
                    Chapter01Ids.Checkpoints.Interior,
                    Chapter01Ids.KeyItems.RitualCore,
                    false,
                    new Vector3(0f, 1f, 82f),
                    new Vector3(18f, 2.5f, 24f))
            };
        }

        private static ChapterMapDefinitionSO.RouteGateDefinition[] CreateChapterMapRouteGateDefinitions()
        {
            return new[]
            {
                new ChapterMapDefinitionSO.RouteGateDefinition(
                    RouteGateA01A02Id,
                    "Entrance To Courtyard",
                    Zone01Id,
                    Zone02Id,
                    Chapter01Ids.Encounters.EntranceTutorial,
                    string.Empty,
                    false),
                new ChapterMapDefinitionSO.RouteGateDefinition(
                    RouteGateA02A03Id,
                    "Courtyard To Interior",
                    Zone02Id,
                    Zone03Id,
                    Chapter01Ids.Encounters.Courtyard,
                    string.Empty,
                    false),
                new ChapterMapDefinitionSO.RouteGateDefinition(
                    RouteGateA03ShortcutId,
                    "Interior Shortcut Return",
                    Zone03Id,
                    Zone04Id,
                    Chapter01Ids.Encounters.Interior,
                    string.Empty,
                    true),
                new ChapterMapDefinitionSO.RouteGateDefinition(
                    RouteGateA03A04Id,
                    "Interior To Boss Gate",
                    Zone04Id,
                    Zone05Id,
                    string.Empty,
                    Chapter01Ids.KeyItems.GateSigil,
                    false),
                new ChapterMapDefinitionSO.RouteGateDefinition(
                    RouteGateA04RitualCoreId,
                    "Gatekeeper To Ritual Core",
                    Zone05Id,
                    Zone05Id,
                    Chapter01Ids.Encounters.Gatekeeper,
                    string.Empty,
                    false)
            };
        }

        private static GameObject CreateAreaRoot(Transform parent, string name, Vector3 position)
        {
            GameObject areaRoot = new GameObject(name);
            areaRoot.transform.SetParent(parent);
            areaRoot.transform.position = position;
            return areaRoot;
        }

        private static void BuildAreaShell(Transform parent, Vector3 center)
        {
            CreateFloor(parent, "Floor", center + new Vector3(0f, -0.5f, 0f), new Vector3(18f, 1f, 20f));
            CreateWall(parent, "Wall_Left", center + new Vector3(-9.5f, 1.5f, 0f), new Vector3(1f, 3f, 20f));
            CreateWall(parent, "Wall_Right", center + new Vector3(9.5f, 1.5f, 0f), new Vector3(1f, 3f, 20f));
            CreateWall(parent, "Wall_Back_Left", center + new Vector3(-6f, 1.5f, -10.5f), new Vector3(6f, 3f, 1f));
            CreateWall(parent, "Wall_Back_Right", center + new Vector3(6f, 1.5f, -10.5f), new Vector3(6f, 3f, 1f));
            CreateWall(parent, "Wall_Front_Left", center + new Vector3(-6f, 1.5f, 10.5f), new Vector3(6f, 3f, 1f));
            CreateWall(parent, "Wall_Front_Right", center + new Vector3(6f, 1.5f, 10.5f), new Vector3(6f, 3f, 1f));
        }

        private static void BuildChapter01MapZones(
            Transform chapterRoot,
            Transform courtyardArea,
            Transform interiorArea,
            Transform bossArea,
            ChapterMapDefinitionSO mapDefinition)
        {
            GameObject zonesRoot = new GameObject(MapZonesRootName);
            zonesRoot.transform.SetParent(chapterRoot);
            zonesRoot.transform.position = Vector3.zero;

            ChapterMapDefinitionSO.MapZoneDefinition[] zones = mapDefinition != null
                ? mapDefinition.Zones
                : CreateChapterMapZoneDefinitions();

            for (int i = 0; i < zones.Length; i++)
            {
                CreateMapZoneMarker(zonesRoot.transform, zones[i], mapDefinition);
            }

            CreateFloor(chapterRoot, "Connector_A01_A02_Floor", new Vector3(0f, -0.5f, 14f), new Vector3(6f, 1f, 8f));
            CreateFloor(chapterRoot, "Connector_A02_A03_Floor", new Vector3(0f, -0.5f, 42f), new Vector3(6f, 1f, 8f));
            CreateFloor(chapterRoot, "Connector_A03_A04_Floor", new Vector3(0f, -0.5f, 70f), new Vector3(6f, 1f, 8f));

            CreateFloor(courtyardArea, "Zone02_LeftEvadeLane_Floor", new Vector3(-6.1f, -0.48f, 28f), new Vector3(3.8f, 0.14f, 17f));
            CreateFloor(courtyardArea, "Zone02_RightEvadeLane_Floor", new Vector3(6.1f, -0.48f, 28f), new Vector3(3.8f, 0.14f, 17f));
            CreateWall(courtyardArea, "Zone02_CenterCover_A", new Vector3(-1.9f, 0.35f, 28.5f), new Vector3(1.4f, 0.7f, 2.6f));
            CreateWall(courtyardArea, "Zone02_CenterCover_B", new Vector3(2.2f, 0.35f, 33f), new Vector3(1.4f, 0.7f, 2.6f));

            CreateWall(interiorArea, "Zone03_CameraPillar_Left_A", new Vector3(-3.4f, 1.4f, 49.5f), new Vector3(0.8f, 2.8f, 0.8f));
            CreateWall(interiorArea, "Zone03_CameraPillar_Right_A", new Vector3(3.4f, 1.4f, 52.5f), new Vector3(0.8f, 2.8f, 0.8f));
            CreateWall(interiorArea, "Zone03_CameraPillar_Left_B", new Vector3(-3.4f, 1.4f, 55.5f), new Vector3(0.8f, 2.8f, 0.8f));
            CreateWall(interiorArea, "Zone03_CameraPillar_Right_B", new Vector3(3.4f, 1.4f, 58.5f), new Vector3(0.8f, 2.8f, 0.8f));

            CreateFloor(interiorArea, "Zone04_SideRouteShortcut_Floor", new Vector3(-5.7f, -0.46f, 58.5f), new Vector3(3.8f, 0.12f, 14f));
            CreateWall(interiorArea, "Zone04_SideRoute_LowCover_A", new Vector3(-2.7f, 0.35f, 55.5f), new Vector3(0.6f, 0.7f, 3.8f));
            CreateWall(interiorArea, "Zone04_SideRoute_LowCover_B", new Vector3(-2.7f, 0.35f, 62f), new Vector3(0.6f, 0.7f, 3.8f));
            CreateWall(interiorArea, "Zone04_ShortcutReturn_Gate_Left", new Vector3(-7.4f, 1.2f, 65.3f), new Vector3(0.45f, 2.4f, 0.45f));
            CreateWall(interiorArea, "Zone04_ShortcutReturn_Gate_Right", new Vector3(-4f, 1.2f, 65.3f), new Vector3(0.45f, 2.4f, 0.45f));

            CreateFloor(bossArea, "Zone05_BossAntechamber_Floor", new Vector3(0f, -0.46f, 75.8f), new Vector3(12f, 0.12f, 4.6f));
            CreateWall(bossArea, "Zone05_BossAntechamber_SupplyMarker", new Vector3(-5.2f, 0.45f, 76.2f), new Vector3(1.2f, 0.9f, 1.2f));
            CreateWall(bossArea, "Zone05_BossArenaBoundary_Left", new Vector3(-7.2f, 0.6f, 81.2f), new Vector3(0.6f, 1.2f, 6f));
            CreateWall(bossArea, "Zone05_BossArenaBoundary_Right", new Vector3(7.2f, 0.6f, 81.2f), new Vector3(0.6f, 1.2f, 6f));
            CreateFloor(bossArea, "Zone05_BossArena_CenterRing", new Vector3(0f, -0.44f, 84f), new Vector3(7.5f, 0.1f, 7.5f));
        }

        private static void BuildChapter01ModularGreybox(Transform chapterRoot)
        {
            GameObject modularRoot = new GameObject(ModularGreyboxRootName);
            modularRoot.transform.SetParent(chapterRoot);
            modularRoot.transform.position = Vector3.zero;

            Transform entranceRoot = CreateModularZoneRoot(modularRoot.transform, "Modular_Zone01_Entrance");
            Transform courtyardRoot = CreateModularZoneRoot(modularRoot.transform, "Modular_Zone02_Courtyard");
            Transform interiorRoot = CreateModularZoneRoot(modularRoot.transform, "Modular_Zone03_Interior");
            Transform sideRouteRoot = CreateModularZoneRoot(modularRoot.transform, "Modular_Zone04_SideRoute");
            Transform bossRoot = CreateModularZoneRoot(modularRoot.transform, "Modular_Zone05_BossApproach");

            CreateModularBlock(entranceRoot, "Modular_Zone01_EntranceArch_LeftPost", new Vector3(-4.8f, 1.45f, 8.9f), new Vector3(0.65f, 2.9f, 0.65f));
            CreateModularBlock(entranceRoot, "Modular_Zone01_EntranceArch_RightPost", new Vector3(4.8f, 1.45f, 8.9f), new Vector3(0.65f, 2.9f, 0.65f));
            CreateModularBlock(entranceRoot, "Modular_Zone01_EntranceArch_TopBeam", new Vector3(0f, 3.05f, 8.9f), new Vector3(10.1f, 0.45f, 0.7f));
            CreateModularBlock(entranceRoot, "Modular_Zone01_TutorialSightline_LeftPlinth", new Vector3(-6.2f, 0.35f, -3.2f), new Vector3(1.2f, 0.7f, 1.2f));
            CreateModularBlock(entranceRoot, "Modular_Zone01_TutorialSightline_RightPlinth", new Vector3(6.2f, 0.35f, -3.2f), new Vector3(1.2f, 0.7f, 1.2f));

            CreateModularBlock(courtyardRoot, "Modular_Zone02_LeftLane_Rail_A", new Vector3(-7.9f, 0.45f, 24.5f), new Vector3(0.45f, 0.9f, 4.4f));
            CreateModularBlock(courtyardRoot, "Modular_Zone02_LeftLane_Rail_B", new Vector3(-7.9f, 0.45f, 32.6f), new Vector3(0.45f, 0.9f, 4.4f));
            CreateModularBlock(courtyardRoot, "Modular_Zone02_RightLane_Rail_A", new Vector3(7.9f, 0.45f, 24.5f), new Vector3(0.45f, 0.9f, 4.4f));
            CreateModularBlock(courtyardRoot, "Modular_Zone02_RightLane_Rail_B", new Vector3(7.9f, 0.45f, 32.6f), new Vector3(0.45f, 0.9f, 4.4f));
            CreateModularBlock(courtyardRoot, "Modular_Zone02_CenterCover_CrateStack", new Vector3(0.2f, 0.55f, 30.6f), new Vector3(1.4f, 1.1f, 1.4f));

            CreateModularBlock(interiorRoot, "Modular_Zone03_CeilingBeam_A", new Vector3(0f, 3.15f, 50.8f), new Vector3(8.1f, 0.28f, 0.45f));
            CreateModularBlock(interiorRoot, "Modular_Zone03_CeilingBeam_B", new Vector3(0f, 3.15f, 56.8f), new Vector3(8.1f, 0.28f, 0.45f));
            CreateModularBlock(interiorRoot, "Modular_Zone03_PillarTrim_Left_A", new Vector3(-4.35f, 1.55f, 52.4f), new Vector3(0.42f, 3.1f, 1.1f));
            CreateModularBlock(interiorRoot, "Modular_Zone03_PillarTrim_Right_A", new Vector3(4.35f, 1.55f, 55.6f), new Vector3(0.42f, 3.1f, 1.1f));

            CreateModularBlock(sideRouteRoot, "Modular_Zone04_SideRoute_Step_A", new Vector3(-6.55f, -0.28f, 53.2f), new Vector3(2.3f, 0.22f, 1.0f));
            CreateModularBlock(sideRouteRoot, "Modular_Zone04_SideRoute_Step_B", new Vector3(-5.1f, -0.16f, 57.5f), new Vector3(2.4f, 0.24f, 1.0f));
            CreateModularBlock(sideRouteRoot, "Modular_Zone04_SideRoute_Step_C", new Vector3(-6.65f, -0.04f, 61.8f), new Vector3(2.3f, 0.26f, 1.0f));
            CreateModularBlock(sideRouteRoot, "Modular_Zone04_SideRoute_CachePlinth", new Vector3(-6.1f, 0.38f, 64.5f), new Vector3(1.4f, 0.76f, 1.4f));

            CreateModularBlock(bossRoot, "Modular_Zone05_AntechamberArch_LeftPost", new Vector3(-5.9f, 1.55f, 78.2f), new Vector3(0.7f, 3.1f, 0.7f));
            CreateModularBlock(bossRoot, "Modular_Zone05_AntechamberArch_RightPost", new Vector3(5.9f, 1.55f, 78.2f), new Vector3(0.7f, 3.1f, 0.7f));
            CreateModularBlock(bossRoot, "Modular_Zone05_AntechamberArch_TopBeam", new Vector3(0f, 3.25f, 78.2f), new Vector3(12.4f, 0.5f, 0.75f));
            CreateModularBlock(bossRoot, "Modular_Zone05_ArenaRune_Left", new Vector3(-5.4f, 0.12f, 87.8f), new Vector3(1.8f, 0.24f, 1.8f));
            CreateModularBlock(bossRoot, "Modular_Zone05_ArenaRune_Right", new Vector3(5.4f, 0.12f, 87.8f), new Vector3(1.8f, 0.24f, 1.8f));
        }

        private static Transform CreateModularZoneRoot(Transform parent, string name)
        {
            GameObject zoneRoot = new GameObject(name);
            zoneRoot.transform.SetParent(parent);
            zoneRoot.transform.position = Vector3.zero;
            return zoneRoot.transform;
        }

        private static GameObject CreateModularBlock(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent);
            block.transform.position = position;
            block.transform.localScale = scale;
            return block;
        }

        private static void CreateMapZoneMarker(
            Transform parent,
            ChapterMapDefinitionSO.MapZoneDefinition zone,
            ChapterMapDefinitionSO mapDefinition)
        {
            GameObject marker = new GameObject(zone.SceneObjectName);
            marker.transform.SetParent(parent);
            marker.transform.position = zone.Center;
            BoxCollider collider = marker.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = zone.Size;
            ChapterMapZoneMarker zoneMarker = marker.AddComponent<ChapterMapZoneMarker>();
            zoneMarker.Configure(mapDefinition, zone.ZoneId);
            EditorUtility.SetDirty(zoneMarker);
        }

        private static void CreateFloor(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent);
            floor.transform.position = position;
            floor.transform.localScale = scale;
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
        }

        private static void CreateCheckpoint(
            Transform parent,
            string name,
            string checkpointId,
            Vector3 position,
            CheckpointRestoreCoordinator coordinator)
        {
            GameObject checkpoint = new GameObject(name);
            checkpoint.transform.SetParent(parent);
            checkpoint.transform.position = position;
            BoxCollider collider = checkpoint.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(2f, 2f, 2f);
            CheckpointRuntimeAnchor anchor = checkpoint.AddComponent<CheckpointRuntimeAnchor>();
            SetObjectReference(anchor, "coordinator", coordinator);
            SetString(anchor, "checkpointIdOverride", checkpointId);
        }

        private static void CreateTriggerVolume(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            TriggerVolumeAction action,
            string payloadId)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent);
            triggerObject.transform.position = position;
            BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            TriggerVolume triggerVolume = triggerObject.AddComponent<TriggerVolume>();
            SetEnum(triggerVolume, "action", (int)action);
            SetString(triggerVolume, "payloadId", payloadId);
            SetBool(triggerVolume, "oneShot", true);
        }

        private static void CreateDoor(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            string requiredAreaId,
            string requiredEncounterId,
            string requiredKeyItemId)
        {
            GameObject door = GameObject.CreatePrimitive(PrimitiveType.Cube);
            door.name = name;
            door.transform.SetParent(parent);
            door.transform.position = position;
            door.transform.localScale = scale;
            DoorController controller = door.AddComponent<DoorController>();
            SetString(controller, "requiredAreaId", requiredAreaId);
            SetString(controller, "requiredEncounterId", requiredEncounterId);
            SetString(controller, "requiredKeyItemId", requiredKeyItemId);
        }

        private static void CreateDoorRequirementHintTrigger(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 size,
            string requiredAreaId,
            string requiredEncounterId,
            string requiredKeyItemId)
        {
            GameObject triggerObject = new GameObject(name);
            triggerObject.transform.SetParent(parent);
            triggerObject.transform.position = position;
            BoxCollider collider = triggerObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = size;
            DoorRequirementHintTrigger trigger = triggerObject.AddComponent<DoorRequirementHintTrigger>();
            SetString(trigger, "requiredAreaId", requiredAreaId);
            SetString(trigger, "requiredEncounterId", requiredEncounterId);
            SetString(trigger, "requiredKeyItemId", requiredKeyItemId);
        }

        private static GameObject CreateEncounterBarrier(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
            barrier.name = name;
            barrier.transform.SetParent(parent);
            barrier.transform.position = position;
            barrier.transform.localScale = scale;
            barrier.SetActive(false);
            return barrier;
        }

        private static bool RebuildChapter01SceneNavMesh(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            MarkSceneNavigationStatic();

#pragma warning disable CS0618
            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
#pragma warning restore CS0618
            EditorSceneManager.MarkSceneDirty(scene);

            if (!System.IO.File.Exists(scene.path))
            {
                return false;
            }

            EditorSceneManager.SaveScene(scene, scene.path);
            string sceneYaml = System.IO.File.ReadAllText(scene.path);
            return !string.IsNullOrWhiteSpace(sceneYaml) && !sceneYaml.Contains("m_NavMeshData: {fileID: 0}");
        }

        private static void MarkSceneNavigationStatic()
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);

                for (int j = 0; j < transforms.Length; j++)
                {
                    if (IsNavigationFloorName(transforms[j].name))
                    {
                        SetNavigationStatic(transforms[j].gameObject);
                    }
                }
            }
        }

        private static bool IsNavigationFloorName(string objectName)
        {
            return objectName == "Floor"
                || objectName.EndsWith("_Floor", System.StringComparison.Ordinal)
                || objectName.EndsWith("_CenterRing", System.StringComparison.Ordinal);
        }

        private static void SetNavigationStatic(GameObject gameObject)
        {
#pragma warning disable CS0618
            StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            GameObjectUtility.SetStaticEditorFlags(gameObject, currentFlags | StaticEditorFlags.NavigationStatic);
#pragma warning restore CS0618
        }

        private static GameObject CreateOrUpdateTraversalMantleObstacle(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject obstacle = FindSceneObject(name);

            if (obstacle == null)
            {
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = name;
            }

            obstacle.transform.SetParent(parent);
            obstacle.transform.position = position;
            obstacle.transform.localScale = scale;
            return obstacle;
        }

        private static EnemyBrain CreateEncounter(
            Transform parent,
            string name,
            string encounterId,
            Vector3 triggerPosition,
            Vector3 triggerSize,
            GameObject enemyPrefab,
            EnemyArchetypeSO archetypeOverride,
            EncounterEnemySpec[] enemySpecs,
            GameObject[] blockersToEnableWhileActive = null)
        {
            GameObject encounterRoot = new GameObject(name);
            encounterRoot.transform.SetParent(parent);
            encounterRoot.transform.position = triggerPosition;
            EnemyBrain firstEnemyBrain = null;

            BoxCollider collider = encounterRoot.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = triggerSize;
            collider.center = Vector3.zero;

            EncounterController controller = encounterRoot.AddComponent<EncounterController>();
            SetString(controller, "encounterId", encounterId);
            SetBool(controller, "activateOnPlayerEnter", true);
            SetBool(controller, "startActive", false);

            if (blockersToEnableWhileActive != null && blockersToEnableWhileActive.Length > 0)
            {
                SetObjectArray(controller, "blockersToEnableWhileActive", blockersToEnableWhileActive);
            }

            if (enemyPrefab == null || enemySpecs == null)
            {
                return null;
            }

            for (int i = 0; i < enemySpecs.Length; i++)
            {
                GameObject resolvedPrefab = enemySpecs[i].PrefabOverride != null
                    ? enemySpecs[i].PrefabOverride
                    : enemyPrefab;
                GameObject enemy = resolvedPrefab != null
                    ? (GameObject)PrefabUtility.InstantiatePrefab(resolvedPrefab)
                    : null;

                if (enemy == null)
                {
                    continue;
                }

                enemy.name = enemySpecs[i].Name;
                enemy.transform.SetParent(encounterRoot.transform);
                enemy.transform.position = enemySpecs[i].Position;
                enemy.transform.rotation = enemySpecs[i].Rotation;
                enemy.transform.localScale = enemySpecs[i].Scale;

                EnemyBrain enemyBrain = enemy.GetComponent<EnemyBrain>();
                EnemyArchetypeSO resolvedArchetype = enemySpecs[i].ArchetypeOverride != null
                    ? enemySpecs[i].ArchetypeOverride
                    : archetypeOverride;

                if (enemyBrain != null && resolvedArchetype != null)
                {
                    SetObjectReference(enemyBrain, "archetype", resolvedArchetype);
                }

                firstEnemyBrain ??= enemyBrain;

                enemy.AddComponent<EnemyEncounterMember>();
            }

            return firstEnemyBrain;
        }

        private static void CreateKeyItemPickup(
            Transform parent,
            string name,
            Vector3 position,
            string keyItemId,
            bool completeChapterOnPickup,
            string requiredEncounterIdForBeacon = "")
        {
            GameObject pickup = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pickup.name = name;
            pickup.transform.SetParent(parent);
            pickup.transform.position = position;
            pickup.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            SphereCollider collider = pickup.GetComponent<SphereCollider>();
            collider.isTrigger = true;
            KeyItemPickup keyItemPickup = pickup.AddComponent<KeyItemPickup>();
            SetString(keyItemPickup, "keyItemId", keyItemId);
            SetBool(keyItemPickup, "completeChapterOnPickup", completeChapterOnPickup);

            if (!string.IsNullOrWhiteSpace(requiredEncounterIdForBeacon))
            {
                KeyItemBeaconView keyItemBeaconView = pickup.AddComponent<KeyItemBeaconView>();
                SetString(keyItemBeaconView, "requiredEncounterId", requiredEncounterIdForBeacon);
            }
        }

        private static void SetAreaEntry(SerializedProperty areasProperty, int index, string areaId, string displayName)
        {
            SerializedProperty element = areasProperty.GetArrayElementAtIndex(index);
            element.FindPropertyRelative("areaId").stringValue = areaId;
            element.FindPropertyRelative("displayName").stringValue = displayName;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetObjectArray(Object target, string propertyName, Object[] values)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            SerializedProperty property = serializedObject.FindProperty(propertyName);
            property.arraySize = values != null ? values.Length : 0;

            if (values != null)
            {
                for (int i = 0; i < values.Length; i++)
                {
                    property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetEnum(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).enumValueIndex = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(target);
        }

        private static bool ConfirmOverwriteTargets()
        {
            if (Application.isBatchMode)
            {
                return true;
            }

            List<string> existingTargets = CollectExistingTargets();

            if (existingTargets.Count == 0)
            {
                return true;
            }

            string message =
                "This action will overwrite the existing Chapter01 skeleton outputs:\n\n- " +
                string.Join("\n- ", existingTargets) +
                "\n\nCancel if you have hand-tuned scene or progression edits you want to keep.";

            return EditorUtility.DisplayDialog(
                "Rebuild Chapter01 Outputs?",
                message,
                "Rebuild",
                "Cancel");
        }

        private static List<string> CollectExistingTargets()
        {
            string[] candidatePaths =
            {
                ScenePath,
                ChapterProgressionPath,
                ChapterMapDefinitionPath
            };

            List<string> results = new List<string>();

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (AssetDatabase.LoadMainAssetAtPath(candidatePaths[i]) != null)
                {
                    results.Add(candidatePaths[i]);
                }
            }

            return results;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            string parent = System.IO.Path.GetDirectoryName(folderPath)?.Replace("\\", "/");
            string folderName = System.IO.Path.GetFileName(folderPath);

            if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static GameObject FindSceneObject(string objectName)
        {
            Scene scene = SceneManager.GetActiveScene();
            GameObject[] roots = scene.GetRootGameObjects();

            for (int i = 0; i < roots.Length; i++)
            {
                Transform[] transforms = roots[i].GetComponentsInChildren<Transform>(true);

                for (int j = 0; j < transforms.Length; j++)
                {
                    if (transforms[j].name == objectName)
                    {
                        return transforms[j].gameObject;
                    }
                }
            }

            return null;
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static Transform FindOrCreateChild(Transform parent, string name, Vector3 localPosition)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                Transform child = parent.GetChild(i);

                if (child.name == name)
                {
                    child.localPosition = localPosition;
                    child.localRotation = Quaternion.identity;
                    child.localScale = Vector3.one;
                    return child;
                }
            }

            GameObject childObject = new GameObject(name);
            Transform childTransform = childObject.transform;
            childTransform.SetParent(parent);
            childTransform.localPosition = localPosition;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = Vector3.one;
            return childTransform;
        }

        private readonly struct EncounterEnemySpec
        {
            public EncounterEnemySpec(string name, Vector3 position, Quaternion rotation, Vector3 scale)
                : this(name, position, rotation, scale, null, null)
            {
            }

            public EncounterEnemySpec(
                string name,
                Vector3 position,
                Quaternion rotation,
                Vector3 scale,
                GameObject prefabOverride,
                EnemyArchetypeSO archetypeOverride)
            {
                Name = name;
                Position = position;
                Rotation = rotation;
                Scale = scale;
                PrefabOverride = prefabOverride;
                ArchetypeOverride = archetypeOverride;
            }

            public string Name { get; }

            public Vector3 Position { get; }

            public Quaternion Rotation { get; }

            public Vector3 Scale { get; }

            public GameObject PrefabOverride { get; }

            public EnemyArchetypeSO ArchetypeOverride { get; }
        }
    }
}
