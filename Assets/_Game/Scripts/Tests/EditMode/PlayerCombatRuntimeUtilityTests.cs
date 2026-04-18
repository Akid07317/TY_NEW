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
    }
}
