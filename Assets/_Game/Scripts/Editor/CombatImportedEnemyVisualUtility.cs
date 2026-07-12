using System.Collections.Generic;
using CampusRPG.AI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;

namespace CampusRPG.Editor
{
    public enum ImportedEnemySourceProfile
    {
        PreviewFallback = 0,
        UserOwnedGhostSamurai = 1
    }

    // Imported art is only applied by explicit preview/candidate flows. Standard Build/Repair restores proxy baseline.
    public static class CombatImportedEnemyVisualUtility
    {
        public const string ImportedVisualRootName = "ImportedEnemyVisualRoot";
        public const string ImportedRoleMarkerRootName = "ImportedEnemyRoleMarkerRoot";
        public const string ImportedWeaponRootName = "ImportedEnemyWeaponRoot";
        public const string GhostSamuraiEnemyMeleeModelPath = "Assets/GhostSamurai_Animset/Model/Model_Unity_Ver1.FBX";
        public const string GhostSamuraiEnemyMobileModelPath = GhostSamuraiEnemyMeleeModelPath;
        public const string GhostSamuraiEnemyRangedModelPath = "Assets/GhostSamurai_Animset/Model/WM_Master_Unity_Bow2.FBX";
        public const string GhostSamuraiKatanaWeaponPath = "Assets/GhostSamurai_Animset/Model/Weapon/SM_Katana01.FBX";
        public const string GhostSamuraiArrowWeaponPath = "Assets/GhostSamurai_Animset/Model/Weapon/SM_Arrow_01.FBX";
        public const string UserOwnedGhostSamuraiEnemyPaletteFolder =
            "Assets/_Game/Animations/Characters/CombatTest/LocalPreview/Materials/Enemy/UserOwnedGhostSamurai";

        private const string ProxyRootName = "CombatProxyVisualRoot";
        private const string MaterialsFolder = "Assets/_Game/Materials";
        private const string LocalPreviewAnimationFolder = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string ImportedAnimatorControllerPathPrefix = LocalPreviewAnimationFolder + "/AC_Enemy_ImportedPreview_";
        private const string ImportedUpperBodyMaskPath = LocalPreviewAnimationFolder + "/AM_Enemy_ImportedUpperBody.mask";
        private const string GhostSamuraiKatanaAPoseRoot = "Assets/GhostSamurai_Animset/Animation/katana/APose";
        private const string GhostSamuraiBowRoot = "Assets/GhostSamurai_Animset/Animation/Bow";
        private const string GhostSamuraiKatanaAttackInplaceFolder = GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace";
        private const string GhostSamuraiBowAttackInplaceFolder = GhostSamuraiBowRoot + "/Attack/Inplace";
        private const string GhostSamuraiKatanaAnchorName = "Weapon_r";
        private const string GhostSamuraiArrowAnchorName = "arrow";
        private const string GhostSamuraiIntegratedBowRendererName = "SK_Bow_02";
        private const float ImportedWalkThreshold = 0.18f;
        private const float ImportedRunThreshold = 0.7f;
        private const int MaxImportedAttackVariantStates = 96;
        private const float AntiAirResponseStateSpeed = 1.12f;
        private const float ChaseRollResponseStateSpeed = 0.88f;
        private const float GuardBreakResponseStateSpeed = 0.78f;
        private const float ImportedGroundingInset = 0.035f;
        private const string CombatPoseLayerName = "CombatPose";
        private const string CombatPoseStateName = "Hold";
        private const string CombatPoseAntiAirReadStateName = "Read_AntiAir";
        private const string CombatPoseChaseRollReadStateName = "Read_ChaseRoll";
        private const string CombatPoseGuardBreakReadStateName = "Read_GuardBreak";
        private const float CombatPoseReadThreshold = 0.05f;
        private const float CombatPoseReadReleaseThreshold = 0.02f;
        private const float CombatPoseReadTransitionDuration = 0.05f;

        private static readonly string[] GhostSamuraiEnemyMeleeVisualPrefabCandidatePaths =
        {
            GhostSamuraiEnemyMeleeModelPath
        };

        private static readonly string[] GhostSamuraiEnemyMobileVisualPrefabCandidatePaths =
        {
            GhostSamuraiEnemyMobileModelPath
        };

        private static readonly string[] GhostSamuraiEnemyRangedVisualPrefabCandidatePaths =
        {
            GhostSamuraiEnemyRangedModelPath
        };

        private static readonly string[] GhostSamuraiEnemyAvatarCandidatePaths =
        {
            GhostSamuraiEnemyMeleeModelPath,
            GhostSamuraiEnemyRangedModelPath
        };

        private static readonly string[] EnemyMeleeVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyMobileVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Polearm.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Dual Wield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyRangedVisualPrefabCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanF_Dummy_Red.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab",
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red.prefab"
        };

        private static readonly string[] EnemyAvatarCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Models/HumanM_Model.fbx",
            "Assets/Kevin Iglesias/Human Animations/Models/HumanF_Model.fbx"
        };

        private static readonly string[] GhostSamuraiKatanaIdleClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Defense/Inplace/GhostSamurai_DefenseR_Loop_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/GhostSamurai_APose_Idle.FBX"
        };

        private static readonly string[] GhostSamuraiKatanaWalkClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiKatanaRunClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Movement/Inplace/GhostSamurai_APose_Strafe_Run_F_Loop_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiKatanaHitClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Hit/Inplace/GhostSamurai_APose_Hit_F_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Hit/Inplace/GhostSamurai_APose_Large_Hit_1_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Hit/Inplace/GhostSamurai_APose_Large_Hit_2_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiKatanaDeathClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Die/Inplace/GhostSamurai_APose_Die01_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiMeleeAttackClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack01_1_ALL_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack02_2_ALL_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiMobileAttackClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack03_4_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiAntiAirAttackClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Air_Attack03_Start_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_JumpAttack03_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_JumpAttack04_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiAntiAirReadClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Deflect/Inplace/GhostSamurai_RAttack_DeflectR90_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Deflect/Inplace/GhostSamurai_LAttack_DeflectL90_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiChaseRollAttackClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Dodge/Inplace/GhostSamurai_APose_Slide_F_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiChaseRollReadClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Movement/Inplace/GhostSamurai_APose_Slide_Start_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Dodge/Inplace/GhostSamurai_APose_Dodge_Attack_F_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Dodge/Inplace/GhostSamurai_APose_Slide_F_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiGuardBreakAttackClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_SPAttack06_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiGuardBreakReadClipCandidatePaths =
        {
            GhostSamuraiKatanaAPoseRoot + "/Deflect/Inplace/GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Attack/Inplace/GhostSamurai_APose_SPAttack06_Inplace.FBX",
            GhostSamuraiKatanaAPoseRoot + "/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowIdleClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Idle_Inplace.FBX",
            GhostSamuraiBowRoot + "/Common/Inplace/GhostSamurai_Bow_Common_Idle_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowWalkClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Movement/Inplace/GhostSamurai_Bow_AimWalk_F_Inplace.FBX",
            GhostSamuraiBowRoot + "/Common/Inplace/GhostSamurai_Bow_Common_StrafeWalkF_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowRunClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Movement/Inplace/GhostSamurai_Bow_AimRun_F_Inplace.FBX",
            GhostSamuraiBowRoot + "/Common/Inplace/GhostSamurai_Bow_Common_StrafeRun_F_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowHitClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Hit/Inplace/GhostSamurai_Bow_Hit_F_Inplace.FBX",
            GhostSamuraiBowRoot + "/Hit/Inplace/GhostSamurai_Bow_Large_Hit_1_Inplace.FBX",
            GhostSamuraiBowRoot + "/Hit/Inplace/GhostSamurai_Bow_Large_Hit_2_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowDeathClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Die/Inplace/GhostSamurai_Bow_Die01_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowAttackClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_Start_Inplace.FBX",
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_Loop_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowAntiAirClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_AirShoot_Start_Inplace.FBX",
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_AirShoot_Loop_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowAntiAirReadClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_AirShoot_Start_Inplace.FBX",
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_AirShoot_Loop_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowChaseRollClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Dodge/Inplace/GhostSamurai_Bow_Dodge_F_Inplace.FBX",
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_SP01_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowChaseRollReadClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Dodge/Inplace/GhostSamurai_Bow_Dodge_F_Inplace.FBX",
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_SP01_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowGuardBreakClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_SP04_Inplace.FBX",
            GhostSamuraiBowRoot + "/Hit/Inplace/GhostSamurai_Bow_Large_Hit_2_Inplace.FBX"
        };

        private static readonly string[] GhostSamuraiBowGuardBreakReadClipCandidatePaths =
        {
            GhostSamuraiBowRoot + "/Attack/Inplace/GhostSamurai_Bow_Shoot_SP04_Inplace.FBX",
            GhostSamuraiBowRoot + "/Hit/Inplace/GhostSamurai_Bow_Large_Hit_2_Inplace.FBX"
        };

        private static readonly string[] DefaultIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] OneHandedIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@CombatIdle1H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] PolearmIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] TwoHandedIdleClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@CombatIdle2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@CombatIdlePolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] RangedIdleClipCandidatePaths =
        {
            "Assets/DoubleL/Bow/Movement/Idle/Idle/Bow_Idle_B.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatIdle01.fbx"
        };

        private static readonly string[] WalkClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_B_InPlace.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx"
        };

        private static readonly string[] OneHandedWalkClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_B_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/1Hand_Up_Walk_A_F.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/1Hand_Up_Walk_A_B.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Walk/HumanM@Walk01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Walk/HumanF@Walk01_Forward.fbx"
        };

        private static readonly string[] RunClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx"
        };

        private static readonly string[] OneHandedRunClipCandidatePaths =
        {
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_B_InPlace.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/1Hand_Up_Run_A_F.fbx",
            "Assets/DoubleL/One Hand Up/Movement/Run/Base/1Hand_Up_Run_A_B.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Run/HumanM@Run01_Forward.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Movement/Run/HumanF@Run01_Forward.fbx"
        };

        private static readonly string[] HitClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@CombatDamage01.fbx"
        };

        private static readonly string[] DeathClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/HumanF@Death01.fbx"
        };

        private static readonly string[] MeleeAttackClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_R.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Shield/HumanF@AttackShield01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_R.fbx"
        };

        private static readonly string[] MobileAttackClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/2H/HumanM@Attack2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/Polearm/HumanF@AttackPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/2H/HumanF@Attack2H01.fbx"
        };

        private static readonly string[] RangedAttackClipCandidatePaths =
        {
            "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx",
            "Assets/DoubleL/Bow/Attack A/Bow_Attack_A_1_All.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@Attack1H01_L.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Female/Combat/1H/HumanF@Attack1H01_L.fbx"
        };

        private static readonly string[] OneHandedHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/Human@ObjectGripHands01.fbx"
        };

        private static readonly string[] PolearmHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanM@WeaponHoldPolearm01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanF@WeaponHoldPolearm01.fbx"
        };

        private static readonly string[] TwoHandedHoldClipCandidatePaths =
        {
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanM@WeaponHold2H01.fbx",
            "Assets/Kevin Iglesias/Human Animations/Animations/Masked Poses/HumanF@WeaponHold2H01.fbx"
        };

        private enum ImportedEnemyAnimationProfile
        {
            Default,
            OneHanded,
            Polearm,
            TwoHanded,
            Ranged
        }

        private enum ImportedEnemyEquipmentKind
        {
            Body = 0,
            Katana = 1,
            Arrow = 2
        }

        public static ImportedEnemySourceProfile SourceProfile { get; set; } = ImportedEnemySourceProfile.PreviewFallback;

        public static bool HasHumanoidVisualSource(CombatProxyVisualKind kind)
        {
            return LoadFirstHumanoidPrefab(GetActiveCandidatePaths(kind)) != null;
        }

        public static string GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind kind)
        {
            return FindFirstCompatibleHumanoidPath(GetActiveCandidatePaths(kind));
        }

        public static bool IsAnimationSourceAllowed(string assetPath)
        {
            return SourceProfile != ImportedEnemySourceProfile.UserOwnedGhostSamurai
                || IsUnderAssetRoot(assetPath, "Assets/GhostSamurai_Animset");
        }

        public static RuntimeAnimatorController EnsureImportedAvatarPreviewController(CombatProxyVisualKind kind)
        {
            ImportedEnemyAnimationProfile profile = ResolveAnimationProfile(kind);
            EnsureGhostSamuraiGroundLocomotionClipImportSettings(GetIdleClipCandidatePaths(profile));
            EnsureGhostSamuraiGroundLocomotionClipImportSettings(GetWalkClipCandidatePaths(profile));
            EnsureGhostSamuraiGroundLocomotionClipImportSettings(GetRunClipCandidatePaths(profile));

            AnimationClip idleClip = LoadFirstAvailableAnimationClip(GetIdleClipCandidatePaths(profile));
            AnimationClip walkClip = LoadFirstAvailableAnimationClip(GetWalkClipCandidatePaths(profile));
            AnimationClip runClip = LoadFirstAvailableAnimationClip(GetRunClipCandidatePaths(profile));
            AnimationClip hitClip = LoadFirstAvailableAnimationClip(GetHitClipCandidatePaths(profile));
            AnimationClip deathClip = LoadFirstAvailableAnimationClip(GetDeathClipCandidatePaths(profile));
            AnimationClip[] meleeAttackClips = LoadAvailableAnimationClips(GetMeleeAttackClipCandidatePaths(), MaxImportedAttackVariantStates);
            AnimationClip[] mobileAttackClips = LoadAvailableAnimationClips(GetMobileAttackClipCandidatePaths(), MaxImportedAttackVariantStates);
            AnimationClip[] rangedAttackClips = LoadAvailableAnimationClips(GetRangedAttackClipCandidatePaths(), MaxImportedAttackVariantStates);
            AnimationClip[] antiAirAttackClips = LoadAvailableAnimationClips(GetAntiAirAttackClipCandidatePaths(profile), MaxImportedAttackVariantStates);
            AnimationClip[] chaseRollAttackClips = LoadAvailableAnimationClips(GetChaseRollAttackClipCandidatePaths(profile), MaxImportedAttackVariantStates);
            AnimationClip[] guardBreakAttackClips = LoadAvailableAnimationClips(GetGuardBreakAttackClipCandidatePaths(profile), MaxImportedAttackVariantStates);
            AnimationClip holdClip = LoadFirstAvailableAnimationClip(GetHoldClipCandidatePaths(profile));
            AnimationClip antiAirReadClip = LoadFirstAvailableAnimationClip(GetAntiAirReadClipCandidatePaths(profile));
            AnimationClip chaseRollReadClip = LoadFirstAvailableAnimationClip(GetChaseRollReadClipCandidatePaths(profile));
            AnimationClip guardBreakReadClip = LoadFirstAvailableAnimationClip(GetGuardBreakReadClipCandidatePaths(profile));
            AnimationClip meleeAttackClip = GetFirstAvailableClip(meleeAttackClips);
            AnimationClip mobileAttackClip = GetFirstAvailableClip(mobileAttackClips);
            AnimationClip rangedAttackClip = GetFirstAvailableClip(rangedAttackClips);
            AnimationClip antiAirAttackClip = GetFirstAvailableClip(antiAirAttackClips, rangedAttackClips);
            AnimationClip chaseRollAttackClip = GetFirstAvailableClip(chaseRollAttackClips, mobileAttackClips);
            AnimationClip guardBreakAttackClip = GetFirstAvailableClip(guardBreakAttackClips, meleeAttackClips);

            if (idleClip == null
                || walkClip == null
                || runClip == null
                || hitClip == null
                || deathClip == null
                || meleeAttackClip == null
                || mobileAttackClip == null
                || rangedAttackClip == null)
            {
                return null;
            }

            EnsureFolder(LocalPreviewAnimationFolder);
            string controllerPath = GetImportedAnimatorControllerPath(kind);

            if (AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath) != null)
            {
                AssetDatabase.DeleteAsset(controllerPath);
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.ResponseReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.AntiAirReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName, AnimatorControllerParameterType.Float);
            controller.AddParameter(EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName, AnimatorControllerParameterType.Float);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            BlendTree locomotionBlendTree = new BlendTree
            {
                name = "BT_Enemy_Imported_Locomotion",
                blendType = BlendTreeType.Simple1D,
                blendParameter = EnemyCombatAnimationPlanUtility.GroundSpeedParameterName,
                useAutomaticThresholds = false
            };
            AssetDatabase.AddObjectToAsset(locomotionBlendTree, controller);
            locomotionBlendTree.AddChild(idleClip, 0f);
            locomotionBlendTree.AddChild(walkClip, ImportedWalkThreshold);
            locomotionBlendTree.AddChild(runClip, ImportedRunThreshold);

            AnimatorState locomotionState = stateMachine.AddState(EnemyCombatAnimationPlanUtility.LocomotionStateName);
            locomotionState.motion = locomotionBlendTree;
            stateMachine.defaultState = locomotionState;

            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.HitStateName, hitClip);
            AddClipState(stateMachine, EnemyCombatAnimationPlanUtility.DeathStateName, deathClip);
            AddAttackClipStates(stateMachine, EnemyCombatAnimationPlanUtility.MeleeAttackStateName, meleeAttackClips);
            AddAttackClipStates(stateMachine, EnemyCombatAnimationPlanUtility.MobileAttackStateName, mobileAttackClips);
            AddAttackClipStates(stateMachine, EnemyCombatAnimationPlanUtility.RangedAttackStateName, rangedAttackClips);
            AddAttackClipStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.AntiAirAttackStateName,
                antiAirAttackClips.Length > 0 ? antiAirAttackClips : rangedAttackClips,
                AntiAirResponseStateSpeed);
            AddAttackClipStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName,
                chaseRollAttackClips.Length > 0 ? chaseRollAttackClips : mobileAttackClips,
                ChaseRollResponseStateSpeed);
            AddAttackClipStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName,
                guardBreakAttackClips.Length > 0 ? guardBreakAttackClips : meleeAttackClips,
                GuardBreakResponseStateSpeed);

            AnimationClip combatPoseClip = holdClip != null ? holdClip : idleClip;

            if (combatPoseClip != null)
            {
                AddCombatPoseLayer(
                    controller,
                    combatPoseClip,
                    antiAirReadClip != null ? antiAirReadClip : antiAirAttackClip,
                    chaseRollReadClip != null ? chaseRollReadClip : chaseRollAttackClip,
                    guardBreakReadClip != null ? guardBreakReadClip : guardBreakAttackClip);
            }

            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return controller;
        }

        public static string GetImportedAvatarPreviewControllerPath(CombatProxyVisualKind kind)
        {
            return GetImportedAnimatorControllerPath(kind);
        }

        public static bool TryApplyHumanoidAvatarPreview(GameObject actor, CombatProxyVisualKind kind, Animator rootAnimator)
        {
            if (actor == null)
            {
                return false;
            }

            Transform proxyRoot = actor.transform.Find(ProxyRootName);

            if (proxyRoot == null)
            {
                return false;
            }

            GameObject visualPrefab = LoadFirstHumanoidPrefab(GetActiveCandidatePaths(kind));

            if (visualPrefab == null)
            {
                return false;
            }

            bool changed = RemoveImportedVisual(actor, rootAnimator);
            GameObject visualInstance = (GameObject)PrefabUtility.InstantiatePrefab(visualPrefab);

            if (visualInstance == null)
            {
                return false;
            }

            visualInstance.name = ImportedVisualRootName;
            visualInstance.transform.SetParent(actor.transform, false);
            visualInstance.transform.localPosition = Vector3.zero;
            visualInstance.transform.localRotation = Quaternion.identity;
            visualInstance.transform.localScale = Vector3.one;

            Animator visualAnimator = FindAvatarAnimator(visualInstance);
            Avatar avatar = visualAnimator != null ? visualAnimator.avatar : null;

            if (avatar == null)
            {
                avatar = SourceProfile == ImportedEnemySourceProfile.UserOwnedGhostSamurai
                    ? LoadFirstAvailableAvatar(new[] { AssetDatabase.GetAssetPath(visualPrefab) })
                    : LoadFirstAvailableAvatar(GetActiveAvatarCandidatePaths());
            }

            if (avatar == null
                || !avatar.isValid
                || (SourceProfile == ImportedEnemySourceProfile.UserOwnedGhostSamurai
                    && !string.Equals(
                        AssetDatabase.GetAssetPath(avatar),
                        AssetDatabase.GetAssetPath(visualPrefab),
                        System.StringComparison.Ordinal)))
            {
                Object.DestroyImmediate(visualInstance);
                return false;
            }

            if (visualAnimator == null)
            {
                visualAnimator = visualInstance.AddComponent<Animator>();
            }

            StripImportedVisualComponents(visualInstance, visualAnimator);
            changed |= AlignImportedVisualToGround(visualInstance, actor.transform);
            changed |= SetProxyRenderersEnabled(proxyRoot, false);

            if (SourceProfile == ImportedEnemySourceProfile.UserOwnedGhostSamurai)
            {
                changed |= ApplyUserOwnedGhostSamuraiEnemyPalette(
                    visualInstance,
                    kind,
                    ImportedEnemyEquipmentKind.Body);

                if (!AttachUserOwnedGhostSamuraiEnemyWeapon(visualInstance, kind))
                {
                    Object.DestroyImmediate(visualInstance);
                    SetProxyRenderersEnabled(proxyRoot, true);
                    return false;
                }

                changed = true;
            }

            visualAnimator.enabled = true;
            visualAnimator.avatar = avatar;
            visualAnimator.applyRootMotion = false;
            visualAnimator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            visualAnimator.updateMode = AnimatorUpdateMode.Normal;
            if (SourceProfile != ImportedEnemySourceProfile.UserOwnedGhostSamurai)
            {
                changed |= AddImportedRoleMarkers(visualInstance.transform, kind);
            }
            EditorUtility.SetDirty(visualAnimator);

            if (rootAnimator != null)
            {
                if (rootAnimator.avatar != null)
                {
                    rootAnimator.avatar = null;
                    changed = true;
                }

                if (rootAnimator.runtimeAnimatorController != null)
                {
                    rootAnimator.runtimeAnimatorController = null;
                    changed = true;
                }

                rootAnimator.enabled = false;

                EditorUtility.SetDirty(rootAnimator);
            }

            EditorUtility.SetDirty(visualInstance.transform);
            EditorUtility.SetDirty(visualInstance);
            return true;
        }

        public static Animator FindImportedPreviewAnimator(GameObject actor)
        {
            if (actor == null)
            {
                return null;
            }

            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);
            return importedVisualRoot != null
                ? importedVisualRoot.GetComponentInChildren<Animator>(true)
                : null;
        }

        public static bool HasUserOwnedGhostSamuraiEnemyWeaponContract(
            GameObject actor,
            CombatProxyVisualKind kind)
        {
            Transform importedRoot = actor != null ? actor.transform.Find(ImportedVisualRootName) : null;

            if (importedRoot == null || importedRoot.Find(ImportedRoleMarkerRootName) != null)
            {
                return false;
            }

            bool isRanged = kind == CombatProxyVisualKind.EnemyRanged;

            if (isRanged)
            {
                Transform integratedBow = FindDeepChild(importedRoot, GhostSamuraiIntegratedBowRendererName);

                if (integratedBow == null || integratedBow.GetComponentInChildren<Renderer>(true) == null)
                {
                    return false;
                }
            }

            string anchorName = isRanged ? GhostSamuraiArrowAnchorName : GhostSamuraiKatanaAnchorName;
            string expectedWeaponPath = isRanged ? GhostSamuraiArrowWeaponPath : GhostSamuraiKatanaWeaponPath;
            Transform anchor = FindDeepChild(importedRoot, anchorName);
            Transform weaponRoot = anchor != null ? anchor.Find(ImportedWeaponRootName) : null;

            if (weaponRoot == null
                || weaponRoot.parent != anchor
                || weaponRoot.childCount != 1
                || !HasIdentityLocalTransform(weaponRoot))
            {
                return false;
            }

            Transform weaponInstance = weaponRoot.GetChild(0);
            Object weaponSource = weaponInstance != null
                ? PrefabUtility.GetCorrespondingObjectFromSource(weaponInstance.gameObject)
                : null;
            string weaponSourcePath = weaponSource != null
                ? AssetDatabase.GetAssetPath(weaponSource)
                : string.Empty;

            return weaponInstance != null
                && string.Equals(
                    weaponInstance.name,
                    System.IO.Path.GetFileNameWithoutExtension(expectedWeaponPath),
                    System.StringComparison.Ordinal)
                && string.Equals(
                    weaponSourcePath,
                    expectedWeaponPath,
                    System.StringComparison.Ordinal)
                && HasIdentityLocalTransform(weaponInstance)
                && weaponInstance.GetComponentInChildren<Renderer>(true) != null;
        }

        public static bool HasUserOwnedGhostSamuraiEnemyMaterialContract(
            GameObject actor,
            CombatProxyVisualKind kind)
        {
            Transform importedRoot = actor != null ? actor.transform.Find(ImportedVisualRootName) : null;
            Renderer[] renderers = importedRoot != null
                ? importedRoot.GetComponentsInChildren<Renderer>(true)
                : System.Array.Empty<Renderer>();

            if (renderers.Length == 0)
            {
                return false;
            }

            HashSet<string> paletteMaterialPaths = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;

                if (materials.Length == 0)
                {
                    return false;
                }

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material material = materials[materialIndex];

                    if (material == null
                        || material.shader == null
                        || !material.shader.isSupported
                        || !string.Equals(material.shader.name, "Standard", System.StringComparison.Ordinal)
                        || material.mainTexture != null
                        || !IsUserOwnedGhostSamuraiEnemyPaletteMaterial(material))
                    {
                        return false;
                    }

                    paletteMaterialPaths.Add(AssetDatabase.GetAssetPath(material));
                }
            }

            int requiredMaterialCount = kind == CombatProxyVisualKind.EnemyRanged ? 8 : 7;
            return paletteMaterialPaths.Count >= requiredMaterialCount;
        }

        public static bool IsUserOwnedGhostSamuraiEnemyPaletteMaterial(Material material)
        {
            return material != null
                && IsUnderAssetRoot(
                    AssetDatabase.GetAssetPath(material),
                    UserOwnedGhostSamuraiEnemyPaletteFolder);
        }

        private static bool AttachUserOwnedGhostSamuraiEnemyWeapon(
            GameObject visualRoot,
            CombatProxyVisualKind kind)
        {
            bool isRanged = kind == CombatProxyVisualKind.EnemyRanged;
            string anchorName = isRanged ? GhostSamuraiArrowAnchorName : GhostSamuraiKatanaAnchorName;
            string weaponPath = isRanged ? GhostSamuraiArrowWeaponPath : GhostSamuraiKatanaWeaponPath;
            ImportedEnemyEquipmentKind equipmentKind = isRanged
                ? ImportedEnemyEquipmentKind.Arrow
                : ImportedEnemyEquipmentKind.Katana;
            Transform anchor = visualRoot != null ? FindDeepChild(visualRoot.transform, anchorName) : null;
            GameObject weaponPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(weaponPath);

            if (anchor == null || weaponPrefab == null)
            {
                return false;
            }

            if (isRanged)
            {
                Transform integratedBow = FindDeepChild(visualRoot.transform, GhostSamuraiIntegratedBowRendererName);

                if (integratedBow == null || integratedBow.GetComponentInChildren<Renderer>(true) == null)
                {
                    return false;
                }
            }

            GameObject weaponRoot = new GameObject(ImportedWeaponRootName);
            weaponRoot.transform.SetParent(anchor, false);
            weaponRoot.transform.localPosition = Vector3.zero;
            weaponRoot.transform.localRotation = Quaternion.identity;
            weaponRoot.transform.localScale = Vector3.one;

            GameObject weaponInstance = (GameObject)PrefabUtility.InstantiatePrefab(weaponPrefab);

            if (weaponInstance == null)
            {
                Object.DestroyImmediate(weaponRoot);
                return false;
            }

            weaponInstance.name = weaponPrefab.name;
            weaponInstance.transform.SetParent(weaponRoot.transform, false);
            weaponInstance.transform.localPosition = Vector3.zero;
            weaponInstance.transform.localRotation = Quaternion.identity;
            weaponInstance.transform.localScale = Vector3.one;
            StripImportedVisualComponents(weaponInstance, null);
            ApplyUserOwnedGhostSamuraiEnemyPalette(weaponInstance, kind, equipmentKind);
            EditorUtility.SetDirty(weaponRoot.transform);
            EditorUtility.SetDirty(weaponRoot);
            EditorUtility.SetDirty(weaponInstance.transform);
            EditorUtility.SetDirty(weaponInstance);
            return true;
        }

        private static bool ApplyUserOwnedGhostSamuraiEnemyPalette(
            GameObject visualRoot,
            CombatProxyVisualKind kind,
            ImportedEnemyEquipmentKind equipmentKind)
        {
            if (visualRoot == null || SourceProfile != ImportedEnemySourceProfile.UserOwnedGhostSamurai)
            {
                return false;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            bool changed = false;

            for (int rendererIndex = 0; rendererIndex < renderers.Length; rendererIndex++)
            {
                Material[] materials = renderers[rendererIndex].sharedMaterials;
                bool rendererChanged = false;

                for (int materialIndex = 0; materialIndex < materials.Length; materialIndex++)
                {
                    Material sourceMaterial = materials[materialIndex];

                    if (IsUserOwnedGhostSamuraiEnemyPaletteMaterial(sourceMaterial)
                        || !TryResolveUserOwnedGhostSamuraiEnemyMaterialStyle(
                            sourceMaterial != null ? sourceMaterial.name : string.Empty,
                            materialIndex,
                            kind,
                            equipmentKind,
                            out string assetName,
                            out Color color,
                            out float metallic,
                            out float smoothness,
                            out Color emissionColor))
                    {
                        continue;
                    }

                    Material paletteMaterial = GetOrCreateUserOwnedGhostSamuraiEnemyPaletteMaterial(
                        assetName,
                        color,
                        metallic,
                        smoothness,
                        emissionColor);

                    if (paletteMaterial == null || ReferenceEquals(sourceMaterial, paletteMaterial))
                    {
                        continue;
                    }

                    materials[materialIndex] = paletteMaterial;
                    rendererChanged = true;
                }

                if (!rendererChanged)
                {
                    continue;
                }

                renderers[rendererIndex].sharedMaterials = materials;
                EditorUtility.SetDirty(renderers[rendererIndex]);
                changed = true;
            }

            if (changed)
            {
                AssetDatabase.SaveAssets();
            }

            return changed;
        }

        private static bool TryResolveUserOwnedGhostSamuraiEnemyMaterialStyle(
            string sourceMaterialName,
            int materialIndex,
            CombatProxyVisualKind kind,
            ImportedEnemyEquipmentKind equipmentKind,
            out string assetName,
            out Color color,
            out float metallic,
            out float smoothness,
            out Color emissionColor)
        {
            string rolePrefix = "GhostSamurai_" + kind;
            string normalizedName = string.IsNullOrWhiteSpace(sourceMaterialName)
                ? string.Empty
                : sourceMaterialName.Trim();
            Color armorColor;
            Color clothColor;
            Color accentColor;

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    armorColor = new Color(0.42f, 0.055f, 0.045f, 1f);
                    clothColor = new Color(0.055f, 0.018f, 0.022f, 1f);
                    accentColor = new Color(0.95f, 0.12f, 0.035f, 1f);
                    break;
                case CombatProxyVisualKind.EnemyMobile:
                    armorColor = new Color(0.035f, 0.3f, 0.32f, 1f);
                    clothColor = new Color(0.018f, 0.055f, 0.065f, 1f);
                    accentColor = new Color(0.05f, 0.9f, 0.95f, 1f);
                    break;
                case CombatProxyVisualKind.EnemyRanged:
                    armorColor = new Color(0.34f, 0.23f, 0.055f, 1f);
                    clothColor = new Color(0.07f, 0.045f, 0.018f, 1f);
                    accentColor = new Color(1f, 0.57f, 0.06f, 1f);
                    break;
                default:
                    armorColor = new Color(0.25f, 0.25f, 0.25f, 1f);
                    clothColor = new Color(0.04f, 0.04f, 0.04f, 1f);
                    accentColor = Color.red;
                    break;
            }

            assetName = string.Empty;
            color = Color.white;
            metallic = 0f;
            smoothness = 0.15f;
            emissionColor = Color.black;

            if (equipmentKind == ImportedEnemyEquipmentKind.Body)
            {
                switch (normalizedName)
                {
                    case "Material__2":
                        assetName = rolePrefix + "_Body_Armor";
                        color = armorColor;
                        metallic = 0.22f;
                        smoothness = 0.28f;
                        return true;
                    case "Material #3":
                        assetName = rolePrefix + "_Body_Cloth";
                        color = clothColor;
                        smoothness = 0.1f;
                        return true;
                    case "Material #1":
                        assetName = rolePrefix + "_Eye_Accent";
                        color = accentColor;
                        smoothness = 0.22f;
                        emissionColor = accentColor * 0.22f;
                        emissionColor.a = 1f;
                        return true;
                    case "M_RecurveBow_01":
                        assetName = rolePrefix + "_Bow_Wood";
                        color = new Color(0.2f, 0.075f, 0.025f, 1f);
                        smoothness = 0.24f;
                        return true;
                    case "Material #280":
                        assetName = rolePrefix + "_Bow_Fittings";
                        color = accentColor * 0.55f;
                        color.a = 1f;
                        metallic = 0.5f;
                        smoothness = 0.4f;
                        return true;
                    default:
                        assetName = rolePrefix + "_Body_Slot_" + Mathf.Max(0, materialIndex);
                        color = armorColor;
                        return true;
                }
            }

            if (equipmentKind == ImportedEnemyEquipmentKind.Katana)
            {
                switch (normalizedName)
                {
                    case "Material #3":
                        assetName = rolePrefix + "_Katana_Grip";
                        color = clothColor;
                        smoothness = 0.1f;
                        return true;
                    case "Material #25":
                        assetName = rolePrefix + "_Katana_Blade";
                        color = new Color(0.62f, 0.68f, 0.75f, 1f);
                        metallic = 0.85f;
                        smoothness = 0.72f;
                        return true;
                    case "Material #10":
                        assetName = rolePrefix + "_Katana_Guard";
                        color = accentColor * 0.42f;
                        color.a = 1f;
                        metallic = 0.55f;
                        smoothness = 0.35f;
                        return true;
                    case "Material #62":
                        assetName = rolePrefix + "_Katana_Edge";
                        color = new Color(0.88f, 0.92f, 0.98f, 1f);
                        metallic = 0.95f;
                        smoothness = 0.82f;
                        return true;
                    default:
                        assetName = rolePrefix + "_Katana_Slot_" + Mathf.Max(0, materialIndex);
                        color = accentColor;
                        metallic = 0.35f;
                        return true;
                }
            }

            switch (normalizedName)
            {
                case "Material #63":
                    assetName = rolePrefix + "_Arrow_Head";
                    color = new Color(0.68f, 0.72f, 0.78f, 1f);
                    metallic = 0.8f;
                    smoothness = 0.62f;
                    return true;
                case "Material #38":
                    assetName = rolePrefix + "_Arrow_Shaft";
                    color = new Color(0.28f, 0.1f, 0.028f, 1f);
                    smoothness = 0.16f;
                    return true;
                case "Material #50":
                    assetName = rolePrefix + "_Arrow_Fletching";
                    color = accentColor;
                    smoothness = 0.18f;
                    return true;
                default:
                    assetName = rolePrefix + "_Arrow_Slot_" + Mathf.Max(0, materialIndex);
                    color = accentColor;
                    return true;
            }
        }

        private static Material GetOrCreateUserOwnedGhostSamuraiEnemyPaletteMaterial(
            string assetName,
            Color color,
            float metallic,
            float smoothness,
            Color emissionColor)
        {
            Shader standardShader = Shader.Find("Standard");

            if (standardShader == null || string.IsNullOrWhiteSpace(assetName))
            {
                return null;
            }

            EnsureFolder(UserOwnedGhostSamuraiEnemyPaletteFolder);
            string materialPath = $"{UserOwnedGhostSamuraiEnemyPaletteFolder}/{assetName}.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);

            if (material == null)
            {
                material = new Material(standardShader)
                {
                    name = assetName
                };
                AssetDatabase.CreateAsset(material, materialPath);
            }

            material.shader = standardShader;
            material.color = color;
            material.mainTexture = null;
            material.SetFloat("_Metallic", Mathf.Clamp01(metallic));
            material.SetFloat("_Glossiness", Mathf.Clamp01(smoothness));
            material.SetColor("_EmissionColor", emissionColor);

            if (emissionColor.maxColorComponent > 0.001f)
            {
                material.EnableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            else
            {
                material.DisableKeyword("_EMISSION");
                material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.EmissiveIsBlack;
            }

            EditorUtility.SetDirty(material);
            return material;
        }

        public static bool RemoveImportedVisual(GameObject actor, Animator rootAnimator)
        {
            if (actor == null)
            {
                return false;
            }

            bool changed = false;
            Transform importedVisualRoot = actor.transform.Find(ImportedVisualRootName);
            Transform proxyRoot = actor.transform.Find(ProxyRootName);

            if (importedVisualRoot == null && proxyRoot != null)
            {
                importedVisualRoot = proxyRoot.Find(ImportedVisualRootName);
            }

            if (importedVisualRoot != null)
            {
                Object.DestroyImmediate(importedVisualRoot.gameObject);
                changed = true;
            }

            if (proxyRoot != null)
            {
                changed |= SetProxyRenderersEnabled(proxyRoot, true);
            }

            if (rootAnimator != null)
            {
                if (!rootAnimator.enabled)
                {
                    rootAnimator.enabled = true;
                    changed = true;
                }

                if (rootAnimator.avatar != null)
                {
                    rootAnimator.avatar = null;
                    changed = true;
                }

                if (rootAnimator.runtimeAnimatorController != null)
                {
                    rootAnimator.runtimeAnimatorController = null;
                    changed = true;
                }

                if (changed)
                {
                    EditorUtility.SetDirty(rootAnimator);
                }
            }

            return changed;
        }

        private static bool AddImportedRoleMarkers(Transform visualRoot, CombatProxyVisualKind kind)
        {
            if (visualRoot == null)
            {
                return false;
            }

            bool changed = false;
            Transform previousMarkerRoot = visualRoot.Find(ImportedRoleMarkerRootName);

            if (previousMarkerRoot != null)
            {
                Object.DestroyImmediate(previousMarkerRoot.gameObject);
                changed = true;
            }

            Material primaryMaterial = LoadRoleMarkerMaterial(kind, false);
            Material accentMaterial = LoadRoleMarkerMaterial(kind, true);
            Transform markerRoot = CreateTransformChild(
                visualRoot,
                ImportedRoleMarkerRootName,
                Vector3.zero,
                Quaternion.identity,
                Vector3.one);

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    CreatePrimitive(markerRoot, "MeleeShoulderLeft", PrimitiveType.Cube, new Vector3(-0.34f, 1.32f, 0.02f), Quaternion.identity, new Vector3(0.22f, 0.16f, 0.24f), primaryMaterial);
                    CreatePrimitive(markerRoot, "MeleeShoulderRight", PrimitiveType.Cube, new Vector3(0.34f, 1.32f, 0.02f), Quaternion.identity, new Vector3(0.22f, 0.16f, 0.24f), primaryMaterial);
                    CreatePrimitive(markerRoot, "MeleeBlade", PrimitiveType.Cube, new Vector3(0.38f, 1.1f, 0.46f), Quaternion.Euler(0f, -8f, 76f), new Vector3(0.08f, 0.08f, 0.86f), accentMaterial);
                    break;
                case CombatProxyVisualKind.EnemyMobile:
                    CreatePrimitive(markerRoot, "MobileFinLeft", PrimitiveType.Cube, new Vector3(-0.4f, 1f, 0.04f), Quaternion.Euler(0f, 0f, -32f), new Vector3(0.12f, 0.42f, 0.08f), accentMaterial);
                    CreatePrimitive(markerRoot, "MobileFinRight", PrimitiveType.Cube, new Vector3(0.4f, 1f, 0.04f), Quaternion.Euler(0f, 0f, 32f), new Vector3(0.12f, 0.42f, 0.08f), accentMaterial);
                    CreatePrimitive(markerRoot, "MobileTail", PrimitiveType.Cube, new Vector3(0f, 0.78f, -0.24f), Quaternion.identity, new Vector3(0.12f, 0.2f, 0.54f), primaryMaterial);
                    break;
                case CombatProxyVisualKind.EnemyRanged:
                    CreatePrimitive(markerRoot, "FocusOrb", PrimitiveType.Sphere, new Vector3(0f, 1.12f, 0.5f), Quaternion.identity, new Vector3(0.2f, 0.2f, 0.2f), accentMaterial);
                    CreatePrimitive(markerRoot, "Staff", PrimitiveType.Cylinder, new Vector3(0.36f, 1.02f, 0.16f), Quaternion.Euler(0f, 0f, 10f), new Vector3(0.05f, 0.62f, 0.05f), primaryMaterial);
                    CreatePrimitive(markerRoot, "CasterPack", PrimitiveType.Cube, new Vector3(0f, 1.12f, -0.18f), Quaternion.identity, new Vector3(0.3f, 0.28f, 0.2f), accentMaterial);
                    break;
                default:
                    Object.DestroyImmediate(markerRoot.gameObject);
                    return changed;
            }

            EditorUtility.SetDirty(markerRoot.gameObject);
            return true;
        }

        private static Material LoadRoleMarkerMaterial(CombatProxyVisualKind kind, bool accent)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(GetRoleMarkerMaterialPath(kind, accent));
        }

        private static string GetRoleMarkerMaterialPath(CombatProxyVisualKind kind, bool accent)
        {
            string suffix = accent ? "Accent" : "Primary";

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMelee" + suffix + ".mat";
                case CombatProxyVisualKind.EnemyMobile:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMobile" + suffix + ".mat";
                case CombatProxyVisualKind.EnemyRanged:
                    return MaterialsFolder + "/M_CombatProxy_EnemyRanged" + suffix + ".mat";
                default:
                    return MaterialsFolder + "/M_CombatProxy_EnemyMelee" + suffix + ".mat";
            }
        }

        private static GameObject CreatePrimitive(
            Transform parent,
            string name,
            PrimitiveType primitiveType,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale,
            Material material)
        {
            GameObject primitive = GameObject.CreatePrimitive(primitiveType);
            primitive.name = name;
            primitive.transform.SetParent(parent, false);
            primitive.transform.localPosition = localPosition;
            primitive.transform.localRotation = localRotation;
            primitive.transform.localScale = localScale;

            Collider collider = primitive.GetComponent<Collider>();

            if (collider != null)
            {
                Object.DestroyImmediate(collider);
            }

            Renderer renderer = primitive.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial = material;
                EditorUtility.SetDirty(renderer);
            }

            return primitive;
        }

        private static Transform CreateTransformChild(
            Transform parent,
            string name,
            Vector3 localPosition,
            Quaternion localRotation,
            Vector3 localScale)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = localRotation;
            child.transform.localScale = localScale;
            return child.transform;
        }

        private static void AddClipState(AnimatorStateMachine stateMachine, string stateName, AnimationClip clip, float speed = 1f)
        {
            AnimatorState state = stateMachine.AddState(stateName);
            state.motion = clip;
            state.speed = Mathf.Max(0.01f, speed);
        }

        private static void AddAttackClipStates(
            AnimatorStateMachine stateMachine,
            string stateName,
            AnimationClip[] clips,
            float speed = 1f)
        {
            if (clips == null || clips.Length == 0)
            {
                return;
            }

            AddClipState(stateMachine, stateName, clips[0], speed);
            int variantCount = Mathf.Min(clips.Length, MaxImportedAttackVariantStates);

            for (int i = 0; i < variantCount; i++)
            {
                AddClipState(
                    stateMachine,
                    EnemyCombatAnimationRelay.FormatAttackVariantStateName(stateName, i + 1),
                    clips[i],
                    speed);
            }
        }

        private static void AddCombatPoseLayer(
            AnimatorController controller,
            AnimationClip basePoseClip,
            AnimationClip antiAirReadClip,
            AnimationClip chaseRollReadClip,
            AnimationClip guardBreakReadClip)
        {
            if (controller == null || basePoseClip == null)
            {
                return;
            }

            AvatarMask avatarMask = EnsureUpperBodyAvatarMask();

            if (avatarMask == null)
            {
                return;
            }

            AnimatorStateMachine stateMachine = new AnimatorStateMachine
            {
                name = CombatPoseLayerName
            };
            AssetDatabase.AddObjectToAsset(stateMachine, controller);

            AnimatorState holdState = stateMachine.AddState(CombatPoseStateName);
            holdState.motion = basePoseClip;
            stateMachine.defaultState = holdState;
            AddCombatPoseResponseState(
                stateMachine,
                holdState,
                CombatPoseAntiAirReadStateName,
                antiAirReadClip,
                EnemyCombatAnimationPlanUtility.AntiAirReadParameterName);
            AddCombatPoseResponseState(
                stateMachine,
                holdState,
                CombatPoseChaseRollReadStateName,
                chaseRollReadClip,
                EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName);
            AddCombatPoseResponseState(
                stateMachine,
                holdState,
                CombatPoseGuardBreakReadStateName,
                guardBreakReadClip,
                EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName);

            AnimatorControllerLayer layer = new AnimatorControllerLayer
            {
                name = CombatPoseLayerName,
                avatarMask = avatarMask,
                blendingMode = AnimatorLayerBlendingMode.Override,
                defaultWeight = 0f,
                iKPass = false,
                stateMachine = stateMachine,
                syncedLayerAffectsTiming = false
            };

            controller.AddLayer(layer);
        }

        private static void AddCombatPoseResponseState(
            AnimatorStateMachine stateMachine,
            AnimatorState holdState,
            string stateName,
            AnimationClip clip,
            string parameterName)
        {
            if (stateMachine == null || holdState == null || clip == null || string.IsNullOrEmpty(parameterName))
            {
                return;
            }

            AnimatorState responseState = stateMachine.AddState(stateName);
            responseState.motion = clip;

            AnimatorStateTransition enterTransition = stateMachine.AddAnyStateTransition(responseState);
            enterTransition.canTransitionToSelf = false;
            enterTransition.hasExitTime = false;
            enterTransition.duration = CombatPoseReadTransitionDuration;
            enterTransition.offset = 0f;
            enterTransition.interruptionSource = TransitionInterruptionSource.None;
            enterTransition.AddCondition(AnimatorConditionMode.Greater, CombatPoseReadThreshold, parameterName);

            AnimatorStateTransition exitTransition = responseState.AddTransition(holdState);
            exitTransition.hasExitTime = false;
            exitTransition.duration = CombatPoseReadTransitionDuration;
            exitTransition.offset = 0f;
            exitTransition.interruptionSource = TransitionInterruptionSource.None;
            exitTransition.AddCondition(AnimatorConditionMode.Less, CombatPoseReadReleaseThreshold, parameterName);
        }

        private static AvatarMask EnsureUpperBodyAvatarMask()
        {
            AvatarMask avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>(ImportedUpperBodyMaskPath);

            if (avatarMask != null)
            {
                return avatarMask;
            }

            avatarMask = new AvatarMask();

            for (int i = 0; i < (int)AvatarMaskBodyPart.LastBodyPart; i++)
            {
                avatarMask.SetHumanoidBodyPartActive((AvatarMaskBodyPart)i, false);
            }

            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Body, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.Head, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftArm, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightArm, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftFingers, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightFingers, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.LeftHandIK, true);
            avatarMask.SetHumanoidBodyPartActive(AvatarMaskBodyPart.RightHandIK, true);

            AssetDatabase.CreateAsset(avatarMask, ImportedUpperBodyMaskPath);
            return avatarMask;
        }

        private static string GetImportedAnimatorControllerPath(CombatProxyVisualKind kind)
        {
            return ImportedAnimatorControllerPathPrefix + kind + ".controller";
        }

        private static ImportedEnemyAnimationProfile ResolveAnimationProfile(CombatProxyVisualKind kind)
        {
            string selectedVisualPrefabPath = GetSelectedHumanoidVisualPrefabPath(kind);

            if (kind == CombatProxyVisualKind.EnemyMelee)
            {
                return ImportedEnemyAnimationProfile.OneHanded;
            }

            if (kind == CombatProxyVisualKind.EnemyMobile)
            {
                if (!string.IsNullOrEmpty(selectedVisualPrefabPath)
                    && selectedVisualPrefabPath.IndexOf("Polearm", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return ImportedEnemyAnimationProfile.Polearm;
                }

                return ImportedEnemyAnimationProfile.TwoHanded;
            }

            if (kind == CombatProxyVisualKind.EnemyRanged)
            {
                return ImportedEnemyAnimationProfile.Ranged;
            }

            return ImportedEnemyAnimationProfile.Default;
        }

        private static string[] GetIdleClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                case ImportedEnemyAnimationProfile.Polearm:
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return MergeCandidatePaths(
                        GhostSamuraiKatanaIdleClipCandidatePaths,
                        OneHandedIdleClipCandidatePaths,
                        PolearmIdleClipCandidatePaths,
                        TwoHandedIdleClipCandidatePaths,
                        DefaultIdleClipCandidatePaths);
                case ImportedEnemyAnimationProfile.Ranged:
                    return MergeCandidatePaths(
                        GhostSamuraiBowIdleClipCandidatePaths,
                        RangedIdleClipCandidatePaths,
                        DefaultIdleClipCandidatePaths);
                default:
                    return DefaultIdleClipCandidatePaths;
            }
        }

        private static string[] GetHoldClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                    return OneHandedHoldClipCandidatePaths;
                case ImportedEnemyAnimationProfile.Polearm:
                    return PolearmHoldClipCandidatePaths;
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return TwoHandedHoldClipCandidatePaths;
                default:
                    return System.Array.Empty<string>();
            }
        }

        private static string[] GetWalkClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                case ImportedEnemyAnimationProfile.Polearm:
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return MergeCandidatePaths(
                        GhostSamuraiKatanaWalkClipCandidatePaths,
                        OneHandedWalkClipCandidatePaths,
                        WalkClipCandidatePaths);
                case ImportedEnemyAnimationProfile.Ranged:
                    return MergeCandidatePaths(
                        GhostSamuraiBowWalkClipCandidatePaths,
                        WalkClipCandidatePaths);
                default:
                    return WalkClipCandidatePaths;
            }
        }

        private static string[] GetRunClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.OneHanded:
                case ImportedEnemyAnimationProfile.Polearm:
                case ImportedEnemyAnimationProfile.TwoHanded:
                    return MergeCandidatePaths(
                        GhostSamuraiKatanaRunClipCandidatePaths,
                        OneHandedRunClipCandidatePaths,
                        RunClipCandidatePaths);
                case ImportedEnemyAnimationProfile.Ranged:
                    return MergeCandidatePaths(
                        GhostSamuraiBowRunClipCandidatePaths,
                        RunClipCandidatePaths);
                default:
                    return RunClipCandidatePaths;
            }
        }

        private static string[] GetHitClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.Ranged:
                    return MergeCandidatePaths(GhostSamuraiBowHitClipCandidatePaths, HitClipCandidatePaths);
                default:
                    return MergeCandidatePaths(GhostSamuraiKatanaHitClipCandidatePaths, HitClipCandidatePaths);
            }
        }

        private static string[] GetDeathClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            switch (profile)
            {
                case ImportedEnemyAnimationProfile.Ranged:
                    return MergeCandidatePaths(GhostSamuraiBowDeathClipCandidatePaths, DeathClipCandidatePaths);
                default:
                    return MergeCandidatePaths(GhostSamuraiKatanaDeathClipCandidatePaths, DeathClipCandidatePaths);
            }
        }

        private static string[] GetMeleeAttackClipCandidatePaths()
        {
            return MergeCandidatePaths(
                GhostSamuraiMeleeAttackClipCandidatePaths,
                GetGhostSamuraiKatanaAttackInplaceClipPaths(),
                MeleeAttackClipCandidatePaths);
        }

        private static string[] GetMobileAttackClipCandidatePaths()
        {
            return MergeCandidatePaths(
                GhostSamuraiMobileAttackClipCandidatePaths,
                GetGhostSamuraiKatanaAttackInplaceClipPaths(),
                MobileAttackClipCandidatePaths);
        }

        private static string[] GetRangedAttackClipCandidatePaths()
        {
            return MergeCandidatePaths(
                GhostSamuraiBowAttackClipCandidatePaths,
                GetGhostSamuraiBowAttackInplaceClipPaths(),
                RangedAttackClipCandidatePaths);
        }

        private static string[] GetGhostSamuraiKatanaAttackInplaceClipPaths()
        {
            return FindFbxPathsInFolder(GhostSamuraiKatanaAttackInplaceFolder);
        }

        private static string[] GetGhostSamuraiBowAttackInplaceClipPaths()
        {
            string[] paths = FindFbxPathsInFolder(GhostSamuraiBowAttackInplaceFolder);
            List<string> filteredPaths = new List<string>();

            for (int i = 0; i < paths.Length; i++)
            {
                string path = paths[i];

                if (path.IndexOf("Idle", System.StringComparison.OrdinalIgnoreCase) >= 0
                    || path.IndexOf("Hold", System.StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                filteredPaths.Add(path);
            }

            return filteredPaths.ToArray();
        }

        private static string[] GetAntiAirAttackClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowAntiAirClipCandidatePaths,
                    GhostSamuraiAntiAirAttackClipCandidatePaths,
                    RangedAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiAntiAirAttackClipCandidatePaths,
                    GhostSamuraiBowAntiAirClipCandidatePaths,
                    MobileAttackClipCandidatePaths);
        }

        private static string[] GetAntiAirReadClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowAntiAirReadClipCandidatePaths,
                    GhostSamuraiBowAntiAirClipCandidatePaths,
                    RangedAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiAntiAirReadClipCandidatePaths,
                    GhostSamuraiAntiAirAttackClipCandidatePaths,
                    MobileAttackClipCandidatePaths);
        }

        private static string[] GetChaseRollAttackClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowChaseRollClipCandidatePaths,
                    GhostSamuraiChaseRollAttackClipCandidatePaths,
                    MobileAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiChaseRollAttackClipCandidatePaths,
                    GhostSamuraiMobileAttackClipCandidatePaths,
                    MobileAttackClipCandidatePaths);
        }

        private static string[] GetChaseRollReadClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowChaseRollReadClipCandidatePaths,
                    GhostSamuraiBowChaseRollClipCandidatePaths,
                    RangedAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiChaseRollReadClipCandidatePaths,
                    GhostSamuraiChaseRollAttackClipCandidatePaths,
                    MobileAttackClipCandidatePaths);
        }

        private static string[] GetGuardBreakAttackClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowGuardBreakClipCandidatePaths,
                    GhostSamuraiGuardBreakAttackClipCandidatePaths,
                    MeleeAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiGuardBreakAttackClipCandidatePaths,
                    GhostSamuraiMobileAttackClipCandidatePaths,
                    MeleeAttackClipCandidatePaths);
        }

        private static string[] GetGuardBreakReadClipCandidatePaths(ImportedEnemyAnimationProfile profile)
        {
            return profile == ImportedEnemyAnimationProfile.Ranged
                ? MergeCandidatePaths(
                    GhostSamuraiBowGuardBreakReadClipCandidatePaths,
                    GhostSamuraiBowGuardBreakClipCandidatePaths,
                    RangedAttackClipCandidatePaths)
                : MergeCandidatePaths(
                    GhostSamuraiGuardBreakReadClipCandidatePaths,
                    GhostSamuraiGuardBreakAttackClipCandidatePaths,
                    MeleeAttackClipCandidatePaths);
        }

        private static string[] MergeCandidatePaths(params string[][] groups)
        {
            List<string> mergedPaths = new List<string>();

            for (int groupIndex = 0; groupIndex < groups.Length; groupIndex++)
            {
                string[] group = groups[groupIndex];

                if (group == null)
                {
                    continue;
                }

                for (int i = 0; i < group.Length; i++)
                {
                    string path = group[i];

                    if (string.IsNullOrEmpty(path) || mergedPaths.Contains(path))
                    {
                        continue;
                    }

                    mergedPaths.Add(path);
                }
            }

            return mergedPaths.ToArray();
        }

        private static string[] FindFbxPathsInFolder(string folder)
        {
            if (!AssetDatabase.IsValidFolder(folder))
            {
                return System.Array.Empty<string>();
            }

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folder });
            List<string> paths = new List<string>();

            for (int i = 0; i < guids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);

                if (string.IsNullOrEmpty(path)
                    || !path.EndsWith(".FBX", System.StringComparison.OrdinalIgnoreCase)
                    || paths.Contains(path))
                {
                    continue;
                }

                paths.Add(path);
            }

            paths.Sort(System.StringComparer.Ordinal);
            return paths.ToArray();
        }

        private static string[] GetCandidatePaths(CombatProxyVisualKind kind)
        {
            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    return EnemyMeleeVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyMobile:
                    return EnemyMobileVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyRanged:
                    return EnemyRangedVisualPrefabCandidatePaths;
                default:
                    return System.Array.Empty<string>();
            }
        }

        private static string[] GetActiveCandidatePaths(CombatProxyVisualKind kind)
        {
            if (SourceProfile != ImportedEnemySourceProfile.UserOwnedGhostSamurai)
            {
                return GetCandidatePaths(kind);
            }

            switch (kind)
            {
                case CombatProxyVisualKind.EnemyMelee:
                    return GhostSamuraiEnemyMeleeVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyMobile:
                    return GhostSamuraiEnemyMobileVisualPrefabCandidatePaths;
                case CombatProxyVisualKind.EnemyRanged:
                    return GhostSamuraiEnemyRangedVisualPrefabCandidatePaths;
                default:
                    return System.Array.Empty<string>();
            }
        }

        private static string[] GetActiveAvatarCandidatePaths()
        {
            return SourceProfile == ImportedEnemySourceProfile.UserOwnedGhostSamurai
                ? GhostSamuraiEnemyAvatarCandidatePaths
                : EnemyAvatarCandidatePaths;
        }

        private static void StripImportedVisualComponents(GameObject visualRoot, Animator preservedAnimator)
        {
            if (visualRoot == null)
            {
                return;
            }

            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                if (animators[i] == preservedAnimator)
                {
                    continue;
                }

                Object.DestroyImmediate(animators[i]);
            }

            MonoBehaviour[] behaviours = visualRoot.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                Object.DestroyImmediate(behaviours[i]);
            }

            Animation[] legacyAnimations = visualRoot.GetComponentsInChildren<Animation>(true);

            for (int i = 0; i < legacyAnimations.Length; i++)
            {
                Object.DestroyImmediate(legacyAnimations[i]);
            }

            Collider[] colliders = visualRoot.GetComponentsInChildren<Collider>(true);

            for (int i = 0; i < colliders.Length; i++)
            {
                Object.DestroyImmediate(colliders[i]);
            }

            Rigidbody[] rigidbodies = visualRoot.GetComponentsInChildren<Rigidbody>(true);

            for (int i = 0; i < rigidbodies.Length; i++)
            {
                Object.DestroyImmediate(rigidbodies[i]);
            }

            CharacterController[] characterControllers = visualRoot.GetComponentsInChildren<CharacterController>(true);

            for (int i = 0; i < characterControllers.Length; i++)
            {
                Object.DestroyImmediate(characterControllers[i]);
            }

            NavMeshAgent[] navMeshAgents = visualRoot.GetComponentsInChildren<NavMeshAgent>(true);

            for (int i = 0; i < navMeshAgents.Length; i++)
            {
                Object.DestroyImmediate(navMeshAgents[i]);
            }
        }

        private static GameObject LoadFirstHumanoidPrefab(string[] candidatePaths)
        {
            string selectedPath = FindFirstCompatibleHumanoidPath(candidatePaths);
            return string.IsNullOrWhiteSpace(selectedPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<GameObject>(selectedPath);
        }

        private static string FindFirstCompatibleHumanoidPath(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(candidatePaths[i]);

                if (prefab != null && IsCompatibleWithHumanoidAvatarPreview(prefab))
                {
                    return candidatePaths[i];
                }
            }

            return null;
        }

        private static bool IsCompatibleWithHumanoidAvatarPreview(GameObject visualPrefab)
        {
            if (visualPrefab == null)
            {
                return false;
            }

            if (visualPrefab.GetComponentInChildren<SkinnedMeshRenderer>(true) == null)
            {
                return false;
            }

            return FindAvatar(visualPrefab) != null;
        }

        private static Avatar FindAvatar(GameObject visualRoot)
        {
            Animator animator = FindAvatarAnimator(visualRoot);

            if (animator != null && animator.avatar != null && animator.avatar.isValid)
            {
                return animator.avatar;
            }

            return LoadFirstAvailableAvatar(GetActiveAvatarCandidatePaths());
        }

        private static Animator FindAvatarAnimator(GameObject visualRoot)
        {
            if (visualRoot == null)
            {
                return null;
            }

            Animator[] animators = visualRoot.GetComponentsInChildren<Animator>(true);

            for (int i = 0; i < animators.Length; i++)
            {
                Animator animator = animators[i];

                if (animator != null && animator.avatar != null && animator.avatar.isValid)
                {
                    return animator;
                }
            }

            return animators.Length > 0 ? animators[0] : null;
        }

        private static Avatar LoadFirstAvailableAvatar(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);

                for (int j = 0; j < assets.Length; j++)
                {
                    Avatar avatar = assets[j] as Avatar;

                    if (avatar != null && avatar.isValid)
                    {
                        return avatar;
                    }
                }
            }

            return null;
        }

        private static AnimationClip LoadFirstAvailableAnimationClip(string[] candidatePaths)
        {
            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (!IsAnimationSourceAllowed(candidatePaths[i]))
                {
                    continue;
                }

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);

                for (int j = 0; j < assets.Length; j++)
                {
                    AnimationClip clip = assets[j] as AnimationClip;

                    if (IsGeneratedPreviewClip(clip))
                    {
                        continue;
                    }

                    return clip;
                }
            }

            return null;
        }

        private static AnimationClip[] LoadAvailableAnimationClips(string[] candidatePaths, int maxCount)
        {
            List<AnimationClip> clips = new List<AnimationClip>();
            int limit = Mathf.Max(1, maxCount);

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                if (!IsAnimationSourceAllowed(candidatePaths[i]))
                {
                    continue;
                }

                Object[] assets = AssetDatabase.LoadAllAssetsAtPath(candidatePaths[i]);

                for (int j = 0; j < assets.Length; j++)
                {
                    AnimationClip clip = assets[j] as AnimationClip;

                    if (IsGeneratedPreviewClip(clip)
                        || clip.name.IndexOf("AutoBackup", System.StringComparison.OrdinalIgnoreCase) >= 0
                        || clips.Contains(clip))
                    {
                        continue;
                    }

                    clips.Add(clip);

                    if (clips.Count >= limit)
                    {
                        return clips.ToArray();
                    }
                }
            }

            return clips.ToArray();
        }

        private static bool IsGeneratedPreviewClip(AnimationClip clip)
        {
            return clip == null
                || clip.name.StartsWith("__preview__", System.StringComparison.OrdinalIgnoreCase);
        }

        private static void EnsureGhostSamuraiGroundLocomotionClipImportSettings(string[] candidatePaths)
        {
            if (candidatePaths == null)
            {
                return;
            }

            for (int i = 0; i < candidatePaths.Length; i++)
            {
                string path = candidatePaths[i];

                if (string.IsNullOrEmpty(path)
                    || path.IndexOf("Assets/GhostSamurai_Animset/", System.StringComparison.Ordinal) < 0)
                {
                    continue;
                }

                ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;

                if (importer == null)
                {
                    continue;
                }

                ModelImporterClipAnimation[] clips = importer.clipAnimations;

                if (clips == null || clips.Length == 0)
                {
                    clips = importer.defaultClipAnimations;
                }

                if (clips == null || clips.Length == 0)
                {
                    continue;
                }

                bool changed = false;

                for (int j = 0; j < clips.Length; j++)
                {
                    changed |= ConfigureGroundLocomotionClip(clips[j]);
                }

                if (!changed)
                {
                    continue;
                }

                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }
        }

        private static bool ConfigureGroundLocomotionClip(ModelImporterClipAnimation clip)
        {
            if (clip == null)
            {
                return false;
            }

            bool changed = false;

            if (clip.wrapMode != WrapMode.Loop)
            {
                clip.wrapMode = WrapMode.Loop;
                changed = true;
            }

            if (!clip.loopTime)
            {
                clip.loopTime = true;
                changed = true;
            }

            if (!clip.loopPose)
            {
                clip.loopPose = true;
                changed = true;
            }

            if (clip.keepOriginalPositionY)
            {
                clip.keepOriginalPositionY = false;
                changed = true;
            }

            if (clip.keepOriginalPositionXZ)
            {
                clip.keepOriginalPositionXZ = false;
                changed = true;
            }

            if (!clip.lockRootHeightY)
            {
                clip.lockRootHeightY = true;
                changed = true;
            }

            if (!clip.lockRootPositionXZ)
            {
                clip.lockRootPositionXZ = true;
                changed = true;
            }

            if (!clip.heightFromFeet)
            {
                clip.heightFromFeet = true;
                changed = true;
            }

            return changed;
        }

        private static AnimationClip GetFirstAvailableClip(params AnimationClip[][] clipGroups)
        {
            for (int groupIndex = 0; groupIndex < clipGroups.Length; groupIndex++)
            {
                AnimationClip[] clips = clipGroups[groupIndex];

                if (clips == null || clips.Length == 0)
                {
                    continue;
                }

                return clips[0];
            }

            return null;
        }

        private static bool SetProxyRenderersEnabled(Transform proxyRoot, bool enabled)
        {
            if (proxyRoot == null)
            {
                return false;
            }

            bool changed = false;
            Renderer[] renderers = proxyRoot.GetComponentsInChildren<Renderer>(true);

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i].enabled == enabled)
                {
                    continue;
                }

                renderers[i].enabled = enabled;
                EditorUtility.SetDirty(renderers[i]);
                changed = true;
            }

            return changed;
        }

        private static bool AlignImportedVisualToGround(GameObject visualRoot, Transform actorRoot)
        {
            if (visualRoot == null || actorRoot == null)
            {
                return false;
            }

            Renderer[] renderers = visualRoot.GetComponentsInChildren<SkinnedMeshRenderer>(true);

            if (renderers.Length == 0)
            {
                renderers = visualRoot.GetComponentsInChildren<Renderer>(true);
            }

            if (renderers.Length == 0)
            {
                return false;
            }

            float minY = float.PositiveInfinity;

            for (int i = 0; i < renderers.Length; i++)
            {
                minY = Mathf.Min(minY, renderers[i].bounds.min.y);
            }

            if (!float.IsFinite(minY))
            {
                return false;
            }

            float desiredLift = actorRoot.position.y - minY - ImportedGroundingInset;

            if (Mathf.Abs(desiredLift) <= 0.0001f)
            {
                return false;
            }

            Vector3 worldPosition = visualRoot.transform.position;
            worldPosition.y += desiredLift;
            visualRoot.transform.position = worldPosition;
            return true;
        }

        private static Transform FindDeepChild(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrEmpty(targetName))
            {
                return null;
            }

            Transform[] transforms = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < transforms.Length; i++)
            {
                if (string.Equals(transforms[i].name, targetName, System.StringComparison.Ordinal))
                {
                    return transforms[i];
                }
            }

            return null;
        }

        private static bool HasIdentityLocalTransform(Transform transform)
        {
            const float positionTolerance = 0.00001f;
            const float rotationToleranceDegrees = 0.01f;
            const float scaleTolerance = 0.00001f;

            return transform != null
                && transform.localPosition.sqrMagnitude <= positionTolerance * positionTolerance
                && Quaternion.Angle(transform.localRotation, Quaternion.identity) <= rotationToleranceDegrees
                && (transform.localScale - Vector3.one).sqrMagnitude <= scaleTolerance * scaleTolerance;
        }

        private static bool IsUnderAssetRoot(string assetPath, string rootPath)
        {
            return !string.IsNullOrWhiteSpace(assetPath)
                && (assetPath.Equals(rootPath, System.StringComparison.OrdinalIgnoreCase)
                    || assetPath.StartsWith(rootPath + "/", System.StringComparison.OrdinalIgnoreCase));
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
