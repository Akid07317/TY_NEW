using CampusRPG.AI;
using NUnit.Framework;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyCombatAnimationRelayTests
    {
        [Test]
        public void ResolveCombatPoseLayerTargetWeight_DisablesHoldOverlayWhileMoving()
        {
            float idleWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.LocomotionStateName,
                0f);
            float walkWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.LocomotionStateName,
                0.4f);
            float attackWeight = EnemyCombatAnimationRelay.ResolveCombatPoseLayerTargetWeight(
                EnemyCombatAnimationPlanUtility.MeleeAttackStateName,
                0f);

            Assert.That(idleWeight, Is.GreaterThan(0f).And.LessThan(0.25f));
            Assert.AreEqual(0f, walkWeight, 0.001f);
            Assert.AreEqual(0f, attackWeight, 0.001f);
        }
    }
}
