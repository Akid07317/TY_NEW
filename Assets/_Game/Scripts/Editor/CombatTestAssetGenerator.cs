using CampusRPG.AI;
using CampusRPG.Character;
using CampusRPG.Combat;
using CampusRPG.Core;
using CampusRPG.Skills;
using CampusRPG.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Editor
{
    public static class CombatTestAssetGenerator
    {
        private const string RootMenu = "CampusRPG/Setup/Create CombatTest Placeholder Assets";
        private const string RebuildImportedPlayerAnimationsMenu = "CampusRPG/Setup/Local Preview/Rebuild CombatTest Imported Player Animations";
        private const string AudioSettingsPath = "Assets/_Game/Data/Audio/SO_AudioSettings.asset";
        private const string SpellBoltProjectilePrefabPath = "Assets/_Game/Prefabs/Combat/PF_Projectile_SpellBolt.prefab";
        private const string SpellBoltImpactVfxPrefabPath = "Assets/_Game/Prefabs/VFX/PF_VFX_ProjectileImpact_SpellBolt.prefab";
        private const string BossTelegraphStylePath = "Assets/_Game/Data/Enemies/SO_BossTelegraphStyle_Gatekeeper.asset";
        private const string BossEngageTelegraphMaterialPath = "Assets/_Game/Materials/M_BossTelegraph_Engage_Gatekeeper.mat";
        private const string BossAttackTelegraphMaterialPath = "Assets/_Game/Materials/M_BossTelegraph_Attack_Gatekeeper.mat";
        private const string BossImpactMarkerMaterialPath = "Assets/_Game/Materials/M_BossImpactMarker_Gatekeeper.mat";
        private const string BossSpawnFlareMaterialPath = "Assets/_Game/Materials/M_BossSpawnFlare_Gatekeeper.mat";
        private const string BossGroundTelegraphPrefabPath = "Assets/_Game/Prefabs/UI/PF_BossGroundTelegraph_Gatekeeper.prefab";
        private const string BossImpactMarkerPrefabPath = "Assets/_Game/Prefabs/UI/PF_BossImpactMarker_Gatekeeper.prefab";
        private const string BossSpawnFlarePrefabPath = "Assets/_Game/Prefabs/UI/PF_BossSpawnFlare_Gatekeeper.prefab";
        private const string Light01Path = "Assets/_Game/Data/Combat/SO_Attack_Light_01.asset";
        private const string Light02Path = "Assets/_Game/Data/Combat/SO_Attack_Light_02.asset";
        private const string Light03Path = "Assets/_Game/Data/Combat/SO_Attack_Light_03.asset";
        private const string HeavyPath = "Assets/_Game/Data/Combat/SO_Attack_Heavy_01.asset";
        private const string DodgeFollowUpPath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp.asset";
        private const string EnhancedDodgePath = "Assets/_Game/Data/Combat/SO_Attack_DodgeFollowUp_Enhanced.asset";
        private const string CounterPath = "Assets/_Game/Data/Combat/SO_Attack_Counter.asset";
        private const string EnhancedCounterPath = "Assets/_Game/Data/Combat/SO_Attack_Counter_Enhanced.asset";
        private const string PlayerAnimationRootFolder = "Assets/_Game/Animations/Characters/CombatTest";
        private const string PlayerAnimatorControllerPath = PlayerAnimationRootFolder + "/AC_Player_CombatTest.controller";
        private const string PlayerIdleClipPath = PlayerAnimationRootFolder + "/AN_Player_Idle_CombatTest.anim";
        private const string PlayerLocomotionWalkForwardClipPath = PlayerAnimationRootFolder + "/AN_Player_Walk_CombatTest.anim";
        private const string PlayerLocomotionWalkBackwardClipPath = PlayerAnimationRootFolder + "/AN_Player_Walk_Backward_CombatTest.anim";
        private const string PlayerLocomotionWalkLeftClipPath = PlayerAnimationRootFolder + "/AN_Player_Walk_Left_CombatTest.anim";
        private const string PlayerLocomotionWalkRightClipPath = PlayerAnimationRootFolder + "/AN_Player_Walk_Right_CombatTest.anim";
        private const string PlayerLocomotionRunForwardClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_CombatTest.anim";
        private const string PlayerLocomotionRunBackwardClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_Backward_CombatTest.anim";
        private const string PlayerLocomotionRunLeftClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_Left_CombatTest.anim";
        private const string PlayerLocomotionRunRightClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_Right_CombatTest.anim";
        private const string PlayerLocomotionRunForwardLeftClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_ForwardLeft_CombatTest.anim";
        private const string PlayerLocomotionRunForwardRightClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_ForwardRight_CombatTest.anim";
        private const string PlayerLocomotionRunBackwardLeftClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_BackwardLeft_CombatTest.anim";
        private const string PlayerLocomotionRunBackwardRightClipPath = PlayerAnimationRootFolder + "/AN_Player_Run_BackwardRight_CombatTest.anim";
        private const string PlayerAirborneClipPath = PlayerAnimationRootFolder + "/AN_Player_Airborne_CombatTest.anim";
        private const string PlayerBlockClipPath = PlayerAnimationRootFolder + "/AN_Player_Block_CombatTest.anim";
        private const string PlayerDodgeClipPath = PlayerAnimationRootFolder + "/AN_Player_Dodge_CombatTest.anim";
        private const string PlayerHitClipPath = PlayerAnimationRootFolder + "/AN_Player_Hit_CombatTest.anim";
        private const string PlayerDeathClipPath = PlayerAnimationRootFolder + "/AN_Player_Death_CombatTest.anim";
        private const string PlayerLocomotionBlendTreeName = "BT_Player_Locomotion_CombatTest";
        private const string PlayerLocomotionStateName = "Locomotion";
        private const string PlayerBlockStateName = "Block";
        private const string PlayerAirborneStateName = "Airborne";
        private const string PlayerDodgeStateName = "Dodge";
        private const string PlayerHitStateName = "Hit";
        private const string PlayerDeathStateName = "Death";
        private const string GroundSpeedParameterName = "GroundSpeed";
        private const string MoveXParameterName = "MoveX";
        private const string MoveYParameterName = "MoveY";
        private const string IsGroundedParameterName = "IsGrounded";
        private const string IsBlockingParameterName = "IsBlocking";
        private const string VerticalSpeedParameterName = "VerticalSpeed";
        private const string PlayerProxyRootPath = "CombatProxyVisualRoot";
        private const string PlayerProxyTorsoPath = PlayerProxyRootPath + "/Torso";
        private const string PlayerProxyChestPath = PlayerProxyRootPath + "/Chest";
        private const string PlayerProxyHeadPath = PlayerProxyRootPath + "/Head";
        private const string PlayerProxyForwardMarkerPath = PlayerProxyRootPath + "/ForwardMarker";
        private const string PlayerProxyGuardPath = PlayerProxyRootPath + "/Guard";
        private const string PlayerProxyBladePath = PlayerProxyRootPath + "/Blade";
        private const string PlayerProxyWeaponGripPath = PlayerProxyRootPath + "/WeaponGrip";
        private const string PlayerProxyWeaponGripGuardPath = PlayerProxyWeaponGripPath + "/Guard";
        private const string PlayerProxyWeaponGripBladePath = PlayerProxyWeaponGripPath + "/Blade";
        private static bool allowImportedPlayerAnimationPreviewBuild;

        [MenuItem(RootMenu)]
        public static void CreateCombatTestAssets()
        {
            EnsureFolder("Assets/_Game/Data");
            EnsureFolder("Assets/_Game/Data/Audio");
            EnsureFolder("Assets/_Game/Data/Characters");
            EnsureFolder("Assets/_Game/Data/Combat");
            EnsureFolder("Assets/_Game/Data/Enemies");
            EnsureFolder("Assets/_Game/Data/Skills");
            EnsureFolder("Assets/_Game/Animations");
            EnsureFolder("Assets/_Game/Animations/Characters");
            EnsureFolder(PlayerAnimationRootFolder);
            EnsureFolder("Assets/_Game/Materials");
            EnsureFolder("Assets/_Game/Prefabs");
            EnsureFolder("Assets/_Game/Prefabs/Combat");
            EnsureFolder("Assets/_Game/Prefabs/UI");
            EnsureFolder("Assets/_Game/Prefabs/VFX");

            AudioSettingsSO audioSettings = CreateOrLoadAsset<AudioSettingsSO>(AudioSettingsPath);
            PlayerBaseStatsSO playerStats = CreateOrLoadAsset<PlayerBaseStatsSO>(
                "Assets/_Game/Data/Characters/SO_PlayerBaseStats.asset");
            CombatBalanceSO combatBalance = CreateOrLoadAsset<CombatBalanceSO>(
                "Assets/_Game/Data/Combat/SO_CombatBalance.asset");
            GameObject spellBoltImpactVfx = BuildImpactEffectPrefab(
                SpellBoltImpactVfxPrefabPath,
                "PF_VFX_ProjectileImpact_SpellBolt");
            GameObject spellBoltProjectile = BuildProjectilePrefab(
                SpellBoltProjectilePrefabPath,
                "PF_Projectile_SpellBolt",
                spellBoltImpactVfx);
            Material engageTelegraphMaterial = CreateOrLoadMaterial(
                BossEngageTelegraphMaterialPath,
                new Color(0.86f, 0.71f, 0.28f, 0.8f));
            Material attackTelegraphMaterial = CreateOrLoadMaterial(
                BossAttackTelegraphMaterialPath,
                new Color(0.9f, 0.24f, 0.18f, 0.82f));
            Material impactMarkerMaterial = CreateOrLoadMaterial(
                BossImpactMarkerMaterialPath,
                new Color(1f, 0.34f, 0.22f, 0.88f));
            Material spawnFlareMaterial = CreateOrLoadMaterial(
                BossSpawnFlareMaterialPath,
                new Color(1f, 0.76f, 0.3f, 0.72f));
            GameObject bossGroundTelegraphPrefab = BuildBossVisualPrefab(
                BossGroundTelegraphPrefabPath,
                "PF_BossGroundTelegraph_Gatekeeper",
                new Vector3(1f, 0.03f, 1f),
                engageTelegraphMaterial);
            GameObject bossImpactMarkerPrefab = BuildBossVisualPrefab(
                BossImpactMarkerPrefabPath,
                "PF_BossImpactMarker_Gatekeeper",
                new Vector3(1f, 0.025f, 1f),
                impactMarkerMaterial);
            GameObject bossSpawnFlarePrefab = BuildBossVisualPrefab(
                BossSpawnFlarePrefabPath,
                "PF_BossSpawnFlare_Gatekeeper",
                new Vector3(0.6f, 0.5f, 0.6f),
                spawnFlareMaterial);
            BossTelegraphStyleSO bossTelegraphStyle = CreateOrLoadAsset<BossTelegraphStyleSO>(BossTelegraphStylePath);

            AttackDefinitionSO light01 = CreateAttackAsset(
                Light01Path,
                "Light_01",
                "Light Combo I",
                1.0f,
                0.10f,
                0.08f,
                0.22f,
                1.6f,
                0.45f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO light02 = CreateAttackAsset(
                Light02Path,
                "Light_02",
                "Light Combo II",
                1.1f,
                0.10f,
                0.08f,
                0.24f,
                1.7f,
                0.5f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO light03 = CreateAttackAsset(
                Light03Path,
                "Light_03",
                "Light Combo III",
                1.4f,
                0.14f,
                0.10f,
                0.30f,
                1.9f,
                0.55f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO heavy = CreateAttackAsset(
                HeavyPath,
                "Heavy_01",
                "Heavy Strike",
                1.8f,
                0.20f,
                0.12f,
                0.42f,
                2.1f,
                0.65f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO dodgeFollowUp = CreateAttackAsset(
                DodgeFollowUpPath,
                "DodgeFollowUp",
                "Dodge Follow-Up",
                1.4f,
                0.08f,
                0.10f,
                0.25f,
                1.8f,
                0.55f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO counter = CreateAttackAsset(
                CounterPath,
                "Counter",
                "Counter Slash",
                1.5f,
                0.10f,
                0.10f,
                0.28f,
                1.9f,
                0.6f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO empoweredCounter = CreateAttackAsset(
                EnhancedCounterPath,
                "Counter_Enhanced",
                "Counter Slash+",
                2.5f,
                0.12f,
                0.12f,
                0.32f,
                2.1f,
                0.7f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO empoweredDodge = CreateAttackAsset(
                EnhancedDodgePath,
                "DodgeFollowUp_Enhanced",
                "Dodge Follow-Up+",
                2.0f,
                0.08f,
                0.12f,
                0.28f,
                2.0f,
                0.65f,
                hitboxActivationMode: AttackHitboxActivationMode.TimedWindow);
            AttackDefinitionSO enemyMelee = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Melee.asset",
                "Enemy_Melee",
                "Guard Swing",
                1.0f,
                0.18f,
                0.10f,
                0.35f,
                1.8f,
                0.5f);
            AttackDefinitionSO enemyMobile = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Mobile.asset",
                "Enemy_Mobile",
                "Feint Dash",
                0.95f,
                0.12f,
                0.08f,
                0.28f,
                1.7f,
                0.45f,
                0.35f);
            AttackDefinitionSO enemyRanged = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Ranged.asset",
                "Enemy_Ranged",
                "Arc Bolt",
                0.9f,
                0.22f,
                0.10f,
                0.32f,
                4.2f,
                0.35f,
                forwardMovement: 0f,
                projectilePrefab: spellBoltProjectile,
                projectileSpeed: 12f,
                projectileLifetimeSeconds: 1.1f,
                projectileSpawnOffset: 0.45f,
                projectileTrajectoryMode: ProjectileTrajectoryMode.Arc,
                projectileArcHeight: 1.1f);
            AttackDefinitionSO enemyBoss = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper.asset",
                "Enemy_Gatekeeper",
                "Gate Slam",
                1.6f,
                0.28f,
                0.12f,
                0.42f,
                2.3f,
                0.7f);
            AttackDefinitionSO enemyBossReach = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Reach.asset",
                "Enemy_Gatekeeper_Reach",
                "Hall Sweep",
                1.15f,
                0.4f,
                0.14f,
                0.55f,
                3.8f,
                0.9f);
            AttackDefinitionSO enemyBossProjectile = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Arc.asset",
                "Enemy_Gatekeeper_Arc",
                "Core Bolt",
                1.05f,
                0.34f,
                0.10f,
                0.48f,
                5.2f,
                0.45f,
                forwardMovement: 0f,
                projectilePrefab: spellBoltProjectile,
                projectileSpeed: 13f,
                projectileLifetimeSeconds: 1.2f,
                projectileSpawnOffset: 0.6f,
                projectileTrajectoryMode: ProjectileTrajectoryMode.Arc,
                projectileArcHeight: 1.2f);
            AttackDefinitionSO enemyBossBurst = CreateAttackAsset(
                "Assets/_Game/Data/Enemies/SO_Attack_Enemy_Gatekeeper_Burst.asset",
                "Enemy_Gatekeeper_Burst",
                "Gate Lance",
                0.92f,
                0.26f,
                0.10f,
                0.40f,
                4.7f,
                0.40f,
                forwardMovement: 0f,
                projectilePrefab: spellBoltProjectile,
                projectileSpeed: 17f,
                projectileLifetimeSeconds: 1.0f,
                projectileSpawnOffset: 0.55f,
                projectileTrajectoryMode: ProjectileTrajectoryMode.Straight,
                projectileArcHeight: 0f);

            EnemyArchetypeSO enemyArchetype = CreateOrLoadAsset<EnemyArchetypeSO>(
                "Assets/_Game/Data/Enemies/SO_Enemy_Melee.asset");
            EnemyArchetypeSO mobileArchetype = CreateOrLoadAsset<EnemyArchetypeSO>(
                "Assets/_Game/Data/Enemies/SO_Enemy_Mobile.asset");
            EnemyArchetypeSO rangedArchetype = CreateOrLoadAsset<EnemyArchetypeSO>(
                "Assets/_Game/Data/Enemies/SO_Enemy_Ranged.asset");
            EnemyArchetypeSO gatekeeperArchetype = CreateOrLoadAsset<EnemyArchetypeSO>(
                "Assets/_Game/Data/Enemies/SO_Enemy_Gatekeeper.asset");
            SkillDefinitionSO skillSpellBolt = CreateOrLoadAsset<SkillDefinitionSO>(
                "Assets/_Game/Data/Skills/SO_Skill_SpellBolt.asset");
            SkillDefinitionSO skillForceBurst = CreateOrLoadAsset<SkillDefinitionSO>(
                "Assets/_Game/Data/Skills/SO_Skill_ForceBurst.asset");

            ConfigureAudioSettings(audioSettings);
            ConfigurePlayerStats(playerStats);
            ConfigureCombatBalance(combatBalance);
            ConfigureMeleeArchetype(enemyArchetype, enemyMelee);
            ConfigureMobileArchetype(mobileArchetype, enemyMobile);
            ConfigureRangedArchetype(rangedArchetype, enemyRanged);
            ConfigureBossArchetype(gatekeeperArchetype, enemyBoss, enemyBossReach, enemyBossBurst, enemyBossProjectile);
            ConfigureBossTelegraphStyle(
                bossTelegraphStyle,
                bossGroundTelegraphPrefab,
                bossImpactMarkerPrefab,
                bossSpawnFlarePrefab,
                engageTelegraphMaterial,
                attackTelegraphMaterial,
                impactMarkerMaterial,
                spawnFlareMaterial);
            ConfigureSkill(
                skillSpellBolt,
                "Skill_SpellBolt",
                "Spell Bolt",
                20f,
                6f,
                0.18f,
                9f,
                1.6f,
                0.75f,
                SkillTargetMode.LockedTarget,
                spellBoltProjectile,
                14f,
                1.4f,
                0.5f,
                ProjectileTrajectoryMode.Straight,
                0f);
            ConfigureSkill(
                skillForceBurst,
                "Skill_ForceBurst",
                "Force Burst",
                35f,
                12f,
                0.30f,
                2.5f,
                2.2f,
                2.25f,
                SkillTargetMode.Self);
            EnsurePlayerCombatAnimationAssets(
                light01,
                light02,
                light03,
                heavy,
                dodgeFollowUp,
                counter,
                empoweredCounter,
                empoweredDodge);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Selection.activeObject = enemyArchetype;
            Debug.Log("CombatTest placeholder assets created or updated using the public-safe proxy baseline.");
        }

        public static RuntimeAnimatorController EnsurePlayerCombatAnimationAssets()
        {
            EnsureFolder("Assets/_Game/Animations");
            EnsureFolder("Assets/_Game/Animations/Characters");
            EnsureFolder(PlayerAnimationRootFolder);

            return EnsurePlayerCombatAnimationAssets(
                false,
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light01Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light02Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light03Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(DodgeFollowUpPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedCounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedDodgePath));
        }

        [MenuItem(RebuildImportedPlayerAnimationsMenu)]
        public static void RebuildPlayerCombatAnimationAssetsForLocalPreviewMenu()
        {
            if (EnsurePlayerCombatAnimationAssetsForLocalPreview() == null)
            {
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("CombatTest player animation assets rebuilt from imported preview sources. Treat these outputs as local-preview-only.");
        }

        public static RuntimeAnimatorController EnsurePlayerCombatAnimationAssetsForLocalPreview()
        {
            if (!CombatImportedPlayerVisualUtility.UseImportedPlayerSourcesForLocalPreview)
            {
                Debug.LogWarning(
                    "Imported player preview is disabled. Enable 'CampusRPG/Setup/CombatTest/Prefer Imported Player Sources When Available' before rebuilding imported preview animations.");
                return null;
            }

            if (!CombatImportedPlayerVisualUtility.HasPlayerVisualSource())
            {
                Debug.LogWarning("No imported player visual source was found. Install a supported player package first.");
                return null;
            }

            EnsureFolder("Assets/_Game/Animations");
            EnsureFolder("Assets/_Game/Animations/Characters");
            EnsureFolder(PlayerAnimationRootFolder);

            return EnsurePlayerCombatAnimationAssets(
                true,
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light01Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light02Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light03Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(DodgeFollowUpPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedCounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedDodgePath));
        }

        public static float GetPlayerDodgeAnimationDuration()
        {
            return GetConfiguredClipDuration(PlayerDodgeClipPath);
        }

        public static float GetPlayerHitAnimationDuration()
        {
            return GetConfiguredClipDuration(PlayerHitClipPath);
        }

        private static void ConfigurePlayerStats(PlayerBaseStatsSO asset)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("maxHealth").floatValue = 100f;
            serializedObject.FindProperty("maxMana").floatValue = 100f;
            serializedObject.FindProperty("attack").floatValue = 20f;
            serializedObject.FindProperty("defense").floatValue = 10f;
            serializedObject.FindProperty("moveSpeed").floatValue = 6f;
            serializedObject.FindProperty("rotationSpeed").floatValue = 720f;
            serializedObject.FindProperty("jumpHeight").floatValue = 1.6f;
            serializedObject.FindProperty("groundAcceleration").floatValue = 24f;
            serializedObject.FindProperty("groundDeceleration").floatValue = 20f;
            serializedObject.FindProperty("lockOnStrafeSpeedScale").floatValue = 0.92f;
            serializedObject.FindProperty("lockOnBackwardSpeedScale").floatValue = 0.82f;
            serializedObject.FindProperty("mantleDurationSeconds").floatValue = 0.22f;
            serializedObject.FindProperty("mantleMinHeight").floatValue = 0.5f;
            serializedObject.FindProperty("mantleMaxHeight").floatValue = 1.25f;
            serializedObject.FindProperty("mantleForwardDistance").floatValue = 0.8f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureCombatBalance(CombatBalanceSO asset)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("inputBufferSeconds").floatValue = 0.2f;
            serializedObject.FindProperty("counterWindowSeconds").floatValue = 0.8f;
            serializedObject.FindProperty("dodgeFollowUpWindowSeconds").floatValue = 0.8f;
            serializedObject.FindProperty("dodgeDurationSeconds").floatValue = 0.25f;
            serializedObject.FindProperty("dodgeInvulnerableSeconds").floatValue = 0.2f;
            serializedObject.FindProperty("dodgeDistance").floatValue = 2.8f;
            serializedObject.FindProperty("dodgeBackwardDistanceScale").floatValue = 0.88f;
            serializedObject.FindProperty("guardCounterGaugeGain").floatValue = 20f;
            serializedObject.FindProperty("dodgeAgilityGaugeGain").floatValue = 25f;
            serializedObject.FindProperty("defaultHitStopSeconds").floatValue = 0.05f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureMeleeArchetype(EnemyArchetypeSO asset, AttackDefinitionSO enemyMelee)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("archetypeType").enumValueIndex = (int)EnemyArchetypeType.Melee;
            serializedObject.FindProperty("maxHealth").floatValue = 60f;
            serializedObject.FindProperty("baseAttack").floatValue = 10f;
            serializedObject.FindProperty("hitStunSeconds").floatValue = 0.18f;
            serializedObject.FindProperty("moveSpeed").floatValue = 3.5f;
            serializedObject.FindProperty("aggroDistance").floatValue = 10f;
            serializedObject.FindProperty("engageDurationSeconds").floatValue = 0f;
            serializedObject.FindProperty("attackDistance").floatValue = 2f;
            serializedObject.FindProperty("attackCooldown").floatValue = 1.2f;
            serializedObject.FindProperty("preferredCombatDistance").floatValue = 1.5f;
            serializedObject.FindProperty("strafeDistance").floatValue = 0.9f;
            serializedObject.FindProperty("strafeDurationSeconds").floatValue = 0.2f;
            serializedObject.FindProperty("dropTableId").stringValue = "Default";

            SerializedProperty attacks = serializedObject.FindProperty("attacks");
            attacks.arraySize = 1;
            attacks.GetArrayElementAtIndex(0).objectReferenceValue = enemyMelee;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureMobileArchetype(EnemyArchetypeSO asset, AttackDefinitionSO enemyMobile)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("archetypeType").enumValueIndex = (int)EnemyArchetypeType.Mobile;
            serializedObject.FindProperty("maxHealth").floatValue = 52f;
            serializedObject.FindProperty("baseAttack").floatValue = 9f;
            serializedObject.FindProperty("hitStunSeconds").floatValue = 0.14f;
            serializedObject.FindProperty("moveSpeed").floatValue = 4.6f;
            serializedObject.FindProperty("aggroDistance").floatValue = 11f;
            serializedObject.FindProperty("engageDurationSeconds").floatValue = 0f;
            serializedObject.FindProperty("attackDistance").floatValue = 1.8f;
            serializedObject.FindProperty("attackCooldown").floatValue = 1.05f;
            serializedObject.FindProperty("preferredCombatDistance").floatValue = 1.45f;
            serializedObject.FindProperty("strafeDistance").floatValue = 1.1f;
            serializedObject.FindProperty("strafeDurationSeconds").floatValue = 0.45f;
            serializedObject.FindProperty("dropTableId").stringValue = "Default";

            SerializedProperty attacks = serializedObject.FindProperty("attacks");
            attacks.arraySize = 1;
            attacks.GetArrayElementAtIndex(0).objectReferenceValue = enemyMobile;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureRangedArchetype(EnemyArchetypeSO asset, AttackDefinitionSO enemyRanged)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("archetypeType").enumValueIndex = (int)EnemyArchetypeType.Ranged;
            serializedObject.FindProperty("maxHealth").floatValue = 44f;
            serializedObject.FindProperty("baseAttack").floatValue = 8f;
            serializedObject.FindProperty("hitStunSeconds").floatValue = 0.16f;
            serializedObject.FindProperty("moveSpeed").floatValue = 3.8f;
            serializedObject.FindProperty("aggroDistance").floatValue = 13f;
            serializedObject.FindProperty("engageDurationSeconds").floatValue = 0f;
            serializedObject.FindProperty("attackDistance").floatValue = 4.2f;
            serializedObject.FindProperty("attackCooldown").floatValue = 1.35f;
            serializedObject.FindProperty("preferredCombatDistance").floatValue = 3.2f;
            serializedObject.FindProperty("strafeDistance").floatValue = 0.6f;
            serializedObject.FindProperty("strafeDurationSeconds").floatValue = 0.18f;
            serializedObject.FindProperty("dropTableId").stringValue = "Default";

            SerializedProperty attacks = serializedObject.FindProperty("attacks");
            attacks.arraySize = 1;
            attacks.GetArrayElementAtIndex(0).objectReferenceValue = enemyRanged;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureBossArchetype(
            EnemyArchetypeSO asset,
            AttackDefinitionSO attackClose,
            AttackDefinitionSO attackReach,
            AttackDefinitionSO attackBurst,
            AttackDefinitionSO attackProjectile)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("archetypeType").enumValueIndex = (int)EnemyArchetypeType.Boss;
            serializedObject.FindProperty("maxHealth").floatValue = 180f;
            serializedObject.FindProperty("baseAttack").floatValue = 16f;
            serializedObject.FindProperty("hitStunSeconds").floatValue = 0.12f;
            serializedObject.FindProperty("moveSpeed").floatValue = 3.2f;
            serializedObject.FindProperty("aggroDistance").floatValue = 14f;
            serializedObject.FindProperty("engageDurationSeconds").floatValue = 0.85f;
            serializedObject.FindProperty("attackDistance").floatValue = 2.2f;
            serializedObject.FindProperty("attackCooldown").floatValue = 1.5f;
            serializedObject.FindProperty("preferredCombatDistance").floatValue = 2.1f;
            serializedObject.FindProperty("strafeDistance").floatValue = 0.75f;
            serializedObject.FindProperty("strafeDurationSeconds").floatValue = 0.3f;
            serializedObject.FindProperty("dropTableId").stringValue = "BossGatekeeper";

            SerializedProperty attacks = serializedObject.FindProperty("attacks");
            attacks.arraySize = 4;
            attacks.GetArrayElementAtIndex(0).objectReferenceValue = attackClose;
            attacks.GetArrayElementAtIndex(1).objectReferenceValue = attackReach;
            attacks.GetArrayElementAtIndex(2).objectReferenceValue = attackBurst;
            attacks.GetArrayElementAtIndex(3).objectReferenceValue = attackProjectile;

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static void ConfigureSkill(
            SkillDefinitionSO asset,
            string skillId,
            string displayName,
            float manaCost,
            float cooldownSeconds,
            float castDurationSeconds,
            float range,
            float damageMultiplier,
            float impactRadius,
            SkillTargetMode targetMode,
            GameObject projectilePrefab = null,
            float projectileSpeed = 0f,
            float projectileLifetimeSeconds = 0f,
            float projectileSpawnOffset = 0f,
            ProjectileTrajectoryMode projectileTrajectoryMode = ProjectileTrajectoryMode.PrefabDefault,
            float projectileArcHeight = 0f)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("skillId").stringValue = skillId;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("manaCost").floatValue = manaCost;
            serializedObject.FindProperty("cooldownSeconds").floatValue = cooldownSeconds;
            serializedObject.FindProperty("castDurationSeconds").floatValue = castDurationSeconds;
            serializedObject.FindProperty("range").floatValue = range;
            serializedObject.FindProperty("damageMultiplier").floatValue = damageMultiplier;
            serializedObject.FindProperty("impactRadius").floatValue = impactRadius;
            serializedObject.FindProperty("targetMode").enumValueIndex = (int)targetMode;
            serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedObject.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedObject.FindProperty("projectileLifetimeSeconds").floatValue = projectileLifetimeSeconds;
            serializedObject.FindProperty("projectileSpawnOffset").floatValue = projectileSpawnOffset;
            serializedObject.FindProperty("projectileTrajectoryMode").enumValueIndex = (int)projectileTrajectoryMode;
            serializedObject.FindProperty("projectileArcHeight").floatValue = projectileArcHeight;
            serializedObject.FindProperty("effectPrefab").objectReferenceValue = null;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static AttackDefinitionSO CreateAttackAsset(
            string path,
            string attackId,
            string displayName,
            float damageMultiplier,
            float startup,
            float active,
            float recovery,
            float range,
            float radius,
            float forwardMovement = 0.5f,
            AttackHitboxActivationMode hitboxActivationMode = AttackHitboxActivationMode.TimedWindow,
            GameObject projectilePrefab = null,
            float projectileSpeed = 0f,
            float projectileLifetimeSeconds = 0f,
            float projectileSpawnOffset = 0f,
            ProjectileTrajectoryMode projectileTrajectoryMode = ProjectileTrajectoryMode.PrefabDefault,
            float projectileArcHeight = 0f)
        {
            AttackDefinitionSO asset = CreateOrLoadAsset<AttackDefinitionSO>(path);
            SerializedObject serializedObject = new SerializedObject(asset);
            serializedObject.FindProperty("attackId").stringValue = attackId;
            serializedObject.FindProperty("displayName").stringValue = displayName;
            serializedObject.FindProperty("animationStateName").stringValue = attackId;
            serializedObject.FindProperty("damageMultiplier").floatValue = damageMultiplier;
            serializedObject.FindProperty("startupSeconds").floatValue = startup;
            serializedObject.FindProperty("activeSeconds").floatValue = active;
            serializedObject.FindProperty("recoverySeconds").floatValue = recovery;
            serializedObject.FindProperty("hitStopSeconds").floatValue = 0.05f;
            serializedObject.FindProperty("forwardMovement").floatValue = forwardMovement;
            serializedObject.FindProperty("range").floatValue = range;
            serializedObject.FindProperty("radius").floatValue = radius;
            serializedObject.FindProperty("hitboxShape").enumValueIndex = projectilePrefab == null
                ? (int)AttackHitboxShape.Box
                : (int)AttackHitboxShape.LegacyForwardSphere;
            serializedObject.FindProperty("hitboxLocalCenter").vector3Value = projectilePrefab == null
                ? new Vector3(0f, 0f, range * 0.5f)
                : new Vector3(0f, 0f, range);
            serializedObject.FindProperty("hitboxHalfExtents").vector3Value = new Vector3(
                Mathf.Max(radius * 1.15f, 0.35f),
                Mathf.Max(radius * 1.25f, 0.45f),
                Mathf.Max(range * 0.4f, radius));
            serializedObject.FindProperty("hitboxRadius").floatValue = radius;
            serializedObject.FindProperty("hitboxActivationMode").enumValueIndex = (int)hitboxActivationMode;
            serializedObject.FindProperty("projectilePrefab").objectReferenceValue = projectilePrefab;
            serializedObject.FindProperty("projectileSpeed").floatValue = projectileSpeed;
            serializedObject.FindProperty("projectileLifetimeSeconds").floatValue = projectileLifetimeSeconds;
            serializedObject.FindProperty("projectileSpawnOffset").floatValue = projectileSpawnOffset;
            serializedObject.FindProperty("projectileTrajectoryMode").enumValueIndex = (int)projectileTrajectoryMode;
            serializedObject.FindProperty("projectileArcHeight").floatValue = projectileArcHeight;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static RuntimeAnimatorController EnsurePlayerCombatAnimationAssets(bool allowImportedPlayerPreview, params AttackDefinitionSO[] attackDefinitions)
        {
            bool previousAllowImportedPreview = allowImportedPlayerAnimationPreviewBuild;
            allowImportedPlayerAnimationPreviewBuild = allowImportedPlayerPreview;

            try
            {
                AnimationClip idleClip = CreateOrUpdatePlayerIdleClip();
                AnimationClip walkForwardClip = CreateOrUpdatePlayerWalkForwardClip();
                AnimationClip walkBackwardClip = CreateOrUpdatePlayerWalkBackwardClip();
                AnimationClip walkLeftClip = CreateOrUpdatePlayerWalkLeftClip();
                AnimationClip walkRightClip = CreateOrUpdatePlayerWalkRightClip();
                AnimationClip runForwardClip = CreateOrUpdatePlayerRunForwardClip();
                AnimationClip runBackwardClip = CreateOrUpdatePlayerRunBackwardClip();
                AnimationClip runLeftClip = CreateOrUpdatePlayerRunLeftClip();
                AnimationClip runRightClip = CreateOrUpdatePlayerRunRightClip();
                AnimationClip runForwardLeftClip = CreateOrUpdatePlayerRunForwardLeftClip();
                AnimationClip runForwardRightClip = CreateOrUpdatePlayerRunForwardRightClip();
                AnimationClip runBackwardLeftClip = CreateOrUpdatePlayerRunBackwardLeftClip();
                AnimationClip runBackwardRightClip = CreateOrUpdatePlayerRunBackwardRightClip();
                AnimationClip airborneClip = CreateOrUpdatePlayerAirborneClip();
                AnimationClip blockClip = CreateOrUpdatePlayerBlockClip();
                AnimationClip dodgeClip = CreateOrUpdatePlayerDodgeClip();
                AnimationClip hitClip = CreateOrUpdatePlayerHitClip();
                AnimationClip deathClip = CreateOrUpdatePlayerDeathClip();
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);

                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorControllerPath);
                }

                AnimatorControllerLayer layer = EnsureBaseLayer(controller);
                AnimatorStateMachine stateMachine = layer.stateMachine;
                ClearStateMachine(stateMachine);
                EnsurePlayerAnimatorParameters(controller);

                BlendTree locomotionBlendTree = CreateOrUpdateLocomotionBlendTree(
                    controller,
                    idleClip,
                    walkForwardClip,
                    walkBackwardClip,
                    walkLeftClip,
                    walkRightClip,
                    runForwardClip,
                    runBackwardClip,
                    runLeftClip,
                    runRightClip,
                    runForwardLeftClip,
                    runForwardRightClip,
                    runBackwardLeftClip,
                    runBackwardRightClip);

                AnimatorState locomotionState = stateMachine.AddState(PlayerLocomotionStateName);
                locomotionState.motion = locomotionBlendTree;
                stateMachine.defaultState = locomotionState;

                AnimatorState blockState = stateMachine.AddState(PlayerBlockStateName);
                blockState.motion = blockClip;

                AnimatorState airborneState = stateMachine.AddState(PlayerAirborneStateName);
                airborneState.motion = airborneClip;

                AnimatorState dodgeState = stateMachine.AddState(PlayerDodgeStateName);
                dodgeState.motion = dodgeClip;

                AnimatorState hitState = stateMachine.AddState(PlayerHitStateName);
                hitState.motion = hitClip;

                AnimatorState deathState = stateMachine.AddState(PlayerDeathStateName);
                deathState.motion = deathClip;

                AddBlockingTransition(locomotionState, blockState, true);
                AddBlockingTransition(blockState, locomotionState, false);
                AddGroundedTransition(locomotionState, airborneState, false);
                AddGroundedTransition(blockState, airborneState, false);
                AddAirborneRecoveryTransition(airborneState, locomotionState, false);
                AddAirborneRecoveryTransition(airborneState, blockState, true);
                AddReturnToLocomotionTransition(dodgeState, locomotionState);
                AddReturnToLocomotionTransition(hitState, locomotionState);

                for (int i = 0; i < attackDefinitions.Length; i++)
                {
                    AttackDefinitionSO attackDefinition = attackDefinitions[i];

                    if (attackDefinition == null || string.IsNullOrWhiteSpace(attackDefinition.AnimationStateName))
                    {
                        continue;
                    }

                    AnimationClip attackClip = CreateOrUpdateAttackClip(attackDefinition);
                    SyncAttackAnimationMetadata(attackDefinition, attackClip);
                    AnimatorState attackState = stateMachine.AddState(attackDefinition.AnimationStateName);
                    attackState.motion = attackClip;

                    AddReturnToLocomotionTransition(attackState, locomotionState);
                }

                EditorUtility.SetDirty(stateMachine);
                EditorUtility.SetDirty(controller);
                return controller;
            }
            finally
            {
                allowImportedPlayerAnimationPreviewBuild = previousAllowImportedPreview;
            }
        }

        private static void EnsurePlayerAnimatorParameters(AnimatorController controller)
        {
            if (controller == null)
            {
                return;
            }

            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int i = parameters.Length - 1; i >= 0; i--)
            {
                controller.RemoveParameter(parameters[i]);
            }

            controller.AddParameter(GroundSpeedParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveXParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(MoveYParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(IsGroundedParameterName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(IsBlockingParameterName, AnimatorControllerParameterType.Bool);
            controller.AddParameter(VerticalSpeedParameterName, AnimatorControllerParameterType.Float);
        }

        private static BlendTree CreateOrUpdateLocomotionBlendTree(
            AnimatorController controller,
            Motion idleMotion,
            Motion walkForwardMotion,
            Motion walkBackwardMotion,
            Motion walkLeftMotion,
            Motion walkRightMotion,
            Motion runForwardMotion,
            Motion runBackwardMotion,
            Motion runLeftMotion,
            Motion runRightMotion,
            Motion runForwardLeftMotion,
            Motion runForwardRightMotion,
            Motion runBackwardLeftMotion,
            Motion runBackwardRightMotion)
        {
            BlendTree blendTree = LoadBlendTreeAsset(PlayerLocomotionBlendTreeName);

            if (blendTree == null)
            {
                blendTree = new BlendTree
                {
                    name = PlayerLocomotionBlendTreeName
                };
                AssetDatabase.AddObjectToAsset(blendTree, controller);
            }

            blendTree.blendType = BlendTreeType.FreeformCartesian2D;
            blendTree.blendParameter = MoveXParameterName;
            blendTree.blendParameterY = MoveYParameterName;
            blendTree.children = new[]
            {
                CreateCartesianBlendChild(idleMotion, 0f, 0f),
                CreateCartesianBlendChild(walkForwardMotion, 0f, 0.5f),
                CreateCartesianBlendChild(walkBackwardMotion, 0f, -0.5f),
                CreateCartesianBlendChild(walkLeftMotion, -0.5f, 0f),
                CreateCartesianBlendChild(walkRightMotion, 0.5f, 0f),
                CreateCartesianBlendChild(runForwardMotion, 0f, 1f),
                CreateCartesianBlendChild(runBackwardMotion, 0f, -1f),
                CreateCartesianBlendChild(runLeftMotion, -1f, 0f),
                CreateCartesianBlendChild(runRightMotion, 1f, 0f),
                CreateCartesianBlendChild(runForwardLeftMotion, -0.85f, 0.85f),
                CreateCartesianBlendChild(runForwardRightMotion, 0.85f, 0.85f),
                CreateCartesianBlendChild(runBackwardLeftMotion, -0.85f, -0.85f),
                CreateCartesianBlendChild(runBackwardRightMotion, 0.85f, -0.85f)
            };
            EditorUtility.SetDirty(blendTree);
            return blendTree;
        }

        private static BlendTree LoadBlendTreeAsset(string blendTreeName)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(PlayerAnimatorControllerPath);

            for (int i = 0; i < assets.Length; i++)
            {
                BlendTree blendTree = assets[i] as BlendTree;

                if (blendTree != null && string.Equals(blendTree.name, blendTreeName, global::System.StringComparison.Ordinal))
                {
                    return blendTree;
                }
            }

            return null;
        }

        private static ChildMotion CreateCartesianBlendChild(Motion motion, float positionX, float positionY)
        {
            return new ChildMotion
            {
                motion = motion,
                position = new Vector2(positionX, positionY),
                timeScale = 1f
            };
        }

        private static void AddBlockingTransition(AnimatorState fromState, AnimatorState toState, bool blockingValue)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                blockingValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                IsBlockingParameterName);
        }

        private static void AddGroundedTransition(AnimatorState fromState, AnimatorState toState, bool groundedValue)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(
                groundedValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                IsGroundedParameterName);
        }

        private static void AddAirborneRecoveryTransition(AnimatorState fromState, AnimatorState toState, bool blockingValue)
        {
            AnimatorStateTransition transition = fromState.AddTransition(toState);
            transition.hasExitTime = false;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
            transition.AddCondition(AnimatorConditionMode.If, 0f, IsGroundedParameterName);
            transition.AddCondition(
                blockingValue ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot,
                0f,
                IsBlockingParameterName);
        }

        private static void AddReturnToLocomotionTransition(AnimatorState fromState, AnimatorState locomotionState)
        {
            AnimatorStateTransition transition = fromState.AddTransition(locomotionState);
            transition.hasExitTime = true;
            transition.exitTime = 1f;
            transition.hasFixedDuration = true;
            transition.duration = 0.05f;
            transition.canTransitionToSelf = false;
        }

        private static AnimatorControllerLayer EnsureBaseLayer(AnimatorController controller)
        {
            if (controller.layers != null && controller.layers.Length > 0)
            {
                return controller.layers[0];
            }

            AnimatorControllerLayer baseLayer = new AnimatorControllerLayer
            {
                name = "Base Layer",
                defaultWeight = 1f,
                stateMachine = new AnimatorStateMachine()
            };

            controller.AddLayer(baseLayer);
            return controller.layers[0];
        }

        private readonly struct ProxyCurveKey
        {
            public ProxyCurveKey(float time, float value)
            {
                Time = time;
                Value = value;
            }

            public float Time { get; }

            public float Value { get; }
        }

        private enum PlayerProxyMotionProfile
        {
            Idle,
            Light01,
            Light02,
            Light03,
            Heavy01,
            DodgeFollowUp,
            DodgeFollowUpEnhanced,
            Counter,
            CounterEnhanced,
            GenericAttack
        }

        private static void ClearStateMachine(AnimatorStateMachine stateMachine)
        {
            AnimatorStateTransition[] anyStateTransitions = stateMachine.anyStateTransitions;

            for (int i = anyStateTransitions.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveAnyStateTransition(anyStateTransitions[i]);
            }

            AnimatorTransition[] entryTransitions = stateMachine.entryTransitions;

            for (int i = entryTransitions.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveEntryTransition(entryTransitions[i]);
            }

            ChildAnimatorState[] states = stateMachine.states;

            for (int i = states.Length - 1; i >= 0; i--)
            {
                stateMachine.RemoveState(states[i].state);
            }
        }

        private static AnimationClip CreateOrUpdateAttackClip(AttackDefinitionSO attackDefinition)
        {
            float duration = Mathf.Max(
                0.12f,
                attackDefinition.StartupSeconds + attackDefinition.ActiveSeconds + attackDefinition.RecoverySeconds);
            float openTime = Mathf.Clamp(attackDefinition.StartupSeconds, 0f, Mathf.Max(0f, duration - 0.04f));
            float minimumWindow = Mathf.Min(0.05f, Mathf.Max(0.01f, duration * 0.1f));
            float closeTime = Mathf.Clamp(
                attackDefinition.StartupSeconds + Mathf.Max(attackDefinition.ActiveSeconds, minimumWindow),
                openTime + 0.01f,
                Mathf.Max(openTime + 0.01f, duration - 0.001f));

            AnimationEvent[] animationEvents =
            {
                new AnimationEvent
                {
                    functionName = "AnimationEvent_OpenAttackHitbox",
                    time = openTime
                },
                new AnimationEvent
                {
                    functionName = "AnimationEvent_CloseAttackHitbox",
                    time = closeTime
                }
            };

            AnimationClip importedClip = TryLoadImportedPlayerClip(ResolveImportedAttackClipCandidatePaths(attackDefinition.AnimationStateName));

            if (importedClip != null)
            {
                float importedAttackDuration = ResolveImportedAttackDuration(importedClip.length, duration);
                AnimationClip clip = CreateOrUpdateImportedClip(
                    GetPlayerAttackClipPath(attackDefinition.AnimationStateName),
                    importedClip,
                    importedAttackDuration,
                    false,
                    animationEvents);
                ApplyPlayerProxyMotionCurves(
                    clip,
                    ResolvePlayerProxyMotionProfile(GetPlayerAttackClipPath(attackDefinition.AnimationStateName), attackDefinition.AnimationStateName),
                    importedAttackDuration,
                    openTime,
                    closeTime);
                EditorUtility.SetDirty(clip);
                return clip;
            }

            return CreateOrUpdatePlaceholderClip(
                GetPlayerAttackClipPath(attackDefinition.AnimationStateName),
                ResolvePlaceholderAttackDuration(duration),
                false,
                animationEvents,
                attackDefinition.AnimationStateName,
                openTime,
                closeTime);
        }

        private static string GetPlayerAttackClipPath(string animationStateName)
        {
            return $"{PlayerAnimationRootFolder}/AN_Player_{animationStateName}_CombatTest.anim";
        }

        private static AnimationClip CreateOrUpdatePlayerIdleClip()
        {
            AnimationClip importedClip = TryLoadImportedPlayerClip(ResolveImportedIdleClipCandidatePaths());

            if (importedClip != null)
            {
                return CreateOrUpdateImportedClip(
                    PlayerIdleClipPath,
                    importedClip,
                    importedClip.length,
                    true,
                    System.Array.Empty<AnimationEvent>());
            }

            return CreateOrUpdatePlaceholderClip(PlayerIdleClipPath, 1f, true, System.Array.Empty<AnimationEvent>());
        }

        private static AnimationClip CreateOrUpdatePlayerWalkForwardClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionWalkForwardClipPath,
                ResolveImportedWalkForwardClipCandidatePaths(),
                0.9f,
                true,
                "Player Walk Forward");
        }

        private static AnimationClip CreateOrUpdatePlayerWalkBackwardClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionWalkBackwardClipPath,
                ResolveImportedWalkBackwardClipCandidatePaths(),
                0.9f,
                true,
                "Player Walk Backward");
        }

        private static AnimationClip CreateOrUpdatePlayerWalkLeftClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionWalkLeftClipPath,
                ResolveImportedWalkLeftClipCandidatePaths(),
                0.9f,
                true,
                "Player Walk Left");
        }

        private static AnimationClip CreateOrUpdatePlayerWalkRightClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionWalkRightClipPath,
                ResolveImportedWalkRightClipCandidatePaths(),
                0.9f,
                true,
                "Player Walk Right");
        }

        private static AnimationClip CreateOrUpdatePlayerRunForwardClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunForwardClipPath,
                ResolveImportedRunForwardClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Forward");
        }

        private static AnimationClip CreateOrUpdatePlayerRunBackwardClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunBackwardClipPath,
                ResolveImportedRunBackwardClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Backward");
        }

        private static AnimationClip CreateOrUpdatePlayerRunLeftClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunLeftClipPath,
                ResolveImportedRunLeftClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Left");
        }

        private static AnimationClip CreateOrUpdatePlayerRunRightClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunRightClipPath,
                ResolveImportedRunRightClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Right");
        }

        private static AnimationClip CreateOrUpdatePlayerRunForwardLeftClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunForwardLeftClipPath,
                ResolveImportedRunForwardLeftClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Forward Left");
        }

        private static AnimationClip CreateOrUpdatePlayerRunForwardRightClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunForwardRightClipPath,
                ResolveImportedRunForwardRightClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Forward Right");
        }

        private static AnimationClip CreateOrUpdatePlayerRunBackwardLeftClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunBackwardLeftClipPath,
                ResolveImportedRunBackwardLeftClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Backward Left");
        }

        private static AnimationClip CreateOrUpdatePlayerRunBackwardRightClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerLocomotionRunBackwardRightClipPath,
                ResolveImportedRunBackwardRightClipCandidatePaths(),
                0.8f,
                true,
                "Player Run Backward Right");
        }

        private static AnimationClip CreateOrUpdatePlayerAirborneClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerAirborneClipPath,
                ResolveImportedAirborneClipCandidatePaths(),
                0.6f,
                true,
                "Player Airborne");
        }

        private static AnimationClip CreateOrUpdatePlayerBlockClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerBlockClipPath,
                ResolveImportedBlockClipCandidatePaths(),
                0.8f,
                true,
                "Player Block");
        }

        private static AnimationClip CreateOrUpdatePlayerDodgeClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerDodgeClipPath,
                ResolveImportedDodgeClipCandidatePaths(),
                0.4f,
                false,
                "Player Dodge",
                0.42f);
        }

        private static AnimationClip CreateOrUpdatePlayerHitClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerHitClipPath,
                ResolveImportedHitClipCandidatePaths(),
                0.35f,
                false,
                "Player Hit",
                0.38f);
        }

        private static AnimationClip CreateOrUpdatePlayerDeathClip()
        {
            return CreateOrUpdateMotionClip(
                PlayerDeathClipPath,
                ResolveImportedDeathClipCandidatePaths(),
                1.2f,
                false,
                "Player Death");
        }

        private static AnimationClip CreateOrUpdateMotionClip(
            string targetPath,
            string[] candidatePaths,
            float fallbackDuration,
            bool loopTime,
            string fallbackName,
            float importedDuration = -1f)
        {
            AnimationClip importedClip = TryLoadImportedPlayerClip(candidatePaths);

            if (importedClip != null)
            {
                float resolvedDuration = importedDuration > 0f
                    ? Mathf.Min(importedDuration, importedClip.length)
                    : importedClip.length;
                return CreateOrUpdateImportedClip(
                    targetPath,
                    importedClip,
                    resolvedDuration,
                    loopTime,
                    System.Array.Empty<AnimationEvent>());
            }

            float placeholderDuration = importedDuration > 0f
                ? Mathf.Max(fallbackDuration, importedDuration)
                : fallbackDuration;
            return CreateOrUpdatePlaceholderClip(
                targetPath,
                placeholderDuration,
                loopTime,
                System.Array.Empty<AnimationEvent>(),
                fallbackName);
        }

        private static AnimationClip CreateOrUpdateImportedClip(
            string path,
            AnimationClip sourceClip,
            float duration,
            bool loopTime,
            AnimationEvent[] animationEvents)
        {
            if (sourceClip == null)
            {
                return null;
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            EditorUtility.CopySerialized(sourceClip, clip);
            clip.name = System.IO.Path.GetFileNameWithoutExtension(path);
            AnimationUtility.SetAnimationEvents(clip, animationEvents ?? System.Array.Empty<AnimationEvent>());
            float clipDuration = Mathf.Max(0.01f, duration);

            if (sourceClip.length > 0f)
            {
                clipDuration = Mathf.Min(clipDuration, sourceClip.length);
            }

            ConfigureClipSettings(clip, clipDuration, loopTime);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static float ResolveImportedAttackDuration(float sourceDuration, float gameplayDuration)
        {
            if (sourceDuration <= 0f)
            {
                return gameplayDuration;
            }

            float settleExtension = Mathf.Clamp(gameplayDuration * 0.18f, 0.05f, 0.12f);
            float targetDuration = gameplayDuration + settleExtension;
            return Mathf.Clamp(targetDuration, gameplayDuration, sourceDuration);
        }

        private static float ResolvePlaceholderAttackDuration(float gameplayDuration)
        {
            float settleExtension = Mathf.Clamp(gameplayDuration * 0.18f, 0.05f, 0.12f);
            return gameplayDuration + settleExtension;
        }

        private static void SyncAttackAnimationMetadata(AttackDefinitionSO attackDefinition, AnimationClip attackClip)
        {
            if (attackDefinition == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(attackDefinition);
            SerializedProperty durationProperty = serializedObject.FindProperty("animationDurationSeconds");

            if (durationProperty == null)
            {
                return;
            }

            durationProperty.floatValue = attackClip != null
                ? ResolveClipConfiguredDuration(attackClip)
                : 0f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(attackDefinition);
        }

        private static AnimationClip TryLoadImportedPlayerClip(string[] candidatePaths)
        {
            if (!allowImportedPlayerAnimationPreviewBuild
                || !CombatImportedPlayerVisualUtility.ShouldUseImportedPlayerSources
                || !CombatImportedPlayerVisualUtility.HasPlayerVisualSource()
                || candidatePaths == null)
            {
                return null;
            }

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                AnimationClip clip = LoadAnimationClipAsset(candidatePaths[i]);

                if (clip != null)
                {
                    return clip;
                }
            }

            return null;
        }

        private static AnimationClip LoadAnimationClipAsset(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
            {
                return null;
            }

            string clipName = null;
            int separatorIndex = assetPath.LastIndexOf('#');

            if (separatorIndex >= 0 && separatorIndex < assetPath.Length - 1)
            {
                clipName = assetPath.Substring(separatorIndex + 1);
                assetPath = assetPath.Substring(0, separatorIndex);
            }

            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(assetPath);

            if (clip != null && (clipName == null || string.Equals(clip.name, clipName, global::System.StringComparison.Ordinal)))
            {
                return clip;
            }

            Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);

            for (int i = 0; i < assets.Length; i++)
            {
                clip = assets[i] as AnimationClip;

                if (clip == null || string.Equals(clip.name, "__preview__", global::System.StringComparison.Ordinal))
                {
                    continue;
                }

                if (clipName == null || string.Equals(clip.name, clipName, global::System.StringComparison.Ordinal))
                {
                    return clip;
                }
            }

            return null;
        }

        private static string[] ResolveImportedIdleClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim",
                "Assets/DoubleL/One Hand Up/Movement/Idle/Idle/1Hand_Up_Stand_Idle_A_2.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx"
            };
        }

        private static string[] ResolveImportedWalkForwardClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
                "Assets/ithappy/Creative_Characters_FREE/Animations/Other_Animations/Walk_Forward.anim"
            };
        }

        private static string[] ResolveImportedWalkBackwardClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_B_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_B_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Backward.fbx"
            };
        }

        private static string[] ResolveImportedWalkLeftClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_L_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_L90_A_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Left.fbx"
            };
        }

        private static string[] ResolveImportedWalkRightClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_R_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_R90_A_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Right.fbx"
            };
        }

        private static string[] ResolveImportedRunForwardClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
                "Assets/ithappy/Creative_Characters_FREE/Animations/Other_Animations/Run_Forward.anim"
            };
        }

        private static string[] ResolveImportedRunBackwardClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_B_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Backward.fbx"
            };
        }

        private static string[] ResolveImportedRunLeftClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_L_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_L90_A_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Left.fbx"
            };
        }

        private static string[] ResolveImportedRunRightClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_R_InPlace.anim",
                "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_R90_A_InPlace.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Right.fbx"
            };
        }

        private static string[] ResolveImportedRunForwardLeftClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_ForwardLeft.fbx"
            };
        }

        private static string[] ResolveImportedRunForwardRightClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_ForwardRight.fbx"
            };
        }

        private static string[] ResolveImportedRunBackwardLeftClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_BackwardLeft.fbx"
            };
        }

        private static string[] ResolveImportedRunBackwardRightClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_BackwardRight.fbx"
            };
        }

        private static string[] ResolveImportedAirborneClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Fall01.fbx",
                "Assets/ithappy/Creative_Characters_FREE/Animations/Other_Animations/Jump_Loop.anim",
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim"
            };
        }

        private static string[] ResolveImportedBlockClipCandidatePaths()
        {
            return new[]
            {
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Idle.anim",
                "Assets/DoubleL/One Hand Up/Sheild/Idle/1Hand_Up_Shield_Block_Idle_1.fbx",
                "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx#Block_With_Hands"
            };
        }

        private static string[] ResolveImportedDodgeClipCandidatePaths()
        {
            return new[]
            {
                "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx#Dodge_Sidestep",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Begin.fbx",
                "Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim"
            };
        }

        private static string[] ResolveImportedHitClipCandidatePaths()
        {
            return new[]
            {
                "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx#Hit_Reaction_Light",
                "Assets/DoubleL/Demo/Anim/Hit_F_1_InPlace.anim",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx"
            };
        }

        private static string[] ResolveImportedDeathClipCandidatePaths()
        {
            return new[]
            {
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
                "Assets/ithappy/Creative_Characters_FREE/Animations/Animation_Mesh/Aminset_Basic.fbx#Death_Forward"
            };
        }

        private static string[] ResolveImportedAttackClipCandidatePaths(string animationStateName)
        {
            switch (animationStateName)
            {
                case "Light_01":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx"
                    };
                case "Light_02":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx"
                    };
                case "Light_03":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx"
                    };
                case "Heavy_01":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx"
                    };
                case "DodgeFollowUp":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_1_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_1_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx"
                    };
                case "DodgeFollowUp_Enhanced":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx"
                    };
                case "Counter":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_2_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_2_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_2_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx"
                    };
                case "Counter_Enhanced":
                    return new[]
                    {
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_A/InPlace/1Hand_Up_Attack_A_3_InPlace.fbx",
                        "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_3_InPlace.anim",
                        "Assets/DoubleL/One Hand Up/Attack_B/InPlace/1Hand_Up_Attack_B_3_InPlace.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx"
                    };
                default:
                    return null;
            }
        }

        private static AnimationClip CreateOrUpdatePlaceholderClip(
            string path,
            float duration,
            bool loopTime,
            AnimationEvent[] animationEvents,
            string animationStateName = null,
            float openTime = 0f,
            float closeTime = 0f)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

            if (clip == null)
            {
                clip = new AnimationClip();
                AssetDatabase.CreateAsset(clip, path);
            }

            ClearClipContent(clip);
            clip.frameRate = 60f;

            EditorCurveBinding scaleXBinding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.x");
            EditorCurveBinding scaleYBinding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.y");
            EditorCurveBinding scaleZBinding = EditorCurveBinding.FloatCurve(string.Empty, typeof(Transform), "m_LocalScale.z");
            AnimationCurve constantScaleCurve = AnimationCurve.Constant(0f, duration, 1f);

            AnimationUtility.SetEditorCurve(clip, scaleXBinding, constantScaleCurve);
            AnimationUtility.SetEditorCurve(clip, scaleYBinding, constantScaleCurve);
            AnimationUtility.SetEditorCurve(clip, scaleZBinding, constantScaleCurve);
            ApplyPlayerProxyMotionCurves(
                clip,
                ResolvePlayerProxyMotionProfile(path, animationStateName),
                duration,
                openTime,
                closeTime);
            AnimationUtility.SetAnimationEvents(clip, animationEvents ?? System.Array.Empty<AnimationEvent>());
            ConfigureClipSettings(clip, duration, loopTime);
            EditorUtility.SetDirty(clip);
            return clip;
        }

        private static PlayerProxyMotionProfile ResolvePlayerProxyMotionProfile(string path, string animationStateName)
        {
            if (string.Equals(path, PlayerIdleClipPath, global::System.StringComparison.Ordinal))
            {
                return PlayerProxyMotionProfile.Idle;
            }

            switch (animationStateName)
            {
                case "Light_01":
                    return PlayerProxyMotionProfile.Light01;
                case "Light_02":
                    return PlayerProxyMotionProfile.Light02;
                case "Light_03":
                    return PlayerProxyMotionProfile.Light03;
                case "Heavy_01":
                    return PlayerProxyMotionProfile.Heavy01;
                case "DodgeFollowUp":
                    return PlayerProxyMotionProfile.DodgeFollowUp;
                case "DodgeFollowUp_Enhanced":
                    return PlayerProxyMotionProfile.DodgeFollowUpEnhanced;
                case "Counter":
                    return PlayerProxyMotionProfile.Counter;
                case "Counter_Enhanced":
                    return PlayerProxyMotionProfile.CounterEnhanced;
                default:
                    return PlayerProxyMotionProfile.GenericAttack;
            }
        }

        private static void ApplyPlayerProxyMotionCurves(
            AnimationClip clip,
            PlayerProxyMotionProfile profile,
            float duration,
            float openTime,
            float closeTime)
        {
            if (clip == null)
            {
                return;
            }

            if (profile == PlayerProxyMotionProfile.Idle)
            {
                ApplyIdleProxyCurves(clip, duration);
                return;
            }

            float anticipationTime = Mathf.Clamp(
                Mathf.Max(duration * 0.16f, openTime * 0.65f),
                0.03f,
                Mathf.Max(0.03f, duration * 0.45f));
            float strikeTime = Mathf.Clamp(
                closeTime > openTime ? Mathf.Lerp(openTime, closeTime, 0.45f) : duration * 0.5f,
                anticipationTime + 0.01f,
                Mathf.Max(anticipationTime + 0.01f, duration * 0.78f));
            float settleTime = Mathf.Clamp(
                Mathf.Max(closeTime, strikeTime + duration * 0.14f),
                strikeTime + 0.01f,
                duration);

            switch (profile)
            {
                case PlayerProxyMotionProfile.Light01:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(-0.04f, 0.85f, 0.02f),
                        new Vector3(0.08f, 0.9f, 0.12f),
                        new Vector3(-0.03f, 1.11f, 0.2f),
                        new Vector3(0.08f, 1.14f, 0.31f),
                        new Vector3(-0.05f, 1.6f, 0.04f),
                        new Vector3(0.07f, 1.66f, 0.16f),
                        new Vector3(-0.05f, 0.93f, 0.5f),
                        new Vector3(0.03f, 0.96f, 0.98f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.14f, 0.18f, 1.12f),
                        new Vector3(0.22f, 1.02f, 0.3f),
                        new Vector3(0.48f, 1.08f, 0.8f),
                        new Vector3(0.06f, 0.07f, 0.64f),
                        new Vector3(0.08f, 0.07f, 1.24f));
                    break;
                case PlayerProxyMotionProfile.Light02:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(0.05f, 0.85f, 0.01f),
                        new Vector3(-0.09f, 0.9f, 0.12f),
                        new Vector3(0.05f, 1.11f, 0.18f),
                        new Vector3(-0.08f, 1.14f, 0.31f),
                        new Vector3(0.05f, 1.6f, 0.03f),
                        new Vector3(-0.07f, 1.65f, 0.16f),
                        new Vector3(0.05f, 0.93f, 0.5f),
                        new Vector3(-0.04f, 0.96f, 1.02f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.14f, 0.18f, 1.16f),
                        new Vector3(0.43f, 1.01f, 0.25f),
                        new Vector3(-0.04f, 1.08f, 0.76f),
                        new Vector3(0.06f, 0.07f, 0.64f),
                        new Vector3(0.08f, 0.07f, 1.2f));
                    break;
                case PlayerProxyMotionProfile.Light03:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(0f, 0.83f, -0.02f),
                        new Vector3(0f, 0.9f, 0.17f),
                        new Vector3(0f, 1.08f, 0.14f),
                        new Vector3(0f, 1.17f, 0.34f),
                        new Vector3(0f, 1.57f, 0.01f),
                        new Vector3(0f, 1.69f, 0.19f),
                        new Vector3(0f, 0.9f, 0.46f),
                        new Vector3(0f, 1.0f, 1.06f),
                        new Vector3(0.12f, 0.18f, 0.42f),
                        new Vector3(0.16f, 0.18f, 1.26f),
                        new Vector3(0.18f, 1.18f, 0.18f),
                        new Vector3(0.12f, 1.3f, 0.92f),
                        new Vector3(0.06f, 0.07f, 0.72f),
                        new Vector3(0.1f, 0.08f, 1.38f));
                    break;
                case PlayerProxyMotionProfile.Heavy01:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(-0.02f, 0.8f, -0.04f),
                        new Vector3(0f, 0.93f, 0.22f),
                        new Vector3(-0.01f, 1.04f, 0.1f),
                        new Vector3(0f, 1.2f, 0.38f),
                        new Vector3(0f, 1.54f, -0.02f),
                        new Vector3(0f, 1.72f, 0.24f),
                        new Vector3(0f, 0.88f, 0.42f),
                        new Vector3(0f, 1.04f, 1.18f),
                        new Vector3(0.12f, 0.18f, 0.4f),
                        new Vector3(0.18f, 0.18f, 1.42f),
                        new Vector3(0.16f, 1.18f, 0.12f),
                        new Vector3(0.08f, 1.38f, 1.08f),
                        new Vector3(0.06f, 0.07f, 0.72f),
                        new Vector3(0.11f, 0.08f, 1.56f));
                    break;
                case PlayerProxyMotionProfile.DodgeFollowUp:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(-0.01f, 0.84f, 0.01f),
                        new Vector3(0f, 0.89f, 0.18f),
                        new Vector3(0f, 1.1f, 0.2f),
                        new Vector3(0f, 1.14f, 0.33f),
                        new Vector3(0f, 1.59f, 0.05f),
                        new Vector3(0f, 1.64f, 0.16f),
                        new Vector3(0f, 0.92f, 0.52f),
                        new Vector3(0f, 0.96f, 1.04f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.15f, 0.18f, 1.24f),
                        new Vector3(0.28f, 1.02f, 0.28f),
                        new Vector3(0.3f, 1.1f, 0.94f),
                        new Vector3(0.06f, 0.07f, 0.68f),
                        new Vector3(0.08f, 0.07f, 1.32f));
                    break;
                case PlayerProxyMotionProfile.DodgeFollowUpEnhanced:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(-0.01f, 0.83f, 0.02f),
                        new Vector3(0.01f, 0.9f, 0.21f),
                        new Vector3(0f, 1.09f, 0.2f),
                        new Vector3(0.01f, 1.16f, 0.36f),
                        new Vector3(0f, 1.58f, 0.05f),
                        new Vector3(0.01f, 1.67f, 0.19f),
                        new Vector3(0f, 0.92f, 0.52f),
                        new Vector3(0f, 0.98f, 1.14f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.16f, 0.18f, 1.38f),
                        new Vector3(0.27f, 1.02f, 0.26f),
                        new Vector3(0.3f, 1.14f, 1.06f),
                        new Vector3(0.06f, 0.07f, 0.68f),
                        new Vector3(0.09f, 0.07f, 1.46f));
                    break;
                case PlayerProxyMotionProfile.Counter:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(0.03f, 0.85f, 0.02f),
                        new Vector3(0f, 0.91f, 0.17f),
                        new Vector3(0.04f, 1.1f, 0.18f),
                        new Vector3(0f, 1.16f, 0.34f),
                        new Vector3(0.03f, 1.6f, 0.04f),
                        new Vector3(0f, 1.68f, 0.19f),
                        new Vector3(0.04f, 0.93f, 0.5f),
                        new Vector3(0f, 0.98f, 1.04f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.15f, 0.18f, 1.24f),
                        new Vector3(0.44f, 1.02f, 0.28f),
                        new Vector3(0.18f, 1.12f, 0.98f),
                        new Vector3(0.06f, 0.07f, 0.66f),
                        new Vector3(0.08f, 0.07f, 1.36f));
                    break;
                case PlayerProxyMotionProfile.CounterEnhanced:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(0.03f, 0.84f, 0.02f),
                        new Vector3(0f, 0.92f, 0.2f),
                        new Vector3(0.04f, 1.1f, 0.17f),
                        new Vector3(0f, 1.18f, 0.37f),
                        new Vector3(0.03f, 1.6f, 0.03f),
                        new Vector3(0f, 1.7f, 0.22f),
                        new Vector3(0.04f, 0.93f, 0.5f),
                        new Vector3(0f, 0.99f, 1.14f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.16f, 0.18f, 1.36f),
                        new Vector3(0.44f, 1.02f, 0.26f),
                        new Vector3(0.16f, 1.14f, 1.08f),
                        new Vector3(0.06f, 0.07f, 0.66f),
                        new Vector3(0.09f, 0.07f, 1.48f));
                    break;
                default:
                    ApplyAttackProxyCurves(
                        clip,
                        duration,
                        anticipationTime,
                        strikeTime,
                        settleTime,
                        new Vector3(0f, 0.85f, 0.02f),
                        new Vector3(0f, 0.9f, 0.12f),
                        new Vector3(0f, 1.1f, 0.2f),
                        new Vector3(0f, 1.14f, 0.3f),
                        new Vector3(0f, 1.6f, 0.04f),
                        new Vector3(0f, 1.66f, 0.16f),
                        new Vector3(0f, 0.93f, 0.5f),
                        new Vector3(0f, 0.96f, 0.98f),
                        new Vector3(0.11f, 0.18f, 0.44f),
                        new Vector3(0.14f, 0.18f, 1.1f),
                        new Vector3(0.24f, 1.02f, 0.3f),
                        new Vector3(0.3f, 1.08f, 0.78f),
                        new Vector3(0.06f, 0.07f, 0.64f),
                        new Vector3(0.08f, 0.07f, 1.22f));
                    break;
            }
        }

        private static void ApplyIdleProxyCurves(AnimationClip clip, float duration)
        {
            float midpoint = duration * 0.5f;
            SetLocalPositionCurve(clip, PlayerProxyTorsoPath, 'y',
                new ProxyCurveKey(0f, 0.88f),
                new ProxyCurveKey(midpoint, 0.905f),
                new ProxyCurveKey(duration, 0.88f));
            SetLocalPositionCurve(clip, PlayerProxyTorsoPath, 'z',
                new ProxyCurveKey(0f, 0.04f),
                new ProxyCurveKey(midpoint, 0.055f),
                new ProxyCurveKey(duration, 0.04f));
            SetLocalPositionCurve(clip, PlayerProxyHeadPath, 'y',
                new ProxyCurveKey(0f, 1.62f),
                new ProxyCurveKey(midpoint, 1.645f),
                new ProxyCurveKey(duration, 1.62f));
            SetLocalPositionCurve(clip, PlayerProxyHeadPath, 'z',
                new ProxyCurveKey(0f, 0.08f),
                new ProxyCurveKey(midpoint, 0.1f),
                new ProxyCurveKey(duration, 0.08f));
            SetLocalScaleCurve(clip, PlayerProxyForwardMarkerPath, 'z',
                new ProxyCurveKey(0f, 0.56f),
                new ProxyCurveKey(midpoint, 0.62f),
                new ProxyCurveKey(duration, 0.56f));
        }

        private static void ApplyAttackProxyCurves(
            AnimationClip clip,
            float duration,
            float anticipationTime,
            float strikeTime,
            float settleTime,
            Vector3 anticipationTorsoPosition,
            Vector3 strikeTorsoPosition,
            Vector3 anticipationChestPosition,
            Vector3 strikeChestPosition,
            Vector3 anticipationHeadPosition,
            Vector3 strikeHeadPosition,
            Vector3 anticipationForwardMarkerPosition,
            Vector3 strikeForwardMarkerPosition,
            Vector3 anticipationForwardMarkerScale,
            Vector3 strikeForwardMarkerScale,
            Vector3 anticipationBladePosition,
            Vector3 strikeBladePosition,
            Vector3 anticipationBladeScale,
            Vector3 strikeBladeScale)
        {
            SetVector3PropertyCurves(
                clip,
                PlayerProxyTorsoPath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationTorsoPosition.x), new ProxyCurveKey(strikeTime, strikeTorsoPosition.x), new ProxyCurveKey(settleTime, 0f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 0.88f), new ProxyCurveKey(anticipationTime, anticipationTorsoPosition.y), new ProxyCurveKey(strikeTime, strikeTorsoPosition.y), new ProxyCurveKey(settleTime, 0.89f), new ProxyCurveKey(duration, 0.88f) },
                new[] { new ProxyCurveKey(0f, 0.04f), new ProxyCurveKey(anticipationTime, anticipationTorsoPosition.z), new ProxyCurveKey(strikeTime, strikeTorsoPosition.z), new ProxyCurveKey(settleTime, 0.07f), new ProxyCurveKey(duration, 0.04f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyChestPath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationChestPosition.x), new ProxyCurveKey(strikeTime, strikeChestPosition.x), new ProxyCurveKey(settleTime, 0f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 1.12f), new ProxyCurveKey(anticipationTime, anticipationChestPosition.y), new ProxyCurveKey(strikeTime, strikeChestPosition.y), new ProxyCurveKey(settleTime, 1.13f), new ProxyCurveKey(duration, 1.12f) },
                new[] { new ProxyCurveKey(0f, 0.24f), new ProxyCurveKey(anticipationTime, anticipationChestPosition.z), new ProxyCurveKey(strikeTime, strikeChestPosition.z), new ProxyCurveKey(settleTime, 0.27f), new ProxyCurveKey(duration, 0.24f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyHeadPath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationHeadPosition.x), new ProxyCurveKey(strikeTime, strikeHeadPosition.x), new ProxyCurveKey(settleTime, 0f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 1.62f), new ProxyCurveKey(anticipationTime, anticipationHeadPosition.y), new ProxyCurveKey(strikeTime, strikeHeadPosition.y), new ProxyCurveKey(settleTime, 1.64f), new ProxyCurveKey(duration, 1.62f) },
                new[] { new ProxyCurveKey(0f, 0.08f), new ProxyCurveKey(anticipationTime, anticipationHeadPosition.z), new ProxyCurveKey(strikeTime, strikeHeadPosition.z), new ProxyCurveKey(settleTime, 0.12f), new ProxyCurveKey(duration, 0.08f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyForwardMarkerPath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerPosition.x), new ProxyCurveKey(strikeTime, strikeForwardMarkerPosition.x), new ProxyCurveKey(settleTime, 0f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 0.94f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerPosition.y), new ProxyCurveKey(strikeTime, strikeForwardMarkerPosition.y), new ProxyCurveKey(settleTime, 0.95f), new ProxyCurveKey(duration, 0.94f) },
                new[] { new ProxyCurveKey(0f, 0.62f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerPosition.z), new ProxyCurveKey(strikeTime, strikeForwardMarkerPosition.z), new ProxyCurveKey(settleTime, 0.76f), new ProxyCurveKey(duration, 0.62f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyForwardMarkerPath,
                "m_LocalScale",
                new[] { new ProxyCurveKey(0f, 0.14f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerScale.x), new ProxyCurveKey(strikeTime, strikeForwardMarkerScale.x), new ProxyCurveKey(settleTime, 0.14f), new ProxyCurveKey(duration, 0.14f) },
                new[] { new ProxyCurveKey(0f, 0.18f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerScale.y), new ProxyCurveKey(strikeTime, strikeForwardMarkerScale.y), new ProxyCurveKey(settleTime, 0.18f), new ProxyCurveKey(duration, 0.18f) },
                new[] { new ProxyCurveKey(0f, 0.56f), new ProxyCurveKey(anticipationTime, anticipationForwardMarkerScale.z), new ProxyCurveKey(strikeTime, strikeForwardMarkerScale.z), new ProxyCurveKey(settleTime, 0.74f), new ProxyCurveKey(duration, 0.56f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyBladePath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0.34f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.x), new ProxyCurveKey(strikeTime, strikeBladePosition.x), new ProxyCurveKey(settleTime, 0.34f), new ProxyCurveKey(duration, 0.34f) },
                new[] { new ProxyCurveKey(0f, 1.04f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.y), new ProxyCurveKey(strikeTime, strikeBladePosition.y), new ProxyCurveKey(settleTime, 1.05f), new ProxyCurveKey(duration, 1.04f) },
                new[] { new ProxyCurveKey(0f, 0.54f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.z), new ProxyCurveKey(strikeTime, strikeBladePosition.z), new ProxyCurveKey(settleTime, 0.66f), new ProxyCurveKey(duration, 0.54f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyWeaponGripBladePath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0.58f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.x - 0.34f + 0.58f), new ProxyCurveKey(strikeTime, strikeBladePosition.x - 0.34f + 0.58f), new ProxyCurveKey(settleTime, 0.58f), new ProxyCurveKey(duration, 0.58f) },
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.y - 1.04f), new ProxyCurveKey(strikeTime, strikeBladePosition.y - 1.04f), new ProxyCurveKey(settleTime, 0.01f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, anticipationBladePosition.z - 0.54f), new ProxyCurveKey(strikeTime, strikeBladePosition.z - 0.54f), new ProxyCurveKey(settleTime, 0.12f), new ProxyCurveKey(duration, 0f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyBladePath,
                "m_LocalScale",
                new[] { new ProxyCurveKey(0f, 0.07f), new ProxyCurveKey(anticipationTime, anticipationBladeScale.x), new ProxyCurveKey(strikeTime, strikeBladeScale.x), new ProxyCurveKey(settleTime, 0.07f), new ProxyCurveKey(duration, 0.07f) },
                new[] { new ProxyCurveKey(0f, 0.07f), new ProxyCurveKey(anticipationTime, anticipationBladeScale.y), new ProxyCurveKey(strikeTime, strikeBladeScale.y), new ProxyCurveKey(settleTime, 0.07f), new ProxyCurveKey(duration, 0.07f) },
                new[] { new ProxyCurveKey(0f, 0.82f), new ProxyCurveKey(anticipationTime, anticipationBladeScale.z), new ProxyCurveKey(strikeTime, strikeBladeScale.z), new ProxyCurveKey(settleTime, 0.94f), new ProxyCurveKey(duration, 0.82f) });
            SetVector3PropertyCurves(
                clip,
                PlayerProxyWeaponGripBladePath,
                "m_LocalScale",
                new[] { new ProxyCurveKey(0f, 1.14f), new ProxyCurveKey(anticipationTime, Mathf.Max(0.7f, anticipationBladeScale.z * 1.35f)), new ProxyCurveKey(strikeTime, Mathf.Max(0.82f, strikeBladeScale.z * 1.35f)), new ProxyCurveKey(settleTime, 1.24f), new ProxyCurveKey(duration, 1.14f) },
                new[] { new ProxyCurveKey(0f, 0.08f), new ProxyCurveKey(anticipationTime, Mathf.Max(0.08f, anticipationBladeScale.x)), new ProxyCurveKey(strikeTime, Mathf.Max(0.09f, strikeBladeScale.x)), new ProxyCurveKey(settleTime, 0.08f), new ProxyCurveKey(duration, 0.08f) },
                new[] { new ProxyCurveKey(0f, 0.08f), new ProxyCurveKey(anticipationTime, Mathf.Max(0.08f, anticipationBladeScale.y)), new ProxyCurveKey(strikeTime, Mathf.Max(0.09f, strikeBladeScale.y)), new ProxyCurveKey(settleTime, 0.08f), new ProxyCurveKey(duration, 0.08f) });
            SetLocalPositionCurve(clip, PlayerProxyGuardPath, 'z',
                new ProxyCurveKey(0f, 0.34f),
                new ProxyCurveKey(anticipationTime, 0.28f),
                new ProxyCurveKey(strikeTime, 0.4f),
                new ProxyCurveKey(settleTime, 0.36f),
                new ProxyCurveKey(duration, 0.34f));
            SetVector3PropertyCurves(
                clip,
                PlayerProxyWeaponGripGuardPath,
                "m_LocalPosition",
                new[] { new ProxyCurveKey(0f, 0.02f), new ProxyCurveKey(anticipationTime, -0.02f), new ProxyCurveKey(strikeTime, 0.05f), new ProxyCurveKey(settleTime, 0.03f), new ProxyCurveKey(duration, 0.02f) },
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, 0f), new ProxyCurveKey(strikeTime, 0.01f), new ProxyCurveKey(settleTime, 0f), new ProxyCurveKey(duration, 0f) },
                new[] { new ProxyCurveKey(0f, 0f), new ProxyCurveKey(anticipationTime, -0.06f), new ProxyCurveKey(strikeTime, 0.06f), new ProxyCurveKey(settleTime, 0.02f), new ProxyCurveKey(duration, 0f) });
        }

        private static void SetVector3PropertyCurves(
            AnimationClip clip,
            string path,
            string propertyPrefix,
            ProxyCurveKey[] xKeys,
            ProxyCurveKey[] yKeys,
            ProxyCurveKey[] zKeys)
        {
            SetFloatCurve(clip, path, propertyPrefix + ".x", xKeys);
            SetFloatCurve(clip, path, propertyPrefix + ".y", yKeys);
            SetFloatCurve(clip, path, propertyPrefix + ".z", zKeys);
        }

        private static void SetLocalPositionCurve(AnimationClip clip, string path, char axis, params ProxyCurveKey[] keys)
        {
            SetFloatCurve(clip, path, "m_LocalPosition." + axis, keys);
        }

        private static void SetLocalScaleCurve(AnimationClip clip, string path, char axis, params ProxyCurveKey[] keys)
        {
            SetFloatCurve(clip, path, "m_LocalScale." + axis, keys);
        }

        private static void SetFloatCurve(AnimationClip clip, string path, string propertyName, params ProxyCurveKey[] keys)
        {
            EditorCurveBinding binding = EditorCurveBinding.FloatCurve(path, typeof(Transform), propertyName);
            AnimationUtility.SetEditorCurve(clip, binding, CreateCurve(keys));
        }

        private static AnimationCurve CreateCurve(params ProxyCurveKey[] keys)
        {
            Keyframe[] keyframes = new Keyframe[keys.Length];

            for (int i = 0; i < keys.Length; i++)
            {
                keyframes[i] = new Keyframe(keys[i].Time, keys[i].Value);
            }

            return new AnimationCurve(keyframes);
        }

        private static void ClearClipContent(AnimationClip clip)
        {
            EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(clip);

            for (int i = 0; i < curveBindings.Length; i++)
            {
                AnimationUtility.SetEditorCurve(clip, curveBindings[i], null);
            }

            EditorCurveBinding[] objectReferenceBindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);

            for (int i = 0; i < objectReferenceBindings.Length; i++)
            {
                AnimationUtility.SetObjectReferenceCurve(clip, objectReferenceBindings[i], null);
            }

            AnimationUtility.SetAnimationEvents(clip, System.Array.Empty<AnimationEvent>());
        }

        private static void ConfigureClipSettings(AnimationClip clip, float duration, bool loopTime)
        {
            SerializedObject serializedObject = new SerializedObject(clip);
            SerializedProperty clipSettings = serializedObject.FindProperty("m_AnimationClipSettings");

            if (clipSettings != null)
            {
                SerializedProperty loopTimeProperty = clipSettings.FindPropertyRelative("m_LoopTime");

                if (loopTimeProperty != null)
                {
                    loopTimeProperty.boolValue = loopTime;
                }

                SerializedProperty startTimeProperty = clipSettings.FindPropertyRelative("m_StartTime");

                if (startTimeProperty != null)
                {
                    startTimeProperty.floatValue = 0f;
                }

                SerializedProperty stopTimeProperty = clipSettings.FindPropertyRelative("m_StopTime");

                if (stopTimeProperty != null)
                {
                    stopTimeProperty.floatValue = duration;
                }
            }

            serializedObject.ApplyModifiedPropertiesWithoutUndo();
        }

        private static float ResolveClipConfiguredDuration(AnimationClip clip)
        {
            if (clip == null)
            {
                return 0f;
            }

            SerializedObject serializedObject = new SerializedObject(clip);
            SerializedProperty clipSettings = serializedObject.FindProperty("m_AnimationClipSettings");

            if (clipSettings == null)
            {
                return Mathf.Max(0f, clip.length);
            }

            SerializedProperty startTimeProperty = clipSettings.FindPropertyRelative("m_StartTime");
            SerializedProperty stopTimeProperty = clipSettings.FindPropertyRelative("m_StopTime");
            float startTime = startTimeProperty != null ? startTimeProperty.floatValue : 0f;
            float stopTime = stopTimeProperty != null ? stopTimeProperty.floatValue : clip.length;
            return Mathf.Max(0f, stopTime - startTime);
        }

        private static float GetConfiguredClipDuration(string clipPath)
        {
            return ResolveClipConfiguredDuration(AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath));
        }

        private static GameObject BuildProjectilePrefab(string prefabPath, string prefabName, GameObject impactEffectPrefab)
        {
            GameObject projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = prefabName;
            projectile.transform.localScale = new Vector3(0.25f, 0.25f, 0.25f);

            Collider collider = projectile.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            ProjectileController projectileController = projectile.AddComponent<ProjectileController>();
            SerializedObject serializedObject = new SerializedObject(projectileController);
            serializedObject.FindProperty("defaultTrajectoryMode").enumValueIndex = (int)ProjectileTrajectoryMode.Straight;
            serializedObject.FindProperty("defaultArcHeight").floatValue = 0f;
            serializedObject.FindProperty("impactEffectPrefab").objectReferenceValue = impactEffectPrefab;
            serializedObject.FindProperty("spawnedImpactLifetimeSeconds").floatValue = 0.18f;
            serializedObject.FindProperty("playLaunchSound").boolValue = true;
            serializedObject.FindProperty("launchSoundVolume").floatValue = 0.08f;
            serializedObject.FindProperty("launchSoundStartFrequency").floatValue = 1040f;
            serializedObject.FindProperty("launchSoundEndFrequency").floatValue = 760f;
            serializedObject.FindProperty("playImpactSound").boolValue = true;
            serializedObject.FindProperty("impactSoundVolume").floatValue = 0.12f;
            serializedObject.FindProperty("impactSoundStartFrequency").floatValue = 480f;
            serializedObject.FindProperty("impactSoundEndFrequency").floatValue = 180f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(projectileController);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(projectile, prefabPath);
            Object.DestroyImmediate(projectile);
            return prefab;
        }

        private static void ConfigureAudioSettings(AudioSettingsSO audioSettings)
        {
            if (audioSettings == null)
            {
                return;
            }

            SerializedObject serializedObject = new SerializedObject(audioSettings);
            serializedObject.FindProperty("masterVolume").floatValue = 1f;
            serializedObject.FindProperty("sfxVolume").floatValue = 1f;
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(audioSettings);
        }

        private static GameObject BuildImpactEffectPrefab(string prefabPath, string prefabName)
        {
            GameObject effect = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            effect.name = prefabName;
            effect.transform.localScale = Vector3.one * 0.15f;

            Collider collider = effect.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            TransientVisualEffect visualEffect = effect.AddComponent<TransientVisualEffect>();
            SerializedObject serializedObject = new SerializedObject(visualEffect);
            serializedObject.FindProperty("lifetimeSeconds").floatValue = 0.18f;
            serializedObject.FindProperty("startScale").vector3Value = new Vector3(0.15f, 0.15f, 0.15f);
            serializedObject.FindProperty("endScale").vector3Value = new Vector3(0.75f, 0.75f, 0.75f);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(visualEffect);

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(effect, prefabPath);
            Object.DestroyImmediate(effect);
            return prefab;
        }

        private static void ConfigureBossTelegraphStyle(
            BossTelegraphStyleSO asset,
            GameObject groundTelegraphPrefab,
            GameObject impactMarkerPrefab,
            GameObject spawnFlarePrefab,
            Material engageTelegraphMaterial,
            Material attackTelegraphMaterial,
            Material impactMarkerMaterial,
            Material spawnFlareMaterial)
        {
            SerializedObject serializedObject = new SerializedObject(asset);
            bool hasCustomVisualOverrides =
                HasCustomReference(serializedObject.FindProperty("groundTelegraphVisualPrefab"), BossGroundTelegraphPrefabPath)
                || HasCustomReference(serializedObject.FindProperty("impactMarkerVisualPrefab"), BossImpactMarkerPrefabPath)
                || HasCustomReference(serializedObject.FindProperty("spawnFlareVisualPrefab"), BossSpawnFlarePrefabPath)
                || HasCustomReference(serializedObject.FindProperty("engageTelegraphMaterial"), BossEngageTelegraphMaterialPath)
                || HasCustomReference(serializedObject.FindProperty("attackTelegraphMaterial"), BossAttackTelegraphMaterialPath)
                || HasCustomReference(serializedObject.FindProperty("impactMarkerMaterial"), BossImpactMarkerMaterialPath)
                || HasCustomReference(serializedObject.FindProperty("spawnFlareMaterial"), BossSpawnFlareMaterialPath);

            if (!hasCustomVisualOverrides)
            {
                serializedObject.FindProperty("cuePanelBackgroundColor").colorValue = new Color(0.08f, 0.1f, 0.12f, 0.95f);
                serializedObject.FindProperty("defaultCueAccentColor").colorValue = new Color(0.94f, 0.84f, 0.58f, 1f);
                serializedObject.FindProperty("straightProjectileCueAccentColor").colorValue = new Color(0.34f, 0.86f, 0.97f, 1f);
                serializedObject.FindProperty("arcProjectileCueAccentColor").colorValue = new Color(0.97f, 0.53f, 0.28f, 1f);
                serializedObject.FindProperty("rangedCueAccentColor").colorValue = new Color(0.74f, 0.91f, 0.67f, 1f);
                serializedObject.FindProperty("encounterPulseColor").colorValue = new Color(0.92f, 0.62f, 0.18f, 0.22f);
                serializedObject.FindProperty("attackPulseColor").colorValue = new Color(0.9f, 0.2f, 0.18f, 0.26f);
                serializedObject.FindProperty("engageTelegraphColor").colorValue = new Color(0.86f, 0.71f, 0.28f, 1f);
                serializedObject.FindProperty("attackTelegraphColor").colorValue = new Color(0.9f, 0.24f, 0.18f, 1f);
                serializedObject.FindProperty("impactMarkerColor").colorValue = new Color(1f, 0.34f, 0.22f, 1f);
                serializedObject.FindProperty("spawnFlareColor").colorValue = new Color(1f, 0.76f, 0.3f, 1f);
            }

            AssignGeneratedReference(
                serializedObject.FindProperty("groundTelegraphVisualPrefab"),
                groundTelegraphPrefab,
                BossGroundTelegraphPrefabPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("impactMarkerVisualPrefab"),
                impactMarkerPrefab,
                BossImpactMarkerPrefabPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("spawnFlareVisualPrefab"),
                spawnFlarePrefab,
                BossSpawnFlarePrefabPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("engageTelegraphMaterial"),
                engageTelegraphMaterial,
                BossEngageTelegraphMaterialPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("attackTelegraphMaterial"),
                attackTelegraphMaterial,
                BossAttackTelegraphMaterialPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("impactMarkerMaterial"),
                impactMarkerMaterial,
                BossImpactMarkerMaterialPath);
            AssignGeneratedReference(
                serializedObject.FindProperty("spawnFlareMaterial"),
                spawnFlareMaterial,
                BossSpawnFlareMaterialPath);
            serializedObject.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(asset);
        }

        private static Material CreateOrLoadMaterial(string path, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);

            if (material == null)
            {
                Shader shader = ResolvePlaceholderShader();
                material = new Material(shader);
                material.color = color;
                AssetDatabase.CreateAsset(material, path);
                EditorUtility.SetDirty(material);
            }

            return material;
        }

        private static GameObject BuildBossVisualPrefab(string prefabPath, string prefabName, Vector3 scale, Material previewMaterial)
        {
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existingPrefab != null)
            {
                return existingPrefab;
            }

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.name = prefabName;
            visual.transform.localScale = scale;

            Collider collider = visual.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = visual.GetComponent<Renderer>();

            if (renderer != null && previewMaterial != null)
            {
                renderer.sharedMaterial = previewMaterial;
                EditorUtility.SetDirty(renderer);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(visual, prefabPath);
            Object.DestroyImmediate(visual);
            return prefab;
        }

        private static void AssignGeneratedReference(SerializedProperty property, UnityEngine.Object generatedAsset, string generatedAssetPath)
        {
            if (property == null || generatedAsset == null)
            {
                return;
            }

            UnityEngine.Object currentReference = property.objectReferenceValue;

            if (currentReference == null)
            {
                property.objectReferenceValue = generatedAsset;
                return;
            }

            string currentPath = AssetDatabase.GetAssetPath(currentReference);

            if (string.Equals(currentPath, generatedAssetPath, global::System.StringComparison.Ordinal))
            {
                property.objectReferenceValue = generatedAsset;
            }
        }

        private static bool HasCustomReference(SerializedProperty property, string generatedAssetPath)
        {
            if (property == null)
            {
                return false;
            }

            UnityEngine.Object reference = property.objectReferenceValue;

            if (reference == null)
            {
                return false;
            }

            string currentPath = AssetDatabase.GetAssetPath(reference);
            return !string.IsNullOrEmpty(currentPath)
                && !string.Equals(currentPath, generatedAssetPath, global::System.StringComparison.Ordinal);
        }

        private static Shader ResolvePlaceholderShader()
        {
            Shader shader = Shader.Find("Unlit/Color");

            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            return shader;
        }

        private static T CreateOrLoadAsset<T>(string path) where T : ScriptableObject
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);

            if (asset != null)
            {
                return asset;
            }

            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
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
    }
}
