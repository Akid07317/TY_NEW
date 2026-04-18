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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
                hitboxActivationMode: AttackHitboxActivationMode.AnimationEvent);
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
            Debug.Log("CombatTest placeholder assets created or updated.");
        }

        public static RuntimeAnimatorController EnsurePlayerCombatAnimationAssets()
        {
            EnsureFolder("Assets/_Game/Animations");
            EnsureFolder("Assets/_Game/Animations/Characters");
            EnsureFolder(PlayerAnimationRootFolder);

            return EnsurePlayerCombatAnimationAssets(
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light01Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light02Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(Light03Path),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(HeavyPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(DodgeFollowUpPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(CounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedCounterPath),
                AssetDatabase.LoadAssetAtPath<AttackDefinitionSO>(EnhancedDodgePath));
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

        private static RuntimeAnimatorController EnsurePlayerCombatAnimationAssets(params AttackDefinitionSO[] attackDefinitions)
        {
            AnimationClip idleClip = CreateOrUpdatePlaceholderClip(PlayerIdleClipPath, 1f, true, System.Array.Empty<AnimationEvent>());
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerAnimatorControllerPath);

            if (controller == null)
            {
                controller = AnimatorController.CreateAnimatorControllerAtPath(PlayerAnimatorControllerPath);
            }

            AnimatorControllerLayer layer = EnsureBaseLayer(controller);
            AnimatorStateMachine stateMachine = layer.stateMachine;
            ClearStateMachine(stateMachine);

            AnimatorState idleState = stateMachine.AddState("Idle");
            idleState.motion = idleClip;
            stateMachine.defaultState = idleState;

            for (int i = 0; i < attackDefinitions.Length; i++)
            {
                AttackDefinitionSO attackDefinition = attackDefinitions[i];

                if (attackDefinition == null || string.IsNullOrWhiteSpace(attackDefinition.AnimationStateName))
                {
                    continue;
                }

                AnimationClip attackClip = CreateOrUpdateAttackClip(attackDefinition);
                AnimatorState attackState = stateMachine.AddState(attackDefinition.AnimationStateName);
                attackState.motion = attackClip;

                AnimatorStateTransition toIdleTransition = attackState.AddTransition(idleState);
                toIdleTransition.hasExitTime = true;
                toIdleTransition.exitTime = 1f;
                toIdleTransition.hasFixedDuration = true;
                toIdleTransition.duration = 0.05f;
                toIdleTransition.canTransitionToSelf = false;
            }

            EditorUtility.SetDirty(stateMachine);
            EditorUtility.SetDirty(controller);
            return controller;
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

            return CreateOrUpdatePlaceholderClip(
                GetPlayerAttackClipPath(attackDefinition.AnimationStateName),
                duration,
                false,
                animationEvents);
        }

        private static string GetPlayerAttackClipPath(string animationStateName)
        {
            return $"{PlayerAnimationRootFolder}/AN_Player_{animationStateName}_CombatTest.anim";
        }

        private static AnimationClip CreateOrUpdatePlaceholderClip(
            string path,
            float duration,
            bool loopTime,
            AnimationEvent[] animationEvents)
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
            AnimationUtility.SetAnimationEvents(clip, animationEvents ?? System.Array.Empty<AnimationEvent>());
            ConfigureClipSettings(clip, duration, loopTime);
            EditorUtility.SetDirty(clip);
            return clip;
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
