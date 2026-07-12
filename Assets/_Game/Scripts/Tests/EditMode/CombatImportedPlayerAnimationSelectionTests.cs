using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CampusRPG.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class CombatImportedPlayerAnimationSelectionTests
    {
        private const string PlayerControllerPath = "Assets/_Game/Animations/Characters/CombatTest/AC_Player_CombatTest.controller";

        [Test]
        public void UserOwnedGhostSamuraiProfile_KeepsModelWeaponAndAnimationsOnOneSource()
        {
            ImportedPlayerSourceProfile previousProfile = CombatImportedPlayerVisualUtility.SourceProfile;

            try
            {
                CombatImportedPlayerVisualUtility.SourceProfile = ImportedPlayerSourceProfile.UserOwnedGhostSamurai;

                Assert.IsTrue(
                    CombatImportedPlayerVisualUtility.IsAnimationSourceAllowed(
                        "Assets/GhostSamurai_Animset/Animation/katana/APose/GhostSamurai_APose_Idle.FBX"));
                Assert.IsFalse(
                    CombatImportedPlayerVisualUtility.IsAnimationSourceAllowed(
                        "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@CombatIdle01.fbx"));

                if (!AssetDatabase.IsValidFolder("Assets/GhostSamurai_Animset"))
                {
                    return;
                }

                Assert.IsTrue(CombatImportedPlayerVisualUtility.HasPlayerVisualSource());
                Assert.AreEqual(
                    CombatImportedPlayerVisualUtility.GhostSamuraiPlayerModelPath,
                    CombatImportedPlayerVisualUtility.GetSelectedPlayerVisualPrefabPath());
                Assert.AreEqual(
                    CombatImportedPlayerVisualUtility.GhostSamuraiPlayerWeaponPath,
                    CombatImportedPlayerVisualUtility.GetSelectedPlayerWeaponPrefabPath());
            }
            finally
            {
                CombatImportedPlayerVisualUtility.SourceProfile = previousProfile;
            }
        }

        [Test]
        public void ImportedBaseLocomotionCandidatePaths_PreferGhostSamuraiMovementSources()
        {
            Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> expectations =
                new Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)>
                {
                    { "Idle", ("ResolveImportedIdleClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/GhostSamurai_APose_Idle.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Idle.anim") },
                    { "Walk_Forward", ("ResolveImportedWalkForwardClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_F_Loop_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_F_InPlace.anim") },
                    { "Walk_Backward", ("ResolveImportedWalkBackwardClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_B_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_B_InPlace.anim") },
                    { "Walk_Left", ("ResolveImportedWalkLeftClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_L_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_L_InPlace.anim") },
                    { "Walk_Right", ("ResolveImportedWalkRightClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Walk_R_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Walk_R_InPlace.anim") },
                    { "Run_Forward", ("ResolveImportedRunForwardClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_F_Loop_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_F_InPlace.anim") },
                    { "Run_Backward", ("ResolveImportedRunBackwardClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_B_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_B_InPlace.anim") },
                    { "Run_Left", ("ResolveImportedRunLeftClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_L_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_L_InPlace.anim") },
                    { "Run_Right", ("ResolveImportedRunRightClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_R_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Run_R_InPlace.anim") },
                    { "Run_ForwardLeft", ("ResolveImportedRunForwardLeftClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_FL_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardLeft.fbx") },
                    { "Run_ForwardRight", ("ResolveImportedRunForwardRightClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_FR_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_ForwardRight.fbx") },
                    { "Run_BackwardLeft", ("ResolveImportedRunBackwardLeftClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_BL_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardLeft.fbx") },
                    { "Run_BackwardRight", ("ResolveImportedRunBackwardRightClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Strafe_Run_BR_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Strafe/StrafeRun/HumanM@StrafeRun01_BackwardRight.fbx") },
                    { "Airborne", ("ResolveImportedAirborneClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Movement/Inplace/GhostSamurai_APose_Jump_Loop_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Fall01.fbx") }
                };

            foreach (KeyValuePair<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> pair in expectations)
            {
                AssertCandidatePathsPreferGhostSamurai(
                    pair.Key,
                    pair.Value.MethodName,
                    pair.Value.ExpectedFirstPath,
                    pair.Value.ExpectedTrackedFallbackPath);
            }
        }

        [Test]
        public void ImportedDefenseAndReactiveCandidatePaths_PreferGhostSamuraiReadabilitySources()
        {
            Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> expectations =
                new Dictionary<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)>
                {
                    { "Block", ("ResolveImportedBlockClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Loop_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Idle.anim") },
                    { "Dodge", ("ResolveImportedDodgeClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Dodge_F_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Begin.fbx") },
                    { "CombatRoll", ("ResolveImportedCombatRollClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Slide_F_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Jump_B_InPlace.anim") },
                    { "AirDodge", ("ResolveImportedAirDodgeClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Avoid_F_1_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Movement/Jump/HumanM@Jump01 - Begin.fbx") },
                    { "Hit", ("ResolveImportedHitClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Hit/Inplace/GhostSamurai_APose_Hit_F_Inplace.FBX", "Assets/DoubleL/Demo/Anim/Hit_F_1_InPlace.anim") },
                    { "GuardBreak", ("ResolveImportedGuardBreakClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Broken_Inplace.FBX", "Assets/DoubleL/Demo/Anim/OneHand_Up_Shield_Block_Hit_1_InPlace.anim") },
                    { "Death", ("ResolveImportedDeathClipCandidatePaths", "Assets/GhostSamurai_Animset/Animation/katana/APose/Die/Inplace/GhostSamurai_APose_Die01_Inplace.FBX", "Assets/Kevin Iglesias/Human Animations/Animations/Male/Combat/HumanM@Death01.fbx") }
                };

            foreach (KeyValuePair<string, (string MethodName, string ExpectedFirstPath, string ExpectedTrackedFallbackPath)> pair in expectations)
            {
                AssertCandidatePathsPreferGhostSamurai(
                    pair.Key,
                    pair.Value.MethodName,
                    pair.Value.ExpectedFirstPath,
                    pair.Value.ExpectedTrackedFallbackPath);
            }

            string[] guardBreakPaths = InvokeCandidatePathResolver("ResolveImportedGuardBreakClipCandidatePaths");
            string[] hitPaths = InvokeCandidatePathResolver("ResolveImportedHitClipCandidatePaths");

            Assert.AreNotEqual(hitPaths[0], guardBreakPaths[0], "GuardBreak should stay on a broken-defense pose instead of reusing the standard hit lead clip.");
            Assert.That(
                guardBreakPaths,
                Has.Some.EqualTo("Assets/GhostSamurai_Animset/Animation/katana/APose/Hit/Inplace/GhostSamurai_APose_Large_Hit_2_Inplace.FBX"),
                "GuardBreak should keep a heavier fallback inside the GhostSamurai package itself.");
        }

        [Test]
        public void ImportedAttackCandidatePaths_PreferGhostSamuraiKatanaSources_ThenTrackedFallbacks()
        {
            Dictionary<string, string> expectedFirstPaths = new Dictionary<string, string>
            {
                { "Light_01", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack01_1_ALL_Inplace.FBX" },
                { "Light_02", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack04_Inplace.FBX" },
                { "Light_03", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack02_Inplace.FBX" },
                { "Heavy_01", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX" },
                { "DodgeFollowUp", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Dodge_Attack_F_Inplace.FBX" },
                { "DodgeFollowUp_Enhanced", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Dodge_Attack_B_Inplace.FBX" },
                { "Counter", "Assets/GhostSamurai_Animset/Animation/katana/APose/Deflect/Inplace/GhostSamurai_LAttack_DeflectR_CounterExecution_Inplace.FBX" },
                { "Counter_Enhanced", "Assets/GhostSamurai_Animset/Animation/katana/APose/Deflect/Inplace/GhostSamurai_RAttack_DeflectL_CounterExecution_Inplace.FBX" }
            };
            Dictionary<string, string> expectedTrackedFallbackPaths = new Dictionary<string, string>
            {
                { "Light_01", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim" },
                { "Light_02", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
                { "Light_03", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "Heavy_01", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "DodgeFollowUp", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim" },
                { "DodgeFollowUp_Enhanced", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
                { "Counter", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_B_1_InPlace.anim" },
                { "Counter_Enhanced", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" }
            };

            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedAttackClipCandidatePaths",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod);

            foreach (KeyValuePair<string, string> pair in expectedFirstPaths)
            {
                string[] candidatePaths = resolveMethod.Invoke(null, new object[] { pair.Key }) as string[];

                Assert.IsNotNull(candidatePaths, pair.Key);
                Assert.That(candidatePaths, Is.Not.Empty, pair.Key);
                Assert.AreEqual(pair.Value, candidatePaths[0], pair.Key);
                StringAssert.Contains("GhostSamurai_Animset", candidatePaths[0], pair.Key);

                if (AssetDatabase.IsValidFolder("Assets/GhostSamurai_Animset"))
                {
                    Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(candidatePaths[0]), candidatePaths[0]);
                }

                string expectedTrackedFallback = expectedTrackedFallbackPaths[pair.Key];
                Assert.That(candidatePaths.Contains(expectedTrackedFallback), pair.Key);
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(expectedTrackedFallback), expectedTrackedFallback);
                StringAssert.Contains("OneHand", expectedTrackedFallback, pair.Key);
            }

            Assert.AreNotEqual(
                expectedTrackedFallbackPaths["Light_02"],
                expectedTrackedFallbackPaths["Counter"],
                "Counter should not keep sharing Light_02's primary imported source.");
        }

        [Test]
        public void ImportedSwordArtCandidatePaths_PreferGhostSamuraiPurposeBuiltSources_ThenTrackedFallbacks()
        {
            Dictionary<string, string> expectedFirstPaths = new Dictionary<string, string>
            {
                { "SwordArt_SidewindCut", "Assets/GhostSamurai_Animset/Animation/katana/APose/Dodge/Inplace/GhostSamurai_APose_Dodge_Attack_F_Inplace.FBX" },
                { "SwordArt_CrossStep", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack02_4_ALL_Inplace.FBX" },
                { "SwordArt_RisingCleave", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX" },
                { "SwordArt_IronGateBreak", "Assets/GhostSamurai_Animset/Animation/katana/APose/Defense/Inplace/GhostSamurai_DefenseR_Parry_Up_Execution_Inplace.FBX" },
                { "SwordArt_FallingStar", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_JumpAttack04_Inplace.FBX" },
                { "SwordArt_MoonSever", "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack03_Inplace.FBX" }
            };
            Dictionary<string, string> expectedTrackedFallbackPaths = new Dictionary<string, string>
            {
                { "SwordArt_SidewindCut", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_1_InPlace.anim" },
                { "SwordArt_CrossStep", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" },
                { "SwordArt_RisingCleave", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "SwordArt_IronGateBreak", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "SwordArt_FallingStar", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_3_InPlace.anim" },
                { "SwordArt_MoonSever", "Assets/DoubleL/Demo/Anim/OneHand_Up_Attack_2_InPlace.anim" }
            };

            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedAttackClipCandidatePaths",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod);

            foreach (KeyValuePair<string, string> pair in expectedFirstPaths)
            {
                string[] candidatePaths = resolveMethod.Invoke(null, new object[] { pair.Key }) as string[];

                Assert.IsNotNull(candidatePaths, pair.Key);
                Assert.That(candidatePaths, Is.Not.Empty, pair.Key);
                Assert.AreEqual(pair.Value, candidatePaths[0], pair.Key);
                StringAssert.Contains("GhostSamurai_Animset", candidatePaths[0], pair.Key);

                if (AssetDatabase.IsValidFolder("Assets/GhostSamurai_Animset"))
                {
                    Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(candidatePaths[0]), candidatePaths[0]);
                }

                string expectedTrackedFallback = expectedTrackedFallbackPaths[pair.Key];
                Assert.That(candidatePaths.Contains(expectedTrackedFallback), pair.Key);
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(expectedTrackedFallback), expectedTrackedFallback);
            }

            string[] counterPaths = resolveMethod.Invoke(null, new object[] { "Counter" }) as string[];
            string[] ironGateBreakPaths = resolveMethod.Invoke(null, new object[] { "SwordArt_IronGateBreak" }) as string[];

            Assert.IsNotNull(counterPaths);
            Assert.IsNotNull(ironGateBreakPaths);
            Assert.AreNotEqual(counterPaths[0], ironGateBreakPaths[0], "Iron Gate Break should no longer preview off the same lead clip as Counter.");
            Assert.AreEqual(
                "Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_SPAttack06_Inplace.FBX",
                ironGateBreakPaths[1],
                "Iron Gate Break should keep a heavier GhostSamurai attack fallback after the defensive parry-up lead.");
            Assert.That(
                ironGateBreakPaths,
                Has.Some.EqualTo("Assets/GhostSamurai_Animset/Animation/katana/APose/Attack/Inplace/GhostSamurai_APose_Attack03_4_ALL_Inplace.FBX"),
                "Iron Gate Break should retain a committed heavy fallback inside the GhostSamurai package.");
        }

        [Test]
        public void ImportedPreviewAttackDurationOverrides_KeepRetimedStatesShort()
        {
            Dictionary<string, float> expectedDurations = new Dictionary<string, float>
            {
                { "Light_01", 0.58f },
                { "Light_02", 0.78f },
                { "Light_03", 0.98f },
                { "Heavy_01", 1.02f },
                { "DodgeFollowUp", 0.6f },
                { "DodgeFollowUp_Enhanced", 0.68f },
                { "Counter", 0.56640005f },
                { "Counter_Enhanced", 0.78f },
                { "SwordArt_SidewindCut", 0.6f },
                { "SwordArt_CrossStep", 0.62f },
                { "SwordArt_RisingCleave", 1f },
                { "SwordArt_IronGateBreak", 0.9f },
                { "SwordArt_FallingStar", 1.05f },
                { "SwordArt_MoonSever", 0.72f }
            };

            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                "ResolveImportedPreviewAttackDurationOverride",
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod);

            foreach (KeyValuePair<string, float> pair in expectedDurations)
            {
                Assert.AreEqual(
                    pair.Value,
                    (float)resolveMethod.Invoke(null, new object[] { pair.Key }),
                    0.0001f,
                    pair.Key);
            }

            Assert.AreEqual(0f, (float)resolveMethod.Invoke(null, new object[] { "UnknownAttack" }), 0.0001f);
            Assert.Less(
                expectedDurations["SwordArt_CrossStep"],
                expectedDurations["Heavy_01"],
                "Roll follow-up should stay snappier than committed heavy attacks in local preview.");
            Assert.Greater(
                expectedDurations["SwordArt_FallingStar"],
                expectedDurations["Light_03"],
                "Falling Star should keep a larger readable follow-through than the light combo finisher.");
        }

        [Test]
        public void ImportedPreviewClipRefresh_RemovesStaleProxyCurves()
        {
            const string tempClipPath = "Assets/_Game/Animations/Characters/CombatTest/TMP_ImportedPreviewClipRefresh.anim";
            AssetDatabase.DeleteAsset(tempClipPath);

            try
            {
                AnimationClip staleClip = new AnimationClip();
                AnimationUtility.SetEditorCurve(
                    staleClip,
                    EditorCurveBinding.FloatCurve("CombatProxyVisualRoot/Torso", typeof(Transform), "m_LocalPosition.x"),
                    AnimationCurve.Linear(0f, 0f, 0.1f, 1f));
                AssetDatabase.CreateAsset(staleClip, tempClipPath);

                AnimationClip sourceClip = new AnimationClip();
                AnimationUtility.SetEditorCurve(
                    sourceClip,
                    EditorCurveBinding.FloatCurve(string.Empty, typeof(Animator), "RootT.x"),
                    AnimationCurve.Linear(0f, 0f, 0.1f, 0.25f));

                MethodInfo createMethod = typeof(CombatTestAssetGenerator).GetMethod(
                    "CreateOrUpdateImportedClip",
                    BindingFlags.Static | BindingFlags.NonPublic);

                Assert.IsNotNull(createMethod);
                Assert.IsNotNull(createMethod.Invoke(
                    null,
                    new object[] { tempClipPath, sourceClip, 0.1f, false, System.Array.Empty<AnimationEvent>() }));

                AnimationClip refreshedClip = AssetDatabase.LoadAssetAtPath<AnimationClip>(tempClipPath);
                EditorCurveBinding[] curveBindings = AnimationUtility.GetCurveBindings(refreshedClip);

                Assert.IsNotNull(refreshedClip);
                Assert.That(
                    curveBindings,
                    Has.None.Matches<EditorCurveBinding>(binding => binding.path.StartsWith("CombatProxyVisualRoot")),
                    "Imported preview refresh must not keep hidden proxy curves on the visible imported model.");
                Assert.That(
                    curveBindings,
                    Has.Some.Matches<EditorCurveBinding>(binding => binding.type == typeof(Animator) && binding.propertyName == "RootT.x"));
            }
            finally
            {
                AssetDatabase.DeleteAsset(tempClipPath);
            }
        }

        [Test]
        public void CommittedPlayerController_KeepsGuardBreakReturnPathIntact_ForPreviewLane()
        {
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(PlayerControllerPath);

            Assert.IsNotNull(controller);
            Assert.That(controller.layers, Is.Not.Empty);

            AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
            AnimatorState guardBreakState = FindState(stateMachine, "GuardBreak");

            Assert.IsNotNull(guardBreakState);
            Assert.AreEqual(1f, guardBreakState.speed, 0.001f, "GuardBreak preview should stay aligned with the generated controller contract.");

            foreach (ChildAnimatorState childState in stateMachine.states)
            {
                AnimatorState state = childState.state;

                Assert.IsNotNull(state, "Committed player controller contains a missing state reference.");

                foreach (AnimatorStateTransition transition in state.transitions)
                {
                    Assert.IsFalse(transition.isExit, state.name + " should return to a real state instead of a stale exit stub.");
                    Assert.IsTrue(
                        transition.destinationState != null || transition.destinationStateMachine != null,
                        state.name + " contains a dangling transition destination in the local-preview lane.");
                }
            }

            Assert.That(
                guardBreakState.transitions,
                Has.Some.Matches<AnimatorStateTransition>(
                    transition => transition.destinationState != null
                        && transition.destinationState.name == "Locomotion"),
                "GuardBreak should still return to Locomotion after its readable recovery.");
        }

        private static void AssertCandidatePathsPreferGhostSamurai(
            string label,
            string methodName,
            string expectedFirstPath,
            string expectedTrackedFallbackPath)
        {
            string[] candidatePaths = InvokeCandidatePathResolver(methodName);

            Assert.IsNotNull(candidatePaths, label);
            Assert.That(candidatePaths, Is.Not.Empty, label);
            Assert.AreEqual(expectedFirstPath, candidatePaths[0], label);
            StringAssert.Contains("GhostSamurai_Animset", candidatePaths[0], label);

            if (AssetDatabase.IsValidFolder("Assets/GhostSamurai_Animset"))
            {
                Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(candidatePaths[0]), candidatePaths[0]);
            }

            Assert.That(candidatePaths.Contains(expectedTrackedFallbackPath), label);
            Assert.IsNotNull(AssetDatabase.LoadMainAssetAtPath(expectedTrackedFallbackPath), expectedTrackedFallbackPath);
        }

        private static string[] InvokeCandidatePathResolver(string methodName)
        {
            MethodInfo resolveMethod = typeof(CombatTestAssetGenerator).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);

            Assert.IsNotNull(resolveMethod, methodName);
            return resolveMethod.Invoke(null, null) as string[];
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
    }
}
