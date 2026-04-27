using CampusRPG.Camera;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class ActionCameraFeedbackUtilityTests
    {
        [Test]
        public void ResolveEvasiveImpulse_OnlyHighlightsRollAndAirDodge()
        {
            Assert.IsFalse(ActionCameraFeedbackUtility.ResolveEvasiveImpulse(PlayerEvasiveActionType.GroundDodge).HasImpulse);

            ActionCameraImpulsePlan rollPlan = ActionCameraFeedbackUtility.ResolveEvasiveImpulse(PlayerEvasiveActionType.CombatRoll);
            Assert.IsTrue(rollPlan.HasImpulse);
            Assert.Greater(rollPlan.Distance, 0.04f);
            Assert.AreEqual(ActionCameraFeedbackUtility.ImpulsePriorityMinor, rollPlan.Priority);

            ActionCameraImpulsePlan airDodgePlan = ActionCameraFeedbackUtility.ResolveEvasiveImpulse(PlayerEvasiveActionType.AirDodge);
            Assert.IsTrue(airDodgePlan.HasImpulse);
            Assert.Greater(airDodgePlan.DurationSeconds, rollPlan.DurationSeconds);
            Assert.AreEqual(rollPlan.Priority, airDodgePlan.Priority);
        }

        [Test]
        public void ResolvePlayerAttackImpulse_MapsSwordArtWeight()
        {
            AttackDefinitionSO fallingStar = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO crossStep = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO moonSever = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO lightAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(fallingStar, "animationStateName", "SwordArt_FallingStar");
                SetPrivateField(crossStep, "animationStateName", "SwordArt_CrossStep");
                SetPrivateField(moonSever, "animationStateName", "SwordArt_MoonSever");
                SetPrivateField(lightAttack, "animationStateName", "Light_01");

                ActionCameraImpulsePlan fallingPlan = ActionCameraFeedbackUtility.ResolvePlayerAttackImpulse(fallingStar);
                ActionCameraImpulsePlan crossStepPlan = ActionCameraFeedbackUtility.ResolvePlayerAttackImpulse(crossStep);
                ActionCameraImpulsePlan moonSeverPlan = ActionCameraFeedbackUtility.ResolvePlayerAttackImpulse(moonSever);

                Assert.IsTrue(fallingPlan.HasImpulse);
                Assert.IsTrue(crossStepPlan.HasImpulse);
                Assert.IsTrue(moonSeverPlan.HasImpulse);
                Assert.Greater(fallingPlan.Distance, crossStepPlan.Distance);
                Assert.Greater(fallingPlan.Priority, crossStepPlan.Priority);
                Assert.Greater(moonSeverPlan.Distance, crossStepPlan.Distance);
                Assert.AreEqual(crossStepPlan.Priority, moonSeverPlan.Priority);
                Assert.IsFalse(ActionCameraFeedbackUtility.ResolvePlayerAttackImpulse(lightAttack).HasImpulse);
            }
            finally
            {
                Object.DestroyImmediate(lightAttack);
                Object.DestroyImmediate(moonSever);
                Object.DestroyImmediate(crossStep);
                Object.DestroyImmediate(fallingStar);
            }
        }

        [Test]
        public void ResolveEnemyResponseImpulse_MapsAntiAirAndRollCatch()
        {
            AttackDefinitionSO antiAir = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO chaseRoll = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO normalAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(antiAir, "enemyTargetResponse", EnemyTargetResponseType.AntiAir);
                SetPrivateField(chaseRoll, "enemyTargetResponse", EnemyTargetResponseType.ChaseRoll);

                ActionCameraImpulsePlan antiAirPlan = ActionCameraFeedbackUtility.ResolveEnemyResponseImpulse(antiAir);
                ActionCameraImpulsePlan chaseRollPlan = ActionCameraFeedbackUtility.ResolveEnemyResponseImpulse(chaseRoll);

                Assert.IsTrue(antiAirPlan.HasImpulse);
                Assert.IsTrue(chaseRollPlan.HasImpulse);
                Assert.Greater(chaseRollPlan.Distance, antiAirPlan.Distance);
                Assert.Greater(chaseRollPlan.Priority, antiAirPlan.Priority);
                Assert.IsFalse(ActionCameraFeedbackUtility.ResolveEnemyResponseImpulse(normalAttack).HasImpulse);
            }
            finally
            {
                Object.DestroyImmediate(normalAttack);
                Object.DestroyImmediate(chaseRoll);
                Object.DestroyImmediate(antiAir);
            }
        }

        [Test]
        public void TryRequestImpulse_ActivatesCameraWhenPlanHasImpulse()
        {
            GameObject cameraObject = null;
            GameObject sourceObject = null;

            try
            {
                cameraObject = new GameObject("Camera");
                sourceObject = new GameObject("ActionSource");
                ThirdPersonCameraController cameraController = cameraObject.AddComponent<ThirdPersonCameraController>();

                Assert.IsTrue(ActionCameraFeedbackUtility.TryRequestImpulse(
                    cameraController,
                    sourceObject.transform,
                    new ActionCameraImpulsePlan(0.08f, 0.1f)));
                Assert.IsTrue(cameraController.HasActiveImpactImpulse);
                Assert.AreEqual(0, cameraController.CurrentImpactImpulsePriority);

                cameraController.ResetRuntimeState();

                Assert.IsFalse(ActionCameraFeedbackUtility.TryRequestImpulse(
                    cameraController,
                    sourceObject.transform,
                    ActionCameraImpulsePlan.None));
                Assert.IsFalse(cameraController.HasActiveImpactImpulse);
            }
            finally
            {
                if (sourceObject != null)
                {
                    Object.DestroyImmediate(sourceObject);
                }

                if (cameraObject != null)
                {
                    Object.DestroyImmediate(cameraObject);
                }
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
