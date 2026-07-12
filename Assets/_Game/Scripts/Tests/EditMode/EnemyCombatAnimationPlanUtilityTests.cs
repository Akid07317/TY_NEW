using CampusRPG.AI;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class EnemyCombatAnimationPlanUtilityTests
    {
        [Test]
        public void ResolvePlan_BoostsChaseLocomotionIntoRunBand()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                0.55f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.LocomotionStateName, plan.StateName);
            Assert.Greater(plan.GroundSpeedNormalized, 0.7f);
        }

        [Test]
        public void ResolvePlan_KeepsChaseLocomotionInWalkBand_WhenSpeedSampleDrops()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Melee,
                nameof(EnemyChaseState),
                0f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.LocomotionStateName, plan.StateName);
            Assert.That(plan.GroundSpeedNormalized, Is.GreaterThan(0.3f).And.LessThan(0.5f));
        }

        [Test]
        public void ResolvePlan_KeepsStrafeLocomotionInWalkBand_WhenSpeedSampleDrops()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Mobile,
                nameof(EnemyStrafeState),
                0f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.LocomotionStateName, plan.StateName);
            Assert.That(plan.GroundSpeedNormalized, Is.GreaterThan(0.2f).And.LessThan(0.35f));
        }

        [Test]
        public void ResolvePlan_KeepsStrafeLocomotionBelowFullSprint()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Mobile,
                nameof(EnemyStrafeState),
                0.55f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.LocomotionStateName, plan.StateName);
            Assert.That(plan.GroundSpeedNormalized, Is.GreaterThan(0.45f).And.LessThan(0.8f));
        }

        [Test]
        public void ResolvePlan_ClearsGroundSpeedOutsideLocomotion()
        {
            EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                EnemyArchetypeType.Ranged,
                nameof(EnemyAttackState),
                0.9f);

            Assert.AreEqual(EnemyCombatAnimationPlanUtility.RangedAttackStateName, plan.StateName);
            Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
        }

        [Test]
        public void ResolvePlan_UsesAntiAirReadState_ForResponseAttack()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);

                EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0.4f,
                    attack);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.AntiAirAttackStateName, plan.StateName);
                Assert.AreEqual(EnemyCombatAnimationPlanUtility.RangedAttackStateName, plan.FallbackStateName);
                Assert.AreEqual(EnemyTargetResponseType.AntiAir, plan.TargetResponse);
                Assert.AreEqual("Anti-Air Read", plan.ResponseReadLabel);
                Assert.Greater(plan.ResponseReadNormalized, 0.95f);
                Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolvePlan_UsesRollCatchReadState_ForResponseAttack()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

                EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0.4f,
                    attack);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName, plan.StateName);
                Assert.AreEqual(EnemyCombatAnimationPlanUtility.MobileAttackStateName, plan.FallbackStateName);
                Assert.AreEqual(EnemyTargetResponseType.ChaseRoll, plan.TargetResponse);
                Assert.AreEqual("Roll Catch Read", plan.ResponseReadLabel);
                Assert.That(plan.ResponseReadNormalized, Is.GreaterThan(0.8f).And.LessThan(1f));
                Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolvePlan_UsesGuardBreakReadState_ForBreakingAttack()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "breaksGuard", true);

                EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0.4f,
                    attack,
                    EnemyAttackPresentationPhase.Startup,
                    0.5f);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.GuardBreakAttackStateName, plan.StateName);
                Assert.AreEqual(EnemyCombatAnimationPlanUtility.MeleeAttackStateName, plan.FallbackStateName);
                Assert.AreEqual(EnemyTargetResponseType.GuardBreak, plan.TargetResponse);
                Assert.AreEqual("Guard Break Read", plan.ResponseReadLabel);
                Assert.That(plan.ResponseReadNormalized, Is.GreaterThan(0.55f).And.LessThan(0.95f));
                Assert.AreEqual(0f, plan.GroundSpeedNormalized, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolvePlan_KeepsGenericAttackState_ForBossAttackWithoutResponse()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                EnemyCombatAnimationPlan plan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0.4f,
                    attack);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.MeleeAttackStateName, plan.StateName);
                Assert.AreEqual(EnemyCombatAnimationPlanUtility.MeleeAttackStateName, plan.FallbackStateName);
                Assert.AreEqual(EnemyTargetResponseType.None, plan.TargetResponse);
                Assert.AreEqual(string.Empty, plan.ResponseReadLabel);
                Assert.AreEqual(0f, plan.ResponseReadNormalized, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolvePlan_RampsAntiAirReadAcrossStartup()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);

                EnemyCombatAnimationPlan earlyPlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0f,
                    attack,
                    EnemyAttackPresentationPhase.Startup,
                    0f);
                EnemyCombatAnimationPlan middlePlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0f,
                    attack,
                    EnemyAttackPresentationPhase.Startup,
                    0.5f);
                EnemyCombatAnimationPlan latePlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0f,
                    attack,
                    EnemyAttackPresentationPhase.Startup,
                    1f);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.AntiAirAttackStateName, earlyPlan.StateName);
                Assert.That(earlyPlan.ResponseReadNormalized, Is.GreaterThan(0.25f).And.LessThan(middlePlan.ResponseReadNormalized));
                Assert.That(middlePlan.ResponseReadNormalized, Is.GreaterThan(earlyPlan.ResponseReadNormalized).And.LessThan(latePlan.ResponseReadNormalized));
                Assert.AreEqual(1f, latePlan.ResponseReadNormalized, 0.001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolvePlan_FadesRollCatchReadDuringRecovery()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

                EnemyCombatAnimationPlan recoveryStartPlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0f,
                    attack,
                    EnemyAttackPresentationPhase.Recovery,
                    0f);
                EnemyCombatAnimationPlan recoveryEndPlan = EnemyCombatAnimationPlanUtility.ResolvePlan(
                    EnemyArchetypeType.Boss,
                    nameof(EnemyAttackState),
                    0f,
                    attack,
                    EnemyAttackPresentationPhase.Recovery,
                    1f);

                Assert.AreEqual(EnemyCombatAnimationPlanUtility.ChaseRollAttackStateName, recoveryStartPlan.StateName);
                Assert.AreEqual(0.9f, recoveryStartPlan.ResponseReadNormalized, 0.001f);
                Assert.That(recoveryEndPlan.ResponseReadNormalized, Is.GreaterThan(0.15f).And.LessThan(0.25f));
                Assert.Less(recoveryEndPlan.ResponseReadNormalized, recoveryStartPlan.ResponseReadNormalized);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            System.Reflection.FieldInfo field = instance.GetType().GetField(
                fieldName,
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
