using System.Collections.Generic;
using CampusRPG.AI;
using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Composition;
using CampusRPG.Core;
using CampusRPG.Input;
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
        private const string ApplyImportedVisualMenu = "CampusRPG/Setup/Apply Imported Player Visuals To CombatTest Player Prefab (Local Preview)";
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
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyMeleePrefabPath);
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyMobilePrefabPath);
            repairedAnyPrefab |= RepairEnemyPrefab(EnemyRangedPrefabPath);

            if (!repairedAnyPrefab)
            {
                Debug.LogWarning("CombatTest prefab repair skipped because no target prefabs were found.");
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatTest prefab wiring repaired: duplicate required components removed and player animation relay connected.");
        }

        [MenuItem(ApplyImportedVisualMenu)]
        public static void ApplyImportedVisualsToCombatTestPlayerPrefab()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath) == null)
            {
                Debug.LogWarning("CombatTest player prefab does not exist yet.");
                return;
            }

            GameObject player = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);

            try
            {
                Animator animator = GetOrAddComponent<Animator>(player);

                if (!CombatImportedPlayerVisualUtility.TryApply(player, animator))
                {
                    Debug.LogWarning("No imported player visual source was found. Install a supported local package first.");
                    return;
                }

                PrefabUtility.SaveAsPrefabAsset(player, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(player);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Applied imported player visuals to PF_Player_CombatTest for local preview. Do not commit this prefab state to the public repository.");
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
            directionalLight.type = LightType.Directional;
            directionalLight.intensity = 1.2f;
            lightObject.transform.rotation = Quaternion.Euler(45f, -35f, 0f);

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.transform.localScale = new Vector3(4f, 1f, 4f);

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
            PlayerStateMachine playerStateMachine = player.AddComponent<PlayerStateMachine>();
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

            SetObjectReference(playerCharacter, "baseStats", AssetDatabase.LoadAssetAtPath<PlayerBaseStatsSO>(PlayerStatsPath));
            SetObjectReference(playerCharacter, "motor", playerMotor);
            SetObjectReference(playerCharacter, "stateMachine", playerStateMachine);
            SetObjectReference(playerCharacter, "combatController", playerCombatController);
            SetObjectReference(playerCharacter, "skillController", skillController);
            SetObjectReference(playerCharacter, "health", health);
            SetObjectReference(playerCharacter, "mana", mana);
            SetObjectReference(playerCharacter, "gauges", gauge);

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
            SetBool(playerCombatController, "prototypeGrantDodgeFollowUpOnAnyDodge", false);

            SetObjectReference(attackExecutor, "attackOrigin", attackOrigin);
            SetLayerMask(attackExecutor, "targetMask", ~0);
            SetObjectReference(hitboxController, "attackExecutor", attackExecutor);
            SetObjectReference(animationRelay, "playerCharacter", playerCharacter);
            SetObjectReference(animationRelay, "combatController", playerCombatController);
            SetObjectReference(animationRelay, "stateMachine", playerStateMachine);
            SetObjectReference(animationRelay, "motor", playerMotor);
            SetObjectReference(animationRelay, "animator", animator);
            SetFloat(animationRelay, "dodgeAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerDodgeAnimationDuration());
            SetFloat(animationRelay, "hitAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerHitAnimationDuration());
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
            CombatImportedPlayerVisualUtility.RemoveImportedVisual(player, animator);
            CombatProxyVisualUtility.Apply(player, CombatProxyVisualKind.Player);

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

            NavMeshAgent agent = enemy.AddComponent<NavMeshAgent>();
            agent.angularSpeed = 720f;
            agent.acceleration = 30f;
            agent.stoppingDistance = stoppingDistance;

            EnemyBrain enemyBrain = enemy.AddComponent<EnemyBrain>();
            EnemyStateMachine enemyStateMachine = enemy.AddComponent<EnemyStateMachine>();
            EnemySensing enemySensing = enemy.AddComponent<EnemySensing>();
            EnemyMotor enemyMotor = enemy.AddComponent<EnemyMotor>();
            EnemyAttackController enemyAttackController = enemy.AddComponent<EnemyAttackController>();
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
            SetObjectReference(damageableReceiver, "health", health);
            SetObjectReference(damageableReceiver, "enemyBrain", enemyBrain);
            SetObjectReference(lockOnTarget, "targetTransform", lockPoint);
            SetLayerMask(enemySensing, "targetMask", ~0);
            CombatProxyVisualUtility.Apply(enemy, ResolveEnemyVisualKind(prefabPath));

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
                PlayerCombatController playerCombatController = EnsureSingleComponent<PlayerCombatController>(player, ref changed);
                SkillController skillController = EnsureSingleComponent<SkillController>(player, ref changed);
                LockOnTargetSelector lockOnTargetSelector = EnsureSingleComponent<LockOnTargetSelector>(player, ref changed);
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

                SetObjectReference(playerCharacter, "motor", playerMotor);
                SetObjectReference(playerCharacter, "stateMachine", playerStateMachine);
                SetObjectReference(playerCharacter, "combatController", playerCombatController);
                SetObjectReference(playerCharacter, "skillController", skillController);
                SetObjectReference(playerCharacter, "health", health);
                SetObjectReference(playerCharacter, "mana", mana);
                SetObjectReference(playerCharacter, "gauges", gauge);

                SetObjectReference(playerCombatController, "attackExecutor", attackExecutor);
                SetObjectReference(playerCombatController, "hitboxController", hitboxController);
                SetObjectReference(playerCombatController, "animationRelay", animationRelay);

                SetObjectReference(animationRelay, "playerCharacter", playerCharacter);
                SetObjectReference(animationRelay, "combatController", playerCombatController);
                SetObjectReference(animationRelay, "stateMachine", playerStateMachine);
                SetObjectReference(animationRelay, "motor", playerMotor);
                SetObjectReference(animationRelay, "animator", animator);
                SetFloat(animationRelay, "dodgeAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerDodgeAnimationDuration());
                SetFloat(animationRelay, "hitAnimationDurationSeconds", CombatTestAssetGenerator.GetPlayerHitAnimationDuration());
                SetObjectReference(attackExecutor, "attackOrigin", attackOrigin);
                SetObjectReference(hitboxController, "attackExecutor", attackExecutor);
                SetObjectReference(skillController, "owner", playerCharacter);
                SetObjectReference(skillController, "mana", mana);
                SetObjectReference(skillController, "attackExecutor", attackExecutor);
                SetObjectReference(skillController, "lockOnTargetSelector", lockOnTargetSelector);
                SetObjectReference(skillController, "castOrigin", castOrigin);
                SetObjectReference(damageableReceiver, "health", health);
                SetObjectReference(damageableReceiver, "playerCharacter", playerCharacter);
                ConfigurePlayerAnimator(animator, playerAnimatorController);
                changed |= CombatImportedPlayerVisualUtility.RemoveImportedVisual(player, animator);
                changed |= CombatProxyVisualUtility.Apply(player, CombatProxyVisualKind.Player);

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
                EnemyBrain enemyBrain = EnsureSingleComponent<EnemyBrain>(enemy, ref changed);
                EnemyStateMachine enemyStateMachine = EnsureSingleComponent<EnemyStateMachine>(enemy, ref changed);
                EnemySensing enemySensing = EnsureSingleComponent<EnemySensing>(enemy, ref changed);
                EnemyMotor enemyMotor = EnsureSingleComponent<EnemyMotor>(enemy, ref changed);
                EnemyAttackController enemyAttackController = EnsureSingleComponent<EnemyAttackController>(enemy, ref changed);
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
                SetObjectReference(damageableReceiver, "health", health);
                SetObjectReference(damageableReceiver, "enemyBrain", enemyBrain);
                SetObjectReference(lockOnTarget, "targetTransform", lockPoint);
                changed |= CombatProxyVisualUtility.Apply(enemy, ResolveEnemyVisualKind(prefabPath));

                PrefabUtility.SaveAsPrefabAsset(enemy, prefabPath);
                return true;
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(enemy);
            }
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
