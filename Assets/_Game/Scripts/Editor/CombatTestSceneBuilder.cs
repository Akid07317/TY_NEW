using System.Collections.Generic;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Input;
using CampusRPG.Multiplayer;
using CampusRPG.Save;
using CampusRPG.Skills;
using CampusRPG.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.AI;

namespace CampusRPG.Editor
{
    public static class CombatTestSceneBuilder
    {
        private const string RootMenu = "CampusRPG/Setup/Build CombatTest Scene";
        private const string ForceRebuildMenu = "CampusRPG/Setup/Build CombatTest Scene (Force Rebuild)";
        private const string RepairPrefabMenu = "CampusRPG/Setup/Repair CombatTest Prefab Wiring";
        private const string RepairEnemyPrefabMenu = "CampusRPG/Setup/Repair CombatTest Enemy Prefab Wiring";
        private const string RepairSceneNavMeshMenu = "CampusRPG/Setup/Repair CombatTest Scene NavMesh";
        private const string RepairSceneLightingMenu = "CampusRPG/Setup/Repair CombatTest Scene Lighting";
        private const string ApplyImportedVisualMenu = "CampusRPG/Setup/Local Preview/Apply Imported Player Visuals To CombatTest Player Prefab";
        private const string ApplyImportedEnemyVisualMenu = "CampusRPG/Setup/Local Preview/Apply Imported Enemy Avatar Chain To CombatTest Enemy Prefabs";
        private const string RefreshScenePrefabInstancesMenu = "CampusRPG/Setup/Local Preview/Refresh CombatTest Scene Prefab Instances From Sources";
        private const string ScenePath = "Assets/_Game/Scenes/CombatTest.unity";
        private const string PlayerPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Player_CombatTest.prefab";
        private const string EnemyMeleePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Melee_CombatTest.prefab";
        private const string EnemyMobilePrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Mobile_CombatTest.prefab";
        private const string EnemyRangedPrefabPath = "Assets/_Game/Prefabs/Characters/PF_Enemy_Ranged_CombatTest.prefab";
        private const string InputAssetPath = "Assets/_Game/Data/Input/CampusInputActions.inputactions";
        private const string AudioSettingsPath = "Assets/_Game/Data/Audio/SO_AudioSettings.asset";
        private const string PlayerStatsPath = "Assets/_Game/Data/Characters/SO_PlayerBaseStats.asset";
        private const string CombatBalancePath = "Assets/_Game/Data/Combat/SO_CombatBalance.asset";
        private const string Light01Path = "Assets/_Game/Data/Combat/SO_Attack_Light_01.asset";
        private const string Light02Path = "Assets/_Game/Data/Combat/SO_Attack_Light_02.asset";
        private const string Light03Path = "Assets/_Game/Data/Combat/SO_Attack_Light_03.asset";
        private const float CombatTestDirectionalLightIntensity = 0.85f;
        private const float CombatTestAmbientIntensity = 0.7f;
        private const float CombatTestReflectionIntensity = 0.75f;
        private const float PlayerAnimationCrossFadeSeconds = 0.035f;
        private const float PlayerAnimationLocomotionDampSeconds = 0.05f;
        private const float PlayerHitStunSeconds = 0.08f;
        private const float EnemyAnimationCrossFadeSeconds = 0.05f;
        private const float EnemyAnimationLocomotionDampSeconds = 0.04f;
        private const float EnemyCapsuleRadius = 0.5f;
        private const float EnemyCapsuleHeight = 2f;
        private const float EnemyCapsuleCenterY = 1f;
        private const float EnemyAgentBaseOffset = 0f;
        private const float EnemyAttackRangePadding = 0.08f;
        private const float EnemyAttackMaxHitAngle = 45f;
        private static readonly string[] LocalPreviewOnlySourceRoots =
        {
            "Assets/GhostSamurai_Animset/",
            "Assets/Kevin Iglesias/",
            "Assets/DoubleL/",
            "Assets/ithappy/",
            "Assets/JC_LP_MedievalCharacters_LITE/",
            "Assets/Free medieval weapons/",
            "Assets/MYFG-Weapon Pack Lite/",
            "Assets/Polytope Studio/"
        };
        private const string HeavyPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string DodgeFollowUpPath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp.asset";
        private const string CounterPath = "Assets/_Game/Data/Combat/SO_Attack_Counter.asset";
        private const string EnhancedCounterPath = "Assets/_Game/Data/Combat/SO_Attack_Counter_Enhanced.asset";
        private const string EnhancedDodgePath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp_Enhanced.asset";
        private const string EnemyMeleeArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Melee.asset";
        private const string EnemyMobileArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Mobile.asset";
        private const string EnemyRangedArchetypePath = "Assets/_Game/Data/Enemies/SO_Enemy_Ranged.asset";
        private const string Skill1Path = "Assets/_Game/Data/Skills/SO_Skill_SpellBolt.asset";
        private const string Skill2Path = "Assets/_Game/Data/Skills/SO_Skill_ForceBurst.asset";
        private const string MantleProbeOriginName = "MantleProbeOrigin";
        private static readonly Vector3 MantleProbeOriginLocalPosition = new Vector3(0f, 1.0f, 0.18f);

        [MenuItem(RootMenu)]
        public static void BuildCombatTestScene()
        {
            if (!ConfirmOverwriteTargets())
            {
                return;
            }

            BuildCombatTestSceneInternal();
        }

        [MenuItem(ForceRebuildMenu)]
        public static void ForceBuildCombatTestScene()
        {
            BuildCombatTestSceneInternal();
        }

        [MenuItem(RepairPrefabMenu)]
        public static void RepairCombatTestPrefabWiring()
        {
            RuntimeAnimatorController playerAnimatorController = CombatTestAssetGenerator.EnsurePlayerCombatAnimationAssets();
            bool repairedAnyPrefab = false;
            repairedAnyPrefab |= RepairPlayerPrefab(PlayerPrefabPath, playerAnimatorController);
            repairedAnyPrefab |= RepairCombatTestEnemyPrefabs();

            if (!repairedAnyPrefab)
            {
                Debug.LogWarning("CombatTest prefab repair skipped because no target prefabs were found.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatTest prefab wiring repaired: duplicate required components removed, player animation relay reconnected, and the player/enemy proxy baselines were restored.");
        }

        [MenuItem(RepairEnemyPrefabMenu)]
        public static void RepairCombatTestEnemyPrefabWiring()
        {
            bool repairedAnyPrefab = RepairCombatTestEnemyPrefabs();

            if (!repairedAnyPrefab)
            {
                Debug.LogWarning("CombatTest enemy prefab repair skipped because no target prefabs were found.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatTest enemy prefab wiring repaired: proxy baseline restored and enemy physical footing aligned.");
        }

        [MenuItem(RepairSceneNavMeshMenu)]
        public static void RepairCombatTestSceneNavMesh()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogWarning("CombatTest scene NavMesh repair skipped because the scene asset does not exist yet.");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            bool built = RebuildCombatTestSceneNavMesh(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);

            Debug.Log(
                built
                    ? "CombatTest scene NavMesh rebuilt and saved."
                    : "CombatTest scene NavMesh repair completed, but no baked NavMesh data was produced.");
        }

        [MenuItem(RepairSceneLightingMenu)]
        public static void RepairCombatTestSceneLighting()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogWarning("CombatTest scene lighting repair skipped because the scene asset does not exist yet.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();

            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            GameObject lightObject = GameObject.Find("Directional Light");

            if (lightObject == null)
            {
                lightObject = new GameObject("Directional Light");
                lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            }

            Light directionalLight = lightObject.GetComponent<Light>();

            if (directionalLight == null)
            {
                directionalLight = lightObject.AddComponent<Light>();
            }

            ApplyCombatTestLightingPreset(directionalLight);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            Debug.Log("CombatTest scene lighting repaired for readable local preview playback.");
        }

        [MenuItem(ApplyImportedVisualMenu)]
        public static void ApplyImportedVisualsToCombatTestPlayerPrefab()
        {
            if (!CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview)
            {
                Debug.LogWarning(
                    "Imported player preview is disabled. Enable 'CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available' before applying local preview visuals.");
                return;
            }

            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogWarning("CombatTest player prefab does not exist yet.");
                return;
            }

            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                PlayerCharacter playerCharacter = GetOrAddComponent<PlayerCharacter>(player);
                PlayerCombatController playerCombatController = GetOrAddComponent<PlayerCombatController>(player);
                PlayerStateMachine playerStateMachine = GetOrAddComponent<PlayerStateMachine>(player);
                PlayerMotor playerMotor = GetOrAddComponent<PlayerMotor>(player);
                Animator animator = GetOrAddComponent<Animator>(player);
                PlayerCombatAnimationRelay animationRelay = GetOrAddComponent<PlayerCombatAnimationRelay>(player);
                DamageableReceiver damageableReceiver = GetOrAddComponent<DamageableReceiver>(player);
                RuntimeAnimatorController playerAnimatorController = CombatTestAssetGenerator.EnsurePlayerCombatAnimationAssetsForLocalPreview();

                if (playerAnimatorController == null)
                {
                    return;
                }

                if (!CombatImportedPlayerVisualUtility.TryApply(player, animator))
                {
                    Debug.LogWarning("No imported player visual source was found. Install a supported player package first.");
                    return;
                }

                ConfigurePlayerAnimator(animator, playerAnimatorController);
                CombatProxyVisualUtility.Apply(player, CombatProxyVisualKind.Player);
                ConfigurePlayerCombatAnimationRelay(
                    animationRelay,
                    playerCharacter,
                    playerCombatController,
                    playerStateMachine,
                    playerMotor,
                    animator);
                SetFloat(damageableReceiver, "playerHitStunSeconds", PlayerHitStunSeconds);
                ConfigurePlayerWeaponPresentation(animationRelay, player, syncImportedWeaponPreview: true);

                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied imported player visuals to PF_Player_CombatTest for local preview. Repair/Build paths will restore the proxy baseline.");
        }

        [MenuItem(ApplyImportedEnemyVisualMenu)]
        public static void ApplyImportedEnemyAvatarChainToCombatTestEnemyPrefabs()
        {
            bool foundAnyPrefab = false;
            bool appliedAnyVisual = false;

            appliedAnyVisual |= TryApplyImportedEnemyAvatarPreview(
                EnemyMeleePrefabPath,
                CombatProxyVisualKind.EnemyMelee,
                ref foundAnyPrefab);
            appliedAnyVisual |= TryApplyImportedEnemyAvatarPreview(
                EnemyMobilePrefabPath,
                CombatProxyVisualKind.EnemyMobile,
                ref foundAnyPrefab);
            appliedAnyVisual |= TryApplyImportedEnemyAvatarPreview(
                EnemyRangedPrefabPath,
                CombatProxyVisualKind.EnemyRanged,
                ref foundAnyPrefab);

            if (!foundAnyPrefab)
            {
                Debug.LogWarning("CombatTest enemy prefabs do not exist yet.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (!appliedAnyVisual)
            {
                Debug.LogWarning("No compatible imported enemy humanoid source was found. Enemy local preview remains on the proxy baseline.");
                return;
            }

            Debug.Log("Applied imported enemy Avatar chain to CombatTest enemy prefabs for local preview. Standard Build/Repair paths will restore the proxy baseline.");
        }

        [MenuItem(RefreshScenePrefabInstancesMenu)]
        public static void RefreshCombatTestScenePrefabInstancesFromSources()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            {
                Debug.LogWarning("CombatTest scene prefab instance refresh skipped because the scene asset does not exist yet.");
                return;
            }

            Scene scene = EditorSceneManager.GetActiveScene();

            if (!scene.IsValid() || scene.path != ScenePath)
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }

            bool refreshedAnyInstance = false;
            refreshedAnyInstance |= RefreshScenePrefabInstanceFromSource("Player", PlayerPrefabPath);
            refreshedAnyInstance |= RefreshScenePrefabInstanceFromSource("Enemy_Melee_A", EnemyMeleePrefabPath);
            refreshedAnyInstance |= RefreshScenePrefabInstanceFromSource("Enemy_Mobile_A", EnemyMobilePrefabPath);
            refreshedAnyInstance |= RefreshScenePrefabInstanceFromSource("Enemy_Ranged_A", EnemyRangedPrefabPath);

            if (!refreshedAnyInstance)
            {
                Debug.LogWarning("CombatTest scene prefab instance refresh found no matching player or enemy prefab instances.");
                return;
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatTest scene prefab instances refreshed from their source prefabs while preserving scene names and transforms.");
        }

        public static void EnsureCombatTestContent()
        {
            CombatTestAssetGenerator.CreateCombatTestAssets();
            EnsureFolder("Assets/_Game/Prefabs");
            EnsureFolder("Assets/_Game/Prefabs/Characters");
            BuildPlayerPrefab();
            BuildEnemyPrefab(EnemyMeleePrefabPath, "PF_Enemy_Melee_CombatTest", EnemyMeleeArchetypePath, 1.6f);
            BuildEnemyPrefab(EnemyMobilePrefabPath, "PF_Enemy_Mobile_CombatTest", EnemyMobileArchetypePath, 1.35f);
            BuildEnemyPrefab(EnemyRangedPrefabPath, "PF_Enemy_Ranged_CombatTest", EnemyRangedArchetypePath, 2.6f);
        }

        private static void BuildCombatTestSceneInternal()
        {
            EnsureCombatTestContent();

            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject meleeEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMeleePrefabPath);
            GameObject mobileEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyMobilePrefabPath);
            GameObject rangedEnemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EnemyRangedPrefabPath);
            InputActionAsset inputActions = AssetDatabase.LoadAssetAtPath<InputActionAsset>(InputAssetPath);
            if (playerPrefab == null || meleeEnemyPrefab == null || mobileEnemyPrefab == null || rangedEnemyPrefab == null)
            {
                Debug.LogError("Failed to build CombatTest scene because a required prefab asset is missing.");
                return;
            }

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            GameObject bootstrap = new GameObject("Bootstrap");
            GameBootstrap gameBootstrap = bootstrap.AddComponent<GameBootstrap>();
            InputReader inputReader = bootstrap.AddComponent<InputReader>();
            SetObjectReference(inputReader, "actionsAsset", inputActions);
            SetObjectReference(gameBootstrap, "inputReader", inputReader);
            SetBool(gameBootstrap, "keepAliveAcrossScenes", false);

            GameObject lightObject = new GameObject("Directional Light");
            Light directionalLight = lightObject.AddComponent<Light>();
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);
            ApplyCombatTestLightingPreset(directionalLight);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f);
            SetCombatTestNavigationStatic(ground);

            CreateWall("NorthWall", new Vector3(0f, 1.5f, 18f), new Vector3(24f, 3f, 1f));
            CreateWall("SouthWall", new Vector3(0f, 1.5f, -12f), new Vector3(24f, 3f, 1f));
            CreateWall("EastWall", new Vector3(12f, 1.5f, 3f), new Vector3(1f, 3f, 30f));
            CreateWall("WestWall", new Vector3(-12f, 1.5f, 3f), new Vector3(1f, 3f, 30f));

            GameObject playerSpawn = new GameObject("PlayerSpawn");
            playerSpawn.transform.position = new Vector3(0f, 0f, -2f);

            GameObject meleeEnemySpawn = new GameObject("EnemySpawn_Melee");
            meleeEnemySpawn.transform.position = new Vector3(0f, 0f, 8f);
            GameObject mobileEnemySpawn = new GameObject("EnemySpawn_Mobile");
            mobileEnemySpawn.transform.position = new Vector3(-4.5f, 0f, 10f);
            GameObject rangedEnemySpawn = new GameObject("EnemySpawn_Ranged");
            rangedEnemySpawn.transform.position = new Vector3(4.5f, 0f, 12f);

            GameObject player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.name = "Player";
            player.transform.position = playerSpawn.transform.position;
            player.transform.rotation = Quaternion.identity;

            InstantiateCombatEnemy(meleeEnemyPrefab, "Enemy_Melee_A", meleeEnemySpawn.transform.position);
            InstantiateCombatEnemy(mobileEnemyPrefab, "Enemy_Mobile_A", mobileEnemySpawn.transform.position);
            InstantiateCombatEnemy(rangedEnemyPrefab, "Enemy_Ranged_A", rangedEnemySpawn.transform.position);

            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            UnityEngine.Camera camera = cameraObject.AddComponent<UnityEngine.Camera>();
            camera.nearClipPlane = 0.1f;
            camera.farClipPlane = 200f;
            cameraObject.AddComponent<AudioListener>();
            ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();
            cameraObject.transform.position = new Vector3(0f, 3.2f, -6f);
            cameraObject.transform.rotation = Quaternion.Euler(14f, 0f, 0f);

            PlayerCharacter playerCharacter = player.GetComponent<PlayerCharacter>();
            LockOnTargetSelector lockOnTargetSelector = player.GetComponent<LockOnTargetSelector>();
            CombatDebugHUD debugHud = new GameObject("CombatDebugHUD").AddComponent<CombatDebugHUD>();
            SwordArtHudPresenter swordArtHud = new GameObject("SwordArtHUD").AddComponent<SwordArtHudPresenter>();
            GameObject checkpointFlow = new GameObject("CheckpointFlow");
            CheckpointRestoreCoordinator checkpointCoordinator = checkpointFlow.AddComponent<CheckpointRestoreCoordinator>();
            SaveService saveService = checkpointFlow.GetComponent<SaveService>();
            CheckpointService checkpointService = checkpointFlow.GetComponent<CheckpointService>();
            SceneRuntimeContext sceneContext = new GameObject("SceneRuntimeContext").AddComponent<SceneRuntimeContext>();

            GameObject checkpoint = new GameObject("Checkpoint_CP01");
            checkpoint.transform.position = playerSpawn.transform.position;
            BoxCollider checkpointCollider = checkpoint.AddComponent<BoxCollider>();
            checkpointCollider.isTrigger = true;
            CheckpointRuntimeAnchor checkpointAnchor = checkpoint.AddComponent<CheckpointRuntimeAnchor>();

            SetObjectReference(playerCharacter, "inputReader", inputReader);
            SetObjectReference(playerCharacter, "cameraTransform", cameraObject.transform);
            SetObjectReference(lockOnTargetSelector, "inputReader", inputReader);
            SetObjectReference(lockOnTargetSelector, "cameraController", cameraController);
            SetObjectReference(lockOnTargetSelector, "cameraTransform", cameraObject.transform);
            SetLayerMask(lockOnTargetSelector, "targetMask", ~0);
            SetObjectReference(cameraController, "followTarget", player.transform);
            SetObjectReference(cameraController, "inputReader", inputReader);
            SetObjectReference(debugHud, "playerCharacter", playerCharacter);
            SetObjectReference(debugHud, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(debugHud, "swordArtHudPresenter", swordArtHud);
            SetObjectReference(swordArtHud, "playerCharacter", playerCharacter);
            SetObjectReference(checkpointCoordinator, "player", playerCharacter);
            SetObjectReference(checkpointCoordinator, "saveService", saveService);
            SetObjectReference(checkpointCoordinator, "checkpointService", checkpointService);
            SetObjectReference(sceneContext, "bootstrap", gameBootstrap);
            SetObjectReference(sceneContext, "inputReader", inputReader);
            SetObjectReference(sceneContext, "playerCharacter", playerCharacter);
            SetObjectReference(sceneContext, "cameraController", cameraController);
            SetObjectReference(sceneContext, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(sceneContext, "checkpointRestoreCoordinator", checkpointCoordinator);
            SetString(checkpointCoordinator, "chapterId", "CombatTest");
            SetString(checkpointCoordinator, "defaultCheckpointId", "CP01");
            SetBool(checkpointCoordinator, "autoLoadFromSaveOnStart", false);
            SetBool(checkpointCoordinator, "autoSaveOnStart", true);
            SetFloat(checkpointCoordinator, "respawnDelaySeconds", 0.5f);
            SetString(saveService, "fileName", "slot_auto_combat_test.json");
            SetObjectReference(checkpointAnchor, "coordinator", checkpointCoordinator);
            SetString(checkpointAnchor, "checkpointIdOverride", "CP01");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            RebuildCombatTestSceneNavMesh(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            EnsureSceneContextAudioSettings(ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log(
                "CombatTest scene, three enemy prefabs, and debug HUD were generated. " +
                "Existing outputs at Assets/_Game/Scenes/CombatTest.unity and Assets/_Game/Prefabs/Characters may have been overwritten.");
        }

        private static GameObject BuildPlayerPrefab()
        {
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "PF_Player_CombatTest";
            Object.DestroyImmediate(player.GetComponent<Collider>());

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 1.8f;
            characterController.radius = 0.35f;
            characterController.center = new Vector3(0f, 0.9f, 0f);

            PlayerCharacter playerCharacter = player.AddComponent<PlayerCharacter>();
            PlayerMotor playerMotor = player.AddComponent<PlayerMotor>();
            PlayerMovementProbe playerMovementProbe = player.AddComponent<PlayerMovementProbe>();
            PlayerStateMachine playerStateMachine = player.AddComponent<PlayerStateMachine>();
            NetworkPlayerDeathStateBridge deathStateBridge = player.AddComponent<NetworkPlayerDeathStateBridge>();
            PlayerCombatController playerCombatController = player.AddComponent<PlayerCombatController>();
            SkillController skillController = player.AddComponent<SkillController>();
            LockOnTargetSelector lockOnTargetSelector = player.AddComponent<LockOnTargetSelector>();
            Animator animator = GetOrAddComponent<Animator>(player);
            PlayerCombatAnimationRelay animationRelay = player.AddComponent<PlayerCombatAnimationRelay>();
            AttackExecutor attackExecutor = GetOrAddComponent<AttackExecutor>(player);
            HitboxController hitboxController = GetOrAddComponent<HitboxController>(player);
            DamageableReceiver damageableReceiver = GetOrAddComponent<DamageableReceiver>(player);
            HealthComponent health = player.AddComponent<HealthComponent>();
            ManaComponent mana = player.AddComponent<ManaComponent>();
            GaugeComponent gauge = player.AddComponent<GaugeComponent>();
            RuntimeAnimatorController playerAnimatorController = CombatTestAssetGenerator.EnsurePlayerCombatAnimationAssets();

            Transform attackOrigin = CreateChild(player.transform, "AttackOrigin", new Vector3(0f, 1f, 0.9f));
            Transform castOrigin = CreateChild(player.transform, "CastOrigin", new Vector3(0f, 1.1f, 0.7f));
            Transform mantleProbeOrigin = CreateChild(player.transform, MantleProbeOriginName, MantleProbeOriginLocalPosition);

            SetObjectReference(playerCharacter, "baseStats", AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>(PlayerStatsPath));
            SetObjectReference(playerCharacter, "motor", playerMotor);
            SetObjectReference(playerCharacter, "stateMachine", playerStateMachine);
            SetObjectReference(playerCharacter, "combatController", playerCombatController);
            SetObjectReference(playerCharacter, "skillController", skillController);
            SetObjectReference(playerCharacter, "movementProbe", playerMovementProbe);
            SetObjectReference(playerCharacter, "health", health);
            SetObjectReference(playerCharacter, "mana", mana);
            SetObjectReference(playerCharacter, "gauges", gauge);
            SetObjectReference(playerCharacter, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(playerMotor, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(playerMovementProbe, "probeOrigin", mantleProbeOrigin);
            SetObjectReference(deathStateBridge, "health", health);
            SetObjectReference(deathStateBridge, "stateMachine", playerStateMachine);

            SetObjectReference(playerCombatController, "balance", AssetDatabase.LoadAssetAtPath<CombatBalanceSO>(CombatBalancePath));
            SetObjectReference(playerCombatController, "attackExecutor", attackExecutor);
            SetObjectReference(playerCombatController, "hitboxController", hitboxController);
            SetObjectReference(playerCombatController, "animationRelay", animationRelay);
            SetObjectReference(playerCombatController, "heavyAttack", AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyPath));
            SetObjectReference(playerCombatController, "dodgeFollowUpAttack", AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(DodgeFollowUpPath));
            SetObjectReference(playerCombatController, "empoweredDodgeFollowUpAttack", AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedDodgePath));
            SetObjectReference(playerCombatController, "counterAttack", AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CounterPath));
            SetObjectReference(playerCombatController, "empoweredCounterAttack", AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedCounterPath));
            SetAttackCombo(
                playerCombatController,
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light01Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light02Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light03Path));
            SetSwordArts(playerCombatController, CombatTestAssetGenerator.EnsureCombatTestSwordArtAssets());
            SetBool(playerCombatController, "prototypeGrantDodgeFollowUpOnAnyDodge", false);

            SetObjectReference(attackExecutor, "attackOrigin", attackOrigin);
            SetLayerMask(attackExecutor, "targetMask", ~0);
            SetObjectReference(hitboxController, "attackExecutor", attackExecutor);
            ConfigurePlayerCombatAnimationRelay(animationRelay, playerCharacter, playerCombatController, playerStateMachine, playerMotor, animator);
            ConfigurePlayerAnimator(animator, playerAnimatorController);

            SetObjectReference(skillController, "owner", playerCharacter);
            SetObjectReference(skillController, "mana", mana);
            SetObjectReference(skillController, "attackExecutor", attackExecutor);
            SetObjectReference(skillController, "lockOnTargetSelector", lockOnTargetSelector);
            SetObjectReference(skillController, "castOrigin", castOrigin);
            SetObjectReference(skillController, "skill1", AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(Skill1Path));
            SetObjectReference(skillController, "skill2", AssetDatabase.LoadAssetAtPath<SkillDefinitionSO>(Skill2Path));

            SetObjectReference(damageableReceiver, "health", health);
            SetObjectReference(damageableReceiver, "playerCharacter", playerCharacter);
            SetFloat(damageableReceiver, "playerHitStunSeconds", PlayerHitStunSeconds);
            RestorePlayerVisualBaseline(player, animator);
            ConfigurePlayerWeaponPresentation(animationRelay, player, syncImportedWeaponPreview: false);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            Object.DestroyImmediate(player);
            return prefab;
        }

        private static GameObject BuildEnemyPrefab(
            string prefabPath,
            string prefabName,
            string archetypePath,
            float stoppingDistance)
        {
            GameObject enemy = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemy.name = prefabName;

            CapsuleCollider capsuleCollider = enemy.GetComponent<CapsuleCollider>();
            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.angularSpeed = 720f;
            agent.acceleration = 30f;
            agent.stoppingDistance = stoppingDistance;
            ConfigureEnemyPhysicalFooting(capsuleCollider, agent);

            EnemyBrain enemyBrain = enemy.AddComponent<EnemyBrain>();
            EnemyStateMachine enemyStateMachine = enemy.AddComponent<EnemyStateMachine>();
            EnemySensing enemySensing = enemy.AddComponent<EnemySensing>();
            EnemyMotor enemyMotor = enemy.AddComponent<EnemyMotor>();
            EnemyAttackController enemyAttackController = enemy.AddComponent<EnemyAttackController>();
            EnemyVisualPresentationRelay enemyVisualPresentationRelay = enemy.AddComponent<EnemyVisualPresentationRelay>();
            DamageableReceiver damageableReceiver = GetOrAddComponent<DamageableReceiver>(enemy);
            HealthComponent health = enemy.AddComponent<HealthComponent>();
            LockOnTarget lockOnTarget = enemy.AddComponent<LockOnTarget>();

            Transform attackOrigin = CreateChild(enemy.transform, "AttackOrigin", new Vector3(0f, 1f, 0.9f));
            Transform lockPoint = CreateChild(enemy.transform, "LockPoint", new Vector3(0f, 1.2f, 0f));

            SetObjectReference(enemyBrain, "archetype", AssetDatabase.LoadAssetAtPath<EnemyArchetypeSO>(archetypePath));
            SetObjectReference(enemyBrain, "stateMachine", enemyStateMachine);
            SetObjectReference(enemyBrain, "sensing", enemySensing);
            SetObjectReference(enemyBrain, "motor", enemyMotor);
            SetObjectReference(enemyBrain, "attackController", enemyAttackController);
            SetObjectReference(enemyBrain, "health", health);

            SetObjectReference(enemyAttackController, "attackOrigin", attackOrigin);
            SetFloat(enemyAttackController, "rangePadding", EnemyAttackRangePadding);
            SetFloat(enemyAttackController, "maxHitAngle", EnemyAttackMaxHitAngle);
            SetObjectReference(damageableReceiver, "health", health);
            SetObjectReference(damageableReceiver, "enemyBrain", enemyBrain);
            SetObjectReference(lockOnTarget, "targetTransform", lockPoint);
            SetLayerMask(enemySensing, "targetMask", ~0);
            RestoreEnemyVisualBaseline(enemy);
            CombatProxyVisualUtility.Apply(enemy, ResolveEnemyVisualKind(prefabPath));
            ConfigureEnemyVisualPresentationRelay(enemy, enemyVisualPresentationRelay, enemyBrain, enemyStateMachine);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
            Object.DestroyImmediate(enemy);
            return prefab;
        }

        private static bool RepairPlayerPrefab(string prefabPath, RuntimeAnimatorController playerAnimatorController)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return false;
            }

            GameObject player = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                bool changed = false;
                PlayerCharacter playerCharacter = EnsureSingleComponent<PlayerCharacter>(player, ref changed);
                PlayerMotor playerMotor = EnsureSingleComponent<PlayerMotor>(player, ref changed);
                PlayerStateMachine playerStateMachine = EnsureSingleComponent<PlayerStateMachine>(player, ref changed);
                NetworkPlayerDeathStateBridge deathStateBridge = EnsureSingleComponent<NetworkPlayerDeathStateBridge>(player, ref changed);
                PlayerCombatController playerCombatController = EnsureSingleComponent<PlayerCombatController>(player, ref changed);
                SkillController skillController = EnsureSingleComponent<SkillController>(player, ref changed);
                LockOnTargetSelector lockOnTargetSelector = EnsureSingleComponent<LockOnTargetSelector>(player, ref changed);
                PlayerMovementProbe playerMovementProbe = EnsureSingleComponent<PlayerMovementProbe>(player, ref changed);
                Animator animator = EnsureSingleComponent<Animator>(player, ref changed);
                PlayerCombatAnimationRelay animationRelay = EnsureSingleComponent<PlayerCombatAnimationRelay>(player, ref changed);
                AttackExecutor attackExecutor = EnsureSingleComponent<AttackExecutor>(player, ref changed);
                HitboxController hitboxController = EnsureSingleComponent<HitboxController>(player, ref changed);
                DamageableReceiver damageableReceiver = EnsureSingleComponent<DamageableReceiver>(player, ref changed);
                HealthComponent health = EnsureSingleComponent<HealthComponent>(player, ref changed);
                ManaComponent mana = EnsureSingleComponent<ManaComponent>(player, ref changed);
                GaugeComponent gauge = EnsureSingleComponent<GaugeComponent>(player, ref changed);
                Transform attackOrigin = FindOrCreateChild(player.transform, "AttackOrigin", new Vector3(0f, 1f, 0.9f), ref changed);
                Transform castOrigin = FindOrCreateChild(player.transform, "CastOrigin", new Vector3(0f, 1.1f, 0.7f), ref changed);
                Transform mantleProbeOrigin = FindOrCreateChild(player.transform, MantleProbeOriginName, MantleProbeOriginLocalPosition, ref changed);

                SetObjectReference(playerCharacter, "motor", playerMotor);
                SetObjectReference(playerCharacter, "stateMachine", playerStateMachine);
                SetObjectReference(playerCharacter, "combatController", playerCombatController);
                SetObjectReference(playerCharacter, "skillController", skillController);
                SetObjectReference(playerCharacter, "movementProbe", playerMovementProbe);
                SetObjectReference(playerCharacter, "health", health);
                SetObjectReference(playerCharacter, "mana", mana);
                SetObjectReference(playerCharacter, "gauges", gauge);
                SetObjectReference(playerCharacter, "lockOnTargetSelector", lockOnTargetSelector);
                SetObjectReference(playerMotor, "lockOnTargetSelector", lockOnTargetSelector);
                SetObjectReference(playerMovementProbe, "probeOrigin", mantleProbeOrigin);
                SetObjectReference(deathStateBridge, "health", health);
                SetObjectReference(deathStateBridge, "stateMachine", playerStateMachine);

                SetObjectReference(playerCombatController, "attackExecutor", attackExecutor);
                SetObjectReference(playerCombatController, "hitboxController", hitboxController);
                SetObjectReference(playerCombatController, "animationRelay", animationRelay);
                SetSwordArts(playerCombatController, CombatTestAssetGenerator.EnsureCombatTestSwordArtAssets());

                ConfigurePlayerCombatAnimationRelay(animationRelay, playerCharacter, playerCombatController, playerStateMachine, playerMotor, animator);
                SetObjectReference(attackExecutor, "attackOrigin", attackOrigin);
                SetObjectReference(hitboxController, "attackExecutor", attackExecutor);
                SetObjectReference(skillController, "owner", playerCharacter);
                SetObjectReference(skillController, "mana", mana);
                SetObjectReference(skillController, "attackExecutor", attackExecutor);
                SetObjectReference(skillController, "lockOnTargetSelector", lockOnTargetSelector);
                SetObjectReference(skillController, "castOrigin", castOrigin);
                SetObjectReference(damageableReceiver, "health", health);
                SetObjectReference(damageableReceiver, "playerCharacter", playerCharacter);
                SetFloat(damageableReceiver, "playerHitStunSeconds", PlayerHitStunSeconds);
                ConfigurePlayerAnimator(animator, playerAnimatorController);
                changed |= RestorePlayerVisualBaseline(player, animator);
                ConfigurePlayerWeaponPresentation(animationRelay, player, syncImportedWeaponPreview: false);

                PrefabUtility.SaveAsPrefabAsset(player, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }
        }

        private static bool RepairEnemyPrefab(string prefabPath)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return false;
            }

            GameObject enemy = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                bool changed = false;
                CapsuleCollider capsuleCollider = EnsureSingleComponent<CapsuleCollider>(enemy, ref changed);
                NavMeshAgent agent = EnsureSingleComponent<NavMeshAgent>(enemy, ref changed);
                EnemyBrain enemyBrain = EnsureSingleComponent<EnemyBrain>(enemy, ref changed);
                EnemyStateMachine enemyStateMachine = EnsureSingleComponent<EnemyStateMachine>(enemy, ref changed);
                EnemySensing enemySensing = EnsureSingleComponent<EnemySensing>(enemy, ref changed);
                EnemyMotor enemyMotor = EnsureSingleComponent<EnemyMotor>(enemy, ref changed);
                EnemyAttackController enemyAttackController = EnsureSingleComponent<EnemyAttackController>(enemy, ref changed);
                EnemyVisualPresentationRelay enemyVisualPresentationRelay = EnsureSingleComponent<EnemyVisualPresentationRelay>(enemy, ref changed);
                DamageableReceiver damageableReceiver = EnsureSingleComponent<DamageableReceiver>(enemy, ref changed);
                HealthComponent health = EnsureSingleComponent<HealthComponent>(enemy, ref changed);
                LockOnTarget lockOnTarget = EnsureSingleComponent<LockOnTarget>(enemy, ref changed);
                Transform attackOrigin = FindOrCreateChild(enemy.transform, "AttackOrigin", new Vector3(0f, 1f, 0.9f), ref changed);
                Transform lockPoint = FindOrCreateChild(enemy.transform, "LockPoint", new Vector3(0f, 1.2f, 0f), ref changed);

                SetObjectReference(enemyBrain, "stateMachine", enemyStateMachine);
                SetObjectReference(enemyBrain, "sensing", enemySensing);
                SetObjectReference(enemyBrain, "motor", enemyMotor);
                SetObjectReference(enemyBrain, "attackController", enemyAttackController);
                SetObjectReference(enemyBrain, "health", health);
                SetObjectReference(enemyAttackController, "attackOrigin", attackOrigin);
                SetFloat(enemyAttackController, "rangePadding", EnemyAttackRangePadding);
                SetFloat(enemyAttackController, "maxHitAngle", EnemyAttackMaxHitAngle);
                SetObjectReference(damageableReceiver, "health", health);
                SetObjectReference(damageableReceiver, "enemyBrain", enemyBrain);
                SetObjectReference(lockOnTarget, "targetTransform", lockPoint);
                SetLayerMask(enemySensing, "targetMask", ~0);
                changed |= ConfigureEnemyPhysicalFooting(capsuleCollider, agent);
                changed |= RestoreEnemyVisualBaseline(enemy);
                changed |= CombatProxyVisualUtility.Apply(enemy, ResolveEnemyVisualKind(prefabPath));
                ConfigureEnemyVisualPresentationRelay(enemy, enemyVisualPresentationRelay, enemyBrain, enemyStateMachine);

                PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(enemy);
            }
        }

        private static bool RepairCombatTestEnemyPrefabs()
        {
            bool repairedAnyPrefab = false;
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyMeleePrefabPath);
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyMobilePrefabPath);
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyRangedPrefabPath);
            return repairedAnyPrefab;
        }

        private static void InstantiateCombatEnemy(GameObject prefab, string name, Vector3 position)
        {
            if (prefab == null)
            {
                return;
            }

            GameObject enemy = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            if (enemy == null)
            {
                return;
            }

            enemy.name = name;
            enemy.transform.position = position;
            enemy.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        }

        private static bool ConfigureEnemyPhysicalFooting(CapsuleCollider capsuleCollider, NavMeshAgent agent)
        {
            bool changed = false;

            if (capsuleCollider != null)
            {
                if (!Mathf.Approximately(capsuleCollider.radius, EnemyCapsuleRadius))
                {
                    capsuleCollider.radius = EnemyCapsuleRadius;
                    changed = true;
                }

                if (!Mathf.Approximately(capsuleCollider.height, EnemyCapsuleHeight))
                {
                    capsuleCollider.height = EnemyCapsuleHeight;
                    changed = true;
                }

                Vector3 center = capsuleCollider.center;

                if (!Mathf.Approximately(center.x, 0f)
                    || !Mathf.Approximately(center.y, EnemyCapsuleCenterY)
                    || !Mathf.Approximately(center.z, 0f))
                {
                    capsuleCollider.center = new Vector3(0f, EnemyCapsuleCenterY, 0f);
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(capsuleCollider);
                }
            }

            if (agent != null)
            {
                bool agentChanged = false;

                if (!Mathf.Approximately(agent.radius, EnemyCapsuleRadius))
                {
                    agent.radius = EnemyCapsuleRadius;
                    agentChanged = true;
                }

                if (!Mathf.Approximately(agent.height, EnemyCapsuleHeight))
                {
                    agent.height = EnemyCapsuleHeight;
                    agentChanged = true;
                }

                if (!Mathf.Approximately(agent.baseOffset, EnemyAgentBaseOffset))
                {
                    agent.baseOffset = EnemyAgentBaseOffset;
                    agentChanged = true;
                }

                if (!Mathf.Approximately(agent.angularSpeed, 720f))
                {
                    agent.angularSpeed = 720f;
                    agentChanged = true;
                }

                if (!Mathf.Approximately(agent.acceleration, 30f))
                {
                    agent.acceleration = 30f;
                    agentChanged = true;
                }

                if (agentChanged)
                {
                    EditorUtility.SetDirty(agent);
                    changed = true;
                }
            }

            return changed;
        }

        private static void ConfigureEnemyVisualPresentationRelay(
            GameObject enemy,
            EnemyVisualPresentationRelay relay,
            EnemyBrain enemyBrain,
            EnemyStateMachine enemyStateMachine)
        {
            if (enemy == null || relay == null)
            {
                return;
            }

            Transform visualRoot = EnemyVisualPresentationRelay.FindDefaultVisualRoot(enemy.transform);
            Transform accentTransform = EnemyVisualPresentationRelay.FindDefaultAccentTransform(visualRoot);
            SetObjectReference(relay, "enemyBrain", enemyBrain);
            SetObjectReference(relay, "stateMachine", enemyStateMachine);
            SetObjectReference(relay, "visualRoot", visualRoot);
            SetObjectReference(relay, "accentTransform", accentTransform);
            relay.enabled = true;
            EditorUtility.SetDirty(relay);
        }

        private static void ConfigureEnemyImportedAnimator(Animator animator, RuntimeAnimatorController controller)
        {
            if (animator == null)
            {
                return;
            }

            animator.enabled = true;
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            animator.Rebind();
            EditorUtility.SetDirty(animator);
        }

        private static void ConfigurePlayerCombatAnimationRelay(
            PlayerCombatAnimationRelay relay,
            PlayerCharacter playerCharacter,
            PlayerCombatController playerCombatController,
            PlayerStateMachine playerStateMachine,
            PlayerMotor playerMotor,
            Animator animator)
        {
            if (relay == null)
            {
                return;
            }

            SetObjectReference(relay, "playerCharacter", playerCharacter);
            SetObjectReference(relay, "combatController", playerCombatController);
            SetObjectReference(relay, "stateMachine", playerStateMachine);
            SetObjectReference(relay, "motor", playerMotor);
            SetObjectReference(relay, "animator", animator);
            SetFloat(relay, "crossFadeSeconds", PlayerAnimationCrossFadeSeconds);
            SetFloat(relay, "locomotionDampSeconds", PlayerAnimationLocomotionDampSeconds);
            SetFloat(relay, "dodgeAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerDodgeAnimationDuration());
            SetFloat(relay, "hitAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerHitAnimationDuration());
        }

        private static void ConfigureEnemyCombatAnimationRelay(
            EnemyCombatAnimationRelay relay,
            EnemyBrain enemyBrain,
            EnemyStateMachine enemyStateMachine,
            Animator animator)
        {
            if (relay == null)
            {
                return;
            }

            SetObjectReference(relay, "enemyBrain", enemyBrain);
            SetObjectReference(relay, "stateMachine", enemyStateMachine);
            SetObjectReference(relay, "animator", animator);
            SetFloat(relay, "crossFadeSeconds", EnemyAnimationCrossFadeSeconds);
            SetFloat(relay, "locomotionDampSeconds", EnemyAnimationLocomotionDampSeconds);
        }

        private static bool RestoreEnemyVisualBaseline(GameObject enemy)
        {
            if (enemy == null)
            {
                return false;
            }

            bool changed = false;
            Animator animator = enemy.GetComponent<Animator>();
            changed |= RemoveImportedEnemyPreviewInstanceRoots(enemy);
            changed |= CombatImportedEnemyVisualUtility.RemoveImportedVisual(enemy, animator);

            EnemyCombatAnimationRelay importedAnimationRelay = enemy.GetComponent<EnemyCombatAnimationRelay>();

            if (importedAnimationRelay != null)
            {
                Object.DestroyImmediate(importedAnimationRelay);
                changed = true;
            }

            if (animator != null)
            {
                Object.DestroyImmediate(animator);
                changed = true;
            }

            EnemyVisualPresentationRelay proxyRelay = enemy.GetComponent<EnemyVisualPresentationRelay>();

            if (proxyRelay != null && !proxyRelay.enabled)
            {
                proxyRelay.enabled = true;
                EditorUtility.SetDirty(proxyRelay);
                changed = true;
            }

            return changed;
        }

        private static bool RemoveImportedEnemyPreviewInstanceRoots(GameObject enemy)
        {
            Transform[] transforms = enemy.GetComponentsInChildren<Transform>(true);
            List<GameObject> instanceRootsToRemove = new List<GameObject>();

            for (int i = 0; i < transforms.Length; i++)
            {
                Transform transform = transforms[i];

                if (transform == null || transform == enemy.transform)
                {
                    continue;
                }

                GameObject candidate = transform.gameObject;
                if (!ShouldRemoveImportedEnemyPreviewObject(candidate))
                {
                    continue;
                }

                GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(candidate);
                if (instanceRoot == null || instanceRoot == enemy)
                {
                    instanceRoot = candidate;
                }

                if (instanceRoot == enemy || instanceRootsToRemove.Contains(instanceRoot))
                {
                    continue;
                }

                instanceRootsToRemove.Add(instanceRoot);
            }

            for (int i = 0; i < instanceRootsToRemove.Count; i++)
            {
                Object.DestroyImmediate(instanceRootsToRemove[i]);
            }

            return instanceRootsToRemove.Count > 0;
        }

        private static bool ShouldRemoveImportedEnemyPreviewObject(GameObject candidate)
        {
            if (candidate == null)
            {
                return false;
            }

            if (candidate.name.StartsWith(
                    CombatImportedEnemyVisualUtility.ImportedVisualRootName,
                    System.StringComparison.Ordinal))
            {
                return true;
            }

            string sourceAssetPath = GetPrefabSourceAssetPath(candidate);
            if (string.IsNullOrEmpty(sourceAssetPath))
            {
                return false;
            }

            for (int i = 0; i < LocalPreviewOnlySourceRoots.Length; i++)
            {
                if (sourceAssetPath.StartsWith(LocalPreviewOnlySourceRoots[i], System.StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetPrefabSourceAssetPath(GameObject candidate)
        {
            if (candidate == null)
            {
                return string.Empty;
            }

            Object sourceObject = PrefabUtility.GetCorrespondingObjectFromSource(candidate);
            return sourceObject != null
                ? AssetDatabase.GetAssetPath(sourceObject)
                : string.Empty;
        }

        private static bool TryApplyImportedEnemyAvatarPreview(
            string prefabPath,
            CombatProxyVisualKind visualKind,
            ref bool foundAnyPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) == null)
            {
                return false;
            }

            foundAnyPrefab = true;
            RuntimeAnimatorController enemyAnimatorController = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(visualKind);

            if (enemyAnimatorController == null)
            {
                Debug.LogWarning($"Imported enemy Avatar preview could not build a local preview AnimatorController for {visualKind}. Verify the supported local animation sources are installed first.");
                return false;
            }

            GameObject enemy = PrefabUtility.LoadPrefabContents(prefabPath);

            try
            {
                RestoreEnemyVisualBaseline(enemy);
                CombatProxyVisualUtility.Apply(enemy, visualKind);

                Animator animator = enemy.GetComponent<Animator>();

                if (!CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(enemy, visualKind, animator))
                {
                    return false;
                }

                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);
                if (importedAnimator == null)
                {
                    Debug.LogWarning($"Imported enemy Avatar preview did not produce a driven visual Animator for {visualKind}.");
                    return false;
                }

                ConfigureEnemyImportedAnimator(importedAnimator, enemyAnimatorController);

                EnemyVisualPresentationRelay relay = GetOrAddComponent<EnemyVisualPresentationRelay>(enemy);
                EnemyBrain enemyBrain = GetOrAddComponent<EnemyBrain>(enemy);
                EnemyStateMachine enemyStateMachine = GetOrAddComponent<EnemyStateMachine>(enemy);
                EnemyCombatAnimationRelay importedAnimationRelay = GetOrAddComponent<EnemyCombatAnimationRelay>(enemy);
                relay.enabled = false;
                EditorUtility.SetDirty(relay);
                ConfigureEnemyCombatAnimationRelay(importedAnimationRelay, enemyBrain, enemyStateMachine, importedAnimator);
                PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(enemy);
            }
        }

        private static bool RefreshScenePrefabInstanceFromSource(string instanceName, string sourcePrefabPath)
        {
            GameObject instance = GameObject.Find(instanceName);

            if (instance == null)
            {
                Debug.LogWarning($"CombatTest scene prefab instance refresh could not find {instanceName}.");
                return false;
            }

            GameObject instanceRoot = PrefabUtility.GetNearestPrefabInstanceRoot(instance);

            if (instanceRoot == null)
            {
                Debug.LogWarning($"CombatTest scene object {instanceName} is not a prefab instance.");
                return false;
            }

            string actualSourcePath = GetPrefabSourceAssetPath(instanceRoot);

            if (!string.Equals(actualSourcePath, sourcePrefabPath, System.StringComparison.Ordinal))
            {
                Debug.LogWarning(
                    $"CombatTest scene object {instanceName} points to {actualSourcePath}, not expected source prefab {sourcePrefabPath}.");
                return false;
            }

            string sceneName = instanceRoot.name;
            Transform sceneTransform = instanceRoot.transform;
            Transform parent = sceneTransform.parent;
            int siblingIndex = sceneTransform.GetSiblingIndex();
            Vector3 localPosition = sceneTransform.localPosition;
            Quaternion localRotation = sceneTransform.localRotation;
            Vector3 localScale = sceneTransform.localScale;
            bool activeSelf = instanceRoot.activeSelf;

            PrefabUtility.RevertPrefabInstance(instanceRoot, InteractionMode.AutomatedAction);

            instanceRoot.name = sceneName;
            sceneTransform = instanceRoot.transform;
            sceneTransform.SetParent(parent);
            sceneTransform.SetSiblingIndex(siblingIndex);
            sceneTransform.localPosition = localPosition;
            sceneTransform.localRotation = localRotation;
            sceneTransform.localScale = localScale;
            instanceRoot.SetActive(activeSelf);
            EditorUtility.SetDirty(instanceRoot);
            EditorUtility.SetDirty(sceneTransform);
            PrefabUtility.RecordPrefabInstancePropertyModifications(instanceRoot);
            PrefabUtility.RecordPrefabInstancePropertyModifications(sceneTransform);
            return true;
        }

        private static Transform CreateChild(Transform parent, string name, Vector3 localPosition)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.identity;
            child.transform.localScale = Vector3.one;
            return child.transform;
        }

        private static void CreateWall(string name, Vector3 position, Vector3 localScale)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.position = position;
            wall.transform.localScale = localScale;
            SetCombatTestNavigationStatic(wall);
        }

        private static void SetCombatTestNavigationStatic(GameObject gameObject)
        {
            if (gameObject == null)
            {
                return;
            }

            StaticEditorFlags currentFlags = GameObjectUtility.GetStaticEditorFlags(gameObject);
            GameObjectUtility.SetStaticEditorFlags(gameObject, currentFlags | StaticEditorFlags.NavigationStatic);
        }

        private static bool RebuildCombatTestSceneNavMesh(Scene scene)
        {
            if (!scene.IsValid())
            {
                return false;
            }

            MarkSceneNavigationStatic("Ground");
            MarkSceneNavigationStatic("NorthWall");
            MarkSceneNavigationStatic("SouthWall");
            MarkSceneNavigationStatic("EastWall");
            MarkSceneNavigationStatic("WestWall");

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

        private static void MarkSceneNavigationStatic(string objectName)
        {
            GameObject target = GameObject.Find(objectName);

            if (target == null)
            {
                return;
            }

            SetCombatTestNavigationStatic(target);
        }

        private static void ApplyCombatTestLightingPreset(Light directionalLight)
        {
            if (directionalLight != null)
            {
                directionalLight.type = LightType.Directional;
                directionalLight.intensity = CombatTestDirectionalLightIntensity;
            }

            RenderSettings.ambientIntensity = CombatTestAmbientIntensity;
            RenderSettings.reflectionIntensity = CombatTestReflectionIntensity;
        }

        private static void SetAttackCombo(PlayerCombatController controller, params AttackDefinitionSO[] attacks)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty comboProperty = serializedObject.FindProperty("lightAttackCombo");
            comboProperty.arraySize = attacks.Length;

            for (int i = 0; i < attacks.Length; i++)
            {
                comboProperty.GetArrayElementAtIndex(i).objectReferenceValue = attacks[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetSwordArts(PlayerCombatController controller, params SwordArtDefinitionSO[] swordArts)
        {
            SerializedObject serializedObject = new SerializedObject(controller);
            SerializedProperty swordArtsProperty = serializedObject.FindProperty("swordArts");
            swordArtsProperty.arraySize = swordArts.Length;

            for (int i = 0; i < swordArts.Length; i++)
            {
                swordArtsProperty.GetArrayElementAtIndex(i).objectReferenceValue = swordArts[i];
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(controller);
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serializedObject = new SerializedObject(target);
            serializedObject.FindProperty(propertyName).objectReferenceValue = value;
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

        private static void EnsureSceneContextAudioSettings(string scenePath)
        {
            string audioSettingsGuid = AssetDatabase.AssetPathToGUID(AudioSettingsPath);

            if (string.IsNullOrWhiteSpace(audioSettingsGuid))
            {
                Debug.LogWarning("CombatTest scene build could not wire SceneRuntimeContext.audioSettings because SO_AudioSettings is missing.");
                return;
            }

            if (!System.IO.File.Exists(scenePath))
            {
                Debug.LogWarning("CombatTest scene build could not patch SceneRuntimeContext.audioSettings because the scene file does not exist yet.");
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
                Debug.LogWarning("CombatTest scene build could not resolve SceneRuntimeContext.audioSettings serialized property.");
                return;
            }

            sceneContents = sceneContents.Replace(nullReference, desiredReference);
            System.IO.File.WriteAllText(scenePath, sceneContents);
            AssetDatabase.ImportAsset(scenePath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigurePlayerAnimator(Animator animator, RuntimeAnimatorController controller)
        {
            if (animator == null)
            {
                return;
            }

            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.updateMode = AnimatorUpdateMode.Normal;
            EditorUtility.SetDirty(animator);
        }

        private static bool RestorePlayerVisualBaseline(GameObject player, Animator animator)
        {
            bool changed = CombatImportedPlayerVisualUtility.RemoveImportedVisual(player, animator);
            changed |= CombatProxyVisualUtility.Apply(player, CombatProxyVisualKind.Player);
            return changed;
        }

        private static void ConfigurePlayerWeaponPresentation(
            PlayerCombatAnimationRelay animationRelay,
            GameObject player,
            bool syncImportedWeaponPreview)
        {
            if (animationRelay == null || player == null)
            {
                return;
            }

            if (syncImportedWeaponPreview)
            {
                CombatImportedPlayerVisualUtility.SyncImportedWeaponPreview(player, player.GetComponent<Animator>());
            }

            SetObjectReference(animationRelay, "proxyWeaponGrip", PlayerCombatAnimationRelay.FindDefaultProxyWeaponGrip(player.transform));
            SetObjectReference(animationRelay, "importedWeaponAnchor", PlayerCombatAnimationRelay.FindDefaultImportedWeaponAnchor(player.transform));
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static T EnsureSingleComponent<T>(GameObject gameObject, ref bool changed) where T : Component
        {
            T[] components = gameObject.GetComponents<T>();

            if (components.Length == 0)
            {
                changed = true;
                return gameObject.AddComponent<T>();
            }

            T primaryComponent = components[0];

            for (int i = components.Length - 1; i >= 1; i--)
            {
                Object.DestroyImmediate(components[i]);
                changed = true;
            }

            return primaryComponent;
        }

        private static Transform FindOrCreateChild(Transform parent, string name, Vector3 localPosition, ref bool changed)
        {
            Transform child = parent.Find(name);

            if (child != null)
            {
                return child;
            }

            changed = true;
            return CreateChild(parent, name, localPosition);
        }

        private static CombatProxyVisualKind ResolveEnemyVisualKind(string prefabPath)
        {
            if (string.Equals(prefabPath, EnemyMobilePrefabPath, global::System.StringComparison.Ordinal))
            {
                return CombatProxyVisualKind.EnemyMobile;
            }

            if (string.Equals(prefabPath, EnemyRangedPrefabPath, global::System.StringComparison.Ordinal))
            {
                return CombatProxyVisualKind.EnemyRanged;
            }

            return CombatProxyVisualKind.EnemyMelee;
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
                "This action will overwrite the existing CombatTest outputs:\n\n- " +
                string.Join("\n- ", existingTargets) +
                "\n\nUse this when you want a clean regenerated test scene. If you have hand-tuned scene or prefab edits, cancel and duplicate them first.";

            bool confirmed = EditorUtility.DisplayDialog(
                "Rebuild CombatTest Outputs?",
                message,
                "Rebuild",
                "Cancel");

            if (!confirmed)
            {
                Debug.LogWarning("CombatTest rebuild cancelled to avoid overwriting existing scene or prefabs.");
            }

            return confirmed;
        }

        private static List<string> CollectExistingTargets()
        {
            string[] candidatePaths =
            {
                ScenePath,
                PlayerPrefabPath,
                EnemyMeleePrefabPath,
                EnemyMobilePrefabPath,
                EnemyRangedPrefabPath
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
    }
}
