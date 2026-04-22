using CampusRPG.AI;
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
            Assert.Greater(pose.AccentLocalScale.x, 1.1f);
            Assert.Greater(pose.AccentLocalScale.y, 1.1f);
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
    }
}
