using CampusRPG.AI;
using NUnit.Framework;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyCombatAnimationPlanUtilityTests
    {
        [Test]
        public void ResolvePlan_ForChase_UsesLocomotionAndPreservesGroundSpeed()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                0.72f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.LocomotionStateName, plan.StateName);
            Assert.AreEqual(0.72f, plan.GroundSpeedNormalized, 0.001f);
        }

        [Test]
        public void ResolvePlan_ForMeleeAttack_UsesMeleeAttackState()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyAttackState),
                0.91f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.MeleeAttackStateName, plan.StateName);
            Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
        }

        [Test]
        public void ResolvePlan_ForMobileAttack_UsesMobileAttackState()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Mobile,
                nameof(EnemyAttackState),
                0.54f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.MobileAttackStateName, plan.StateName);
            Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
        }

        [Test]
        public void ResolvePlan_ForRangedAttack_UsesRangedAttackState()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0.33f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.RangedAttackStateName, plan.StateName);
            Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
        }

        [Test]
        public void ResolvePlan_ForHitAndDeath_UsesDedicatedStates()
        {
            EnemyCombatAnimationPlan hitPlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyHitState),
                0.6f);
            EnemyCombatAnimationPlan deathPlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyDeathState),
                0.6f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.HitStateName, hitPlan.StateName);
            Assert.AreEqual(EnemyCombatAnimationPlanUtility.DeathStateName, deathPlan.StateName);
            Assert.AreEqual(0f, hitPlan.GroundSpeedNormalized, 0.001f);
            Assert.AreEqual(0f, deathPlan.GroundSpeedNormalized, 0.001f);
        }
    }
}
