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
        private const string ScenePath = "Assets/_Game/Scenes/Chapter01_Combined.unity";
        private const string ChapterProgressionPath = "Assets/_Game/Data/Chapter/SO_Chapter01_Progression.asset";
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

        private static void BuildChapter01CombinedSceneInternal()
        {
            CombatTestSceneBuilder.EnsureCombatTestContent();
            EnsureFolder("Assets/_Game/Data");
            EnsureFolder("Assets/_Game/Data/Chapter");
            EnsureFolder("Assets/_Game/Scenes");

            ChapterProgressionSO chapterProgression = CreateOrLoadChapterProgressionAsset();
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
                || chapterProgression == null)
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
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();

            SetObjectReference(playerCharacter, "inputReader", inputReader);
            SetObjectReference(playerCharacter, "cameraTransform", cameraObject.transform);
            SetObjectReference(lockOnTargetSelector, "inputReader", inputReader);
            SetObjectReference(lockOnTargetSelector, "cameraController", cameraController);
            SetObjectReference(lockOnTargetSelector, "cameraTransform", cameraObject.transform);
            SetLayerMask(lockOnTargetSelector, "targetMask", ~0);
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("Chapter01 combined scene graybox and progression asset were generated.");
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
            CreateWall(parent, "Wall_Back", center + new Vector3(0f, 1.5f, -10.5f), new Vector3(18f, 3f, 1f));
            CreateWall(parent, "Wall_Front", center + new Vector3(0f, 1.5f, 10.5f), new Vector3(18f, 3f, 1f));
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
                ChapterProgressionPath
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
