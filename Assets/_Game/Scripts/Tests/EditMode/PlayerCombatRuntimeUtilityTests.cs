using System.Reflection;
using CampusRPG.Character;
using CampusRPG.Combat;
using NUnit.Framework;
using UnityEngine;

namespace CampusRPG.Tests.EditMode
{
    public sealed class PlayerCombatRuntimeUtilityTests
    {
        [Test]
        public void TickComboState_ResetsCombo_WhenWindowExpires()
        {
            PlayerCombatComboState comboState = PlayerCombatRuntimeUtility.TickComboState(2, 0.1f, 0.2f);
            Assert.AreEqual(0, comboState.NextLightAttackIndex);
            Assert.AreEqual(0f, comboState.ComboResetTimer, 0.0001f);
        }

        [Test]
        public void ResolveComboStateAfterAttackFinished_AdvancesWithinCombo_AndResetsAtEnd()
        {
            PlayerCombatComboState advancedState = PlayerCombatRuntimeUtility.ResolveComboStateAfterAttackFinished(
                PlayerAttackRequest.Light,
                0,
                3,
                0.8f);
            Assert.AreEqual(1, advancedState.NextLightAttackIndex);
            Assert.AreEqual(0.8f, advancedState.ComboResetTimer, 0.0001f);

            PlayerCombatComboState resetState = PlayerCombatRuntimeUtility.ResolveComboStateAfterAttackFinished(
                PlayerAttackRequest.Light,
                2,
                3,
                0.8f);
            Assert.AreEqual(0, resetState.NextLightAttackIndex);
            Assert.AreEqual(0f, resetState.ComboResetTimer, 0.0001f);
        }

        [Test]
        public void ResolveCounterAttack_UsesEmpoweredVariant_WhenGaugeIsFull()
        {
            GameObject gaugeObject = new GameObject("Gauge");
            GaugeComponent gauges = gaugeObject.AddComponent<GaugeComponent>();
            AttackDefinitionSO normalAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO empoweredAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                gauges.AddCounter(100f);
                Assert.AreSame(empoweredAttack, PlayerCombatRuntimeUtility.ResolveCounterAttack(gauges, normalAttack, empoweredAttack));
                Assert.AreEqual(0f, gauges.CounterGauge, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(empoweredAttack);
                Object.DestroyImmediate(normalAttack);
                Object.DestroyImmediate(gaugeObject);
            }
        }

        [Test]
        public void ResolveDodgeFollowUpAttack_UsesEmpoweredVariant_WhenGaugeIsFull()
        {
            GameObject gaugeObject = new GameObject("Gauge");
            GaugeComponent gauges = gaugeObject.AddComponent<GaugeComponent>();
            AttackDefinitionSO normalAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();
            AttackDefinitionSO empoweredAttack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                gauges.AddAgility(100f);
                Assert.AreSame(empoweredAttack, PlayerCombatRuntimeUtility.ResolveDodgeFollowUpAttack(gauges, normalAttack, empoweredAttack));
                Assert.AreEqual(0f, gauges.AgilityGauge, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(empoweredAttack);
                Object.DestroyImmediate(normalAttack);
                Object.DestroyImmediate(gaugeObject);
            }
        }

        [Test]
        public void ResolveAttackRecoverySeconds_PreservesMinimumVisibleDuration_ForLightAttacks()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "animationStateName", "Light_01");
                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "activeSeconds", 0.08f);
                SetPrivateField(attack, "recoverySeconds", 0.22f);
                SetPrivateField(attack, "animationDurationSeconds", 0.56f);

                float resolvedRecovery = PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attack);

                Assert.AreEqual(0.38f, resolvedRecovery, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolveAttackRecoverySeconds_ExtendsHeavyAttacks_UntilTheSwingReadsComplete()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "animationStateName", "Heavy_01");
                SetPrivateField(attack, "startupSeconds", 0.2f);
                SetPrivateField(attack, "activeSeconds", 0.12f);
                SetPrivateField(attack, "recoverySeconds", 0.42f);
                SetPrivateField(attack, "animationDurationSeconds", 1.6f);

                float resolvedRecovery = PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attack);

                Assert.AreEqual(0.64f, resolvedRecovery, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolveAttackRecoverySeconds_UsesConfiguredRecovery_WhenAnimationTailIsShort()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "animationStateName", "Heavy_01");
                SetPrivateField(attack, "startupSeconds", 0.2f);
                SetPrivateField(attack, "activeSeconds", 0.1f);
                SetPrivateField(attack, "recoverySeconds", 0.42f);
                SetPrivateField(attack, "animationDurationSeconds", 0.68f);

                float resolvedRecovery = PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attack);

                Assert.AreEqual(0.42f, resolvedRecovery, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolveAttackRecoverySeconds_ExtendsSwordArts_ToKeepFollowThroughReadable()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "animationStateName", "SwordArt_FallingStar");
                SetPrivateField(attack, "startupSeconds", 0.16f);
                SetPrivateField(attack, "activeSeconds", 0.14f);
                SetPrivateField(attack, "recoverySeconds", 0.42f);
                SetPrivateField(attack, "animationDurationSeconds", 1.05f);

                float resolvedRecovery = PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attack);

                Assert.AreEqual(0.66f, resolvedRecovery, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolveAttackRecoverySeconds_ExtendsFastSwordArts_WithoutForcingFullClip()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "animationStateName", "SwordArt_MoonSever");
                SetPrivateField(attack, "startupSeconds", 0.1f);
                SetPrivateField(attack, "activeSeconds", 0.1f);
                SetPrivateField(attack, "recoverySeconds", 0.26f);
                SetPrivateField(attack, "animationDurationSeconds", 0.72f);

                float resolvedRecovery = PlayerCombatRuntimeUtility.ResolveAttackRecoverySeconds(attack);

                Assert.AreEqual(0.5f, resolvedRecovery, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ResolveAttackForwardMovementDelta_DistributesLungeAcrossFrames()
        {
            AttackDefinitionSO attack = ScriptableObject.CreateInstance<AttackDefinitionSO>();

            try
            {
                SetPrivateField(attack, "forwardMovement", 0.5f);
                SetPrivateField(attack, "forwardMovementStartSeconds", 0.04f);
                SetPrivateField(attack, "forwardMovementDurationSeconds", 0.14f);
                SetPrivateField(
                    attack,
                    "forwardMovementCurve",
                    new AnimationCurve(
                        new Keyframe(0f, 0f, 0.57f, 0.57f),
                        new Keyframe(0.35f, 0.2f, 0.57f, 1.7f),
                        new Keyframe(0.75f, 0.88f, 1.7f, 0.48f),
                        new Keyframe(1f, 1f, 0.48f, 0.48f)));

                float elapsed = 0f;
                float totalDistance = 0f;
                float maxFrameDelta = 0f;
                float frameStep = 1f / 60f;

                for (int i = 0; i < 30; i++)
                {
                    float nextElapsed = elapsed + frameStep;
                    float frameDelta = PlayerCombatRuntimeUtility.ResolveAttackForwardMovementDelta(
                        attack,
                        elapsed,
                        nextElapsed);
                    totalDistance += frameDelta;
                    maxFrameDelta = Mathf.Max(maxFrameDelta, frameDelta);
                    elapsed = nextElapsed;
                }

                Assert.AreEqual(0f, PlayerCombatRuntimeUtility.ResolveAttackForwardMovementDelta(attack, 0f, 0.03f), 0.0001f);
                Assert.AreEqual(0.5f, totalDistance, 0.0001f);
                Assert.Less(maxFrameDelta, 0.12f);
            }
            finally
            {
                Object.DestroyImmediate(attack);
            }
        }

        [Test]
        public void ShouldSnapProxyWeaponFollow_ReturnsTrue_DuringImmediateFollowWindow()
        {
            bool shouldSnap = PlayerCombatRuntimeUtility.ShouldSnapProxyWeaponFollow(
                hasSnappedToAnchor: true,
                isImmediateFollowState: false,
                immediateFollowTimer: 0.08f,
                rotationDeltaDegrees: 3f,
                positionDelta: 0.005f,
                snapRotationThresholdDegrees: 12f,
                snapPositionThreshold: 0.025f);

            Assert.IsTrue(shouldSnap);
        }

        [Test]
        public void ShouldSnapProxyWeaponFollow_ReturnsTrue_WhenRotationGapIsLarge()
        {
            bool shouldSnap = PlayerCombatRuntimeUtility.ShouldSnapProxyWeaponFollow(
                hasSnappedToAnchor: true,
                isImmediateFollowState: false,
                immediateFollowTimer: 0f,
                rotationDeltaDegrees: 18f,
                positionDelta: 0.01f,
                snapRotationThresholdDegrees: 12f,
                snapPositionThreshold: 0.025f);

            Assert.IsTrue(shouldSnap);
        }

        [Test]
        public void ShouldSnapProxyWeaponFollow_ReturnsFalse_ForSmallLocomotionDrift()
        {
            bool shouldSnap = PlayerCombatRuntimeUtility.ShouldSnapProxyWeaponFollow(
                hasSnappedToAnchor: true,
                isImmediateFollowState: false,
                immediateFollowTimer: 0f,
                rotationDeltaDegrees: 4f,
                positionDelta: 0.01f,
                snapRotationThresholdDegrees: 12f,
                snapPositionThreshold: 0.025f);

            Assert.IsFalse(shouldSnap);
        }

        private static void SetPrivateField<TValue>(object instance, string fieldName, TValue value)
        {
            FieldInfo field = instance.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(instance, value);
        }
    }
}
