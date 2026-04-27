using System.Collections.Generic;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
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
    public static class BossTestSceneBuilder
    {
        private const string RootMenu = "CampusRPG/Setup/Build BossTest Scene";
        private const string ForceRebuildMenu = "CampusRPG/Setup/Build BossTest Scene (Force Rebuild)";
        private const string ScenePath = "Assets/_Game/Scenes/BossTest.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string EnemyMeleePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab";
        private const string InputAssetPath = "Assets/_Game/Data/Input/CampusInputActions.inputactions";
        private const string AudioSettingsPath = "Assets/_Game/Data/Audio/SO_AudioSettings.asset";
        private const string ChapterProgressionPath = "Assets/_Game/Data/Chapter/SO_Chapter01_Progression.asset";
        private const string BossEnemyArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Gatekeeper.asset";
        private const string BossTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";
        private const string MantleProbeOriginName = "MantleProbeOrigin";
        private static readonly Vector3 MantleProbeOriginLocalPosition = new Vector3(0f, 1.0f, 0.18f);

        [MenuItem(RootMenu)]
        public static void BuildBossTestScene()
        {
            if (!ConfirmOverwriteTargets())
            {
                return;
            }

            BuildBossTestSceneInternal();
        }

        [MenuItem(ForceRebuildMenu)]
        public static void ForceBuildBossTestScene()
        {
            BuildBossTestSceneInternal();
        }

        private static void BuildBossTestSceneInternal()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMeleePrefabPath);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            AudioSettingsSO audioSettings = AssetDatabase.LoadAssetAtPath<AudioSettingsSO>(AudioSettingsPath);
            ChapterProgressionSO progression = AssetDatabase.LoadAssetAtPath<ChapterProgressionSO>(ChapterProgressionPath);
            EnemyArchetypeSO bossArchetype = AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(BossEnemyArchetypePath);
            BossTelegraphStyleSO bossTelegraphStyle = AssetDatabase.LoadAssetAtPath<BossTelegraphStyleSO>(BossTelegraphStylePath);

            if (playerPrefab == null
                || enemyPrefab == null
                || inputActions == null
                || progression == null
                || bossArchetype == null)
            {
                Debug.LogError("Failed to build BossTest scene because a required player, enemy, input, progression, or boss archetype asset is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject root = new GameObject("BossTestRoot");
            GameObject arena = new GameObject("BossTestArena");
            arena.transform.SetParent(root.transform);

            BuildArenaShell(arena.transform);

            GameObject playerSpawn = new GameObject("BossTest_PlayerSpawn");
            playerSpawn.transform.SetParent(root.transform);
            playerSpawn.transform.position = new Vector3(0f, 0f, -8f);

            GameObject bossSpawn = new GameObject("BossTest_BossSpawn");
            bossSpawn.transform.SetParent(root.transform);
            bossSpawn.transform.position = new Vector3(0f, 0f, 7.5f);

            GameObject bootstrap = new GameObject("Bootstrap");
            GameBootstrap gameBootstrap = bootstrap.AddComponent<GameBootstrap>();
            InputReader inputReader = bootstrap.AddComponent<InputReader>();
            SetObjectReference(inputReader, "actionsAsset", inputActions);
            SetObjectReference(gameBootstrap, "inputReader", inputReader);
            SetBool(gameBootstrap, "keepAliveAcrossScenes", false);

            GameObject flowRoot = new GameObject("BossTestFlow");
            CheckpointRestoreCoordinator checkpointCoordinator = flowRoot.AddComponent<CheckpointRestoreCoordinator>();
            SaveService saveService = flowRoot.GetComponent<SaveService>();
            CheckpointService checkpointService = flowRoot.GetComponent<CheckpointService>();
            ChapterProgressService chapterProgressService = flowRoot.AddComponent<ChapterProgressService>();
            CombatDebugHUD debugHud = new GameObject("CombatDebugHUD").AddComponent<CombatDebugHUD>();
            BossPresentationRig bossPresentationRig = new GameObject("BossPresentationRig").AddComponent<BossPresentationRig>();
            SceneRuntimeContext sceneContext = new GameObject("SceneRuntimeContext").AddComponent<SceneRuntimeContext>();

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = playerSpawn.transform.position;
            player.transform.rotation = Quaternion.identity;

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 220f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            cameraObject.transform.position = new Vector3(0f, 3.4f, -12.5f);
            cameraObject.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

            PlayerCharacter playerCharacter = player.GetComponent<PlayerCharacter>();
            PlayerMotor playerMotor = player.GetComponent<PlayerMotor>();
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            PlayerMovementProbe movementProbe = player.GetComponent<PlayerMovementProbe>();
            Transform mantleProbeOrigin = FindOrCreateChild(player.transform, MantleProbeOriginName, MantleProbeOriginLocalPosition);

            SetObjectReference(playerCharacter, "inputReader", inputReader);
            SetObjectReference(playerCharacter, "cameraTransform", cameraObject.transform);
            SetObjectReference(playerCharacter, "movementProbe", movementProbe);
            SetObjectReference(playerCharacter, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(playerMotor, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(movementProbe, "probeOrigin", mantleProbeOrigin);
            SetObjectReference(lockOnTargetSelector, "inputReader", inputReader);
            SetObjectReference(lockOnTargetSelector, "cameraController", cameraController);
            SetObjectReference(lockOnTargetSelector, "cameraTransform", cameraObject.transform);
            SetLayerMask(lockOnTargetSelector, "targetMask", ~0);
            SetObjectReference(cameraController, "followTarget", player.transform);
            SetObjectReference(cameraController, "inputReader", inputReader);
            SetObjectReference(debugHud, "playerCharacter", playerCharacter);
            SetObjectReference(debugHud, "lockOnTargetSelector", lockOnTargetSelector);

            SetObjectReference(chapterProgressService, "progression", progression);
            SetObjectReference(chapterProgressService, "checkpointRestoreCoordinator", checkpointCoordinator);
            SetObjectReference(checkpointCoordinator, "player", playerCharacter);
            SetObjectReference(checkpointCoordinator, "saveService", saveService);
            SetObjectReference(checkpointCoordinator, "checkpointService", checkpointService);
            SetObjectReference(checkpointCoordinator, "chapterProgressService", chapterProgressService);
            SetString(checkpointCoordinator, "chapterId", progression.ChapterId);
            SetString(checkpointCoordinator, "defaultCheckpointId", "BossTest_Start");
            SetBool(checkpointCoordinator, "autoLoadFromSaveOnStart", false);
            SetBool(checkpointCoordinator, "autoSaveOnStart", true);
            SetFloat(checkpointCoordinator, "respawnDelaySeconds", 0.5f);
            SetString(saveService, "fileName", "slot_auto_boss_test.json");

            GameObject checkpoint = new GameObject("Checkpoint_BossTest_Start");
            checkpoint.transform.SetParent(root.transform);
            checkpoint.transform.position = playerSpawn.transform.position;
            BoxCollider checkpointCollider = checkpoint.AddComponent<BoxCollider>();
            checkpointCollider.isTrigger = true;
            checkpointCollider.size = new Vector3(2f, 2f, 2f);
            CheckpointRuntimeAnchor checkpointAnchor = checkpoint.AddComponent<CheckpointRuntimeAnchor>();
            SetObjectReference(checkpointAnchor, "coordinator", checkpointCoordinator);
            SetString(checkpointAnchor, "checkpointIdOverride", "BossTest_Start");

            GameObject bossArenaBarrier = CreateEncounterBarrier(
                arena.transform,
                "BossArenaBarrier",
                new Vector3(0f, 1.5f, -12.25f),
                new Vector3(8f, 3f, 1f));
            GameObject encounterRoot = CreateBossEncounter(
                arena.transform,
                enemyPrefab,
                bossArchetype,
                bossSpawn.transform.position,
                bossArenaBarrier,
                chapterProgressService,
                out EnemyBrain bossBrain,
                out EncounterController bossEncounter);

            bossPresentationRig.Configure(bossBrain, bossEncounter, bossTelegraphStyle);
            bossPresentationRig.ApplyConfiguration();

            SetObjectReference(sceneContext, "bootstrap", gameBootstrap);
            SetObjectReference(sceneContext, "inputReader", inputReader);
            SetObjectReference(sceneContext, "playerCharacter", playerCharacter);
            SetObjectReference(sceneContext, "cameraController", cameraController);
            SetObjectReference(sceneContext, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(sceneContext, "checkpointRestoreCoordinator", checkpointCoordinator);
            SetObjectReference(sceneContext, "chapterProgressService", chapterProgressService);
            SetObjectReference(sceneContext, "audioSettings", audioSettings);

            GameObject lightObject = new GameObject("Directional Light");
            Light directionalLight = lightObject.AddComponent<Light>();
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.05f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            RenderSettings.ambientIntensity = 0.78f;
            RenderSettings.reflectionIntensity = 0.72f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RebuildBossTestNavMesh(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureBossArchetypeReference(ScenePath);
            EnsureSceneContextAudioSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("BossTest scene generated as an independent public-safe Gatekeeper boss verification arena.");
        }

        private static GameObject CreateBossEncounter(
            Transform parent,
            GameObject enemyPrefab,
            EnemyArchetypeSO bossArchetype,
            Vector3 bossPosition,
            GameObject bossArenaBarrier,
            ChapterProgressService chapterProgressService,
            out EnemyBrain bossBrain,
            out EncounterController bossEncounter)
        {
            GameObject encounterRoot = new GameObject("Encounter_BossTest_Gatekeeper");
            encounterRoot.transform.SetParent(parent);
            encounterRoot.transform.position = new Vector3(0f, 1f, 0f);

            BoxCollider encounterTrigger = encounterRoot.AddComponent<BoxCollider>();
            encounterTrigger.isTrigger = true;
            encounterTrigger.size = new Vector3(17f, 3f, 14f);
            encounterTrigger.center = Vector3.zero;

            bossEncounter = encounterRoot.AddComponent<EncounterController>();
            SetString(bossEncounter, "encounterId", Chapter01Ids.Encounters.Gatekeeper);
            SetBool(bossEncounter, "activateOnPlayerEnter", true);
            SetBool(bossEncounter, "startActive", false);
            SetObjectReference(bossEncounter, "chapterProgressService", chapterProgressService);
            SetObjectArray(bossEncounter, "blockersToEnableWhileActive", new Object[] { bossArenaBarrier });

            GameObject boss = (GameObject)PrefabUtility.InstantiatePrefab(enemyPrefab);
            boss.name = "Boss_Gatekeeper";
            boss.transform.SetParent(encounterRoot.transform);
            boss.transform.position = bossPosition;
            boss.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            boss.transform.localScale = new Vector3(1.6f, 1.8f, 1.6f);

            if (PrefabUtility.IsPartOfPrefabInstance(boss))
            {
                PrefabUtility.UnpackPrefabInstance(
                    boss,
                    PrefabUnpackMode.Completely,
                    InteractionMode.AutomatedAction);
            }

            bossBrain = boss.GetComponent<EnemyBrain>();
            SetObjectReference(bossBrain, "archetype", bossArchetype);

            HealthComponent bossHealth = boss.GetComponent<HealthComponent>();
            DamageableReceiver damageableReceiver = boss.GetComponent<DamageableReceiver>();
            EnemyEncounterMember encounterMember = boss.GetComponent<EnemyEncounterMember>();

            if (encounterMember == null)
            {
                encounterMember = boss.AddComponent<EnemyEncounterMember>();
            }

            SetObjectReference(encounterMember, "ownerEncounter", bossEncounter);
            SetObjectReference(encounterMember, "enemyBrain", bossBrain);
            SetObjectReference(encounterMember, "health", bossHealth);
            SetObjectReference(damageableReceiver, "enemyBrain", bossBrain);
            SetObjectArray(bossEncounter, "members", new Object[] { encounterMember });
            return encounterRoot;
        }

        private static void BuildArenaShell(Transform parent)
        {
            CreateFloor(parent, "BossTest_Ground", new Vector3(0f, -0.5f, 0f), new Vector3(20f, 1f, 26f));
            CreateWall(parent, "BossTest_Wall_North", new Vector3(0f, 1.5f, 13.5f), new Vector3(20f, 3f, 1f));
            CreateWall(parent, "BossTest_Wall_South", new Vector3(0f, 1.5f, -13.5f), new Vector3(20f, 3f, 1f));
            CreateWall(parent, "BossTest_Wall_East", new Vector3(10.5f, 1.5f, 0f), new Vector3(1f, 3f, 26f));
            CreateWall(parent, "BossTest_Wall_West", new Vector3(-10.5f, 1.5f, 0f), new Vector3(1f, 3f, 26f));
            CreateFloor(parent, "BossTest_CenterMark", new Vector3(0f, 0.03f, 0f), new Vector3(3.2f, 0.05f, 3.2f));
        }

        private static void CreateFloor(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = name;
            floor.transform.SetParent(parent);
            floor.transform.position = position;
            floor.transform.localScale = scale;
            SetNavigationStatic(floor);
        }

        private static void CreateWall(Transform parent, string name, Vector3 position, Vector3 scale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position;
            wall.transform.localScale = scale;
            SetNavigationStatic(wall);
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

        private static bool RebuildBossTestNavMesh(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            UnityEditor.AI.NavMeshBuilder.ClearAllNavMeshes();
            UnityEditor.AI.NavMeshBuilder.BuildNavMesh();
            EditorSceneManager.MarkSceneDirty(scene);

            if (!System.IO.File.Exists(scene.path))
            {
                return false;
            }

            EditorSceneManager.SaveScene(scene, scene.path);
            string sceneYaml = System.IO.File.ReadAllText(scene.path);
            return !string.IsNullOrWhiteSpace(sceneYaml) && !sceneYaml.Contains("m_NavMeshData: {fileID: 0}");
        }

        private static void SetNavigationStatic(GameObject gameObject)
        {
            StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            GameObjectUtility.SetStaticEditorFlags(gameObject, currentFlags | StaticEditorFlags.NavigationStatic);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            if (target == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
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
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static void SetBool(Object target, string propertyName, bool value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).boolValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static void SetFloat(Object target, string propertyName, float value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).floatValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static void SetLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).intValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static void SetString(Object target, string propertyName, string value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).stringValue = value;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
            EditorUtility.SetDirty(target);
        }

        private static Transform FindOrCreateChild(Transform parent, string name, Vector3 localPosition)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                child.localPosition = localPosition;
                child.localRotation = Quaternion.identity;
                child.localScale = Vector3.one;
                return child;
            }

            GameObject childObject = new GameObject(name);
            Transform childTransform = childObject.transform;
            childTransform.SetParent(parent);
            childTransform.localPosition = localPosition;
            childTransform.localRotation = Quaternion.identity;
            childTransform.localScale = Vector3.one;
            return childTransform;
        }

        private static void EnsureSceneContextAudioSettings(string scenePath)
        {
            string audioSettingsGuid = AssetDatabase.AssetPathToGUID(AudioSettingsPath);

            if (string.IsNullOrWhiteSpace(audioSettingsGuid) || !System.IO.File.Exists(scenePath))
            {
                return;
            }

            string sceneContents = System.IO.File.ReadAllText(scenePath);
            string desiredReference = $"  audioSettings: {{fileID: 11400000, guid: {audioSettingsGuid}, type: 2}}";

            if (sceneContents.Contains(desiredReference))
            {
                return;
            }

            const string nullReference = "  audioSettings: {fileID: 0}";

            if (!sceneContents.Contains(nullReference))
            {
                return;
            }

            sceneContents = sceneContents.Replace(nullReference, desiredReference);
            System.IO.File.WriteAllText(scenePath, sceneContents);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
        }

        private static void EnsureBossArchetypeReference(string scenePath)
        {
            string bossArchetypeGuid = AssetDatabase.AssetPathToGUID(BossEnemyArchetypePath);

            if (string.IsNullOrWhiteSpace(bossArchetypeGuid) || !System.IO.File.Exists(scenePath))
            {
                return;
            }

            string sceneContents = System.IO.File.ReadAllText(scenePath);
            string desiredReference = $"  archetype: {{fileID: 11400000, guid: {bossArchetypeGuid}, type: 2}}";

            if (sceneContents.Contains(desiredReference))
            {
                return;
            }

            const string nullReference = "  archetype: {fileID: 0}";

            if (!sceneContents.Contains(nullReference))
            {
                return;
            }

            sceneContents = sceneContents.Replace(nullReference, desiredReference);
            System.IO.File.WriteAllText(scenePath, sceneContents);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
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
                "This action will overwrite the existing BossTest scene:\n\n- " +
                string.Join("\n- ", existingTargets) +
                "\n\nCancel if you have hand-tuned BossTest scene edits you want to keep.";

            return EditorUtility.DisplayDialog(
                "Rebuild BossTest Scene?",
                message,
                "Rebuild",
                "Cancel");
        }

        private static List<string> CollectExistingTargets()
        {
            List<string> results = new List<string>();

            if (AssetDatabase.LoadMainAssetAtPath(ScenePath) != null)
            {
                results.Add(ScenePath);
            }

            return results;
        }
    }
}
