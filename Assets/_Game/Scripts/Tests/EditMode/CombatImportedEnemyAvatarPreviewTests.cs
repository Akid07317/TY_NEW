using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CampusRPG.AI;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedEnemyAvatarPreviewTests
    {
        private const string LocalPreviewFolderPath = "Assets/_Game/Animations/Characters/CombatTest/LocalPreview";
        private const string EnemyImportedPreviewControllerPath = LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMelee.controller";
        private const string EnemyMobileImportedPreviewControllerPath = LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMobile.controller";
        private const string EnemyRangedImportedPreviewControllerPath = LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyRanged.controller";
        private const string StableEnemyVisualPrefabPath =
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Sword and Shield.prefab";
        private const string MobileEnemyVisualPrefabPath =
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Melee Animations/Prefabs/Characters/HumanM_Dummy_Red - Polearm.prefab";
        private const string RangedEnemyVisualPrefabPath =
            "Assets/Kevin Iglesias/Human Animations/Unity Demo Scenes/Human Basic Motions/Prefabs/Human_BasicMotionsDummy_M.prefab";
        private const string GhostSamuraiPackagePath = "Assets/GhostSamurai_Animset";
        private const int MinimumGhostSamuraiKatanaAttackVariantStateCount = 50;
        private const int MinimumGhostSamuraiBowAttackVariantStateCount = 25;
        private static readonly string[] GhostSamuraiGroundLocomotionClipPaths =
        {
            "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace.FBX",
            "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_F_Loop_Inplace.FBX",
            "Assets/GhostSamurai_Animset/Animation/Bow/Movement/Inplace/GhostSamurai_Bow_AimWalk_F_Inplace.FBX",
            "Assets/GhostSamurai_Animset/Animation/Bow/Movement/Inplace/GhostSamurai_Bow_AimRun_F_Inplace.FBX",
            "Assets/GhostSamurai_Animset/Animation/Bow/Common/Inplace/GhostSamurai_Bow_Common_StrafeWalkF_Inplace.FBX",
            "Assets/GhostSamurai_Animset/Animation/Bow/Common/Inplace/GhostSamurai_Bow_Common_StrafeRun_F_Inplace.FBX"
        };

        private static readonly string[] CommittedPreviewControllerPaths =
        {
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMelee.controller",
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyMobile.controller",
            LocalPreviewFolderPath + "/AC_Enemy_ImportedPreview_EnemyRanged.controller"
        };

        [Test]
        public void CommittedImportedPreviewControllers_ExposeCurrentResponseReadStates()
        {
            try
            {
                CombatProxyVisualKind[] kinds =
                {
                    CombatProxyVisualKind.EnemyMelee,
                    CombatProxyVisualKind.EnemyMobile,
                    CombatProxyVisualKind.EnemyRanged
                };

                for (int i = 0; i < CommittedPreviewControllerPaths.Length; i++)
                {
                    string controllerPath = CommittedPreviewControllerPaths[i];
                    CombatProxyVisualKind kind = kinds[i];
                    RuntimeAnimatorController controller =
                        CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(kind);
                    AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

                    Assert.IsNotNull(controller, kind.ToString());
                    Assert.IsNotNull(animatorController, controllerPath);
                    AssertImportedResponseReadStates(animatorController, controllerPath);
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void SelectedEnemyVisualPrefabs_PreferDistinctHumanoidSourcesForArchetypeReadability()
        {
            if (!File.Exists(StableEnemyVisualPrefabPath)
                || !File.Exists(MobileEnemyVisualPrefabPath)
                || !File.Exists(RangedEnemyVisualPrefabPath))
            {
                Assert.Ignore("The distinct imported enemy humanoid preview sources are not available in this workspace.");
            }

            Assert.AreEqual(
                StableEnemyVisualPrefabPath,
                CombatImportedEnemyVisualUtility.GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind.EnemyMelee));
            Assert.AreEqual(
                MobileEnemyVisualPrefabPath,
                CombatImportedEnemyVisualUtility.GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind.EnemyMobile));
            Assert.AreEqual(
                RangedEnemyVisualPrefabPath,
                CombatImportedEnemyVisualUtility.GetSelectedHumanoidVisualPrefabPath(CombatProxyVisualKind.EnemyRanged));
        }

        [Test]
        public void UserOwnedGhostSamuraiProfile_BindsStrictEnemyBodiesWeaponsMaterialsAndAttackControllers()
        {
            if (!File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiEnemyMeleeModelPath)
                || !File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiEnemyRangedModelPath)
                || !File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiKatanaWeaponPath)
                || !File.Exists(CombatImportedEnemyVisualUtility.GhostSamuraiArrowWeaponPath))
            {
                Assert.Ignore("GhostSamurai user-owned enemy art sources are not available in this workspace.");
            }

            ImportedEnemySourceProfile previousProfile = CombatImportedEnemyVisualUtility.SourceProfile;
            CombatProxyVisualKind[] kinds =
            {
                CombatProxyVisualKind.EnemyMelee,
                CombatProxyVisualKind.EnemyMobile,
                CombatProxyVisualKind.EnemyRanged
            };
            string[] expectedVisualPaths =
            {
                CombatImportedEnemyVisualUtility.GhostSamuraiEnemyMeleeModelPath,
                CombatImportedEnemyVisualUtility.GhostSamuraiEnemyMobileModelPath,
                CombatImportedEnemyVisualUtility.GhostSamuraiEnemyRangedModelPath
            };
            string[] requiredAttackStates =
            {
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName,
                EnemyCombatAnimationPlanUtility.MobileAttackStateName,
                EnemyCombatAnimationPlanUtility.RangedAttackStateName
            };

            try
            {
                CombatImportedEnemyVisualUtility.SourceProfile = ImportedEnemySourceProfile.UserOwnedGhostSamurai;

                for (int kindIndex = 0; kindIndex < kinds.Length; kindIndex++)
                {
                    CombatProxyVisualKind kind = kinds[kindIndex];
                    Assert.AreEqual(
                        expectedVisualPaths[kindIndex],
                        CombatImportedEnemyVisualUtility.GetSelectedHumanoidVisualPrefabPath(kind),
                        kind.ToString());

                    RuntimeAnimatorController controller =
                        CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(kind);
                    Assert.IsNotNull(controller, kind.ToString());
                    Assert.That(controller.animationClips, Is.Not.Empty, kind.ToString());

                    for (int clipIndex = 0; clipIndex < controller.animationClips.Length; clipIndex++)
                    {
                        AnimationClip clip = controller.animationClips[clipIndex];
                        string clipPath = AssetDatabase.GetAssetPath(clip);
                        Assert.IsTrue(clip.humanMotion, $"{kind}: {clip.name}");
                        Assert.IsTrue(
                            clipPath.StartsWith(GhostSamuraiPackagePath + "/", System.StringComparison.Ordinal),
                            $"{kind}: {clipPath}");
                        StringAssert.DoesNotStartWith("__preview__", clip.name, kind.ToString());
                    }

                    AnimatorController animatorController = controller as AnimatorController;
                    Assert.IsNotNull(animatorController, kind.ToString());
                    Assert.IsNotNull(
                        FindState(animatorController.layers[0].stateMachine, requiredAttackStates[kindIndex]),
                        $"{kind}: {requiredAttackStates[kindIndex]}");

                    GameObject enemy = new GameObject(kind + "StrictArtRoot");

                    try
                    {
                        CombatProxyVisualUtility.Apply(enemy, kind);
                        Assert.IsTrue(
                            CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                                enemy,
                                kind,
                                null),
                            kind.ToString());

                        Animator importedAnimator =
                            CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);
                        Assert.IsNotNull(importedAnimator, kind.ToString());
                        importedAnimator.runtimeAnimatorController = controller;
                        Assert.IsNotNull(importedAnimator.avatar, kind.ToString());
                        Assert.IsTrue(importedAnimator.avatar.isValid, kind.ToString());
                        Assert.IsTrue(importedAnimator.avatar.isHuman, kind.ToString());
                        Assert.IsNull(enemy.GetComponent<Animator>(), kind.ToString());
                        Assert.IsNull(
                            enemy.transform.Find(
                                CombatImportedEnemyVisualUtility.ImportedVisualRootName + "/" +
                                CombatImportedEnemyVisualUtility.ImportedRoleMarkerRootName),
                            kind.ToString());
                        Assert.IsTrue(
                            CombatImportedEnemyVisualUtility.HasUserOwnedGhostSamuraiEnemyWeaponContract(
                                enemy,
                                kind),
                            kind.ToString());
                        Assert.IsTrue(
                            CombatImportedEnemyVisualUtility.HasUserOwnedGhostSamuraiEnemyMaterialContract(
                                enemy,
                                kind),
                            kind.ToString());
                    }
                    finally
                    {
                        Object.DestroyImmediate(enemy);
                    }
                }
            }
            finally
            {
                CombatImportedEnemyVisualUtility.SourceProfile = previousProfile;
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void TryApplyHumanoidAvatarPreview_AddsDistinctArchetypeRoleMarkers()
        {
            if (!File.Exists(StableEnemyVisualPrefabPath))
            {
                Assert.Ignore("The stable imported enemy humanoid preview source is not available in this workspace.");
            }

            AssertImportedRoleMarker(CombatProxyVisualKind.EnemyMelee, "MeleeBlade");
            AssertImportedRoleMarker(CombatProxyVisualKind.EnemyMobile, "MobileTail");
            AssertImportedRoleMarker(CombatProxyVisualKind.EnemyRanged, "FocusOrb");
        }

        [Test]
        public void EnsureImportedAvatarPreviewController_UsesReadableLocomotionThresholds()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            try
            {
                RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);
                AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(EnemyImportedPreviewControllerPath);

                Assert.IsNotNull(controller);
                Assert.IsNotNull(animatorController);

                AnimatorState locomotionState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.LocomotionStateName);

                Assert.IsNotNull(locomotionState);
                Assert.IsInstanceOf<BlendTree>(locomotionState.motion);

                BlendTree blendTree = (BlendTree)locomotionState.motion;

                Assert.That(blendTree.children, Has.Length.EqualTo(3));
                Assert.AreEqual(0f, blendTree.children[0].threshold, 0.001f);
                Assert.Less(blendTree.children[1].threshold, 0.25f);
                Assert.Less(blendTree.children[2].threshold, 0.8f);
                Assert.That(
                    blendTree.children[0].motion.name,
                    Does.Contain(HasGhostSamuraiPackage()
                        ? "GhostSamurai_DefenseR_Loop"
                        : "CombatIdle1H"));
                Assert.That(
                    blendTree.children[1].motion.name,
                    Does.Contain(HasGhostSamuraiPackage()
                        ? "GhostSamurai_APose_Strafe_Walk_F"
                        : "1Hand_Up_Walk"));
                Assert.That(
                    blendTree.children[2].motion.name,
                    Does.Contain(HasGhostSamuraiPackage()
                        ? "GhostSamurai_APose_Strafe_Run_F"
                        : "1Hand_Up_Run"));
                Assert.That(animatorController.layers, Has.Length.GreaterThanOrEqualTo(2));
                Assert.AreEqual("CombatPose", animatorController.layers[1].name);
                Assert.IsNotNull(animatorController.layers[1].avatarMask);
                Assert.AreEqual(0f, animatorController.layers[1].defaultWeight, 0.001f);
                AnimatorStateMachine combatPoseStateMachine = animatorController.layers[1].stateMachine;
                AnimatorState combatPoseHoldState = FindState(combatPoseStateMachine, "Hold");
                AnimatorState antiAirReadState = FindState(combatPoseStateMachine, "Read_AntiAir");
                AnimatorState chaseRollReadState = FindState(combatPoseStateMachine, "Read_ChaseRoll");
                AnimatorState guardBreakReadState = FindState(combatPoseStateMachine, "Read_GuardBreak");

                Assert.IsNotNull(combatPoseHoldState);
                Assert.IsNotNull(antiAirReadState);
                Assert.IsNotNull(chaseRollReadState);
                Assert.IsNotNull(guardBreakReadState);
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.ResponseReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.AntiAirReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName));
                Assert.IsTrue(HasFloatParameter(
                    animatorController,
                    EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName));
                AnimatorState rangedAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.RangedAttackStateName);
                AnimatorState mobileAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.MobileAttackStateName);
                AnimatorState meleeAttackState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.MeleeAttackStateName);
                AnimatorState antiAirState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.AntiAirAttackStateName);
                AnimatorState chaseRollState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName);
                AnimatorState guardBreakState = FindState(
                    animatorController.layers[0].stateMachine,
                    EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName);

                Assert.IsNotNull(rangedAttackState);
                Assert.IsNotNull(mobileAttackState);
                Assert.IsNotNull(meleeAttackState);
                Assert.IsNotNull(antiAirState);
                Assert.IsNotNull(chaseRollState);
                Assert.IsNotNull(guardBreakState);
                AnimatorStateMachine baseStateMachine = animatorController.layers[0].stateMachine;
                AssertAttackVariantStates(
                    baseStateMachine,
                    EnemyCombatAnimationPlanUtility.MeleeAttackStateName,
                    HasGhostSamuraiPackage() ? MinimumGhostSamuraiKatanaAttackVariantStateCount : 2,
                    EnemyImportedPreviewControllerPath);
                AssertAttackVariantStates(
                    baseStateMachine,
                    EnemyCombatAnimationPlanUtility.MobileAttackStateName,
                    HasGhostSamuraiPackage() ? MinimumGhostSamuraiKatanaAttackVariantStateCount : 2,
                    EnemyImportedPreviewControllerPath);
                AssertAttackVariantStates(
                    baseStateMachine,
                    EnemyCombatAnimationPlanUtility.RangedAttackStateName,
                    HasGhostSamuraiPackage() ? MinimumGhostSamuraiBowAttackVariantStateCount : 2,
                    EnemyImportedPreviewControllerPath);
                if (HasGhostSamuraiPackage())
                {
                    Assert.That(meleeAttackState.motion.name, Does.Contain("GhostSamurai_APose_Attack01"));
                    Assert.That(mobileAttackState.motion.name, Does.Contain("GhostSamurai_APose_Attack03"));
                    Assert.That(rangedAttackState.motion.name, Does.Contain("GhostSamurai_Bow_Shoot"));
                    Assert.That(antiAirState.motion.name, Does.Contain("GhostSamurai_APose_Air_Attack03"));
                    Assert.That(chaseRollState.motion.name, Does.Contain("GhostSamurai_APose_Slide_F"));
                    Assert.That(guardBreakState.motion.name, Does.Contain("GhostSamurai_APose_SPAttack06"));
                    Assert.That(
                        combatPoseHoldState.motion.name,
                        Does.Contain("ObjectGrip").Or.Contain("WeaponHold"));
                    Assert.That(antiAirReadState.motion.name, Does.Contain("GhostSamurai_DefenseR_Parry_Up_Execution"));
                    Assert.That(chaseRollReadState.motion.name, Does.Contain("GhostSamurai_APose_Slide"));
                    Assert.That(
                        guardBreakReadState.motion.name,
                        Does.Contain("GhostSamurai_RAttack_DeflectL").And.Contain("CounterExecution"));
                    Assert.AreNotSame(rangedAttackState.motion, antiAirState.motion);
                    Assert.AreNotSame(mobileAttackState.motion, chaseRollState.motion);
                    Assert.AreNotSame(meleeAttackState.motion, guardBreakState.motion);
                    Assert.AreNotSame(antiAirState.motion, antiAirReadState.motion);
                    Assert.AreNotSame(chaseRollState.motion, chaseRollReadState.motion);
                    Assert.AreNotSame(guardBreakState.motion, guardBreakReadState.motion);
                }
                else
                {
                    Assert.AreSame(rangedAttackState.motion, antiAirState.motion);
                    Assert.AreSame(mobileAttackState.motion, chaseRollState.motion);
                    Assert.AreSame(meleeAttackState.motion, guardBreakState.motion);
                }

                Assert.Greater(antiAirState.speed, rangedAttackState.speed);
                Assert.Less(chaseRollState.speed, mobileAttackState.speed);
                Assert.Less(guardBreakState.speed, 1f);
                AssertImportedControllerLocomotionUsesInPlaceClips(
                    CombatProxyVisualKind.EnemyMobile,
                    EnemyMobileImportedPreviewControllerPath,
                    HasGhostSamuraiPackage()
                        ? "GhostSamurai_APose_Strafe_Walk_F"
                        : "1Hand_Up_Walk",
                    HasGhostSamuraiPackage()
                        ? "GhostSamurai_APose_Strafe_Run_F"
                        : "1Hand_Up_Run");
                AssertImportedControllerLocomotionUsesInPlaceClips(
                    CombatProxyVisualKind.EnemyRanged,
                    EnemyRangedImportedPreviewControllerPath,
                    HasGhostSamuraiPackage()
                        ? "GhostSamurai_Bow_AimWalk_F"
                        : "1Hand_Up_Walk",
                    HasGhostSamuraiPackage()
                        ? "GhostSamurai_Bow_AimRun_F"
                        : "1Hand_Up_Run");
            }
            finally
            {
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void RefreshImportedCombatPreview_PullsCombatTestSceneInstancesFromUpdatedPrefabs()
        {
            string source = File.ReadAllText("Assets/_Game/Scripts/Editor/CodexLocalPreviewBatchRunner.cs");

            StringAssert.Contains(
                "CombatTestSceneBuilder.RefreshCombatTestScenePrefabInstancesFromSources();",
                source);
        }

        [Test]
        public void ImportedKatanaEnemyCandidatePaths_PreferGhostSamuraiReadStates_ThenTrackedFallbacks()
        {
            Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> profileExpectations =
                new Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)>
                {
                    {
                        "EnemyMelee_Idle",
                        (
                            "GetIdleClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Loop_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/1H/HumanM@CombatIdle1H01.fbx"
                        )
                    },
                    {
                        "EnemyMelee_Walk",
                        (
                            "GetWalkClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace.FBX",
                            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx"
                        )
                    },
                    {
                        "EnemyMelee_Run",
                        (
                            "GetRunClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_F_Loop_Inplace.FBX",
                            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx"
                        )
                    },
                    {
                        "EnemyMelee_Hit",
                        (
                            "GetHitClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Hit/Inplace/GhostSamurai_APose_Hit_F_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx"
                        )
                    },
                    {
                        "EnemyMelee_Death",
                        (
                            "GetDeathClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Die/Inplace/GhostSamurai_APose_Die01_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx"
                        )
                    }
                };

            foreach (KeyValuePair<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> pair in profileExpectations)
            {
                AssertProfileCandidatePathsPreferGhostSamurai(
                    pair.Key,
                    pair.Value.MethodName,
                    "OneHanded",
                    pair.Value.ExpectedFirstPath,
                    pair.Value.ExpectedTrackedFallbackPath);
            }

            Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> directExpectations =
                new Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)>
                {
                    {
                        "Attack_Melee",
                        (
                            "GetMeleeAttackClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack01_1_ALL_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx"
                        )
                    },
                    {
                        "Attack_Mobile",
                        (
                            "GetMobileAttackClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx"
                        )
                    }
                };

            foreach (KeyValuePair<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> pair in directExpectations)
            {
                AssertDirectCandidatePathsPreferGhostSamurai(
                    pair.Key,
                    pair.Value.MethodName,
                    pair.Value.ExpectedFirstPath,
                    pair.Value.ExpectedTrackedFallbackPath);
            }

            string[] antiAirPaths = InvokeProfileCandidatePathResolver("GetAntiAirAttackClipCandidatePaths", "OneHanded");
            string[] chaseRollPaths = InvokeProfileCandidatePathResolver("GetChaseRollAttackClipCandidatePaths", "OneHanded");
            string[] guardBreakPaths = InvokeProfileCandidatePathResolver("GetGuardBreakAttackClipCandidatePaths", "OneHanded");

            AssertCandidatePathLeadAndFallback(
                antiAirPaths,
                "Attack_AntiAir",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Air_Attack03_Start_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx");
            AssertCandidatePathLeadAndFallback(
                chaseRollPaths,
                "Attack_ChaseRoll",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Slide_F_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx");
            AssertCandidatePathLeadAndFallback(
                guardBreakPaths,
                "Attack_GuardBreak",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack06_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx");
            Assert.That(guardBreakPaths, Has.Length.GreaterThanOrEqualTo(3));
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX",
                guardBreakPaths[1],
                "Attack_GuardBreak should keep a guarded wind-up fallback before dropping to generic heavy swings.");
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX",
                guardBreakPaths[2],
                "Attack_GuardBreak should retain a committed heavy fallback inside the GhostSamurai package.");

            string[] antiAirReadPaths = InvokeProfileCandidatePathResolver("GetAntiAirReadClipCandidatePaths", "OneHanded");
            string[] chaseRollReadPaths = InvokeProfileCandidatePathResolver("GetChaseRollReadClipCandidatePaths", "OneHanded");
            string[] guardBreakReadPaths = InvokeProfileCandidatePathResolver("GetGuardBreakReadClipCandidatePaths", "OneHanded");

            AssertCandidatePathLeadAndFallback(
                antiAirReadPaths,
                "Read_AntiAir",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx");
            AssertCandidatePathLeadAndFallback(
                chaseRollReadPaths,
                "Read_ChaseRoll",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Slide_Start_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx");
            AssertCandidatePathLeadAndFallback(
                guardBreakReadPaths,
                "Read_GuardBreak",
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Deflect/Inplace/GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace.FBX",
                "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx");
        }

        [Test]
        public void ImportedRangedEnemyCandidatePaths_PreferGhostSamuraiBowReadStates_ThenTrackedFallbacks()
        {
            Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> profileExpectations =
                new Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)>
                {
                    {
                        "EnemyRanged_Idle",
                        (
                            "GetIdleClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_Idle_Inplace.FBX",
                            "Assets/DoubleL/Bow/Movement/Idle/Idle/Bow_Idle_B.fbx"
                        )
                    },
                    {
                        "EnemyRanged_Walk",
                        (
                            "GetWalkClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Movement/Inplace/GhostSamurai_Bow_AimWalk_F_Inplace.FBX",
                            "Assets/DoubleL/One Hand Up/Movement/Walk/Base/InPlace/1Hand_Up_Walk_A_F_InPlace.fbx"
                        )
                    },
                    {
                        "EnemyRanged_Run",
                        (
                            "GetRunClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Movement/Inplace/GhostSamurai_Bow_AimRun_F_Inplace.FBX",
                            "Assets/DoubleL/One Hand Up/Movement/Run/Base/InPlace/1Hand_Up_Run_A_F_InPlace.fbx"
                        )
                    },
                    {
                        "EnemyRanged_Hit",
                        (
                            "GetHitClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Hit/Inplace/GhostSamurai_Bow_Hit_F_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatDamage01.fbx"
                        )
                    },
                    {
                        "EnemyRanged_Death",
                        (
                            "GetDeathClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Die/Inplace/GhostSamurai_Bow_Die01_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx"
                        )
                    },
                    {
                        "Attack_Ranged_AntiAir",
                        (
                            "GetAntiAirAttackClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_AirShoot_Start_Inplace.FBX",
                            "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx"
                        )
                    },
                    {
                        "Attack_Ranged_ChaseRoll",
                        (
                            "GetChaseRollAttackClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Dodge/Inplace/GhostSamurai_Bow_Dodge_F_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Polearm/HumanM@AttackPolearm01.fbx"
                        )
                    },
                    {
                        "Attack_Ranged_GuardBreak",
                        (
                            "GetGuardBreakAttackClipCandidatePaths",
                            "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_Shoot_SP04_Inplace.FBX",
                            "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/Shield/HumanM@AttackShield01.fbx"
                        )
                    }
                };

            foreach (KeyValuePair<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> pair in profileExpectations)
            {
                AssertProfileCandidatePathsPreferGhostSamurai(
                    pair.Key,
                    pair.Value.MethodName,
                    "Ranged",
                    pair.Value.ExpectedFirstPath,
                    pair.Value.ExpectedTrackedFallbackPath);
            }

            AssertDirectCandidatePathsPreferGhostSamurai(
                "Attack_Ranged",
                "GetRangedAttackClipCandidatePaths",
                "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_Shoot_Start_Inplace.FBX",
                "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx");
            AssertProfileCandidatePathsPreferGhostSamurai(
                "Read_Ranged_AntiAir",
                "GetAntiAirReadClipCandidatePaths",
                "Ranged",
                "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_AirShoot_Start_Inplace.FBX",
                "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx");
            AssertProfileCandidatePathsPreferGhostSamurai(
                "Read_Ranged_ChaseRoll",
                "GetChaseRollReadClipCandidatePaths",
                "Ranged",
                "Assets/GhostSamurai_Animset/Animation/Bow/Dodge/Inplace/GhostSamurai_Bow_Dodge_F_Inplace.FBX",
                "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx");
            AssertProfileCandidatePathsPreferGhostSamurai(
                "Read_Ranged_GuardBreak",
                "GetGuardBreakReadClipCandidatePaths",
                "Ranged",
                "Assets/GhostSamurai_Animset/Animation/Bow/Attack/Inplace/GhostSamurai_Bow_Shoot_SP04_Inplace.FBX",
                "Assets/DoubleL/Bow/Attack B/Bow_Attack_B_1_All.fbx");
        }

        private static void AssertImportedControllerLocomotionUsesInPlaceClips(
            CombatProxyVisualKind visualKind,
            string controllerPath,
            string expectedWalkName,
            string expectedRunName)
        {
            RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(visualKind);
            AnimatorController animatorController = AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);

            Assert.IsNotNull(controller, visualKind.ToString());
            Assert.IsNotNull(animatorController, controllerPath);

            AnimatorState locomotionState = FindState(
                animatorController.layers[0].stateMachine,
                EnemyCombatAnimationPlanUtility.LocomotionStateName);

            Assert.IsNotNull(locomotionState, controllerPath);
            Assert.IsInstanceOf<BlendTree>(locomotionState.motion, controllerPath);

            BlendTree blendTree = (BlendTree)locomotionState.motion;

            Assert.That(blendTree.children, Has.Length.EqualTo(3), controllerPath);
            Assert.That(blendTree.children[1].motion.name, Does.Contain(expectedWalkName), controllerPath);
            Assert.That(blendTree.children[2].motion.name, Does.Contain(expectedRunName), controllerPath);
        }

        private static void AssertProfileCandidatePathsPreferGhostSamurai(
            string label,
            string methodName,
            string profileName,
            string expectedFirstPath,
            string expectedTrackedFallbackPath)
        {
            string[] candidatePaths = InvokeProfileCandidatePathResolver(methodName, profileName);
            AssertCandidatePathLeadAndFallback(candidatePaths, label, expectedFirstPath, expectedTrackedFallbackPath);
        }

        private static void AssertDirectCandidatePathsPreferGhostSamurai(
            string label,
            string methodName,
            string expectedFirstPath,
            string expectedTrackedFallbackPath)
        {
            string[] candidatePaths = InvokeDirectCandidatePathResolver(methodName);
            AssertCandidatePathLeadAndFallback(candidatePaths, label, expectedFirstPath, expectedTrackedFallbackPath);
        }

        private static void AssertCandidatePathLeadAndFallback(
            string[] candidatePaths,
            string label,
            string expectedFirstPath,
            string expectedTrackedFallbackPath)
        {
            Assert.IsNotNull(candidatePaths, label);
            Assert.That(candidatePaths, Is.Not.Empty, label);
            Assert.AreEqual(expectedFirstPath, candidatePaths[0], label);
            StringAssert.Contains("GhostSamurai_Animset", candidatePaths[0], label);

            if (HasGhostSamuraiPackage())
            {
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(candidatePaths[0]), candidatePaths[0]);
            }

            Assert.That(candidatePaths, Has.Some.EqualTo(expectedTrackedFallbackPath), label);
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(expectedTrackedFallbackPath), expectedTrackedFallbackPath);
        }

        private static string[] InvokeDirectCandidatePathResolver(string methodName)
        {
            MethodInfo resolveMethod = typeof(CombatImportedEnemyVisualUtility).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod, methodName);
            return resolveMethod.Invoke(null, null) as string[];
        }

        private static string[] InvokeProfileCandidatePathResolver(string methodName, string profileName)
        {
            MethodInfo resolveMethod = typeof(CombatImportedEnemyVisualUtility).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            System.Type profileType = typeof(CombatImportedEnemyVisualUtility).GetNestedType(
                "ImportedEnemyAnimationProfile",
                BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod, methodName);
            Assert.IsNotNull(profileType, "ImportedEnemyAnimationProfile");
            object profileValue = System.Enum.Parse(profileType, profileName);
            return resolveMethod.Invoke(null, new[] { profileValue }) as string[];
        }

        private static bool HasGhostSamuraiPackage()
        {
            return AssetDatabase.IsValidFolder(GhostSamuraiPackagePath);
        }

        [Test]
        public void GhostSamuraiGroundLocomotionImports_AreLoopingAndRootLocked()
        {
            if (!HasGhostSamuraiPackage())
            {
                Assert.Ignore("GhostSamurai local-preview package is not available in this workspace.");
            }

            try
            {
                Assert.IsNotNull(CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee));
                Assert.IsNotNull(CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyRanged));

                for (int i = 0; i < GhostSamuraiGroundLocomotionClipPaths.Length; i++)
                {
                    string path = GhostSamuraiGroundLocomotionClipPaths[i];
                    ModelImporter importer = AssetImporter.GetAtPath(path) as ModelImporter;
                    Assert.IsNotNull(importer, path);
                    ModelImporterClipAnimation[] clips = importer.clipAnimations;

                    if (clips == null || clips.Length == 0)
                    {
                        clips = importer.defaultClipAnimations;
                    }

                    Assert.IsNotNull(clips, path);
                    Assert.IsNotEmpty(clips, path);

                    for (int j = 0; j < clips.Length; j++)
                    {
                        ModelImporterClipAnimation clip = clips[j];
                        Assert.AreEqual(WrapMode.Loop, clip.wrapMode, path);
                        Assert.IsTrue(clip.loopTime, path);
                        Assert.IsTrue(clip.loopPose, path);
                        Assert.IsFalse(clip.keepOriginalPositionY, path);
                        Assert.IsFalse(clip.keepOriginalPositionXZ, path);
                        Assert.IsTrue(clip.lockRootHeightY, path);
                        Assert.IsTrue(clip.lockRootPositionXZ, path);
                        Assert.IsTrue(clip.heightFromFeet, path);
                    }
                }
            }
            finally
            {
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void TryApplyHumanoidAvatarPreview_ConfiguresAnimatorAndRestoresProxyBaseline()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            RuntimeAnimatorController controller = CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);
            Assert.IsNotNull(controller);

            GameObject enemy = new GameObject("EnemyPreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, CombatProxyVisualKind.EnemyMelee);
                Animator animator = enemy.AddComponent<Animator>();

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                    enemy,
                    CombatProxyVisualKind.EnemyMelee,
                    animator);

                Assert.IsTrue(applied);
                animator.runtimeAnimatorController = controller;

                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Transform proxyRoot = enemy.transform.Find("CombatProxyVisualRoot");
                Renderer[] proxyRenderers = proxyRoot != null ? proxyRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];

                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(proxyRoot);
                Assert.That(proxyRenderers, Is.Not.Empty);
                Assert.IsNotNull(importedRoot.GetComponentInChildren<SkinnedMeshRenderer>(true));
                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);
                Assert.IsNotNull(importedAnimator);
                importedAnimator.runtimeAnimatorController = controller;
                Assert.IsNull(animator.avatar);
                Assert.IsFalse(animator.enabled);
                Assert.IsNotNull(importedAnimator.avatar);
                Assert.AreSame(controller, importedAnimator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));
                Assert.That(ResolveLowestRendererBoundsY(importedRoot), Is.GreaterThanOrEqualTo(-0.08f));

                bool removed = CombatImportedEnemyVisualUtility.RemoveImportedVisual(enemy, animator);

                Assert.IsTrue(removed);
                Assert.IsNull(enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName));
                Assert.IsTrue(animator.enabled);
                Assert.IsNull(animator.avatar);
                Assert.IsNull(animator.runtimeAnimatorController);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void ImportedPreviewRelay_PreservesAnchoredVisualRootAfterSampledLocomotionBoundsChange()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            RuntimeAnimatorController controller =
                CombatImportedEnemyVisualUtility.EnsureImportedAvatarPreviewController(CombatProxyVisualKind.EnemyMelee);
            Assert.IsNotNull(controller);

            GameObject enemy = new GameObject("EnemyPreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, CombatProxyVisualKind.EnemyMelee);
                Animator rootAnimator = enemy.AddComponent<Animator>();

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                    enemy,
                    CombatProxyVisualKind.EnemyMelee,
                    rootAnimator);

                Assert.IsTrue(applied);

                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);
                importedAnimator.runtimeAnimatorController = controller;
                EnemyCombatAnimationRelay relay = enemy.AddComponent<EnemyCombatAnimationRelay>();
                MethodInfo stabilizeMethod = typeof(EnemyCombatAnimationRelay).GetMethod(
                    "StabilizeImportedPreviewTransforms",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(importedAnimator);
                Assert.IsNotNull(stabilizeMethod);

                stabilizeMethod.Invoke(relay, null);
                Vector3 anchoredLocalPosition = importedRoot.localPosition;
                Quaternion anchoredLocalRotation = importedRoot.localRotation;
                Vector3 anchoredLocalScale = importedRoot.localScale;

                importedAnimator.Play(EnemyCombatAnimationPlanUtility.LocomotionStateName, 0, 0f);
                importedAnimator.SetFloat(EnemyCombatAnimationPlanUtility.GroundSpeedParameterName, 1f);
                importedAnimator.Update(0.35f);
                Bounds sampledBounds = ResolveRendererBounds(importedRoot);
                importedRoot.position += new Vector3(0.8f, 0.6f, -0.45f);

                stabilizeMethod.Invoke(relay, null);
                Bounds stabilizedBounds = ResolveRendererBounds(importedRoot);

                Assert.That(Vector3.Distance(anchoredLocalPosition, importedRoot.localPosition), Is.LessThan(0.0001f));
                Assert.That(Quaternion.Angle(anchoredLocalRotation, importedRoot.localRotation), Is.LessThan(0.001f));
                Assert.That(Vector3.Distance(anchoredLocalScale, importedRoot.localScale), Is.LessThan(0.0001f));
                Assert.AreEqual(sampledBounds.center.x, stabilizedBounds.center.x, 0.001f);
                Assert.AreEqual(sampledBounds.center.z, stabilizedBounds.center.z, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(enemy);
                AssetDatabase.DeleteAsset(EnemyImportedPreviewControllerPath);
                AssetDatabase.DeleteAsset(LocalPreviewFolderPath);
                AssetDatabase.Refresh();
            }
        }

        [Test]
        public void TryApplyHumanoidAvatarPreview_ConfiguresImportedAnimatorWithoutRootAnimator()
        {
            if (!CombatImportedEnemyVisualUtility.HasHumanoidVisualSource(CombatProxyVisualKind.EnemyMelee))
            {
                Assert.Ignore("No compatible imported enemy humanoid preview source is available in this workspace.");
            }

            GameObject enemy = new GameObject("EnemyPreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, CombatProxyVisualKind.EnemyMelee);

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(
                    enemy,
                    CombatProxyVisualKind.EnemyMelee,
                    null);

                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Transform proxyRoot = enemy.transform.Find("CombatProxyVisualRoot");
                Renderer[] proxyRenderers = proxyRoot != null ? proxyRoot.GetComponentsInChildren<Renderer>(true) : new Renderer[0];
                Animator importedAnimator = CombatImportedEnemyVisualUtility.FindImportedPreviewAnimator(enemy);

                Assert.IsTrue(applied);
                Assert.IsNull(enemy.GetComponent<Animator>());
                Assert.IsNotNull(importedRoot);
                Assert.IsNotNull(importedAnimator);
                Assert.IsTrue(importedAnimator.enabled);
                Assert.IsNotNull(importedAnimator.avatar);
                Assert.IsFalse(importedAnimator.applyRootMotion);
                Assert.AreEqual(AnimatorCullingMode.AlwaysAnimate, importedAnimator.cullingMode);
                Assert.AreEqual(AnimatorUpdateMode.Normal, importedAnimator.updateMode);
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => !renderer.enabled));

                bool removed = CombatImportedEnemyVisualUtility.RemoveImportedVisual(enemy, null);

                Assert.IsTrue(removed);
                Assert.IsNull(enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName));
                Assert.That(proxyRenderers, Has.All.Matches<Renderer>(renderer => renderer.enabled));
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        private static void AssertImportedRoleMarker(CombatProxyVisualKind kind, string expectedMarkerName)
        {
            GameObject enemy = new GameObject(kind + "PreviewRoot");

            try
            {
                CombatProxyVisualUtility.Apply(enemy, kind);

                bool applied = CombatImportedEnemyVisualUtility.TryApplyHumanoidAvatarPreview(enemy, kind, null);
                Transform importedRoot = enemy.transform.Find(CombatImportedEnemyVisualUtility.ImportedVisualRootName);
                Transform markerRoot = importedRoot != null
                    ? importedRoot.Find(CombatImportedEnemyVisualUtility.ImportedRoleMarkerRootName)
                    : null;

                Assert.IsTrue(applied, kind.ToString());
                Assert.IsNotNull(importedRoot, kind.ToString());
                Assert.IsNotNull(markerRoot, kind.ToString());
                Assert.IsNotNull(markerRoot.Find(expectedMarkerName), kind.ToString());
                Assert.That(markerRoot.GetComponentsInChildren<Collider>(true), Is.Empty, kind.ToString());
            }
            finally
            {
                Object.DestroyImmediate(enemy);
            }
        }

        private static void AssertImportedResponseReadStates(AnimatorController animatorController, string context)
        {
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.ResponseReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.AntiAirReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.ChaseRollReadParameterName), context);
            Assert.IsTrue(HasFloatParameter(
                animatorController,
                EnemyCombatAnimationPlanUtility.GuardBreakReadParameterName), context);

            AnimatorStateMachine stateMachine = animatorController.layers[0].stateMachine;
            AnimatorState rangedAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.RangedAttackStateName);
            AnimatorState mobileAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MobileAttackStateName);
            AnimatorState meleeAttackState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName);
            AnimatorState antiAirState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.AntiAirAttackStateName);
            AnimatorState chaseRollState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName);
            AnimatorState guardBreakState = FindState(
                stateMachine,
                EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName);

            Assert.IsNotNull(rangedAttackState, context);
            Assert.IsNotNull(mobileAttackState, context);
            Assert.IsNotNull(meleeAttackState, context);
            Assert.IsNotNull(antiAirState, context);
            Assert.IsNotNull(chaseRollState, context);
            Assert.IsNotNull(guardBreakState, context);
            AssertAttackVariantStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName,
                2,
                context);
            AssertAttackVariantStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.MobileAttackStateName,
                2,
                context);
            AssertAttackVariantStates(
                stateMachine,
                EnemyCombatAnimationPlanUtility.RangedAttackStateName,
                2,
                context);

            Assert.That(animatorController.layers, Has.Length.GreaterThanOrEqualTo(2), context);
            AnimatorStateMachine combatPoseStateMachine = animatorController.layers[1].stateMachine;
            Assert.IsNotNull(FindState(combatPoseStateMachine, "Hold"), context);
            Assert.IsNotNull(FindState(combatPoseStateMachine, "Read_AntiAir"), context);
            Assert.IsNotNull(FindState(combatPoseStateMachine, "Read_ChaseRoll"), context);
            Assert.IsNotNull(FindState(combatPoseStateMachine, "Read_GuardBreak"), context);
            Assert.Greater(antiAirState.speed, rangedAttackState.speed, context);
            Assert.Less(chaseRollState.speed, mobileAttackState.speed, context);
            Assert.Less(guardBreakState.speed, meleeAttackState.speed, context);
        }

        private static void AssertAttackVariantStates(
            AnimatorStateMachine stateMachine,
            string baseStateName,
            int expectedMinimumCount,
            string context)
        {
            AnimatorState baseState = FindState(stateMachine, baseStateName);
            Assert.IsNotNull(baseState, context);

            int variantCount = 0;

            for (int i = 1; i <= 96; i++)
            {
                AnimatorState variantState = FindState(
                    stateMachine,
                    EnemyCombatAnimationRelay.FormatAttackVariantStateName(baseStateName, i));

                if (variantState == null)
                {
                    break;
                }

                variantCount++;
                Assert.That(
                    variantState.motion.name,
                    Does.Not.StartWith("__preview__"),
                    context);

                if (i == 1)
                {
                    Assert.AreSame(baseState.motion, variantState.motion, context);
                }

                if (i == 2)
                {
                    Assert.AreNotSame(baseState.motion, variantState.motion, context);
                }
            }

            Assert.That(variantCount, Is.GreaterThanOrEqualTo(expectedMinimumCount), context);
        }

        private static AnimatorState FindState(AnimatorStateMachine stateMachine, string stateName)
        {
            ChildAnimatorState[] states = stateMachine.states;

            for (int i = 0; i < states.Length; i++)
            {
                if (states[i].state != null && states[i].state.name == stateName)
                {
                    return states[i].state;
                }
            }

            return null;
        }

        private static bool HasFloatParameter(AnimatorController controller, string parameterName)
        {
            AnimatorControllerParameter[] parameters = controller.parameters;

            for (int i = 0; i < parameters.Length; i++)
            {
                AnimatorControllerParameter parameter = parameters[i];

                if (parameter.type == AnimatorControllerParameterType.Float
                    && parameter.name == parameterName)
                {
                    return true;
                }
            }

            return false;
        }

        private static float ResolveLowestRendererBoundsY(Transform root)
        {
            return ResolveRendererBounds(root).min.y;
        }

        private static Bounds ResolveRendererBounds(Transform root)
        {
            Renderer[] renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new Renderer[0];

            if (renderers.Length == 0 && root != null)
            {
                renderers = root.GetComponentsInChildren<Renderer>(true);
            }

            Bounds bounds = default;
            bool hasBounds = false;

            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null || !renderers[i].enabled)
                {
                    continue;
                }

                if (!hasBounds)
                {
                    bounds = renderers[i].bounds;
                    hasBounds = true;
                    continue;
                }

                bounds.Encapsulate(renderers[i].bounds);
            }

            Assert.IsTrue(hasBounds, "Expected at least one renderer bound.");
            return bounds;
        }
    }
}
