using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyVisualPresentationUtilityTests
    {
        [Test]
        public void ResolvePose_ForMeleeStartup_DrawsBackBeforeSwing()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);

            Assert.Less(pose.RootLocalEulerAngles.x, -10f);
            Assert.Less(pose.AccentLocalEulerAngles.z, -30f);
            Assert.Less(pose.RootLocalOffset.z, 0f);
        }

        [Test]
        public void ResolvePose_ForMeleeStartup_MakesEarlyAnticipationReadable()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.35f);

            Assert.Less(pose.RootLocalOffset.z, -0.025f);
            Assert.Less(pose.RootLocalEulerAngles.x, -8f);
            Assert.Less(pose.AccentLocalEulerAngles.z, -30f);
        }

        [Test]
        public void ResolvePose_ForMeleeAdvance_CompressesIntoStrike()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Mobile,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Advance,
                0.65f);

            Assert.Greater(pose.RootLocalOffset.z, 0.04f);
            Assert.Greater(pose.AccentLocalEulerAngles.z, 20f);
            Assert.Greater(pose.RootLocalScale.z, 1.04f);
        }

        [Test]
        public void ResolvePose_ForMeleeAdvance_EmphasizesMidStrikeSnap()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Advance,
                0.5f);

            Assert.Greater(pose.RootLocalOffset.z, 0.13f);
            Assert.Greater(pose.AccentLocalEulerAngles.z, 80f);
            Assert.Greater(pose.AccentLocalScale.x, 1.09f);
        }

        [Test]
        public void ResolvePose_ForMobileStartup_AddsLateralWindup()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Mobile,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);

            Assert.Greater(pose.RootLocalOffset.x, 0.02f);
            Assert.Greater(pose.RootLocalEulerAngles.y, 6f);
            Assert.Less(pose.AccentLocalEulerAngles.z, -40f);
        }

        [Test]
        public void ResolvePose_ForBossStartup_CrouchesHeavierThanMelee()
        {
            EnemyVisualPresentationPose meleePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);
            EnemyVisualPresentationPose bossPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);

            Assert.Less(bossPose.RootLocalOffset.y, meleePose.RootLocalOffset.y);
            Assert.Less(bossPose.RootLocalEulerAngles.x, meleePose.RootLocalEulerAngles.x);
            Assert.Less(bossPose.AccentLocalEulerAngles.z, meleePose.AccentLocalEulerAngles.z);
        }

        [Test]
        public void ResolvePose_ForBossAntiAirStartup_LiftsHookTell()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);
            EnemyVisualPresentationPose antiAirPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f,
                EnemyTargetResponseType.AntiAir);

            Assert.Greater(antiAirPose.RootLocalOffset.y, baselinePose.RootLocalOffset.y);
            Assert.Greater(antiAirPose.RootLocalScale.y, baselinePose.RootLocalScale.y);
            Assert.Less(antiAirPose.AccentLocalEulerAngles.x, -15f);
        }

        [Test]
        public void ResolvePose_ForBossChaseRollStartup_LeansIntoPursuitLane()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);
            EnemyVisualPresentationPose chaseRollPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f,
                EnemyTargetResponseType.ChaseRoll);

            Assert.Greater(chaseRollPose.RootLocalOffset.z, baselinePose.RootLocalOffset.z);
            Assert.Greater(chaseRollPose.RootLocalEulerAngles.y, 6f);
            Assert.Greater(chaseRollPose.AccentLocalEulerAngles.z, baselinePose.AccentLocalEulerAngles.z);
        }

        [Test]
        public void ResolvePose_ForBossGuardBreakStartup_CrushesLowerThanBaseline()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f);
            EnemyVisualPresentationPose guardBreakPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.75f,
                EnemyTargetResponseType.GuardBreak);

            Assert.Less(guardBreakPose.RootLocalOffset.y, baselinePose.RootLocalOffset.y);
            Assert.Greater(guardBreakPose.RootLocalScale.x, baselinePose.RootLocalScale.x);
            Assert.Less(guardBreakPose.AccentLocalEulerAngles.z, baselinePose.AccentLocalEulerAngles.z);
        }

        [Test]
        public void ResolvePose_ForBossAntiAirRecovery_KeepsHookReadUntilSettle()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.2f);
            EnemyVisualPresentationPose antiAirPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.2f,
                EnemyTargetResponseType.AntiAir);

            Assert.Greater(antiAirPose.RootLocalOffset.y, baselinePose.RootLocalOffset.y + 0.04f);
            Assert.Greater(antiAirPose.RootLocalEulerAngles.x, baselinePose.RootLocalEulerAngles.x + 5f);
            Assert.Less(antiAirPose.AccentLocalEulerAngles.x, baselinePose.AccentLocalEulerAngles.x - 10f);
        }

        [Test]
        public void ResolvePose_ForBossChaseRollAdvance_CommitsToForwardLane()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Advance,
                0.65f);
            EnemyVisualPresentationPose chaseRollPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Advance,
                0.65f,
                EnemyTargetResponseType.ChaseRoll);

            Assert.Greater(chaseRollPose.RootLocalOffset.z, baselinePose.RootLocalOffset.z + 0.12f);
            Assert.Less(chaseRollPose.RootLocalEulerAngles.y, baselinePose.RootLocalEulerAngles.y - 4f);
            Assert.Greater(chaseRollPose.AccentLocalEulerAngles.z, baselinePose.AccentLocalEulerAngles.z + 34f);
        }

        [Test]
        public void ResolvePose_ForBossGuardBreakRecovery_StaysCrushedBeforeSettling()
        {
            EnemyVisualPresentationPose baselinePose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.25f);
            EnemyVisualPresentationPose guardBreakPose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Boss,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.25f,
                EnemyTargetResponseType.GuardBreak);

            Assert.Less(guardBreakPose.RootLocalOffset.y, baselinePose.RootLocalOffset.y - 0.02f);
            Assert.Less(guardBreakPose.RootLocalEulerAngles.x, baselinePose.RootLocalEulerAngles.x - 6f);
            Assert.Less(guardBreakPose.AccentLocalEulerAngles.z, baselinePose.AccentLocalEulerAngles.z - 14f);
        }

        [Test]
        public void ResolvePose_ForRangedAttack_PulsesAccentAndLift()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.8f);

            Assert.Greater(pose.RootLocalOffset.y, 0.005f);
            Assert.Less(pose.RootLocalOffset.z, -0.015f);
            Assert.Less(pose.AccentLocalEulerAngles.x, -10f);
            Assert.Greater(pose.AccentLocalScale.x, 1.1f);
            Assert.Greater(pose.AccentLocalScale.y, 1.1f);
        }

        [Test]
        public void ResolvePose_ForRangedStartup_BuildsVisibleChargeBeforeRelease()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Startup,
                0.5f);

            Assert.Greater(pose.RootLocalOffset.y, 0.02f);
            Assert.Less(pose.AccentLocalEulerAngles.x, -20f);
            Assert.Greater(pose.AccentLocalScale.x, 1.18f);
        }

        [Test]
        public void ResolvePose_ForMeleeRecovery_HoldsReadableFollowThroughBeforeSettling()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.2f);

            Assert.Greater(pose.RootLocalOffset.z, 0.08f);
            Assert.Greater(pose.RootLocalEulerAngles.x, 10f);
            Assert.Greater(pose.AccentLocalEulerAngles.z, 58f);
        }

        [Test]
        public void ResolvePose_ForRangedRecovery_RecoilsBeforeSettling()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Recovery,
                0.15f);

            Assert.Less(pose.RootLocalOffset.y, 0f);
            Assert.Less(pose.RootLocalOffset.z, -0.02f);
            Assert.Greater(pose.RootLocalEulerAngles.x, 4f);
            Assert.Less(pose.AccentLocalEulerAngles.z, -10f);
            Assert.Greater(pose.AccentLocalScale.x, 1.1f);
        }

        [Test]
        public void ResolvePose_ForRangedAdvance_PulsesProjectileRelease()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0f,
                0f,
                EnemyAttackPresentationPhase.Advance,
                0.5f);

            Assert.Greater(pose.RootLocalOffset.y, 0.04f);
            Assert.Greater(pose.AccentLocalEulerAngles.x, 16f);
            Assert.Greater(pose.AccentLocalScale.x, 1.36f);
        }

        [Test]
        public void ResolvePose_ForChase_AddsStrideMotion()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                1f,
                0.7f,
                EnemyAttackPresentationPhase.None,
                0f);

            Assert.Greater(Mathf.Abs(pose.RootLocalOffset.y), 0.001f);
            Assert.Greater(Mathf.Abs(pose.RootLocalEulerAngles.z), 0.5f);
            Assert.AreNotEqual(Vector3.one, pose.RootLocalScale);
        }

        [Test]
        public void ResolvePose_ForChase_AlternatesReadableStrideAcrossCycle()
        {
            EnemyVisualPresentationPose forwardStep = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                1f,
                0.3f,
                EnemyAttackPresentationPhase.None,
                0f);
            EnemyVisualPresentationPose backStep = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                1f,
                0.92f,
                EnemyAttackPresentationPhase.None,
                0f);

            Assert.Greater(forwardStep.RootLocalOffset.z, 0.04f);
            Assert.Less(backStep.RootLocalOffset.z, -0.04f);
            Assert.Greater(forwardStep.RootLocalOffset.y, 0.035f);
            Assert.Greater(backStep.RootLocalOffset.y, 0.035f);
            Assert.Greater(forwardStep.AccentLocalEulerAngles.z, 10f);
            Assert.Less(backStep.AccentLocalEulerAngles.z, -10f);
        }

        [Test]
        public void ResolvePose_ForMobileChase_AddsReadableStrideAndSideLean()
        {
            EnemyVisualPresentationPose pose = EnemyVisualPresentationUtility.ResolvePose(
                EnemyArchetypeType.Mobile,
                nameof(EnemyChaseState),
                1f,
                0.25f,
                EnemyAttackPresentationPhase.None,
                0f);

            Assert.Greater(pose.RootLocalOffset.z, 0.03f);
            Assert.Greater(pose.RootLocalOffset.x, 0.02f);
            Assert.Greater(pose.RootLocalEulerAngles.y, 5f);
        }
    }
}
